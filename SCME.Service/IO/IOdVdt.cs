using System;
using System.Collections.Generic;
using System.Threading;
using SCME.Service.Properties;
using SCME.Types;
using SCME.Types.dVdt;

namespace SCME.Service.IO
{
    internal class IOdVdt
    {
        private const int REQUEST_DELAY_MS = 50;

        private readonly IOAdapter m_IOAdapter;
        private readonly BroadcastCommunication m_Communication;
        private readonly ushort m_Node;
        private readonly bool m_IsdVdtEmulationHard;
        private IOCommutation m_IOCommutation;
        private bool m_IsdVdtEmulation;
        private TestParameters m_Parameters;
        private DeviceConnectionState m_ConnectionState;
        private volatile DeviceState m_State;
        private volatile TestResults m_Result;
        private volatile bool m_Stop;

        private int m_Timeout = 25000;

        internal IOdVdt(IOAdapter Adapter, BroadcastCommunication Communication)
        {
            m_IOAdapter = Adapter;
            m_Communication = Communication;
            m_IsdVdtEmulationHard = Settings.Default.dVdtEmulation;
            m_IsdVdtEmulation = m_IsdVdtEmulationHard;

            m_Node = (ushort)Settings.Default.dVdtNode;
            m_Result = new TestResults();

            SystemHost.Journal.AppendLog(ComplexParts.DvDt, LogMessageType.Info,
                                         String.Format("dVdt created. Emulation mode: {0}", Settings.Default.dVdtEmulation));
        }


        internal IOCommutation ActiveCommutation
        {
            get { return m_IOCommutation; }
            set { m_IOCommutation = value; }
        }

        internal DeviceConnectionState Initialize(bool Enable, int timeoutdVdt)
        {
            m_Timeout = timeoutdVdt;
            m_IsdVdtEmulation = m_IsdVdtEmulationHard || !Enable;

            m_ConnectionState = DeviceConnectionState.ConnectionInProcess;
            FireConnectionEvent(m_ConnectionState, "dVdt initializing");

            if (m_IsdVdtEmulation)
            {
                m_ConnectionState = DeviceConnectionState.ConnectionSuccess;
                FireConnectionEvent(m_ConnectionState, "dVdt initialized");

                return m_ConnectionState;
            }

            try
            {
                var timeStamp = Environment.TickCount + m_Timeout;

                ClearWarning();

                var devState = (HWDeviceState)ReadRegister(REG_DEVICE_STATE);
                if (devState != HWDeviceState.PowerReady)
                {
                    if (devState == HWDeviceState.Fault)
                    {
                        ClearFault();
                        Thread.Sleep(100);

                        devState = (HWDeviceState)ReadRegister(REG_DEVICE_STATE);

                        if (devState == HWDeviceState.Fault)
                            throw new Exception(string.Format("dVdt is in fault state, reason: {0}",
                                (HWFaultReason)ReadRegister(REG_FAULT_REASON)));
                    }

                    if (devState == HWDeviceState.Disabled)
                        throw new Exception(string.Format("dVdt is in disabled state, reason: {0}",
                                (HWDisableReason)ReadRegister(REG_DISABLE_REASON)));

                    CallAction(ACT_ENABLE_POWER);
                }

                while (Environment.TickCount < timeStamp)
                {
                    Thread.Sleep(100);

                    devState = (HWDeviceState)
                               ReadRegister(REG_DEVICE_STATE);

                    if (devState == HWDeviceState.PowerReady)
                        break;

                    if (devState == HWDeviceState.Fault)
                        throw new Exception(string.Format("dVdt is in fault state, reason: {0}",
                                                          (HWFaultReason)ReadRegister(REG_FAULT_REASON)));
                    if (devState == HWDeviceState.Disabled)
                        throw new Exception(string.Format("dVdt is in disabled state, reason: {0}",
                                                          (HWDisableReason)ReadRegister(REG_DISABLE_REASON)));
                }

                if (Environment.TickCount > timeStamp)
                    throw new Exception("Timeout while waiting for device to power up");

                m_ConnectionState = DeviceConnectionState.ConnectionSuccess;

                FireConnectionEvent(m_ConnectionState, "dVdt initialized");
            }
            catch (Exception ex)
            {
                m_ConnectionState = DeviceConnectionState.ConnectionFailed;
                FireConnectionEvent(m_ConnectionState, String.Format("dVdt initialization error: {0}", ex.Message));
            }

            return m_ConnectionState;
        }

