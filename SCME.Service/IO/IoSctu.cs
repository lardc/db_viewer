using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.ComponentModel;
using SCME.Service.Properties;
using SCME.Types;
using SCME.Types.SCTU;

namespace SCME.Service.IO
{
    internal class IoSctu
    {
        #region Fields
        private readonly IOAdapter _ioAdapter;
        private readonly BroadcastCommunication _broadcastCommunication;
        private readonly bool _isSctuEmulationHard;
        private readonly bool _isNeedSctuInitialization;
        private readonly bool _ReadGraph;
        private bool _isSctuEmulation;
        private ushort _WorkplaceActivationStatusRegisterValueEmulation;
        private bool _UseAnotherControlUnitBoard;
        private ushort _ThisControlUnitBoardNode;
        private ushort _AnotherControlUnitBoardNode;

        private ushort _node;
        private SctuTestParameters _testParameters;
        private DeviceConnectionState _deviceConnectionState;

        private volatile SctuTestResults _testResults;
        private volatile SctuHwState _deviseState;

        //private volatile bool _stop;

        private int _timeout;

        private const int ChargeTimeout = 120000;
        #endregion

        #region Commands
        /// <summary>
        /// перевод блока в начальное состояние (состояние после включения)
        /// </summary>
        private const ushort ACT_DS_NONE = 1;

        /// <summary>
        /// включить заряд конденсаторов
        /// </summary>
        private const ushort ACT_BAT_START_CHARGE = 2;

        /// <summary>
        /// очистить Fault
        /// </summary>
        private const ushort ACT_FAULT_CLEAR = 3;

        /// <summary>
        /// очистить Warning
        /// </summary>
        private const ushort ACT_WARNING_CLEAR = 4;

        /// <summary>
        /// сконфигурировать блок на заданное значение ударного тока
        /// </summary>
        private const ushort ACT_SC_PULSE_CONFIG = 100;

        /// <summary>
        /// запустить формирование ударного тока
        /// </summary>
        private const ushort ACT_SC_PULSE_START = 101;

        /// <summary>
        /// читать часть массива данных
        /// </summary>
        private const ushort ACT_READ_FRAGMENT = 110;

        /// <summary>
        /// задать направление Back чтения массива данных
        /// </summary>
        private const ushort ACT_READ_MOVE_BACK = 111;
        #endregion

        #region Регистры, задающие режим работы
        /// <summary>
        /// Значение ударного тока в Амперах, младший байт. Диапазон задания в двух байтовом значении 100-120000 A
        /// </summary>
        private const ushort REG_SC_VALUE_L = 64;

        /// <summary>
        /// Значение ударного тока в Амперах, старший байт. Диапазон задания в двух байтовом значении 100-120000 A
        /// </summary>
        private const ushort REG_SC_VALUE_H = 65;

        /// <summary>
        /// Тип прибора, 1234 – диод, 5678 – тристор. По умолчанию – диод.
        /// </summary>
        private const ushort REG_DUT_TYPE = 66;

        /// <summary>
        /// Сопротивление шунта, мкОм
        /// </summary>
        private const ushort REG_R_SHUNT = 67;

        /// <summary>
        /// Тип формы импульса: 43690 – синусоидальная, 48059 – трапецеидальная. По умолчанию – синусоидальная
        /// </summary>
        private const ushort REG_WAVEFORM_TYPE = 70;

        /// <summary>
        /// Длительность фронта трапецеидального импульса, мкС. Диапазон 100-1000
        /// </summary>
        private const ushort REG_TRAPEZE_EDGE_TIME = 71;

        /// <summary>
        /// Канал измерения тока или напряжения (в зависимости от выбранного ЗУ). Значение 1 или 2
        /// </summary>
        private const ushort REG_CHANNELBYCLAMPTYPE = 72;

        /// <summary>
        /// Регистр активации рабочего места, расшифровка значений см. в SctuCommonRegisterValues.cs
        /// </summary>
        private const ushort REG_WORKPLACE_ACTIVATION_STATUS = 80;
        #endregion

        #region Регистры измеренных значений
        /// <summary>
        /// Измеренное значение прямого напряжения, мВ.
        /// </summary>
        private const ushort REG_DUT_U = 96;

        /// <summary>
        /// Измеренное значение ударного тока, младшие 16 бит, А.
        /// </summary>
        private const ushort REG_DUT_I_L = 97;

