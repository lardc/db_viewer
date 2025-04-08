using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SCME.Types;
using SCME.Types.QrrTq;
using SCME.Service.Properties;

namespace SCME.Service.IO
{
    internal class IOQrrTq
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
        private Types.QrrTq.TestParameters m_Parameters;
        private volatile DeviceState m_State;
        private volatile Types.QrrTq.TestResults m_Result;
        private readonly bool m_NeedReadGraf;
        private int m_Timeout = 80000;

        internal IOQrrTq(IOAdapter Adapter, BroadcastCommunication Communication)
        {
            m_IOAdapter = Adapter;
            m_Communication = Communication;
            m_Node = (ushort)Settings.Default.QrrTqNode;
            m_IsEmulationHard = Settings.Default.QrrTqEmulation;
            m_NeedReadGraf = Settings.Default.QrrTqReadGraf;
        }

        internal IOCommutation ActiveCommutation
        {
            get { return m_IOCommutation; }
            set { m_IOCommutation = value; }
        }

        private ushort ReadDeviceState(HWDeviceState? WaitedState, int Timeout)
        //реализация чтения состояния конечного автомата.
        //в WaitedState принимается состояние, в которое должен перейти конечный автомат QrrTq
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
                //пока не истёк таймаут - будем перечитывать состояние блока QrrTq через каждые 100 мс до тех пор, пока не окажемся в ожидаемом состоянии WaitedState
                var timeStamp = Environment.TickCount + Timeout;

                while (Environment.TickCount < timeStamp)
                {
                    if (m_Stop)
                    {
                        //QrrTq умеет команду Stop
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
            FireConnectionEvent(m_ConnectionState, "QrrTq initializing");

            if (m_IsEmulation)
            {
                m_ConnectionState = DeviceConnectionState.ConnectionSuccess;
                FireConnectionEvent(m_ConnectionState, "QrrTq initialized");

                return m_ConnectionState;
            }

            //блок QrrTq находится в неизвестном нам состоянии поэтому
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

                                //из данного состояния QrrTq должен перейти в состояние DS_PowerOn
                                WaitedState = HWDeviceState.DS_PowerOn;
                                break;

                            case (ushort)HWDeviceState.DS_PowerOn:
                                //из данного состояния QrrTq должен перейти в состояние DS_Ready
                                WaitedState = HWDeviceState.DS_Ready;
                                break;

                            case (ushort)HWDeviceState.DS_Ready:
                                //блок QrrTq перешёл в состояние DS_Ready - завершаем процесс инициализации
                                m_ConnectionState = DeviceConnectionState.ConnectionSuccess;
                                End = true;
                                FireConnectionEvent(m_ConnectionState, "QrrTq successfully initialized.");
                                break;
                        }
                    }
                    else
                    {
                        //состояние блока QrrTq отличается от ожидаемого: ожидалось состояние WaitedState, а по факту оказалось состояние State
                        switch (State)
                        {
                            case (ushort)HWDeviceState.DS_Fault:
                                //сбрасываем состояние DS_Fault
                                CallAction(ACT_CLR_FAULT);

                                //после сброса состояния DS_Fault оно всегда будет DS_None
                                WaitedState = HWDeviceState.DS_None;
                                break;

                            case (ushort)HWDeviceState.DS_Ready:
                                //сознательно не сбрасываем warning ибо это всегда делается в начале MeasurementLogicRoutine
                                m_ConnectionState = DeviceConnectionState.ConnectionSuccess;
                                End = true;
                                FireConnectionEvent(m_ConnectionState, "QrrTq successfully initialized.");
                                break;

                            case (ushort)HWDeviceState.DS_InProcess:
                                CallAction(ACT_STOP);
                                CallAction(ACT_DISABLE_POWER);
                                WaitedState = HWDeviceState.DS_None;
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
                FireConnectionEvent(m_ConnectionState, String.Format("QrrTq initialization error: {0}.", e.Message));
            }

            return m_ConnectionState;
        }