        internal void Deinitialize()
        {
            var oldState = m_ConnectionState;

            m_ConnectionState = DeviceConnectionState.DisconnectionInProcess;
            FireConnectionEvent(DeviceConnectionState.DisconnectionInProcess, "dVdt disconnecting");

            try
            {
                if (!m_IsdVdtEmulation && oldState == DeviceConnectionState.ConnectionSuccess)
                {
                    Stop();
                    CallAction(ACT_DISABLE_POWER);
                }

                m_ConnectionState = DeviceConnectionState.DisconnectionSuccess;
                FireConnectionEvent(DeviceConnectionState.DisconnectionSuccess, "dVdt disconnected");
            }
            catch (Exception)
            {
                m_ConnectionState = DeviceConnectionState.DisconnectionError;
                FireConnectionEvent(DeviceConnectionState.DisconnectionError, "dVdt disconnection error");
            }
        }

        internal DeviceState Start(TestParameters Parameters, Types.Commutation.TestParameters commParameters)
        {
            m_Parameters = Parameters;

            if (m_State == DeviceState.InProcess)
                throw new Exception("dVdt test is already started");

            m_Result = new TestResults()
            {
                TestTypeId = m_Parameters.TestTypeId,
                Mode = m_Parameters.Mode,
                VoltageRate = (ushort)Parameters.VoltageRate
            };
            m_Stop = false;

            ClearWarning();

            if (!m_IsdVdtEmulation)
            {
                var devState = (HWDeviceState)ReadRegister(REG_DEVICE_STATE);
                if (devState == HWDeviceState.Fault)
                {
                    var faultReason = (HWFaultReason)ReadRegister(REG_FAULT_REASON);
                    FireNotificationEvent(HWWarningReason.None, faultReason,
                                          HWDisableReason.None);

                    throw new Exception(string.Format("dVdt is in fault state, reason: {0}", faultReason));
                }

                if (devState == HWDeviceState.Disabled)
                {
                    var disableReason = (HWDisableReason)ReadRegister(REG_DISABLE_REASON);
                    FireNotificationEvent(HWWarningReason.None,
                                          HWFaultReason.None, disableReason);

                    throw new Exception(string.Format("dVdt is in disabled state, reason: {0}", disableReason));
                }
            }

            MeasurementLogicRoutine(commParameters);

            return m_State;
        }

        internal void Stop()
        {
            CallAction(ACT_STOP);
            m_Stop = true;
            m_State = DeviceState.Stopped;
        }

        internal bool IsReadyToStart()
        {
            var devState = (Types.dVdt.HWDeviceState)ReadRegister(REG_DEVICE_STATE);

            return !((devState == Types.dVdt.HWDeviceState.Fault) || (devState == Types.dVdt.HWDeviceState.Disabled) || (m_State == DeviceState.InProcess));
        }

        #region Standart API

        internal void ClearFault()
        {
            SystemHost.Journal.AppendLog(ComplexParts.DvDt, LogMessageType.Note, "dVdt fault cleared");

            CallAction(ACT_CLEAR_FAULT);
        }

        private void ClearWarning()
        {
            SystemHost.Journal.AppendLog(ComplexParts.DvDt, LogMessageType.Note, "dVdt warning cleared");

            CallAction(ACT_CLEAR_WARNING);
        }

        internal ushort ReadRegister(ushort Address, bool SkipJournal = false)
        {
            ushort value = 0;

            if (!m_IsdVdtEmulation)
                value = m_IOAdapter.Read16(m_Node, Address);

            if (!SkipJournal)
                SystemHost.Journal.AppendLog(ComplexParts.DvDt, LogMessageType.Note,
                                         string.Format("dVdt @ReadRegister, address {0}, value {1}", Address, value));

            return value;
        }

        internal short ReadRegisterS(ushort Address, bool SkipJournal = false)
        {
            short value = 0;

            if (!m_IsdVdtEmulation)
                value = m_IOAdapter.Read16S(m_Node, Address);

            if (!SkipJournal)
                SystemHost.Journal.AppendLog(ComplexParts.DvDt, LogMessageType.Note,
                                         string.Format("dVdt @ReadRegisterS, address {0}, value {1}", Address, value));

            return value;
        }

        internal void WriteRegister(ushort Address, ushort Value, bool SkipJournal = false)
        {
            if (!SkipJournal)
                SystemHost.Journal.AppendLog(ComplexParts.DvDt, LogMessageType.Note,
                                         string.Format("dVdt @WriteRegister, address {0}, value {1}", Address, Value));

            if (m_IsdVdtEmulation)
                return;

            m_IOAdapter.Write16(m_Node, Address, Value);
        }