        /// <summary>
        /// Измеренное значение ударного тока, старшие 16 бит, А.
        /// </summary>
        private const ushort REG_DUT_I_H = 98;
        #endregion

        #region Регистры состояния
        /// <summary>
        /// Регистр состояния установки.
        /// </summary>
        private const ushort REG_DEV_STATE = 99;

        /// <summary>
        /// Код проблемы.
        /// </summary>
        private const ushort REG_FAULT_REASON = 100;

        /// <summary>
        /// Код предупреждения.
        /// </summary>
        private const ushort REG_WARNING = 101;

        /// <summary>
        /// Коэффициент усиления.
        /// </summary>
        private const ushort REG_INFO_K_SHUNT_AMP = 113;

        /// <summary>
        /// Адрес массива значений тока.
        /// </summary>
        private const ushort ARR_SCOPE_V = 1;

        /// <summary>
        /// Адрес массива значений напряжения.
        /// </summary>
        private const ushort ARR_SCOPE_I = 2;
        #endregion

        internal IoSctu(IOAdapter ioAdapter, BroadcastCommunication broadcastCommunication)
        {
            _ioAdapter = ioAdapter;
            _broadcastCommunication = broadcastCommunication;
            _isSctuEmulationHard = Settings.Default.SctuEmulation;
            _isNeedSctuInitialization = Settings.Default.NeedSCTUInitialization;
            _isSctuEmulation = _isSctuEmulationHard;
            _ReadGraph = Settings.Default.SCTUReadGraph;
            _node = (ushort)Settings.Default.SctuNode;
            _testResults = new SctuTestResults();
            _WorkplaceActivationStatusRegisterValueEmulation = 0;
            _UseAnotherControlUnitBoard = Settings.Default.UseAnotherControlUnitBoard;
            _ThisControlUnitBoardNode = ThisControlUnitBoardNode();
            _AnotherControlUnitBoardNode = AnotherControlUnitBoardNode();

            SystemHost.Journal.AppendLog(ComplexParts.Sctu, LogMessageType.Info, String.Format("Sctu created. Emulation mode: {0}", Settings.Default.SctuEmulation));
        }

        internal DeviceConnectionState Initialize(bool isEnable, int timeOut = 25000)
        {
            _timeout = timeOut;
            _isSctuEmulation = _isSctuEmulationHard || !isEnable;

            _deviceConnectionState = DeviceConnectionState.ConnectionInProcess;

            switch (_isNeedSctuInitialization)
            {
                case (false):
                    FireConnectionEvent(_deviceConnectionState, "Sctu skip initializing");
                    break;

                default:
                    FireConnectionEvent(_deviceConnectionState, "Sctu initializing");
                    break;
            }

            if (_isSctuEmulation)
            {
                _deviceConnectionState = DeviceConnectionState.ConnectionSuccess;
                _deviseState = SctuHwState.Ready;
                FireConnectionEvent(_deviceConnectionState, "Sctu initialized");

                return _deviceConnectionState;
            }

            try
            {
                if (!_isNeedSctuInitialization)
                {
                    //если включить SCTU - начнут загружаться сразу оба рабочих места и каждый из них будет пытаться инициализировать установку ударного тока, а инициализировать её можно только один раз, т.е. только одно рабочее место может выполнять инициализацию. именно для решения этого вопроса создан параметр 'NeedSctuInitialization' в конфигурационном файле
                    //чтобы гарантированно дождаться уже инициализированное SCTU - спим в течении времени ChargeTimeout + 5 секунд для страховки
                    Thread.Sleep(ChargeTimeout + 5000);

                    var deviceState = (SctuHwState)ReadRegister(REG_DEV_STATE);

                    if (deviceState == SctuHwState.Ready)
                    {
                        FiredSctuEvent(deviceState, _testResults);

                        _deviceConnectionState = DeviceConnectionState.ConnectionSuccess;
                        FireConnectionEvent(_deviceConnectionState, "Sctu initialization skipped");
                    }

                    //инициализируем значение статуса активации - сразу после инициализации рабочее место свободно
                    WriteRegister16(_ThisControlUnitBoardNode, REG_WORKPLACE_ACTIVATION_STATUS, (ushort)SctuWorkPlaceActivationStatuses.WORKPLACE_IS_FREE);

                    return _deviceConnectionState;
                }

                var timeStamp = Environment.TickCount + ChargeTimeout;

                ClearWarning();

                var devState = (SctuHwState)ReadRegister(REG_DEV_STATE);
                if (devState != SctuHwState.Ready)
                {
                    if (devState == SctuHwState.Fault)
                    {
                        ClearFault();
                        Thread.Sleep(100);

                        devState = (SctuHwState)ReadRegister(REG_DEV_STATE);

                        if (devState == SctuHwState.Fault)
                            throw new Exception(string.Format("Sctu is in fault state, reason: {0}", ReadRegister(REG_FAULT_REASON)));
                    }

                    if (devState == SctuHwState.Disabled)
                        throw new Exception("Sctu is in disabled state");

                    if (devState != SctuHwState.BatteryChargeStart)
                    {
                        CallAction(ACT_BAT_START_CHARGE);
                    }
                }

                while (Environment.TickCount < timeStamp)
                {
                    Thread.Sleep(500); //100

                    devState = (SctuHwState)ReadRegister(REG_DEV_STATE);


                    if (devState == SctuHwState.Ready)
                    {
                        FiredSctuEvent(devState, _testResults);
                        break;
                    }

                    if (devState == SctuHwState.Fault)
                        throw new Exception(string.Format("Sctu is in fault state, reason: {0}", ReadRegister(REG_FAULT_REASON)));

                    if (devState == SctuHwState.Disabled)
                        throw new Exception("Sctu is in disabled state");
                }

                if (Environment.TickCount > timeStamp)
                {
                    throw new Exception("Timeout while waiting for device to power up");
                }

                //инициализируем значение статуса активации - сразу после инициализации рабочее место свободно
                WriteRegister16(_ThisControlUnitBoardNode, REG_WORKPLACE_ACTIVATION_STATUS, (ushort)SctuWorkPlaceActivationStatuses.WORKPLACE_IS_FREE);

                _deviceConnectionState = DeviceConnectionState.ConnectionSuccess;
                FireConnectionEvent(_deviceConnectionState, "Sctu initialized");

            }
            catch (Exception ex)
            {
                _deviceConnectionState = DeviceConnectionState.ConnectionFailed;
                FireConnectionEvent(_deviceConnectionState, string.Format("Sctu initialization error: {0}", ex.Message));
            }

            return _deviceConnectionState;
        }

