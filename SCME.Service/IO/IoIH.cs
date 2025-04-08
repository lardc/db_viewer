using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SCME.Service.Properties;
using SCME.Types;
using SCME.Types.IH;

namespace SCME.Service.IO
{
    internal class IOIH
    {
        private readonly IOGate m_IOGate;
        private readonly IOStLs m_IOStLs;
        private readonly BroadcastCommunication m_Communication;
        private readonly bool m_IsEmulationHard;
        private bool m_IsEmulation;
        private IOCommutation m_IOCommutation;
        private DeviceConnectionState m_ConnectionState;
        private volatile bool m_Stop;
        private Types.IH.TestParameters m_Parameters;
        private volatile DeviceState m_State;
        private volatile Types.IH.TestResults m_Result;

        internal IOIH(IOGate Gate, IOStLs StLs, BroadcastCommunication Communication)
        {
            m_IOGate = Gate;
            m_IOStLs = StLs;

            m_Communication = Communication;
            m_IsEmulationHard = Settings.Default.IHEmulation;
        }

        internal IOCommutation ActiveCommutation
        {
            get { return m_IOCommutation; }
            set { m_IOCommutation = value; }
        }

        internal DeviceConnectionState Initialize(bool Enable, int Timeout)
        {
            m_IsEmulation = m_IsEmulationHard || !Enable;

            m_ConnectionState = DeviceConnectionState.ConnectionInProcess;
            FireConnectionEvent(m_ConnectionState, "IH initializing");

            if (m_IsEmulation)
            {
                m_ConnectionState = DeviceConnectionState.ConnectionSuccess;
                FireConnectionEvent(m_ConnectionState, "IH initialized");

                return m_ConnectionState;
            }

            //физического блока IH не существует, поэтому инициализировать нечего
            m_ConnectionState = DeviceConnectionState.ConnectionSuccess;

            return m_ConnectionState;
        }

        internal void Deinitialize()
        {
            m_ConnectionState = DeviceConnectionState.DisconnectionInProcess;
            FireConnectionEvent(DeviceConnectionState.DisconnectionInProcess, "IH disconnecting");

            try
            {
                //физического блока нет - поэтому управлять нечем
                m_ConnectionState = DeviceConnectionState.DisconnectionSuccess;
                FireConnectionEvent(DeviceConnectionState.DisconnectionSuccess, "IH disconnected");
            }
            catch (Exception)
            {
                m_ConnectionState = DeviceConnectionState.DisconnectionError;
                FireConnectionEvent(DeviceConnectionState.DisconnectionError, "IH disconnection error");
            }
        }

        internal void Stop()
        {
            m_Stop = true;
        }

        internal bool IsReadyToStart()
        {
            //готовность данного виртуального блока определяется готовностью физических блоков
            return m_IOGate.IsReadyToStart() && m_IOStLs.IsReadyToStart();
        }

        internal DeviceState Start(TestParameters Parameters, Types.Commutation.TestParameters commParameters)
        {
            m_Parameters = Parameters;

            if (m_State == DeviceState.InProcess)
                throw new Exception("IH test is already started.");

            m_Result = new TestResults();
            m_Result.TestTypeId = m_Parameters.TestTypeId;

            m_Stop = false;

            if (!m_IsEmulation)
            {
                //смотрим состояние Gate
                m_IOGate.ClearWarning();
                ushort State = m_IOGate.ReadDeviceState();

                switch (State)
                {
                    case (ushort)Types.Gate.HWDeviceState.Fault:
                        ushort faultReason = m_IOGate.ReadFaultReason();
                        FireNotificationEvent(ComplexParts.Gate, (ushort)HWProblemReason.None, (ushort)HWWarningReason.None, faultReason, (ushort)HWDisableReason.None);

                        break;

                    case (ushort)Types.Gate.HWDeviceState.Disabled:
                        ushort disableReason = m_IOGate.ReadDisableReason();
                        FireNotificationEvent(ComplexParts.Gate, (ushort)HWProblemReason.None, (ushort)HWWarningReason.None, (ushort)HWFaultReason.None, disableReason);

                        break;
                }

                //смотрим состояние StLs
                m_IOStLs.ClearWarning();
                State = m_IOStLs.ReadDeviceState();

                switch (State)
                {
                    case (ushort)Types.SL.HWDeviceState.Fault:
                        ushort faultReason = m_IOStLs.ReadFaultReason();
                        FireNotificationEvent(ComplexParts.SL, (ushort)HWProblemReason.None, (ushort)HWWarningReason.None, faultReason, (ushort)HWDisableReason.None);

                        break;

                    case (ushort)Types.SL.HWDeviceState.Disabled:
                        ushort disableReason = m_IOStLs.ReadDisableReason();
                        FireNotificationEvent(ComplexParts.SL, (ushort)HWProblemReason.None, (ushort)HWWarningReason.None, (ushort)HWFaultReason.None, disableReason);

                        break;
                }
            }

            MeasurementLogicRoutine(commParameters);

            return m_State;
        }