        internal void CallAction(ushort Action)
        {
            SystemHost.Journal.AppendLog(ComplexParts.DvDt, LogMessageType.Note,
                                         string.Format("dVdt @Call, action {0}", Action));

            if (m_IsdVdtEmulation)
                return;

            m_IOAdapter.Call(m_Node, Action);
        }

        #endregion

        internal void WriteCalibrationParams(CalibrationParams Parameters)
        {
            SystemHost.Journal.AppendLog(ComplexParts.DvDt, LogMessageType.Note,
                                         "dVdt @WriteCalibrationParams begin");

            //WriteRegister(REG_V_FINE_N, Parameters.VFineN, true);
            //WriteRegister(REG_V_FINE_D, Parameters.VFineD, true);

            //WriteRegister(REG_G500, Parameters.V500, true);
            //WriteRegister(REG_G1000, Parameters.V1000, true);
            //WriteRegister(REG_G1500, Parameters.V1500, true);
            //WriteRegister(REG_G2000, Parameters.V2000, true);
            //WriteRegister(REG_G2500, Parameters.V2500, true);

            //if (!m_IsdVdtEmulation)
            //    m_IOAdapter.Call(m_Node, ACT_SAVE_TO_ROM);

            SystemHost.Journal.AppendLog(ComplexParts.DvDt, LogMessageType.Note,
                                         "dVdt @WriteCalibrationParams end");
        }

        internal CalibrationParams ReadCalibrationParams()
        {
            SystemHost.Journal.AppendLog(ComplexParts.DvDt, LogMessageType.Note,
                                         "dVdt @ReadCalibrationParams begin");

            var parameters = new CalibrationParams
            {
                //VFineN = ReadRegister(REG_V_FINE_N, true),
                //VFineD = ReadRegister(REG_V_FINE_D, true),

                //V500 = ReadRegister(REG_G500, true),
                //V1000 = ReadRegister(REG_G1000, true),
                //V1500 = ReadRegister(REG_G1500, true),
                //V2000 = ReadRegister(REG_G2000, true),
                //V2500 = ReadRegister(REG_G2500, true)
            };

            SystemHost.Journal.AppendLog(ComplexParts.DvDt, LogMessageType.Note,
                                         "dVdt @ReadCalibrationParams end");

            return parameters;
        }


        private void PrepareSteps(List<ushort> Steps)
        {
            //формирует в принятом Steps список шагов роста напряжения на которых прибор тестируется на открытие
            if (Steps != null)
            {
                if (m_Parameters.VoltageRateOffSet <= 0) throw new Exception("scme.service.io.iodvdt.cs PrepareSteps. m_Parameters.VoltageRateOffSet<=0.");

                //очищаем содержимое Steps
                Steps.Clear();

                //исходная точка от которой вычисляется значение роста напряжения есть 500 В/мкс. на 20.04.2017 это ограничено аппаратными возможностями блока dVdt
                ushort VoltageRateStart = 500;
                Steps.Add(VoltageRateStart);

                //значение роста напряжения на любом последующем шаге отличается от текщего шага на величину смещения VoltageRateOffSet, но итоговое значение роста напряжения не должно превысить ограничения роста напряжения VoltageRateLimit
                int VoltageRate = VoltageRateStart;
                int CalcedVoltageRate = 0;

                while (true)
                {
                    CalcedVoltageRate = VoltageRate + m_Parameters.VoltageRateOffSet;

                    if (CalcedVoltageRate >= m_Parameters.VoltageRateLimit)
                    {
                        //CalcedVoltageRate вылезает за VoltageRateLimit. поэтому пишем вместо него VoltageRateLimit
                        Steps.Add(m_Parameters.VoltageRateLimit);

                        break;
                    }
                    else
                    {
                        VoltageRate = CalcedVoltageRate;
                        Steps.Add((ushort)VoltageRate);
                    }

                }

            }
        }