        internal void Deinitialize()
        {
            var oldState = m_ConnectionState;

            m_ConnectionState = DeviceConnectionState.DisconnectionInProcess;
            FireConnectionEvent(DeviceConnectionState.DisconnectionInProcess, "QrrTq disconnecting");

            try
            {
                if (!m_IsEmulation && oldState == DeviceConnectionState.ConnectionSuccess)
                    CallAction(ACT_DISABLE_POWER);

                m_ConnectionState = DeviceConnectionState.DisconnectionSuccess;
                FireConnectionEvent(DeviceConnectionState.DisconnectionSuccess, "QrrTq disconnected");
            }
            catch (Exception)
            {
                m_ConnectionState = DeviceConnectionState.DisconnectionError;
                FireConnectionEvent(DeviceConnectionState.DisconnectionError, "QrrTq disconnection error");
            }
        }

        internal void Stop()
        {
            CallAction(ACT_STOP);

            m_Stop = true;
        }

        internal bool IsReadyToStart()
        {
            var devState = (Types.QrrTq.HWDeviceState)ReadRegister(REG_DEV_STATE);

            return !((devState == Types.QrrTq.HWDeviceState.DS_Fault) || (devState == Types.QrrTq.HWDeviceState.DS_Disabled) || (m_State == DeviceState.InProcess));
        }

