using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SCME.Types;
using SCME.Types.RAC;
using SCME.Service.Properties;

namespace SCME.Service.IO
{
    internal class IORAC
    {
        private const int REQUEST_DELAY_MS = 50;

        private readonly IOAdapter m_IOAdapter;
        private readonly BroadcastCommunication m_Communication;
        private readonly ushort m_Node;
        private readonly bool m_IsEmulationHard;
        private bool m_IsEmulation;
        private IOCommutation m_IOCommutation;
        private DeviceConnectionState m_ConnectionState;
        private volatile bool m_Stop;
        private Types.RAC.TestParameters m_Parameters;
        private volatile DeviceState m_State;
        private volatile Types.RAC.TestResults m_Result;
        private int m_Timeout = 30000;

        internal IORAC(IOAdapter Adapter, BroadcastCommunication Communication)
        {
            m_IOAdapter = Adapter;
            m_Communication = Communication;
            m_Node = (ushort)Settings.Default.RACNode;
            m_IsEmulationHard = Settings.Default.RACEmulation;
        }

        internal IOCommutation ActiveCommutation
        {
            get { return m_IOCommutation; }
            set { m_IOCommutation = value; }
        }

        private ushort ReadDeviceState(HWDeviceState? WaitedState, int Timeout)
        //реализация чтения состояния конечного автомата
        //в WaitedState принимается состояние, в которое должен перейти конечный автомат RAC
        //реализация ожидает перехода конечного автомата в состояние WaitedState в течении времени Timeout
        //реализация возвращает считанный номер состояния конечного автомата
        {
            ushort State = ReadRegister(REG_DEV_STATE);

            if (WaitedState == null)
                return State;

            if (State == (ushort)WaitedState)
                return State;
            else
            {
                //пока не истёк таймаут - будем перечитывать состояние блока RAC через каждые 100 мс до тех пор, пока не окажемся в ожидаемом состоянии WaitedState
                var timeStamp = Environment.TickCount + Timeout;

                while (Environment.TickCount < timeStamp)
                {
                    if (m_Stop)
                    {
                        //RAC умеет команду Stop
                        CallAction(ACT_STOP);
                    }

                    Thread.Sleep(100);

                    State = ReadRegister(REG_DEV_STATE);

                    //считано состояние State, равное ожидаемому состоянию WaitedState - прерываем цикл ожидания
                    if (State == (ushort)WaitedState)
                        return State;
                }

                //раз мы здесь - значит наступил таймаут, а состояния WaitedState мы так не дождались, возвращаем то состояние, которое считали
                return State;
            }
        }