        private void MeasurementLogicRoutine(Types.Commutation.TestParameters Commutation)
        {
            try
            {
                m_State = DeviceState.InProcess;
                FiredVdtEvent(m_State, m_Result);

                WriteRegister(REG_DESIRED_VOLTAGE, m_Parameters.Voltage);

                //перед измерением dVdt исполняем команду включения коммутации см. требование http://elma.pe.local/Tasks/Task/Execute/108699
                if (m_IOCommutation.Switch(Types.Commutation.CommutationMode.DVDT, Commutation.CommutationType, Commutation.Position) == DeviceState.Fault)
                {
                    m_State = DeviceState.Fault;
                    m_Result.Passed = false;
                    FiredVdtEvent(m_State, m_Result);
                    return;
                }

                if (m_IsdVdtEmulation)
                {
                    //в режиме эмуляции эмулируем успешный результат проверки
                    m_State = DeviceState.Success;
                    m_Result.Passed = true;

                    //значение VoltageRate имеет смысл только для режима "Определение"
                    if (m_Parameters.Mode == DvdtMode.Detection)
                        m_Result.VoltageRate = 2200;
                    else m_Result.VoltageRate = 0;
                }
                else
                {
                    if (m_Parameters.Mode == DvdtMode.Confirmation)
                    {
                        //режим "Подтверждение" (Confirmation)
                        bool opened = false;
                        ushort testCount = 0;

                        //повторяем тест подтверждения столько раз, сколько задано в параметре "Число повторений теста подтверждения", тест имеет смысл пока прибор не открылся. если прибор открылся - он считается браком, повторять тест подтверждения для бракованного прибора нет смысла 
                        while ((!opened) && (testCount < m_Parameters.ConfirmationCount))
                        {
                            switch (m_Parameters.VoltageRate)
                            {
                                case VoltageRate.V500:
                                    CallAction(ACT_START_TEST_500);
                                    break;
                                case VoltageRate.V1000:
                                    CallAction(ACT_START_TEST_1000);
                                    break;
                                case VoltageRate.V1600:
                                    CallAction(ACT_START_TEST_1600);
                                    break;
                                case VoltageRate.V2000:
                                    CallAction(ACT_START_TEST_2000);
                                    break;
                                case VoltageRate.V2500:
                                    CallAction(ACT_START_TEST_2500);
                                    break;
                            }

                            m_State = WaitForEndOfTest();
                            opened = (ReadRegisterS(REG_TEST_RESULT) == 0);
                            m_Result.Passed = !opened;

                            //увеличиваем счётчик выполненных тестов "Подтверждение"
                            testCount++;
                        }
                    }
                    else
                    {
                        if (m_Parameters.Mode == DvdtMode.Detection)
                        {
                            //режим "Определение" (Detection)
                            //формируем стек шагов по которым будем шагать
                            var Steps = new List<ushort>();
                            PrepareSteps(Steps);

                            ushort TestVoltageRate = 0;
                            ushort i = 0;
                            bool opened = false;
                            bool moveDown = false;

                            while (true)
                            {
                                //считываем скорость роста напряжения на которой мы собрались тестировать прибор
                                TestVoltageRate = Steps[i];

                                //запускаем проверку прибора на скорости роста напряжения TestVoltageRate
                                WriteRegister(REG_VOLTAGE_RATE, TestVoltageRate);
                                CallAction(ACT_START_TEST_CUSTOM);

                                m_State = WaitForEndOfTest();
                                opened = (ReadRegisterS(REG_TEST_RESULT) == 0);

                                //если мы проверили прибор на максимальной скорости роста напряжения и он не открылся - он годен, тест завершён
                                if ((i == Steps.Count - 1) && (!opened))
                                {
                                    m_Result.Passed = true;
                                    m_Result.VoltageRate = TestVoltageRate;

                                    break;
                                }

                                //если мы спустились на самую первую ступеньку после того, как было зафиксировано открытие прибора и на этой последней ступеньке было зафиксировано открытие прибора - прибор брак, тест завершён
                                if ((i == 0) && opened)
                                {
                                    m_Result.Passed = false;
                                    m_Result.VoltageRate = 0;

                                    break;
                                }

                                //если мы двигаемся вниз и прибор не открылся - прибор годен для TestVoltageRate, тест завершён
                                if (moveDown && (!opened))
                                {
                                    m_Result.Passed = true;
                                    m_Result.VoltageRate = TestVoltageRate;

                                    break;
                                }

                                //если почему-то индекс i стал больше, чем число шагов минус 1
                                if (i > Steps.Count - 1)
                                {
                                    throw new Exception("scme.service.io.iodvdt.cs MeasurementLogicRoutine. (i>Steps.Count-1.");
                                }

                                //если почему-то индекс i стал отрицательным
                                if (i < 0)
                                {
                                    throw new Exception("scme.service.io.iodvdt.cs MeasurementLogicRoutine. i<0.");

                                }

                                //если мы пошли вниз - дорога вверх уже закрыта
                                if (opened || moveDown)
                                {
                                    moveDown = true;
                                    i--;
                                }
                                else i++;

                            }

                        }
                    }

                }

                FiredVdtEvent(m_State, m_Result);
            }

            catch (Exception ex)
            {
                m_State = DeviceState.Fault;
                FiredVdtEvent(m_State, m_Result);
                FireExceptionEvent(ex.Message);

                throw;
            }

        }