        internal DeviceState Start(TestParameters Parameters, Types.Commutation.TestParameters commParameters)
        {
            m_Parameters = Parameters;

            if (m_State == DeviceState.InProcess) throw new Exception("QrrTq test is already started.");

            //запоминаем в каком режиме будут выполняться измерения
            m_Result = new TestResults { Mode = m_Parameters.Mode, OffStateVoltage = m_Parameters.OffStateVoltage, OsvRate = (ushort)m_Parameters.OsvRate };
            m_Result.TestTypeId = m_Parameters.TestTypeId;

            m_Stop = false;

            if (!m_IsEmulation)
            {
                ushort State = ReadRegister(REG_DEV_STATE);

                switch (State)
                {
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

                //блок QrrTq перешёл в состояние DS_Fault
                if (devState == (ushort)HWDeviceState.DS_Fault)
                {
                    ushort faultReason = ReadRegister(REG_FAULT_REASON);

                    FireNotificationEvent((ushort)HWProblemReason.None, (ushort)HWWarningReason.None, faultReason, (ushort)HWDisableReason.None);

                    return DeviceState.Fault;
                }

                //блок QrrTq перешёл в состояние DS_Disabled
                if (devState == (ushort)HWDeviceState.DS_Disabled)
                {
                    ushort disableReason = ReadRegister(REG_DISABLE_REASON);

                    FireNotificationEvent((ushort)HWProblemReason.None, (ushort)HWWarningReason.None, (ushort)HWFaultReason.None, disableReason);

                    return DeviceState.Disabled;
                }

                //блок QrrTq штатно завершил процесс измерения
                if (devState == (ushort)HWDeviceState.DS_Ready)
                {
                    ushort problem = ReadRegister(REG_PROBLEM);
                    ushort warning = ReadRegister(REG_WARNING);

                    //проверим наличие problem от блока QrrTq
                    if (problem != (ushort)HWProblemReason.None)
                        FireNotificationEvent(problem, (ushort)HWWarningReason.None, (ushort)HWFaultReason.None, (ushort)HWDisableReason.None);

                    //проверим наличие warnig от блока QrrTq
                    if (warning != (ushort)HWWarningReason.None)
                        FireNotificationEvent((ushort)HWProblemReason.None, warning, (ushort)HWFaultReason.None, (ushort)HWDisableReason.None);

                    break;
                }

                //читаем число воздействий на испытуемый прибор и сообщаем UI его значение
                FireKindOfFreezingEvent(ReadRegister(REG_KINDOFFREEZING));

                Thread.Sleep(REQUEST_DELAY_MS);
            }

            //мы вывалились из цикла ожидания окончания процесса измерения. убедимся, что мы уложились в отведённое время
            if (Environment.TickCount > timeStamp)
            {
                //цикл ожидания момента окончания измерений закончился позже отведённого нами времени - уведомляем UI и возбуждаем исключение
                FireExceptionEvent("File (IOQrrTq.cs). Method WaitForEndOfTest. QrrTq timeout while waiting end of test");
                throw new Exception("Timeout while waiting for QrrTq test to end");
            }

            //раз мы сюда добрались - значит QrrTq находится в адекватном состоянии и таймаут не наступил
            return DeviceState.Success;
        }

        private void MeasurementLogicRoutine(Types.Commutation.TestParameters Commutation)
        {
            try
            {
                m_State = DeviceState.InProcess;

                //сбрасываем возможные предупреждения
                CallAction(ACT_CLR_WARNING);

                //уведомляем UI о том, что мы находимся в состоянии m_State с результатами измерений m_Result
                FireQrrTqEvent(m_State, m_Result);

                //пишем тип измерения
                WriteRegister(REG_MODE, (ushort)m_Parameters.Mode);

                //пишем амплитуду прямого тока (в А)
                WriteRegister(REG_DIRECT_CURRENT, m_Parameters.DirectCurrent);

                //пишем длительность импульса прямого тока (в мкс)
                WriteRegister(REG_DC_PULSE_WIDTH, m_Parameters.DCPulseWidth);

                //пишем скорость нарастания прямого тока (в А/мкс)
                WriteRegister(REG_DC_RISE_RATE, (ushort)(m_Parameters.DCRiseRate * 10));

                //пишем скорость спада тока (в А/мкс)
                ushort DCFallRate = (ushort)m_Parameters.DCFallRate;
                WriteRegister(REG_DC_FALL_RATE, (ushort)(DCFallRate * 10));

                //пишем прямое повторное напряжение (в В)
                WriteRegister(REG_OFF_STATE_VOLTAGE, m_Parameters.OffStateVoltage);

                //пишем скорость нарастания прямого повторного напряжения (в В/мкс)
                if (m_Parameters.Mode == TMode.QrrTq)
                {
                    ushort OsvRate = (ushort)m_Parameters.OsvRate;
                    WriteRegister(REG_OSV_RATE, OsvRate);
                }

                //пишем метод измерения Trr "Измерение trr по методу 90%-50%"
                int TrrMeasureBy9050Method = m_Parameters.TrrMeasureBy9050Method == false ? 0 : 1;
                WriteRegister(REG_TRR_MEASURE_BY_9050_METHOD, (ushort)TrrMeasureBy9050Method);

                if (m_IsEmulation)
                {
                    //эмулируем успешный результат измерений
                    m_State = DeviceState.Success;

                    Random rnd = new Random();
                    FireKindOfFreezingEvent((ushort)rnd.Next(1, 1000));

                    m_Result.Idc = 1;
                    m_Result.Qrr = 2 / 10f;
                    m_Result.Irr = 3;
                    m_Result.Trr = 4 / 10f;
                    m_Result.DCFactFallRate = 6 / 10f;
                    m_Result.Tq = 5 / 10f;

                    if (m_NeedReadGraf)
                    {
                        for (int i = 1; i <= 2000; i++)
                        {
                            //эмуляция графика тока
                            m_Result.CurrentData.Add(1500);

                            //эмуляция графика напряжения
                            m_Result.VoltageData.Add(600);
                        }
                    }

                    //проверяем отображение Problem, Warning, Fault
                    FireNotificationEvent(7, (ushort)HWWarningReason.None, (ushort)HWFaultReason.None, (ushort)HWDisableReason.None);
                    FireNotificationEvent((ushort)HWProblemReason.None, 2, (ushort)HWFaultReason.None, (ushort)HWDisableReason.None);
                    FireNotificationEvent((ushort)HWProblemReason.None, (ushort)HWWarningReason.None, 1, (ushort)HWDisableReason.None);
                }
                else
                {
                    //запускаем процесс измерения
                    CallAction(ACT_START_TEST);

                    //ждём окончания процесса измерения
                    m_State = WaitForEndOfTest();

                    //если тест успешно завершён - считываем результаты измерения из блока QrrTq
                    if (m_State == DeviceState.Success)
                    {
                        //если значение регистра REG_FINISHED содержит значение 1 (OPRESULT_OK), то можно считать оцифрованные значения тока из EP_SlaveData и результаты измерений из соответствующих регистров
                        ushort finishedResult = ReadRegister(REG_FINISHED);

                        if ((HWFinishedResult)finishedResult == HWFinishedResult.Ok)
                        {
                            m_Result.Idc = ReadRegisterS(REG_RES_IDC);

                            //значения Qrr, Irr, Trr надо читать только в режиме измерения 'Qrr'
                            if (m_Parameters.Mode == TMode.Qrr)
                            {
                                //в регистре хранится умноженное на 10 значение 
                                m_Result.Qrr = ReadRegisterS(REG_RES_QRR) / 10f;

                                m_Result.Irr = ReadRegisterS(REG_RES_IRR);

                                //в регистре хранится умноженное на 10 значение
                                m_Result.Trr = ReadRegisterS(REG_RES_TRR) / 10f;
                            }

                            //в регистре хранится умноженное на 10 значение
                            m_Result.DCFactFallRate = ReadRegisterS(REG_RES_DC_FACT_FALL_RATE) / 10f;

                            //читаем из регистра измеренное значение Tq только в режиме измерения Tq
                            m_Result.Tq = m_Parameters.Mode == TMode.QrrTq ? ReadRegisterS(REG_RES_TQ) / 10f : 0;

                            //если нужно читать данные для построения графиков - читаем их
                            if (m_NeedReadGraf)
                                ReadArrays(m_Result);
                        }
                    }
                }

                FireQrrTqEvent(m_State, m_Result);
            }

            catch (Exception e)
            {
                m_State = DeviceState.Fault;
                FireQrrTqEvent(m_State, m_Result);
                FireExceptionEvent(e.Message);

                throw;
            }
        }

        #region Standart API
        internal void ClearFault()
        {
            //очистка ошибки блока QrrTq
            SystemHost.Journal.AppendLog(ComplexParts.QrrTq, LogMessageType.Note, "QrrTq try to clear fault");
            CallAction(ACT_CLR_FAULT);
        }

        private void ClearWarning()
        {
            //очистка предупреждения блока QrrTq
            SystemHost.Journal.AppendLog(ComplexParts.QrrTq, LogMessageType.Note, "QrrTq try to clear warning");
            CallAction(ACT_CLR_WARNING);
        }

        internal ushort ReadRegister(ushort Address, bool SkipJournal = false)
        //чтение ushort значения регистра с номером Address блока QrrTq
        {
            ushort value = 0;

            if (!m_IsEmulation)
                value = m_IOAdapter.Read16(m_Node, Address);

            if (!SkipJournal)
                SystemHost.Journal.AppendLog(ComplexParts.QrrTq, LogMessageType.Note, string.Format("QrrTq @ReadRegister, address {0}, value {1}", Address, value));

            return value;
        }

        internal short ReadRegisterS(ushort Address, bool SkipJournal = false)
        //чтение short значения регистра с номером Address блока QrrTq
        {
            short value = 0;

            if (!m_IsEmulation) value = m_IOAdapter.Read16S(m_Node, Address);

            if (!SkipJournal) SystemHost.Journal.AppendLog(ComplexParts.QrrTq, LogMessageType.Note, string.Format("QrrTq @ReadRegisterS, address {0}, value {1}", Address, value));

            return value;
        }

        private void ReadArrays(TestResults Result)
        {
            if (Result != null)
            {
                //чтение массивов даннных тока и напряжения
                SystemHost.Journal.AppendLog(ComplexParts.QrrTq, LogMessageType.Note, "QrrTq @ReadArrays begin");

                //читаем массив данных для построения графика тока и напряжения
                Result.CurrentData.Clear();
                Result.VoltageData.Clear();

                if (!m_IsEmulation)
                {
                    Result.CurrentData = m_IOAdapter.ReadArrayFast16S(m_Node, EP_Current).ToList();
                    Result.VoltageData = m_IOAdapter.ReadArrayFast16S(m_Node, EP_Voltage).ToList();
                }

                SystemHost.Journal.AppendLog(ComplexParts.QrrTq, LogMessageType.Note, "QrrTq @ReadArray end");
            }
        }

        internal void WriteRegister(ushort Address, ushort Value, bool SkipJournal = false)
        //запись в регистр с номером Address ushort значения Value
        {
            if (!SkipJournal) SystemHost.Journal.AppendLog(ComplexParts.QrrTq, LogMessageType.Note, string.Format("QrrTq @WriteRegister, address {0}, value {1}", Address, Value));

            if (m_IsEmulation) return;

            m_IOAdapter.Write16(m_Node, Address, Value);
        }

        internal void CallAction(ushort Action)
        {
            SystemHost.Journal.AppendLog(ComplexParts.QrrTq, LogMessageType.Note, string.Format("QrrTq @Call, action {0}", Action));

            if (m_IsEmulation) return;

            m_IOAdapter.Call(m_Node, Action);
        }
        #endregion

        #region Registers
        //описание регистров блока QrrTq
        private const ushort
        //регистры, задающие режим работы:
        //Measurement mode - режим измерения
        REG_MODE = 128,

        //Direct current amplitude (in A) - амплитуда прямого тока(в А)
        REG_DIRECT_CURRENT = 129,

        //Direct current pulse duration (in us) - длительность импульса прямого тока(в мкс)
        REG_DC_PULSE_WIDTH = 130,

        //Direct current rise rate (in A/us) - скорость нарастания прямого тока(в А/мкс)
        REG_DC_RISE_RATE = 131,

        //Direct current fall rate (in A/us) - скорость спада тока(в А/мкс)
        REG_DC_FALL_RATE = 132,

        //Off-state voltage (in V) - прямое повторное напряжение(в В)
        REG_OFF_STATE_VOLTAGE = 133,

        //Off-state voltage rise rate (in V/us)	- скорость нарастания прямого повторного напряжения(в В/мкс)
        REG_OSV_RATE = 134,

        //Измерение trr по методу 90%-50%
        REG_TRR_MEASURE_BY_9050_METHOD = 136,

        //регистры измеренных значений:
        //Actual DC current value (in A) - фактическое значение прямого тока(в А)
        REG_RES_IDC = 214,

        //Reverse recovery charge (in uC) - заряд обратного восстановления(в мкКл)
        REG_RES_QRR = 210,

        //Reverse recovery current amplitude (in A) - ток обратного восстановления(в А)
        REG_RES_IRR = 211,

        //Reverse recovery time (in us)	- время обратного восстановления(в мкс)
        REG_RES_TRR = 212,

        //Turn-off time (in us) – время выключения(в мкс)
        REG_RES_TQ = 213,

        //Фактическая скорость спада тока, А/мкс
        REG_RES_DC_FACT_FALL_RATE = 215,

        //Регистры состояния:
        //Device state - текущее состояние
        REG_DEV_STATE = 192,

        //Fault reason in the case DeviceState -> FAULT – код причины состояния fault(если в состоянии fault)
        REG_FAULT_REASON = 193,

        //Disbale reason in the case DeviceState -> DISABLE – код причины состояния disabled(если в состоянии disabled)
        REG_DISABLE_REASON = 194,

        //Warning if present – код предупреждения
        REG_WARNING = 195,

        // Problem if present - код проблемы    	
        REG_PROBLEM = 196,

        //Indicates that test is done and there is result or fault – код окончания измерений    		
        REG_FINISHED = 198,

        //содержит число воздействий на испытуемый прибор
        REG_KINDOFFREEZING = 199,

        //Data obtained from slave device - график тока обратного восстановления (в А, дискретизация 8нс)
        EP_Current = 1,

        //Voltage data
        EP_Voltage = 2;
        #endregion

        #region Actions
        //описание команд блока QrrTq
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
            SystemHost.Journal.AppendLog(ComplexParts.QrrTq, LogMessageType.Info, Message);
            m_Communication.PostDeviceConnectionEvent(ComplexParts.QrrTq, State, Message);
        }