        internal void Deinitialize()
        {
            var oldState = _deviceConnectionState;

            _deviceConnectionState = DeviceConnectionState.DisconnectionInProcess;
            FireConnectionEvent(DeviceConnectionState.DisconnectionInProcess, "Sctu disconnecting");

            try
            {
                if (!_isSctuEmulation && oldState == DeviceConnectionState.ConnectionSuccess)
                {
                    Stop();
                    //CallAction(ACT_DISABLE_POWER);
                }

                _deviceConnectionState = DeviceConnectionState.DisconnectionSuccess;
                FireConnectionEvent(DeviceConnectionState.DisconnectionSuccess, "Sctu disconnected");
            }
            catch (Exception)
            {
                _deviceConnectionState = DeviceConnectionState.DisconnectionError;
                FireConnectionEvent(DeviceConnectionState.DisconnectionError, "Sctu disconnection error");
            }
        }

        private ushort ThisControlUnitBoardNode()
        {
            //считывает из файла конфигурации номер ноды ControlUnitBoard на шине CAN своего рабочего места 
            return Settings.Default.ThisControlUnitBoardNode;
        }

        private ushort AnotherControlUnitBoardNode()
        {
            //считывает из файла конфигурации номер ноды ControlUnitBoard на шине CAN другого рабочего места
            return Settings.Default.AnotherControlUnitBoardNode;
        }

        internal void Start(SctuTestParameters parameters, IOClamping clamping, IOGateway gateway)
        {
            _testParameters = parameters;
            _testResults = new SctuTestResults();

            //_stop = false;

            ClearWarning();

            if (!_isSctuEmulation)
            {
                var devState = (SctuHwState)ReadRegister(REG_DEV_STATE);

                if (devState == SctuHwState.Fault)
                    throw new Exception(string.Format("Sctu is in fault state, reason: {0}", ReadRegister(REG_FAULT_REASON)));

                if (devState == SctuHwState.Disabled)
                    throw new Exception("Sctu is in disabled state");
            }

            MeasurementLogicRoutine(clamping, gateway);
        }

