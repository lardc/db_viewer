using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SCME.Service.Properties;
using SCME.Types;
using SCME.Types.RCC;

namespace SCME.Service.IO
{
    internal class IORCC
    {
        //проверка сопротивления цепи катод-катод модуля на комплексе АКИМ, КИСПы не используют данный тип теста, поэтому для этого типа теста ничего нет в UI, нет возможности его использования в профилях, результат этого теста не сохраняется в БД
        private readonly IOGate m_IOGate;
        private readonly BroadcastCommunication m_Communication;
        private readonly bool m_IsEmulationHard;
        private bool m_IsEmulation;
        private IOCommutation m_IOCommutation;
        private DeviceConnectionState m_ConnectionState;
        private volatile bool m_Stop;
        private Types.RCC.TestParameters m_Parameters;
        private volatile DeviceState m_State;
        private volatile Types.RCC.TestResults m_Result;

        internal IORCC(IOGate Gate, BroadcastCommunication Communication)
        {
            m_IOGate = Gate;
            m_State = DeviceState.None;

            m_Communication = Communication;
            m_IsEmulationHard = Settings.Default.RCCEmulation;
        }

        internal IOCommutation ActiveCommutation
        {
            get { return m_IOCommutation; }
            set { m_IOCommutation = value; }
        }

        internal DeviceConnectionState Initialize(bool Enable, int Timeout)
        {
            m_State = DeviceState.None;

            m_ConnectionState = DeviceConnectionState.ConnectionInProcess;
            FireConnectionEvent(m_ConnectionState, "RCC initializing");

            m_IsEmulation = m_IsEmulationHard || !Enable;

            if (m_IsEmulation)
            {
                m_ConnectionState = DeviceConnectionState.ConnectionSuccess;
                FireConnectionEvent(m_ConnectionState, "RCC initialized");

                return m_ConnectionState;
            }
            else
                m_ConnectionState = m_IOGate.Initialize(Enable, Timeout);

            return m_ConnectionState;
        }

        internal void Deinitialize()
        {
            m_ConnectionState = DeviceConnectionState.DisconnectionInProcess;
            FireConnectionEvent(DeviceConnectionState.DisconnectionInProcess, "RCC disconnecting");

            try
            {
                if (!m_IsEmulation)
                    m_IOGate.Deinitialize();

                m_ConnectionState = DeviceConnectionState.DisconnectionSuccess;
                FireConnectionEvent(m_ConnectionState, "RCC disconnected");
            }
            catch (Exception)
            {
                m_ConnectionState = DeviceConnectionState.DisconnectionError;
                FireConnectionEvent(DeviceConnectionState.DisconnectionError, "RCC disconnection error");
            }
        }

        internal void Stop()
        {
            m_Stop = true;

            if (!m_IsEmulation)
                m_IOGate.Stop();
        }

        internal bool IsReadyToStart()
        {
            //готовность данного виртуального блока определяется готовностью физического блока Gate
            if (m_IsEmulation)
                return true;
            else
                return m_IOGate.IsReadyToStart();
        }

        internal DeviceState Start(TestParameters Parameters, Types.Commutation.TestParameters commParameters, out RCCResult resultRCC)
        {
            m_Parameters = Parameters;

            if (m_State == DeviceState.InProcess)
                throw new Exception("RCC test is already started.");

            m_Result = new TestResults();
            m_Result.TestTypeId = m_Parameters.TestTypeId;

            m_Stop = false;

            if (!m_IsEmulation)
            {
                //результат прозвонки цепи катод-катод не определён
                resultRCC = RCCResult.OPRESULT_NONE;

                //смотрим состояние Gate
                m_IOGate.ClearWarning();
                ushort State = m_IOGate.ReadDeviceState();

                switch (State)
                {
                    case (ushort)Types.Gate.HWDeviceState.Fault:
                        ushort faultReason = m_IOGate.ReadFaultReason();
                        FireNotificationEvent(ComplexParts.Gate, (ushort)HWProblemReason.None, (ushort)HWWarningReason.None, faultReason, (ushort)HWDisableReason.None);

                        return (DeviceState)State;

                    case (ushort)Types.Gate.HWDeviceState.Disabled:
                        ushort disableReason = m_IOGate.ReadDisableReason();
                        FireNotificationEvent(ComplexParts.Gate, (ushort)HWProblemReason.None, (ushort)HWWarningReason.None, (ushort)HWFaultReason.None, disableReason);

                        return (DeviceState)State;
                }
            }

            MeasurementLogicRoutine(commParameters);

            resultRCC = m_Result.RCC;

            return m_State;
        }