        internal DeviceConnectionState Initialize(bool Enable, int Timeout)
        {
            m_IsEmulation = m_IsEmulationHard || !Enable;

            m_ConnectionState = DeviceConnectionState.ConnectionInProcess;
            FireConnectionEvent(m_ConnectionState, "RAC initializing");

            if (m_IsEmulation)
            {
                m_ConnectionState = DeviceConnectionState.ConnectionSuccess;
                FireConnectionEvent(m_ConnectionState, "RAC initialized");

                return m_ConnectionState;
            }

            //блок RAC находится в неизвестном нам состоянии поэтому
            HWDeviceState? WaitedState = null;
            bool TrueWay;
            ushort State;
            bool End = false;

            try
            {
                while (!End)
                {
                    //чтение состояния конечного автомата
                    State = ReadDeviceState(WaitedState, Timeout);

                    TrueWay = (WaitedState == null);

                    if (!TrueWay)
                        TrueWay = (State == (ushort)WaitedState);

                    if (TrueWay)
                    {
                        switch (State)
                        {
                            case (ushort)HWDeviceState.DS_None:
                                CallAction(ACT_ENABLE_POWER);

                                //из данного состояния RAC должен перейти в состояние DS_Powered
                                WaitedState = HWDeviceState.DS_Powered;
                                break;

                            case (ushort)HWDeviceState.DS_Powered:
                                //блок RAC перешёл в состояние DS_Powered - завершаем процесс инициализации
                                m_ConnectionState = DeviceConnectionState.ConnectionSuccess;
                                End = true;
                                FireConnectionEvent(m_ConnectionState, "RAC successfully initialized.");
                                break;
                        }
                    }
                    else
                    {
                        //состояние блока RAC отличается от ожидаемого: ожидалось состояние WaitedState, а по факту оказалось состояние State
                        switch (State)
                        {
                            case (ushort)HWDeviceState.DS_Fault:
                                //сбрасываем состояние DS_Fault
                                ClearFault();

                                //после сброса состояния DS_Fault оно всегда будет DS_None
                                WaitedState = HWDeviceState.DS_None;
                                break;

                            case (ushort)HWDeviceState.DS_Powered:
                                //сознательно не сбрасываем warning ибо это всегда делается в начале MeasurementLogicRoutine
                                m_ConnectionState = DeviceConnectionState.ConnectionSuccess;
                                End = true;
                                FireConnectionEvent(m_ConnectionState, "RAC successfully initialized.");
                                break;

                            case (ushort)HWDeviceState.DS_InProcess:
                                CallAction(ACT_STOP);
                                WaitedState = HWDeviceState.DS_Powered;
                                break;

                            default:
                                //например случай DS_Disabled - не сбрасываемое состояние говорящее о серьёзном сбое. сбрасывается только при физическом переключении питания
                                throw new Exception(string.Format("state is '{0}', waited state is '{1}'", ((HWDeviceState)State).ToString(), ((HWDeviceState)WaitedState).ToString()));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                m_ConnectionState = DeviceConnectionState.ConnectionFailed;
                FireConnectionEvent(m_ConnectionState, String.Format("RAC initialization error: {0}.", e.Message));
            }

            return m_ConnectionState;
        }

        internal void Deinitialize()
        {
            var oldState = m_ConnectionState;

            m_ConnectionState = DeviceConnectionState.DisconnectionInProcess;
            FireConnectionEvent(DeviceConnectionState.DisconnectionInProcess, "RAC disconnecting");

            try
            {
                if (!m_IsEmulation && oldState == DeviceConnectionState.ConnectionSuccess)
                    CallAction(ACT_DISABLE_POWER);

                m_ConnectionState = DeviceConnectionState.DisconnectionSuccess;
                FireConnectionEvent(DeviceConnectionState.DisconnectionSuccess, "RAC disconnected");
            }
            catch (Exception)
            {
                m_ConnectionState = DeviceConnectionState.DisconnectionError;
                FireConnectionEvent(DeviceConnectionState.DisconnectionError, "RAC disconnection error");
            }
        }

        internal void Stop()
        {
            CallAction(ACT_STOP);

            m_Stop = true;
        }

        internal bool IsReadyToStart()
        {
            var devState = (Types.RAC.HWDeviceState)ReadRegister(REG_DEV_STATE);

            return !((devState == Types.RAC.HWDeviceState.DS_Fault) || (devState == Types.RAC.HWDeviceState.DS_Disabled) || (m_State == DeviceState.InProcess));
        }

        internal DeviceState Start(TestParameters Parameters, Types.Commutation.TestParameters commParameters)
        {
            m_Parameters = Parameters;

            if (m_State == DeviceState.InProcess)
                throw new Exception("RAC test is already started.");

            m_Result = new TestResults();
            m_Result.TestTypeId = m_Parameters.TestTypeId;

            m_Stop = false;

            if (!m_IsEmulation)
            {
                ushort State = ReadRegister(REG_DEV_STATE);

                switch (State)
                {
                    case (ushort)HWDeviceState.DS_None:
                        FireNotificationEvent((ushort)HWProblemReason.StateIsNoGood, (ushort)HWWarningReason.None, (ushort)HWFaultReason.None, (ushort)HWDisableReason.None);

                        break;

                    case (ushort)HWDeviceState.DS_Fault:
                        ushort faultReason = ReadRegister(REG_FAULT_REASON);
                        FireNotificationEvent((ushort)HWProblemReason.None, (ushort)HWWarningReason.None, faultReason, (ushort)HWDisableReason.None);

                        break;

                    case (ushort)HWDeviceState.DS_Disabled:
                        ushort disableReason = ReadRegister(REG_DISABLE_REASON);
                        FireNotificationEvent((ushort)HWProblemReason.None, (ushort)HWWarningReason.None, (ushort)HWFaultReason.None, disableReason);

                        break;
                }
            }

            MeasurementLogicRoutine(commParameters);

            return m_State;
        }

        private DeviceState WaitForEndOfTest()
        {
            //ожидание окончания теста - сначала установка перейдет в состояние DS_InProcess (оно нам не интересно), далее установка перейдет в состояние DS_Ready - ждём именно это состояние
            var timeStamp = Environment.TickCount + m_Timeout;

            while (Environment.TickCount < timeStamp)
            {
                if (m_Stop)
                {
                    CallAction(ACT_STOP);

                    return DeviceState.Stopped;
                }

                ushort devState = ReadRegister(REG_DEV_STATE);
                ushort finished = ReadRegister(REG_FINISHED, true);

                //блок RAC перешёл в состояние DS_Fault
                if (devState == (ushort)HWDeviceState.DS_Fault)
                {
                    ushort faultReason = ReadRegister(REG_FAULT_REASON);
                    FireNotificationEvent((ushort)HWProblemReason.None, (ushort)HWWarningReason.None, faultReason, (ushort)HWDisableReason.None);

                    return DeviceState.Fault;
                }

                //блок RAC перешёл в состояние DS_Disabled
                if (devState == (ushort)HWDeviceState.DS_Disabled)
                {
                    ushort disableReason = ReadRegister(REG_DISABLE_REASON);
                    FireNotificationEvent((ushort)HWProblemReason.None, (ushort)HWWarningReason.None, (ushort)HWFaultReason.None, disableReason);

                    return DeviceState.Disabled;
                }

                switch (finished)
                {
                    case (ushort)HWOperationResult.None:
                        //измерение не завершено - продолжаем ждать
                        break;

                    case (ushort)HWOperationResult.Fail:
                        //измерение завершилось не успешно
                        return DeviceState.Fault;

                    case (ushort)HWOperationResult.OK:
                        //измерение успешно завершено
                        ushort problem = ReadRegister(REG_PROBLEM);
                        ushort warning = ReadRegister(REG_WARNING);

                        //проверим наличие problem от блока RAC
                        if (problem != (ushort)HWProblemReason.None)
                            FireNotificationEvent(problem, (ushort)HWWarningReason.None, (ushort)HWFaultReason.None, (ushort)HWDisableReason.None);

                        //проверим наличие warnig от блока QrrTq
                        if (warning != (ushort)HWWarningReason.None)
                            FireNotificationEvent((ushort)HWProblemReason.None, warning, (ushort)HWFaultReason.None, (ushort)HWDisableReason.None);

                        return DeviceState.Success;

                    default:
                        throw new Exception(string.Format("Unrecognized finished result={0}. Device 'RAC'. ", finished));
                }

                Thread.Sleep(REQUEST_DELAY_MS);
            }

            //мы вывалились из цикла ожидания окончания процесса измерения по таймауту
            //цикл ожидания момента окончания измерений закончился позже отведённого нами времени - уведомляем UI и возбуждаем исключение
            FireExceptionEvent("File (IORAC.cs). Method WaitForEndOfTest. RAC timeout while waiting end of test");

            throw new Exception("Timeout while waiting for RAC test to end");
        }

        private void MeasurementLogicRoutine(Types.Commutation.TestParameters Commutation)
        {
            try
            {
                m_State = DeviceState.InProcess;

                //сбрасываем возможные предупреждения
                CallAction(ACT_CLR_WARNING);

                //уведомляем UI о том, что мы находимся в состоянии m_State с результатами измерений m_Result
                FireRACEvent(m_State, m_Result);

                //пишем тип измерения - сопротивление анод-катод/изоляции, будет всегда только такой тип измерения
                WriteRegister(REG_MEASUREMENT_TYPE, 6);

                //пишем амплитуду напряжения для измерения (в В)
                WriteRegister(REG_RES_VOLTAGE, m_Parameters.ResVoltage);

                if (m_IsEmulation)
                {
                    //эмулируем успешный результат измерений
                    m_State = DeviceState.Success;

                    //эмулируем измеренное значение сопротивления изоляции, в регистре REG_RESULT_R оно будет хранится умноженным на 10
                    m_Result.ResultR = 123 / 10f;

                    //проверяем отображение Fault, Warning, Problem
                    FireNotificationEvent((ushort)HWProblemReason.None, 2, (ushort)HWFaultReason.None, (ushort)HWDisableReason.None);
                    FireNotificationEvent((ushort)HWProblemReason.None, (ushort)HWWarningReason.None, 1, (ushort)HWDisableReason.None);
                    FireNotificationEvent(7, (ushort)HWWarningReason.None, (ushort)HWFaultReason.None, (ushort)HWDisableReason.None);
                }
                else
                {
                    //перед каждым измерением включаем специальную коммутацию для подключения измерительной части блока RAC к испытуемому прибору
                    if (m_IOCommutation.Switch(Types.Commutation.CommutationMode.RAC, Commutation.CommutationType, Commutation.Position) == DeviceState.Fault)
                    {
                        m_State = DeviceState.Fault;

                        //раз коммутация не удалась - выставляем значения измеренных параметров в ноль
                        m_Result.ResultR = 0;

                        FireRACEvent(m_State, m_Result);

                        return;
                    }
                    else
                    {
                        //коммутация успешно включена, запускаем процесс измерения
                        CallAction(ACT_START_TEST);

                        //ждём окончания процесса измерения
                        m_State = WaitForEndOfTest();

                        //если тест успешно завершён - считываем результаты измерения из блока RAC
                        if (m_State == DeviceState.Success)
                        {
                            //в регистре он хранится умноженным на 10, поэтому делим его на 10
                            m_Result.ResultR = ReadRegister(REG_RESULT_R) / 10f;
                        }

                        //тест завершён, выключаем коммутацию
                        if (m_IOCommutation.Switch(Types.Commutation.CommutationMode.None) == DeviceState.Fault)
                        {
                            m_State = DeviceState.Fault;
                            
                            //коммутация не удалась, оставляем содержимое m_Result без изменения
                            FireRACEvent(m_State, m_Result);

                            return;
                        }
                    }
                }

                FireRACEvent(m_State, m_Result);
            }

            catch (Exception e)
            {
                m_IOCommutation.Switch(Types.Commutation.CommutationMode.None);
                m_State = DeviceState.Fault;
                FireRACEvent(m_State, m_Result);
                FireExceptionEvent(e.Message);

                throw;
            }
        }

        #region Registers
        //описание регистров блока RAC
        private const ushort

        //Measurement type - тип измерения (режиму измерителя сопротивления анод-катод/изоляции соответствует 6)
        REG_MEASUREMENT_TYPE = 128,

        //DC voltage value for resistance measurement (in V) - амплитуда напряжения для измерения (в В)
        REG_RES_VOLTAGE = 144,

        //Регистры состояния:
        //Device state - текущее состояние
        REG_DEV_STATE = 192,

        //Fault reason in the case DeviceState -> FAULT - код причины состояния fault (если в состоянии fault)
        REG_FAULT_REASON = 193,

        //Fault reason in the case DeviceState -> DISABLED - код причины состояния disabled (если в состоянии disabled)
        REG_DISABLE_REASON = 194,

        //Warning if present - код предупреждения - код предупреждения
        REG_WARNING = 195,

        //Problem reason - код проблемы
        REG_PROBLEM = 196,

        //Indicates that test is done and there is result or fault - код окончания измерений
        REG_FINISHED = 197,

        //Регистры результата:
        //Resistance result (in MOhm * 10) - сопротивление изоляции (в МОм * 10)
        REG_RESULT_R = 200;
        #endregion

        #region Standart API
        internal void ClearFault()
        {
            //очистка ошибки блока RAC
            SystemHost.Journal.AppendLog(ComplexParts.RAC, LogMessageType.Note, "RAC try to clear fault");
            CallAction(ACT_CLR_FAULT);
        }

        private void ClearWarning()
        {
            //очистка предупреждения блока RAC
            SystemHost.Journal.AppendLog(ComplexParts.RAC, LogMessageType.Note, "RAC try to clear warning");
            CallAction(ACT_CLR_WARNING);
        }

        internal ushort ReadRegister(ushort Address, bool SkipJournal = false)
        //чтение ushort значения регистра с номером Address блока RAC
        {
            ushort value = 0;

            if (!m_IsEmulation)
                value = m_IOAdapter.Read16(m_Node, Address);

            if (!SkipJournal)
                SystemHost.Journal.AppendLog(ComplexParts.RAC, LogMessageType.Note, string.Format("RAC @ReadRegister, address {0}, value {1}", Address, value));

            return value;
        }

        internal short ReadRegisterS(ushort Address, bool SkipJournal = false)
        //чтение short значения регистра с номером Address блока RAC
        {
            short value = 0;

            if (!m_IsEmulation) value = m_IOAdapter.Read16S(m_Node, Address);

            if (!SkipJournal) SystemHost.Journal.AppendLog(ComplexParts.RAC, LogMessageType.Note, string.Format("RAC @ReadRegisterS, address {0}, value {1}", Address, value));

            return value;
        }

        internal void WriteRegister(ushort Address, ushort Value, bool SkipJournal = false)
        //запись в регистр с номером Address ushort значения Value
        {
            if (!SkipJournal) SystemHost.Journal.AppendLog(ComplexParts.RAC, LogMessageType.Note, string.Format("RAC @WriteRegister, address {0}, value {1}", Address, Value));

            if (m_IsEmulation) return;

            m_IOAdapter.Write16(m_Node, Address, Value);
        }

        internal void CallAction(ushort Action)
        {
            SystemHost.Journal.AppendLog(ComplexParts.RAC, LogMessageType.Note, string.Format("RAC @Call, action {0}", Action));

            if (m_IsEmulation) return;

            m_IOAdapter.Call(m_Node, Action);
        }
        #endregion

        #region Actions
        //описание команд блока RAC
        private const ushort
        //Enable
        ACT_ENABLE_POWER = 1,

        //Disable    	
        ACT_DISABLE_POWER = 2,

        //Clear fault    	
        ACT_CLR_FAULT = 3,

        //Clear warning
        ACT_CLR_WARNING = 4,

        //Start test with defined parameters – запуск процесса измерения
        ACT_START_TEST = 100,

        //Stop test sequence – принудительная остановка процесса измерения
        ACT_STOP = 101;
        #endregion

        #region Events
        private void FireConnectionEvent(DeviceConnectionState State, string Message)
        {
            SystemHost.Journal.AppendLog(ComplexParts.RAC, LogMessageType.Info, Message);
            m_Communication.PostDeviceConnectionEvent(ComplexParts.RAC, State, Message);
        }

        private void FireNotificationEvent(ushort Problem, ushort Warning, ushort Fault, ushort Disable)
        {
            SystemHost.Journal.AppendLog(ComplexParts.RAC, LogMessageType.Warning, string.Format("RAC device notification: problem {0}, warning {1}, fault {2}, disable {3}", Problem, Warning, Fault, Disable));
            m_Communication.PostRACNotificationEvent(Problem, Warning, Fault, Disable);
        }

        private void FireRACEvent(DeviceState State, TestResults Result)
        {
            string message = string.Format("RAC test state {0}", State);

            SystemHost.Journal.AppendLog(ComplexParts.RAC, LogMessageType.Info, message);
            m_Communication.PostRACEvent(State, Result);
        }

        private void FireExceptionEvent(string Message)
        {
            SystemHost.Journal.AppendLog(ComplexParts.RAC, LogMessageType.Error, Message);
            m_Communication.PostExceptionEvent(ComplexParts.RAC, Message);
        }
        #endregion
    }
}