        internal void SCTUWaitReady()
        {
            //после импульса тока SCTU довольно долго заряжает свои батареи конденсаторов
            //чтобы иметь возможность опустить пресс сразу после импульса тока - ожидание перехода SCTU в состояние готовности вынесено из реализации измерения в данную реализацию
            if (_isSctuEmulation)
            {
                _deviseState = SctuHwState.Ready;
                FiredSctuEvent(_deviseState, _testResults);

                return;
            }
            else
            {
                SctuHwState state = WaitForReady();
                FiredSctuEvent(state, _testResults);
            }
        }

        private void MeasurementLogicRoutine(IOClamping clamping, IOGateway gateway)
        {
            try
            {
                FiredSctuEvent(SctuHwState.InProcess, _testResults);

                //пишем младшую 16-ти битную половину значения ударного тока
                WriteRegister(REG_SC_VALUE_L, UshortByNum(_testParameters.Value, false));

                //пишем старшую 16-ти битную половину значения ударного тока
                WriteRegister(REG_SC_VALUE_H, UshortByNum(_testParameters.Value, true));

                WriteRegister(REG_DUT_TYPE, (ushort)_testParameters.Type);
                WriteRegister(REG_R_SHUNT, (ushort)_testParameters.ShuntResistance);

                WriteRegister(REG_WAVEFORM_TYPE, (ushort)_testParameters.WaveFormType);

                if (_testParameters.WaveFormType == SctuWaveFormType.Trapezium)
                    WriteRegister(REG_TRAPEZE_EDGE_TIME, _testParameters.TrapezeEdgeTime);

                if (_isSctuEmulation)
                {
                    _deviseState = SctuHwState.WaitTimeOut;
                    _testResults.CurrentValue = 200;
                    _testResults.VoltageValue = 1000;
                    _testResults.MeasureGain = (double)912 / 1000;

                    if (_ReadGraph)
                    {
                        //эмуляция графика тока
                        for (int i = 1; i <= 11000; i++)
                            _testResults.CurrentData.Add(10);

                        //эмуляция графика напряжения
                        for (int i = 1; i <= 11000; i++)
                            _testResults.VoltageData.Add(30);
                    }

                    FiredSctuEvent(_deviseState, _testResults);
                }
                else
                {
                    //чтобы пресс не занимал шину CAN - запрещаем ему опрашивать температуры его столика
                    clamping.SetPermissionToScan(false);

                    try
                    {
                        CallAction(ACT_SC_PULSE_CONFIG);
                        Thread.Sleep(3000);

                        var devState = WaitForPulseConfig();

                        if (devState == SctuHwState.PulseConfigReady)
                        {
                            CallAction(ACT_SC_PULSE_START);
                            WaitForTestEnd();

                            //ожидаем состояние SctuHwState.WaitTimeOut
                            devState = WaitForTimeOut();

                            //чтобы на время чтения результатов теста шина CAN была максимально свободной
                            gateway.SetPermissionToScan(false);

                            try
                            {
                                //читаем результаты
                                _testResults.VoltageValue = ReadRegister(REG_DUT_U);

                                //читаем младшие 16 бит измеренного значения ударного тока
                                ushort DutIL = ReadRegister(REG_DUT_I_L);

                                //читаем старшие 16 бит измеренного значения ударного тока
                                ushort DutIH = ReadRegister(REG_DUT_I_H);

                                //формируем из двух прочитанных 16-ти битных значений int значение ударного тока
                                _testResults.CurrentValue = IntByUshorts(DutIL, DutIH);

                                //читаем значение коэффициента усиления. его надо разделить на 1000, показывать с 3-мя знаками после запятой
                                _testResults.MeasureGain = (double)ReadRegister(REG_INFO_K_SHUNT_AMP) / 1000;

                                if (_ReadGraph)
                                {
                                    //ожидаем готовность данных для построения графиков и только после этого их читаем
                                    WaitForGraphDataReady();

                                    //читаем массивы данных тока и напряжения для построения графиков тока и напряжения
                                    ReadArrays(_testResults);
                                }

                                FiredSctuEvent(devState, _testResults);
                            }

                            finally
                            {
                                //разрешаем опрос Gateway
                                gateway.SetPermissionToScan(true);
                            }
                        }
                    }

                    finally
                    {
                        //разрешаем опрос Clamping
                        clamping.SetPermissionToScan(true);
                    }
                }
            }
            catch (Exception ex)
            {
                _deviseState = SctuHwState.Fault;
                FiredSctuEvent(_deviseState, _testResults);
                FireExceptionEvent(ex.Message);

                throw;
            }
        }