        private void WaitForEndOfGateTest()
        {
            const int m_Timeout = 10000;
            const int REQUEST_DELAY_MS = 50;

            var timeStamp = Environment.TickCount + m_Timeout;

            while (Environment.TickCount < timeStamp)
            {
                if (m_Stop)
                    return;

                var devState = (Types.Gate.HWDeviceState)m_IOGate.ReadDeviceState(true);
                var opResult = (Types.Gate.HWOperationResult)m_IOGate.ReadFinished(true);

                if (devState == Types.Gate.HWDeviceState.Fault)
                {
                    ushort faultReason = m_IOGate.ReadFaultReason();

                    FireNotificationEvent(ComplexParts.Gate, (ushort)HWProblemReason.None, (ushort)HWWarningReason.None, faultReason, (ushort)HWDisableReason.None);

                    throw new Exception(string.Format("RCC virtual device. Gate device is in fault state, reason - {0}", faultReason));
                }

                if (devState == Types.Gate.HWDeviceState.Disabled)
                {
                    ushort disableReason = m_IOGate.ReadDisableReason();

                    FireNotificationEvent(ComplexParts.Gate, (ushort)HWProblemReason.None, (ushort)HWWarningReason.None, (ushort)HWFaultReason.None, disableReason);

                    throw new Exception(string.Format("RCC virtual device. Gate device is in disabled state, reason - {0}", disableReason));
                }

                if (opResult != Types.Gate.HWOperationResult.InProcess)
                {
                    ushort problem = m_IOGate.ReadProblem();
                    if (problem != (ushort)Types.Gate.HWProblemReason.None)
                        FireNotificationEvent(ComplexParts.Gate, problem, (ushort)HWWarningReason.None, (ushort)HWFaultReason.None, (ushort)HWDisableReason.None);

                    ushort warning = m_IOGate.ReadWarning();
                    if (warning != (ushort)Types.Gate.HWWarningReason.None)
                    {
                        FireNotificationEvent(ComplexParts.Gate, (ushort)HWProblemReason.None, warning, (ushort)HWFaultReason.None, (ushort)HWDisableReason.None);
                        m_IOGate.ClearWarning();
                    }

                    break;
                }

                Thread.Sleep(REQUEST_DELAY_MS);
            }

            if (Environment.TickCount > timeStamp)
            {
                string mess = "RCC virtual device. Timeout while waiting for Gate test to end";
                FireExceptionEvent(mess);
                throw new Exception(mess);
            }
        }

        private void MeasurementLogicRoutine(Types.Commutation.TestParameters Commutation)
        {
            try
            {
                m_State = DeviceState.InProcess;

                //уведомляем UI о том, что мы находимся в состоянии m_State с результатами измерений m_Result
                FireEvent(m_State, m_Result);

                if (m_IsEmulation)
                {
                    //эмулируем успешный результат измерений
                    m_State = DeviceState.Success;
                    m_Result.RCC = RCCResult.OPRESULT_OK;
                }
                else
                {
                    //включаем требуемую коммутацию               
                    m_IOCommutation.WriteRegister(IOCommutation.REG_MODULE_TYPE, (ushort)Commutation.CommutationType);
                    m_IOCommutation.CallAction(IOCommutation.ACT_COMM2_RCC);

                    //запускаем прозвонку цепи катод-катод
                    m_IOGate.CallAction(IOGate.ACT_START_RCC);

                    WaitForEndOfGateTest();

                    //прозвонка цепи катод-катод успешно исполнена
                    m_State = DeviceState.Success;

                    //считываем значение регистра результата
                    m_Result.RCC = (RCCResult)m_IOGate.ReadRegister(IOGate.REG_TEST_FINISHED);
                }

                FireEvent(m_State, m_Result);
            }
            catch (Exception e)
            {
                m_State = DeviceState.Fault;
                FireEvent(m_State, m_Result);
                FireExceptionEvent(e.Message);

                throw;
            }
        }

        #region Events
        private void FireConnectionEvent(DeviceConnectionState State, string Message)
        {
            SystemHost.Journal.AppendLog(ComplexParts.RCC, LogMessageType.Info, Message);
            m_Communication.PostDeviceConnectionEvent(ComplexParts.RCC, State, Message);
        }

        private void FireNotificationEvent(ComplexParts Sender, ushort Problem, ushort Warning, ushort Fault, ushort Disable)
        {
            SystemHost.Journal.AppendLog(ComplexParts.RCC, LogMessageType.Warning, string.Format("RCC device notification: {0} problem {1}, {0} warning {2}, {0} fault {3}, {0} disable {4}", Sender, Problem, Warning, Fault, Disable));
            m_Communication.PostRCCNotificationEvent(Problem, Warning, Fault, Disable);
        }

        private void FireEvent(DeviceState State, TestResults Result)
        {
            string message = string.Format("RCC test state {0}, RCC result {1}", State, Result.RCC.ToString());

            SystemHost.Journal.AppendLog(ComplexParts.RCC, LogMessageType.Info, message);
            m_Communication.PostRCCEvent(State, Result);
        }

        private void FireExceptionEvent(string Message)
        {
            SystemHost.Journal.AppendLog(ComplexParts.RCC, LogMessageType.Error, Message);
            m_Communication.PostExceptionEvent(ComplexParts.RCC, Message);
        }
        #endregion
    }
}