        private DeviceState WaitForEndOfTest()
        {
            var timeStamp = Environment.TickCount + m_Timeout;

            while (Environment.TickCount < timeStamp)
            {

                var devState = (HWDeviceState)ReadRegister(REG_DEVICE_STATE, true);

                if (devState == HWDeviceState.Fault)
                {
                    var faultReason = (HWFaultReason)ReadRegister(REG_FAULT_REASON);

                    FireNotificationEvent(HWWarningReason.None, faultReason,
                                          HWDisableReason.None);
                    throw new Exception(string.Format("dVdt device is in fault state, reason: {0}", faultReason));
                }

                if (devState == HWDeviceState.Disabled)
                {
                    var disableReason = (HWDisableReason)ReadRegister(REG_DISABLE_REASON);

                    FireNotificationEvent(HWWarningReason.None,
                                          HWFaultReason.None, disableReason);
                    throw new Exception(string.Format("dVdt device is in disabled state, reason: {0}", disableReason));
                }

                if (devState != HWDeviceState.InProcess)
                {
                    var warning = (HWWarningReason)ReadRegister(REG_WARNING);

                    if (warning != HWWarningReason.None)
                    {
                        FireNotificationEvent(warning, HWFaultReason.None,
                                              HWDisableReason.None);
                        ClearWarning();
                    }

                    break;
                }

                Thread.Sleep(REQUEST_DELAY_MS);
            }

            if (Environment.TickCount > timeStamp)
            {
                FireExceptionEvent("Timeout while waiting for dVdt test to end");
                throw new Exception("Timeout while waiting for dVdt test to end");
            }

            return DeviceState.Success;
        }

        #region Events

        private void FireConnectionEvent(DeviceConnectionState State, string Message)
        {
            SystemHost.Journal.AppendLog(ComplexParts.DvDt, LogMessageType.Info, Message);
            m_Communication.PostDeviceConnectionEvent(ComplexParts.DvDt, State, Message);
        }

        private void FiredVdtEvent(DeviceState State, TestResults Result)
        {
            var message = string.Format("dVdt test state {0}", State);

            if (State == DeviceState.Success)
                message = string.Format("dVdt test result {0}", Result.Passed);

            SystemHost.Journal.AppendLog(ComplexParts.DvDt, LogMessageType.Info, message);
            m_Communication.PostdVdtEvent(State, Result);
        }

        private void FireNotificationEvent(HWWarningReason Warning, HWFaultReason Fault, HWDisableReason Disable)
        {
            SystemHost.Journal.AppendLog(ComplexParts.DvDt, LogMessageType.Warning,
                                         string.Format(
                                             "dVdt device notification: problem None, warning {0}, fault {1}, disable {2}",
                                             Warning, Fault, Disable));

            m_Communication.PostdVdtNotificationEvent(Warning, Fault, Disable);
        }

        private void FireExceptionEvent(string Message)
        {
            SystemHost.Journal.AppendLog(ComplexParts.DvDt, LogMessageType.Error, Message);
            m_Communication.PostExceptionEvent(ComplexParts.DvDt, Message);
        }

        #endregion

        #region Registers

        private const ushort
            ACT_ENABLE_POWER = 1,
            ACT_DISABLE_POWER = 2,
            ACT_CLEAR_FAULT = 3,
            ACT_CLEAR_WARNING = 4,            

            ACT_START_TEST_CUSTOM = 100,
            ACT_START_TEST_500 = 101,
            ACT_START_TEST_1000 = 102,
            ACT_START_TEST_1600 = 103,
            ACT_START_TEST_2000 = 104,
            ACT_START_TEST_2500 = 105,
            ACT_STOP = 109,

            REG_DESIRED_VOLTAGE = 128,
            REG_VOLTAGE_RATE = 129,

            REG_DEVICE_STATE = 192,
            REG_FAULT_REASON = 193,
            REG_DISABLE_REASON = 194,
            REG_WARNING = 195,
            REG_PROBLEM = 196,
            REG_TEST_FINISHED = 197,

            REG_TEST_RESULT = 198;





        #endregion
    }
}