        private SctuHwState WaitForReady()
        {
            //ожидание перехода SCTU в состояние SctuHwState.Ready в течение 1.5 минуты
            var timeStamp = Environment.TickCount + 90000;

            while (Environment.TickCount < timeStamp)
            {
                Thread.Sleep(500); //

                var state = (SctuHwState)ReadRegister(REG_DEV_STATE);

                if (state == SctuHwState.Ready)
                    return SctuHwState.Ready;

                if (state == SctuHwState.Fault)
                    throw new Exception(string.Format("Sctu is in fault state, reason: {0}", ReadRegister(REG_FAULT_REASON)));

                if (state == SctuHwState.Disabled)
                    throw new Exception("Sctu is in disabled state");
            }

            throw new Exception("WaitForReady timeout");
        }

        private SctuHwState WaitForTestEnd()
        {
            var timeStamp = Environment.TickCount + _timeout;
            while (Environment.TickCount < timeStamp)
            {
                Thread.Sleep(500); //100

                var state = (SctuHwState)ReadRegister(REG_DEV_STATE);

                if (state == SctuHwState.PulseEnd)
                    return SctuHwState.PulseEnd;

                if (state == SctuHwState.Fault)
                    throw new Exception(string.Format("Sctu is in fault state, reason: {0}", ReadRegister(REG_FAULT_REASON)));

                if (state == SctuHwState.Disabled)
                    throw new Exception("Sctu is in disabled state");
            }

            throw new Exception("WaitForTestEnd timeout");
        }

        private SctuHwState WaitForTimeOut()
        {
            var timeStamp = Environment.TickCount + 5000;
            while (Environment.TickCount < timeStamp)
            {
                Thread.Sleep(500); //100

                var state = (SctuHwState)ReadRegister(REG_DEV_STATE);

                if (state == SctuHwState.WaitTimeOut)
                    return SctuHwState.WaitTimeOut;

                if (state == SctuHwState.Fault)
                    throw new Exception(string.Format("Sctu is in fault state, reason: {0}", ReadRegister(REG_FAULT_REASON)));

                if (state == SctuHwState.Disabled)
                    throw new Exception("Sctu is in disabled state");
            }

            throw new Exception("WaitForTimeOut timeout");
        }

        private SctuHwState WaitForGraphDataReady()
        {
            //ожидание состояния готовности данных для построения графиков
            var timeStamp = Environment.TickCount + _timeout;

            while (Environment.TickCount < timeStamp)
            {
                Thread.Sleep(500);

                var state = (SctuHwState)ReadRegister(REG_DEV_STATE);

                if ((state == SctuHwState.WaitTimeOut) || (state == SctuHwState.BatteryChargeWait))
                    return state;

                if (state == SctuHwState.Fault)
                    throw new Exception(string.Format("Sctu is in fault state, reason: {0}", ReadRegister(REG_FAULT_REASON)));

                if (state == SctuHwState.Disabled)
                    throw new Exception("Sctu is in disabled state");
            }

            //раз мы здесь - то отведённое на ожидание время вышло
            throw new Exception("WaitForGraphDataReady timeout");
        }

        private SctuHwState WaitForPulseConfig()
        {
            var timeStamp = Environment.TickCount + _timeout;
            while (Environment.TickCount < timeStamp)
            {
                Thread.Sleep(100); //

                var state = (SctuHwState)ReadRegister(REG_DEV_STATE);

                switch (state)
                {
                    case SctuHwState.PulseConfigReady:
                        return state;

                    case SctuHwState.Fault:
                        throw new Exception(string.Format("Sctu is in fault state, reason: {0}", ReadRegister(REG_FAULT_REASON)));

                    case SctuHwState.Disabled:
                        throw new Exception("Sctu is in disabled state");
                }
            }

            throw new Exception("WaitForPulseConfig timeout");
        }

        internal void Stop()
        {
            CallAction(ACT_DS_NONE);

            //спим секунду
            Thread.Sleep(1000);

            //даём команду на зарядку батарей
            CallAction(ACT_BAT_START_CHARGE);

            //_stop = true;
        }