        private DeviceState WaitForEndOfSLTest()
        {
            const int m_Timeout = 25000;
            const int REQUEST_DELAY_MS = 50;

            var timeStamp = Environment.TickCount + m_Timeout;

            while (Environment.TickCount < timeStamp)
            {
                if (m_Stop)
                {
                    m_IOStLs.Stop();
                    return DeviceState.Stopped;
                }

                var devState = (Types.SL.HWDeviceState)m_IOStLs.ReadDeviceState(true);
                var opResult = (Types.SL.HWOperationResult)m_IOStLs.ReadFinished(true);

                if (devState == Types.SL.HWDeviceState.Fault)
                {
                    ushort faultReason = m_IOStLs.ReadFaultReason();

                    FireNotificationEvent(ComplexParts.SL, (ushort)HWProblemReason.None, (ushort)HWWarningReason.None, faultReason, (ushort)HWDisableReason.None);

                    throw new Exception(string.Format("IH virtual device. SL device is in fault state, reason: {0}", faultReason));
                }

                if (devState == Types.SL.HWDeviceState.Disabled)
                {
                    ushort disableReason = m_IOStLs.ReadDisableReason();

                    FireNotificationEvent(ComplexParts.SL, (ushort)HWProblemReason.None, (ushort)HWWarningReason.None, (ushort)HWFaultReason.None, disableReason);

                    throw new Exception(string.Format("IH virtual device. SL device is in disabled state, reason: {0}", disableReason));
                }

                if (opResult != Types.SL.HWOperationResult.InProcess)
                {
                    ushort warning = m_IOStLs.ReadWarning();
                    ushort problem = m_IOStLs.ReadProblem();

                    if (problem != (ushort)Types.SL.HWProblemReason.None)
                    {
                        FireNotificationEvent(ComplexParts.SL, problem, (ushort)HWWarningReason.None, (ushort)HWFaultReason.None, (ushort)HWDisableReason.None);
                    }

                    if (warning != (ushort)HWWarningReason.None)
                    {
                        FireNotificationEvent(ComplexParts.SL, (ushort)HWProblemReason.None, warning, (ushort)HWFaultReason.None, (ushort)HWDisableReason.None);

                        m_IOStLs.ClearWarning();
                    }

                    break;
                }

                Thread.Sleep(REQUEST_DELAY_MS);
            }

            if (Environment.TickCount > timeStamp)
            {
                FireExceptionEvent("IH virtual device. Timeout while waiting for SL test to end");
                throw new Exception("IH virtual device. Timeout while waiting for SL test to end");
            }

            return DeviceState.Success;
        }