        private void FireNotificationEvent(ushort Problem, ushort Warning, ushort Fault, ushort Disable)
        {
            SystemHost.Journal.AppendLog(ComplexParts.QrrTq, LogMessageType.Warning, string.Format("QrrTq device notification: problem {0}, warning {1}, fault {2}, disable {3}", Problem, Warning, Fault, Disable));
            m_Communication.PostQrrTqNotificationEvent(Problem, Warning, Fault, Disable);
        }

        private void FireQrrTqEvent(DeviceState State, TestResults Result)
        {
            string message = string.Format("QrrTq test state {0}", State);

            SystemHost.Journal.AppendLog(ComplexParts.QrrTq, LogMessageType.Info, message);
            m_Communication.PostQrrTqEvent(State, Result);
        }

        private void FireExceptionEvent(string Message)
        {
            SystemHost.Journal.AppendLog(ComplexParts.QrrTq, LogMessageType.Error, Message);
            m_Communication.PostExceptionEvent(ComplexParts.QrrTq, Message);
        }

        private void FireKindOfFreezingEvent(ushort KindOfFreezing)
        {
            string message = string.Format("QrrTq KindOfFreezing {0}", KindOfFreezing);
            SystemHost.Journal.AppendLog(ComplexParts.QrrTq, LogMessageType.Note, message);
            m_Communication.PostQrrTqKindOfFreezingEvent(KindOfFreezing);
        }
        #endregion
    }
}