        private void FireConnectionEvent(DeviceConnectionState state, string message)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Sctu, LogMessageType.Info, message);
            _broadcastCommunication.PostDeviceConnectionEvent(ComplexParts.Sctu, state, message);
        }

        private void FiredSctuEvent(SctuHwState state, SctuTestResults result)
        {
            var message = string.Format("Sctu test state {0}", state);

            if (state == SctuHwState.WaitTimeOut)
                message = string.Format("Sctu test result - Voltage: {0}, Current: {1}, Gain: {2} ", result.VoltageValue, result.CurrentValue, result.MeasureGain);

            SystemHost.Journal.AppendLog(ComplexParts.Sctu, LogMessageType.Info, message);
            _broadcastCommunication.PostSctuEvent(state, result);
        }

        private void FireExceptionEvent(string Message)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Sctu, LogMessageType.Error, Message);
            _broadcastCommunication.PostExceptionEvent(ComplexParts.Sctu, Message);
        }

        #region Standart API
        internal void ClearFault()
        {
            SystemHost.Journal.AppendLog(ComplexParts.Sctu, LogMessageType.Note, "Sctu fault cleared");

            CallAction(ACT_FAULT_CLEAR);
        }

        private void ClearWarning()
        {
            SystemHost.Journal.AppendLog(ComplexParts.Sctu, LogMessageType.Note, "Sctu warning cleared");

            CallAction(ACT_WARNING_CLEAR);
        }

        internal ushort ActivationWorkPlace(ChannelByClumpType ChByClumpType, SctuWorkPlaceActivationStatuses ActivationStatus)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Sctu, LogMessageType.Note, "Sctu activation workplace");

            switch (ActivationStatus)
            {
                //мы хотим активировать рабочее место. прежде чем активировать рабочее место - проверим, что оно ещё не активировано
                case (SctuWorkPlaceActivationStatuses.WORKPLACE_IN_USE):
                    //если рабочее место свободно - активируем его, при этом реализация записи в регистр активации сама позоботится об установке статуса активации для 'другого' рабочеего места
                    if (ReadRegister(REG_WORKPLACE_ACTIVATION_STATUS) == (ushort)SctuWorkPlaceActivationStatuses.WORKPLACE_IS_FREE)
                    {
                        //пишем статус активации
                        WriteRegister(REG_WORKPLACE_ACTIVATION_STATUS, (ushort)ActivationStatus);

                        //пишем какой аналоговый канал нам надо использовать
                        if (ChByClumpType != ChannelByClumpType.NullValue)
                            WriteRegister(REG_CHANNELBYCLAMPTYPE, (ushort)ChByClumpType);
                    }

                    break;

                //мы хотим заблокировать рабочее место - блокировкой рабочего места управляет WriteRegister - поэтому ничего не делаем
                case (SctuWorkPlaceActivationStatuses.WORKPLACE_IS_BLOCKED):
                    break;

                //мы хотим освободить рабочее место
                case (SctuWorkPlaceActivationStatuses.WORKPLACE_IS_FREE):
                    //если рабочее место занято - освобождаем его, при этом реализация записи в регистр активации сама позоботится об установке статуса активации для 'другого' рабочеего места
                    if (ReadRegister(REG_WORKPLACE_ACTIVATION_STATUS) == (ushort)SctuWorkPlaceActivationStatuses.WORKPLACE_IN_USE)
                        WriteRegister(REG_WORKPLACE_ACTIVATION_STATUS, (ushort)ActivationStatus);

                    break;
            }

            //возвращаем номер регистра статуса активации рабочего места вызывающей реализации
            return REG_WORKPLACE_ACTIVATION_STATUS;
        }

        internal ushort ReadRegister(ushort address, bool skipJournal = false)
        {
            ushort? node = null;

            switch (_isSctuEmulation)
            {
                case (true):
                    switch (address)
                    {
                        case (REG_WORKPLACE_ACTIVATION_STATUS):
                            //режим эмуляции, читается регистр активации
                            return _WorkplaceActivationStatusRegisterValueEmulation;

                        default:
                            //режим эмуляции, читается регистр не являющийся регистром активации - вернём 0
                            return 0;
                    }

                default:
                    switch (address)
                    {
                        case (REG_WORKPLACE_ACTIVATION_STATUS):
                            //режим чтения регистра статуса активации установки
                            node = _ThisControlUnitBoardNode;
                            break;

                        default:
                            //режим чтения регистра установки 
                            node = _node;
                            break;
                    }

                    break;
            }

            ushort value = _ioAdapter.Read16((ushort)node, address);

            if (!skipJournal)
                SystemHost.Journal.AppendLog(ComplexParts.Sctu, LogMessageType.Note, string.Format("Sctu @ReadRegister, node {0}, address {1}, value {2}", node, address, value));

            return value;
        }

        internal short ReadRegisterS(ushort address, bool skipJournal = false)
        {
            short value = 0;

            if (!_isSctuEmulation)
                value = _ioAdapter.Read16S(_node, address);

            if (!skipJournal)
                SystemHost.Journal.AppendLog(ComplexParts.Sctu, LogMessageType.Note, string.Format("Sctu @ReadRegisterS, address {0}, value {1}", address, value));

            return value;
        }

        private void ReadArrays(Types.SCTU.SctuTestResults Result)
        {
            //чтение массивов даннных тока и напряжения, это не сырые данные они уже готовы для отображения
            SystemHost.Journal.AppendLog(ComplexParts.Sctu, LogMessageType.Note, "SCTU @ReadArrays begin");

            //читаем массив данных для построения графика напряжения
            Result.VoltageData.Clear();

            if (!_isSctuEmulation)
                Result.VoltageData = _ioAdapter.ReadArrayFast16S(_node, ARR_SCOPE_V).ToList();

            //читаем массив данных для построения графика тока
            Result.CurrentData.Clear();

            if (!_isSctuEmulation)
                Result.CurrentData = _ioAdapter.ReadArrayFast16S(_node, ARR_SCOPE_I).ToList();

            SystemHost.Journal.AppendLog(ComplexParts.Sctu, LogMessageType.Note, "SCTU @ReadArrays end");
        }

        internal ushort UshortByNum(int value, bool Num)
        {
            //принимает на вход value и номер половины 32 битного значения value, которую надо извлечь из этого value
            //возвращает ushort значение по номеру Num: false - запрошены первая половина value, true - запрошена вторая половина value 
            int startindex = Num ? 2 : 0;
            byte[] byteArray = BitConverter.GetBytes(value);

            return BitConverter.ToUInt16(byteArray, startindex);
        }

        internal int IntByUshorts(ushort Lvalue, ushort Hvalue)
        {
            //принимает на вход младшую половину Lvalue и старшую половину Hvalue 
            //возвращает int значение, полученное из принятых Lvalue и Hvalue
            int res = Hvalue;
            res = (res << 16) | Lvalue;

            return res;
        }

        private ushort? CalcStatusActivationForAnotherControlUnitBoard(ushort PreviousStatusActivation, ushort NewStatusActivation)
        {
            //принимает на вход значение старого и нового статуса активации для своего рабочего места и по их значениям вычисляет какой статус активации надо установить для другого рабочего места
            //возвращает значение статуса активации для другого рабочего места
            //           null - если изменение статуса активации не имеет смысла

            //перебираем все возможные варианты перехода статуса активации
            //переход в статус WORKPLACE_IS_FREE
            if (NewStatusActivation == (ushort)SctuWorkPlaceActivationStatuses.WORKPLACE_IS_FREE)
            {
                switch (PreviousStatusActivation)
                {
                    case ((ushort)SctuWorkPlaceActivationStatuses.WORKPLACE_IS_FREE):
                        return null;

                    case ((ushort)SctuWorkPlaceActivationStatuses.WORKPLACE_IN_USE):
                        return (ushort)SctuWorkPlaceActivationStatuses.WORKPLACE_IS_FREE;

                    case ((ushort)SctuWorkPlaceActivationStatuses.WORKPLACE_IS_BLOCKED):
                        return null;

                    default:
                        throw new Exception(string.Format("NewStatusActivation {0}. PreviousStatusActivation {1} is out of range.", NewStatusActivation.ToString(), PreviousStatusActivation.ToString()));
                }
            }
            else
            {
                //переход в статус WORKPLACE_IN_USE
                if (NewStatusActivation == (ushort)SctuWorkPlaceActivationStatuses.WORKPLACE_IN_USE)
                {
                    switch (PreviousStatusActivation)
                    {
                        case ((ushort)SctuWorkPlaceActivationStatuses.WORKPLACE_IS_FREE):
                            return (ushort)SctuWorkPlaceActivationStatuses.WORKPLACE_IS_BLOCKED;

                        case ((ushort)SctuWorkPlaceActivationStatuses.WORKPLACE_IN_USE):
                            return null;

                        case ((ushort)SctuWorkPlaceActivationStatuses.WORKPLACE_IS_BLOCKED):
                            return null;

                        default:
                            throw new Exception(string.Format("NewStatusActivation {0}. PreviousStatusActivation {1} is out of range.", NewStatusActivation.ToString(), PreviousStatusActivation.ToString()));
                    }
                }
                else
                {
                    //переход в статус WORKPLACE_IS_BLOCKED
                    if (NewStatusActivation == (ushort)SctuWorkPlaceActivationStatuses.WORKPLACE_IS_BLOCKED)
                    {
                        switch (PreviousStatusActivation)
                        {
                            case ((ushort)SctuWorkPlaceActivationStatuses.WORKPLACE_IS_FREE):
                                return null;

                            case ((ushort)SctuWorkPlaceActivationStatuses.WORKPLACE_IN_USE):
                                return null;

                            case ((ushort)SctuWorkPlaceActivationStatuses.WORKPLACE_IS_BLOCKED):
                                return null;

                            default:
                                throw new Exception(string.Format("NewStatusActivation {0}. PreviousStatusActivation {1} is out of range.", NewStatusActivation.ToString(), PreviousStatusActivation.ToString()));
                        }
                    }
                    else throw new Exception(string.Format("NewStatusActivation {0} is out of range.", NewStatusActivation.ToString()));
                }
            }
        }

        private ushort? StatusForAnotherControlUnitBoard(ushort NewStatusActivationThisControlUnitBoard)
        {
            //принимает на вход значение статуса активации для своего рабочего места и по его значению и значению статуса активации, которое уже хранится в данном рабочем месте вычисляет значение статуса активации которое при этом надо установить для другого рабочего места           
            //считываем значение статуса активации, которое хранится в своём ControlUnitBoard
            ushort PreviousStatusActivationThisControlUnitBoard = ReadRegister(REG_WORKPLACE_ACTIVATION_STATUS);
            ushort? Result = CalcStatusActivationForAnotherControlUnitBoard(PreviousStatusActivationThisControlUnitBoard, NewStatusActivationThisControlUnitBoard);

            return Result;
        }

        internal void WriteRegister(ushort address, ushort value, bool skipJournal = false)
        {
            switch (address)
            {
                case (REG_WORKPLACE_ACTIVATION_STATUS):
                    {
                        ushort? StatusAnotherControlUnitBoard = null;

                        //если в параметре UseAnotherControlUnitBoard файла настроек разрешено использование другого рабочего места - вычисляем статус активации для ControlUnitBoard другого рабочего места до записи в свой собственный регистр активации
                        if (_UseAnotherControlUnitBoard)
                            StatusAnotherControlUnitBoard = StatusForAnotherControlUnitBoard(value);

                        //если выполняется запись статуса активации, то необходимо писать его сразу в два места хранения
                        //пишем в регистр статуса активации своего ControlUnitBoard
                        WriteRegister16(_ThisControlUnitBoardNode, address, value, skipJournal);

                        //если в параметре UseAnotherControlUnitBoard файла настроек разрешено использование другого рабочего места - вычисляем и пишем статус активации в регистр ControlUnitBoard другого рабочего места
                        if (_UseAnotherControlUnitBoard)
                        {
                            //если есть что писать - пишем в регистр статуса активации платы ControlUnitBoard не являющейся default
                            if (StatusAnotherControlUnitBoard != null)
                                WriteRegister16(_AnotherControlUnitBoardNode, address, (ushort)StatusAnotherControlUnitBoard, skipJournal);
                        }

                        break;
                    }

                default:
                    {
                        WriteRegister16(_node, address, value, skipJournal);
                        break;
                    }
            }
        }

        internal void WriteRegister16(ushort node, ushort address, ushort value, bool skipJournal = false)
        {
            if (!skipJournal)
                SystemHost.Journal.AppendLog(ComplexParts.Sctu, LogMessageType.Note,
                                         string.Format("Sctu @WriteRegister, address {0}, value {1}, node {2}", address, value, node));

            if (_isSctuEmulation)
            {
                if (address == REG_WORKPLACE_ACTIVATION_STATUS)
                    _WorkplaceActivationStatusRegisterValueEmulation = value;

                return;
            }

            _ioAdapter.Write16(node, address, value);
        }

        internal void CallAction(ushort action)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Sctu, LogMessageType.Note, string.Format("Sctu @Call, action {0}", action));

            if (_isSctuEmulation)
                return;

            _ioAdapter.Call(_node, action);

            //после выдачи некоторых команд нужно выдержать паузу
            switch (action)
            {
                case ACT_SC_PULSE_CONFIG:
                    Thread.Sleep(500);
                    break;

                case ACT_SC_PULSE_START:
                    Thread.Sleep(100);
                    break;
            }
        }

        #endregion

    }
}