        private void WaitForEndOfGateTest()
        {
            const int m_Timeout = 50000;
            const int REQUEST_DELAY_MS = 50;

            var timeStamp = Environment.TickCount + m_Timeout;

            while (Environment.TickCount < timeStamp)
            {
                var devState = (Types.Gate.HWDeviceState)m_IOGate.ReadDeviceState(true);
                var opResult = (Types.Gate.HWOperationResult)m_IOGate.ReadFinished(true);

                if (devState == Types.Gate.HWDeviceState.Fault)
                {
                    ushort faultReason = m_IOGate.ReadFaultReason();

                    FireNotificationEvent(ComplexParts.Gate, (ushort)HWProblemReason.None, (ushort)HWWarningReason.None, faultReason, (ushort)HWDisableReason.None);

                    throw new Exception(string.Format("IH virtual device. Gate device is in fault state, reason - {0}", faultReason));
                }

                if (devState == Types.Gate.HWDeviceState.Disabled)
                {
                    ushort disableReason = m_IOGate.ReadDisableReason();

                    FireNotificationEvent(ComplexParts.Gate, (ushort)HWProblemReason.None, (ushort)HWWarningReason.None, (ushort)HWFaultReason.None, disableReason);

                    throw new Exception(string.Format("IH virtual device. Gate device is in disabled state, reason - {0}", disableReason));
                }

                if (opResult != Types.Gate.HWOperationResult.InProcess)
                {                    
                    ushort problem = m_IOGate.ReadProblem();
                    ushort warning = m_IOGate.ReadWarning();

                    if (problem != (ushort)Types.Gate.HWProblemReason.None)
                        FireNotificationEvent(ComplexParts.Gate, problem, (ushort)HWWarningReason.None, (ushort)HWFaultReason.None, (ushort)HWDisableReason.None);

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
                FireExceptionEvent("IH virtual device. Timeout while waiting for Gate test to end");
                throw new Exception("IH virtual device. Timeout while waiting for Gate test to end");
            }
        }

        private void MeasurementLogicRoutine(Types.Commutation.TestParameters Commutation)
        {
            try
            {
                m_State = DeviceState.InProcess;

                //уведомляем UI о том, что мы находимся в состоянии m_State с результатами измерений m_Result
                FireEvent(m_State, m_Result);

                m_IOCommutation.CallAction(IOCommutation.ACT_COMM_IH);

                try
                {
                    m_IOGate.WriteRegister(130, 1);

                    try
                    {
                        m_IOGate.CallAction(IOGate.ACT_START_IH);//102

                        m_IOStLs.WriteRegister(128, 1);
                        m_IOStLs.WriteRegister(140, m_Parameters.Itm);
                        m_IOStLs.WriteRegister(141, 10000);
                        m_IOStLs.WriteRegister(160, 1);
                        m_IOStLs.CallAction(100);

                        if (m_IsEmulation)
                        {
                            //эмулируем успешный результат измерений
                            m_State = DeviceState.Success;
                            m_Result.Ih = 10;

                            //проверяем отображение Problem, Warning, Fault
                            FireNotificationEvent(ComplexParts.IH, 7, (ushort)HWWarningReason.None, (ushort)HWFaultReason.None, (ushort)HWDisableReason.None);
                            FireNotificationEvent(ComplexParts.IH, (ushort)HWProblemReason.None, 2, (ushort)HWFaultReason.None, (ushort)HWDisableReason.None);
                            FireNotificationEvent(ComplexParts.IH, (ushort)HWProblemReason.None, (ushort)HWWarningReason.None, 1, (ushort)HWDisableReason.None);
                        }
                        else
                        {
                            if (WaitForEndOfSLTest() == DeviceState.Success)
                            {
                                WaitForEndOfGateTest();

                                //тесты в обоих блоках завершились успешно, поэтому читаем регистры результатов
                                m_State = DeviceState.Success;
                                m_Result.Ih = m_IOGate.ReadRegister(201);
                            }
                        }
                    }

                    finally
                    {
                        //регистр 130 не зависимо от результата измерения надо выставить в ноль
                        m_IOGate.WriteRegister(130, 0);
                    }
                }

                finally
                {
                    //выполняем команду 110 на блоке коммутации
                    m_IOCommutation.CallAction(110);
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

        #region Standart API
        internal void ClearFault()
        {
            //очистка ошибки виртуального блока IH
            SystemHost.Journal.AppendLog(ComplexParts.IH, LogMessageType.Note, "IH try to clear fault");
            m_IOGate.ClearFault();
            m_IOStLs.ClearFault();
            SystemHost.Journal.AppendLog(ComplexParts.IH, LogMessageType.Note, "IH fault cleared");
        }

        private void ClearWarning()
        {
            //очистка предупреждения виртуального блока IH
            SystemHost.Journal.AppendLog(ComplexParts.IH, LogMessageType.Note, "IH try to clear warning");
            m_IOGate.ClearWarning();
            m_IOStLs.ClearWarning();
            SystemHost.Journal.AppendLog(ComplexParts.IH, LogMessageType.Note, "IH warning cleared");
        }
        #endregion

        #region Events
        private void FireConnectionEvent(DeviceConnectionState State, string Message)
        {
            SystemHost.Journal.AppendLog(ComplexParts.IH, LogMessageType.Info, Message);
            m_Communication.PostDeviceConnectionEvent(ComplexParts.IH, State, Message);
        }

        private void FireNotificationEvent(ComplexParts Sender, ushort Problem, ushort Warning, ushort Fault, ushort Disable)
        {
            SystemHost.Journal.AppendLog(ComplexParts.IH, LogMessageType.Warning, string.Format("IH device notification: {0} problem {1}, {0} warning {2}, {0} fault {3}, {0} disable {4}", Sender, Problem, Warning, Fault, Disable));
            m_Communication.PostIHNotificationEvent(Problem, Warning, Fault, Disable);
        }

        private void FireEvent(DeviceState State, TestResults Result)
        {
            string message = string.Format("IH test state {0}", State);

            SystemHost.Journal.AppendLog(ComplexParts.IH, LogMessageType.Info, message);
            m_Communication.PostIHEvent(State, Result);
        }

        private void FireExceptionEvent(string Message)
        {
            SystemHost.Journal.AppendLog(ComplexParts.IH, LogMessageType.Error, Message);
            m_Communication.PostExceptionEvent(ComplexParts.IH, Message);
        }

        #endregion
    }
}
