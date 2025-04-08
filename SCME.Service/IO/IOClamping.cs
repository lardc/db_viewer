//#define MME_005

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using SCME.Service.Properties;
using SCME.Types;
using SCME.Types.Clamping;

namespace SCME.Service.IO
{
    internal class IOClamping
    {
        private readonly IOAdapter m_IOAdapter;
        private readonly BroadcastCommunication m_Communication;
        private readonly ushort m_Node;
        private readonly ThreadService m_Thread;
        private readonly bool m_IsClampingEmulationHard;
        public bool m_IsClampingEmulation;
        private IOCommutation m_IOCommutation;
        private DeviceConnectionState m_ConnectionState;
        private volatile bool m_Stop;
        private volatile bool m_SendTemperature;
        private volatile bool m_PermissionClampingScan = true;

        private int m_Timeout = 120000;

        //private const float DefaultForce = 5.0f;
        private readonly float _defaultForce;
        private readonly float _longTimeForce;
        private readonly ushort _defaultHeight;
        private ushort aimTemperature;

        private TestParameters _clampingParameters;

        private const ushort ACT_CLEAR_FAULT = 3;
        private const ushort ACT_CLEAR_WARNING = 4;
        private const ushort ACT_CLEAR_HALT = 5;
        private const ushort ACT_SAVE_TO_ROM = 200;
        private const ushort ACT_HOMING = 100;
        private const ushort ACT_GOTO_POSITION = 101;
        private const ushort ACT_START_CLAMPING = 102;
        private const ushort ACT_CLAMPING_UPDATE = 103;
        private const ushort ACT_RELEASE_CLAMPING = 104;
        private const ushort ACT_HALT = 105;
        private const ushort ACT_SET_TEMPERATURE = 108;

        #region Registers

        private const ushort REG_FORCE_FINE_N = 12;
        private const ushort REG_FORCE_FINE_D = 13;
        private const ushort REG_FORCE_OFFSET = 14;
        private const ushort REG_CUSTOM_POS = 64;
        private const ushort REG_FORCE_VAL = 70;
        private const ushort REG_DEVICE_STATE = 96;
        private const ushort REG_FAULT_REASON = 97;
        private const ushort REG_DISABLE_REASON = 98;
        private const ushort REG_WARNING = 99;
        private const ushort REG_PROBLEM = 100;
        private const ushort REG_SLIDING_SENSOR = 105;
        private const ushort REG_FORCE_RESULT = 110;
        private const ushort ARR_FORCE_ACT = 1;
        private const ushort ARR_FORCE_DESIRED = 2;
        private const ushort ARR_FORCE_ERR = 3;

        /// <summary>
        /// Регистр задающий высоту устройства (in mm)
        /// </summary>
        private const ushort REG_DEV_HEIGHT = 71;

        /// <summary>
        /// //Регистр задающий температуру нагрева пластин (in C x10)
        /// </summary>
        private const ushort REG_TEMP_SETPOINT = 72;

        /// <summary>
        /// Температура на канале 1  (нижняя пластина) (в градусах Цельсия х10);
        /// </summary>
        private const ushort REG_TEMP_CH1 = 101;

        /// <summary>
        /// Температура на канале 2 (верхняя пластина) (в градусах Цельсия х10);
        /// </summary>
        private const ushort REG_TEMP_CH2 = 102;

        #endregion

        internal IOClamping(IOAdapter Adapter, BroadcastCommunication Communication)
        {
            m_IOAdapter = Adapter;
            m_Communication = Communication;

            m_Thread = new ThreadService();
            m_Thread.FinishedHandler += Thread_FinishedHandler;

            m_IsClampingEmulationHard = Settings.Default.ClampingSystemEmulation;
            m_IsClampingEmulation = m_IsClampingEmulationHard;

            _defaultForce = Settings.Default.DefaultForce;
            _longTimeForce = Settings.Default.LongTimeForce;
            _defaultHeight = Settings.Default.DefaultHeight;

            m_Node = (ushort)Settings.Default.ClampingSystemNode;
            SystemHost.Journal.AppendLog(ComplexParts.Clamping, LogMessageType.Info, String.Format("Clamping created. Emulation mode: {0}", m_IsClampingEmulation));
        }

        internal IOCommutation ActiveCommutation
        {
            get { return m_IOCommutation; }
            set { m_IOCommutation = value; }
        }

        internal DeviceConnectionState Initialize(bool Enable, int Timeout)
        {
            m_Timeout = Timeout;
            m_IsClampingEmulation = m_IsClampingEmulationHard || !Enable;

            m_ConnectionState = DeviceConnectionState.ConnectionInProcess;
            FireConnectionEvent(m_ConnectionState, "Clamping initializing");

            if (m_IsClampingEmulation)
            {
                m_ConnectionState = DeviceConnectionState.ConnectionSuccess;

                m_Thread.StartCycle(OnTimedHeating, REQUEST_DELAY_MS);

                FireConnectionEvent(m_ConnectionState, "Clamping initialized");

                return m_ConnectionState;
            }

            try
            {
                var timeStamp = Environment.TickCount + m_Timeout;

                ClearWarning();

                var devState = (HWDeviceState)ReadRegister(REG_DEVICE_STATE);
                if (devState != HWDeviceState.Ready)
                {
                    if (devState == HWDeviceState.Fault)
                    {
                        ClearFault();
                        Thread.Sleep(100);

                        devState = (HWDeviceState)ReadRegister(REG_DEVICE_STATE);

                        if (devState == HWDeviceState.Fault)
                            throw new Exception(string.Format("Clamping is in fault state, reason: {0}",
                                ReadRegister(REG_FAULT_REASON)));
                    }

                    if (devState == HWDeviceState.ClampingDone)
                        CallAction(ACT_RELEASE_CLAMPING);
                    else
                        CallAction(ACT_HOMING);
                }

                while (Environment.TickCount < timeStamp)
                {
                    Thread.Sleep(100);

                    devState = (HWDeviceState)ReadRegister(REG_DEVICE_STATE, true);

                    if (devState == HWDeviceState.Ready)
                        break;

                    if (devState == HWDeviceState.Fault)
                        throw new Exception(string.Format("Clamping is in fault state, reason: {0}",
                            (Types.SL.HWFaultReason)ReadRegister(REG_FAULT_REASON)));
                    if (devState == HWDeviceState.Disabled)
                        throw new Exception(string.Format("Clamping is in disabled state, reason: {0}",
                            (Types.SL.HWDisableReason)ReadRegister(REG_DISABLE_REASON)));
                }

                if (Environment.TickCount > timeStamp)
                    throw new Exception("Timeout while waiting for homing is done");

                m_ConnectionState = DeviceConnectionState.ConnectionSuccess;

                m_Thread.StartCycle(OnTimedHeating, REQUEST_DELAY_MS);

                FireConnectionEvent(m_ConnectionState, "Clamping initialized");
            }
            catch (Exception ex)
            {
                m_ConnectionState = DeviceConnectionState.ConnectionFailed;
                FireConnectionEvent(m_ConnectionState,
                                    String.Format("Clamping initialization error: {0}", ex.Message));
            }

            return m_ConnectionState;
        }

        internal void Deinitialize()
        {
            var oldState = m_ConnectionState;

            if (oldState == DeviceConnectionState.ConnectionSuccess)
                m_Thread.StopCycle(true);

            m_ConnectionState = DeviceConnectionState.DisconnectionInProcess;
            FireConnectionEvent(DeviceConnectionState.DisconnectionInProcess, "Clamping disconnecting");

            try
            {
                if (!m_IsClampingEmulation && oldState == DeviceConnectionState.ConnectionSuccess)
                {
                    var devState = (HWDeviceState)
                        ReadRegister(REG_DEVICE_STATE);

                    try
                    {
#if !MME_005
                        WriteRegister(REG_TEMP_SETPOINT, ROOM_TEMP * 10);
                        CallAction(ACT_SET_TEMPERATURE);
#endif
                    }
                    catch (Exception ex)
                    {
                        SystemHost.Journal.AppendLog(ComplexParts.Clamping, LogMessageType.Error, $"Error during Clamp deinitialization, temperature stage: {ex.Message}");
                    }

                    if (devState == HWDeviceState.ClampingDone)
                        Unsqueeze(new TestParameters());
                }

                m_ConnectionState = DeviceConnectionState.DisconnectionSuccess;
                FireConnectionEvent(DeviceConnectionState.DisconnectionSuccess, "Clamping disconnected");
            }
            catch (Exception)
            {
                m_ConnectionState = DeviceConnectionState.DisconnectionError;
                FireConnectionEvent(DeviceConnectionState.DisconnectionError, "Clamping disconnection error");
            }

            m_ConnectionState = DeviceConnectionState.DisconnectionSuccess;
        }

        internal void Stop()
        {
            m_Stop = true;

            //выдаём на зажимное устройство команду Halt
            //при этом оно останавливается в текущей позиции, а если устройство в зажатом состоянии, то снимается усилие зажатия, но оно остаётся в том же положении
            CallAction(ACT_HALT);

            SystemHost.Journal.AppendLog(ComplexParts.Clamping, LogMessageType.Info, "Stop method called");
        }

        #region Standart API

        internal void ClearFault()
        {
            SystemHost.Journal.AppendLog(ComplexParts.Clamping, LogMessageType.Note, "Clamping fault cleared");

            if (m_IsClampingEmulation)
                return;

            CallAction(ACT_CLEAR_FAULT);
        }

        internal void ClearWarning()
        {
            SystemHost.Journal.AppendLog(ComplexParts.Clamping, LogMessageType.Note, "Clamping warning cleared");

            if (m_IsClampingEmulation)
                return;

            CallAction(ACT_CLEAR_WARNING);
        }

        internal void Clear_Halt()
        {
            //если пресс в состоянии Halt то данная реализация изменяет содержимое регистра состояния пресса на значение Ready
            SystemHost.Journal.AppendLog(ComplexParts.Clamping, LogMessageType.Note, "Clamping halt cleared");

            if (m_IsClampingEmulation)
                return;

            CallAction(ACT_CLEAR_HALT);
        }

        internal void CheckAndClearHalt()
        {
            //читает состояние пресса и если оно есть Halt - сбрасывает его
            HWDeviceState devState = (HWDeviceState)ReadRegister(REG_DEVICE_STATE);

            if (devState == HWDeviceState.Halt)
                Clear_Halt();
        }

        internal ushort ReadRegister(ushort Address, bool SkipJournal = false)
        {
            ushort value = 0;

            if (!m_IsClampingEmulation)
                value = m_IOAdapter.Read16(m_Node, Address);

            if (!SkipJournal)
                SystemHost.Journal.AppendLog(ComplexParts.Clamping, LogMessageType.Note,
                                         string.Format("Clamping @ReadRegister, address {0}, value {1}", Address,
                                                       value));

            return value;
        }

        internal short ReadRegisterS(ushort Address, bool SkipJournal = false)
        {
            short value = 0;

            if (!m_IsClampingEmulation)
                value = m_IOAdapter.Read16S(m_Node, Address);

            if (!SkipJournal)
                SystemHost.Journal.AppendLog(ComplexParts.SL, LogMessageType.Note,
                                             string.Format("Clamping @ReadRegisterS, address {0}, value {1}", Address, value));

            return value;
        }

        internal void WriteRegister(ushort Address, ushort Value, bool SkipJournal = false)
        {
            if (!SkipJournal)
                SystemHost.Journal.AppendLog(ComplexParts.Clamping, LogMessageType.Note,
                                         string.Format("Clamping @WriteRegister, address {0}, value {1}", Address,
                                                       Value));

            if (m_IsClampingEmulation)
                return;

            m_IOAdapter.Write16(m_Node, Address, Value);
        }

        internal void WriteRegisterS(ushort Address, short Value, bool SkipJournal = false)
        {
            if (!SkipJournal)
                SystemHost.Journal.AppendLog(ComplexParts.Clamping, LogMessageType.Note,
                                         string.Format("Clamping @WriteRegisterS, address {0}, value {1}", Address, Value));

            if (m_IsClampingEmulation)
                return;

            m_IOAdapter.Write16S(m_Node, Address, Value);
        }

        internal void CallAction(ushort Action)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Clamping, LogMessageType.Note,
                                         string.Format("Clamping @Call, action {0}", Action));

            if (m_IsClampingEmulation)
                return;

            m_IOAdapter.Call(m_Node, Action);
        }

        internal void SetPermissionToScan(bool PermissionToScan)
        {
            //управление разрешением/запретом чтения темеператур столика пресса
            this.m_PermissionClampingScan = PermissionToScan;
        }

        private IList<ushort> ReadArrayFast(ushort Address)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Clamping, LogMessageType.Note,
                                         string.Format("Clamping @ReadArrayFast, endpoint {0}", Address));

            if (m_IsClampingEmulation)
                return new List<ushort>();

            return m_IOAdapter.ReadArrayFast16(m_Node, Address);
        }

        #endregion

        #region Heating
        /*
                private ushort chOneTemp;
                private ushort chTwoTemp;
                private ushort SettingTemperature;
        */
        private void OnTimedHeating()
        {
            //если разрешено чтение температур - читаем температуры. необходимость в управлении возможностью чтения температур возникла в случае использования SCTU для освобождения шины CAN
            if (!m_PermissionClampingScan)
                return;

            ushort chOneTemp = 0;
            ushort chTwoTemp = 0;
            ushort SettingTemperature = 0;

            if (m_IsClampingEmulation)
            {
                if (chOneTemp < aimTemperature)
                    chOneTemp = (ushort)(chOneTemp + 50);
                else
                    chOneTemp = aimTemperature;

                if (chTwoTemp < aimTemperature)
                    chTwoTemp = (ushort)(chTwoTemp + 50);
                else
                    chTwoTemp = aimTemperature;

                //уставка по температуре хранится в регистре REG_TEMP_SETPOINT умноженная на 10
                SettingTemperature = (ushort)(aimTemperature * 10);
            }
            else
            {
                if (!m_PermissionClampingScan)
                    return;

                chOneTemp = ReadRegister(REG_TEMP_CH1, true);

                if (!m_PermissionClampingScan)
                    return;

                chTwoTemp = ReadRegister(REG_TEMP_CH2, true);

                if (!m_PermissionClampingScan)
                    return;

                SettingTemperature = ReadRegister(REG_TEMP_SETPOINT, true);
            }

            if (m_SendTemperature)
            {
                m_Communication.PostClampingTemperatureEvent(HeatingChannel.Bottom, chOneTemp / 10);
                m_Communication.PostClampingTemperatureEvent(HeatingChannel.Top, chTwoTemp / 10);
                m_Communication.PostClampingTemperatureEvent(HeatingChannel.Setting, SettingTemperature / 10);
            }
        }

        private void Thread_FinishedHandler(object Sender, ThreadFinishedEventArgs E)
        {
            if (E.Error != null)
                FireExceptionEvent(E.Error.Message);
        }

        public DeviceState StartHeating(int temperature)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Clamping, LogMessageType.Info, $"Heating to temperature: {temperature}");
            aimTemperature = (ushort)temperature;

            if (m_IsClampingEmulation)
            {
                FireSwitchEvent(SqueezingState.Heating, null, null);

                m_SendTemperature = true;
                return DeviceState.Heating;
            }

            try
            {
                var warning = (HWWarningReason)ReadRegister(REG_WARNING);

                if (warning != HWWarningReason.None)
                {
                    FireNotificationEvent(warning, HWProblemReason.None, HWFaultReason.None);
                    ClearWarning();
                }

                var devState = (HWDeviceState)ReadRegister(REG_DEVICE_STATE);

                if (devState == HWDeviceState.Fault)
                {
                    FireNotificationEvent(HWWarningReason.None, HWProblemReason.None,
                                          (HWFaultReason)ReadRegister(REG_FAULT_REASON));

                    return DeviceState.Fault;
                }

                FireSwitchEvent(SqueezingState.Heating, null, null);

#if !MME_005
                WriteRegister(REG_TEMP_SETPOINT, (ushort)(temperature * 10));
                CallAction(ACT_SET_TEMPERATURE);
#endif
                m_SendTemperature = true;

                return DeviceState.Heating;
            }
            catch (Exception ex)
            {
                FireExceptionEvent(ex.Message);
                return DeviceState.Fault;
            }
        }

        internal void StopHeating()
        {
            SystemHost.Journal.AppendLog(ComplexParts.Clamping, LogMessageType.Info, "Disable heating, temperature: ROOM");

            if (m_IsClampingEmulation)
            {
                FireSwitchEvent(SqueezingState.Heating, null, null);

                return;
            }

            try
            {
                var warning = (HWWarningReason)ReadRegister(REG_WARNING);

                if (warning != HWWarningReason.None)
                {
                    FireNotificationEvent(warning, HWProblemReason.None, HWFaultReason.None);
                    ClearWarning();
                }

                var devState = (HWDeviceState)ReadRegister(REG_DEVICE_STATE);

                if (devState == HWDeviceState.Fault)
                {
                    FireNotificationEvent(HWWarningReason.None, HWProblemReason.None, (HWFaultReason)ReadRegister(REG_FAULT_REASON));

                    return;
                }

                FireSwitchEvent(SqueezingState.Heating, null, null);

#if !MME_005
                WriteRegister(REG_TEMP_SETPOINT, ROOM_TEMP * 10);
                CallAction(ACT_SET_TEMPERATURE);
#endif

                m_SendTemperature = true;
            }
            catch (Exception ex)
            {
                FireExceptionEvent(ex.Message);
            }
        }

        #endregion

        internal DeviceState Squeeze(TestParameters ClampingParameters, bool SkipReadingArrays = true)
        {
            if (ClampingParameters.SkipClamping)
                return DeviceState.Success;

            _clampingParameters = ClampingParameters;
            if (_clampingParameters.Height == 0)
                _clampingParameters.Height = ClampingParameters.Height = _defaultHeight;

            SystemHost.Journal.AppendLog(ComplexParts.Clamping, LogMessageType.Info, String.Format("Clamping force: {0}", ClampingParameters.CustomForce));

            if (m_IsClampingEmulation)
            {
                FireSwitchEvent(SqueezingState.Squeezing, null, null);
                Thread.Sleep(500);
                FireSwitchEvent(SqueezingState.Up, null, null);

                return DeviceState.Success;
            }

            m_Stop = false;

            try
            {
                var warning = (HWWarningReason)ReadRegister(REG_WARNING);

                if (warning != HWWarningReason.None)
                {
                    FireNotificationEvent(warning, HWProblemReason.None, HWFaultReason.None);
                    ClearWarning();
                }

                var devState = (HWDeviceState)ReadRegister(REG_DEVICE_STATE);

                if (devState == HWDeviceState.Fault)
                {
                    FireNotificationEvent(HWWarningReason.None, HWProblemReason.None, (HWFaultReason)ReadRegister(REG_FAULT_REASON));

                    return DeviceState.Fault;
                }

                //если сработала система безопасности - прижим остановится и перейдёт в состояние Halt
                if (devState == HWDeviceState.Halt)
                    return DeviceState.Stopped;

                FireSwitchEvent(SqueezingState.Squeezing, null, null);

                WriteRegister(REG_FORCE_VAL, (ushort)(ClampingParameters.CustomForce * 10));
                WriteRegister(REG_DEV_HEIGHT, ClampingParameters.Height);

                /*
#if !MME_005
                WriteRegister(REG_FORCE_VAL, (ushort)(ClampingParameters.CustomForce * 10));
                WriteRegister(REG_DEV_HEIGHT, ClampingParameters.Height);
#else
                    WriteRegister(REG_FORCE_VAL, (ushort)(ClampingParameters.CustomForce * 1000));
#endif
                */

                CallAction(ACT_START_CLAMPING);

                var result = WaitForEndOfTest(true);

                List<float> arrayF = null;
                List<float> arrayFd = null;

                if ((result == DeviceState.Success) && !SkipReadingArrays)
                {
                    arrayF = ReadArrayFast(ARR_FORCE_ACT).Select(Arg => Arg / 1000.0f).ToList();
                    arrayFd = ReadArrayFast(ARR_FORCE_DESIRED).Select(Arg => Arg / 1000.0f).ToList();
                }

                FireSwitchEvent(result == DeviceState.Success
                    ? SqueezingState.Up
                    : SqueezingState.Undeterminated, arrayF, arrayFd);

                return result;
            }
            catch (Exception ex)
            {
                FireExceptionEvent(ex.Message);
                return DeviceState.Fault;
            }
        }

        internal DeviceState SetCustomForce()
        {
            const bool isDefault = false;
            return UpdateClamp(isDefault);
        }

        internal DeviceState ReturnForceToDefault()
        {
            const bool isDefault = true;
            return UpdateClamp(isDefault);
        }

        internal bool GetButtonState(ComplexButtons Button)
        {
            switch (Button)
            {
                //true  - зажим стоит в рабочем положении
                //false - зажим выдвинут из рабочего положения
                case ComplexButtons.ClampSlidingSensor:
                    if (m_IsClampingEmulation)
                        return true;
                    else return ReadRegister(REG_SLIDING_SENSOR) != 0;

                default:
                    string mess = String.Format("Realization does not know Button={0}.", Button.ToString());
                    throw new Exception(string.Format("{0}.{1}.{2}", GetType().Name, MethodBase.GetCurrentMethod().Name, mess));
            }
        }

        private DeviceState UpdateClamp(bool isDefault)
        {
            if (m_IsClampingEmulation)
            {
                return DeviceState.Success;
            }
            else
            {
                var devState = (HWDeviceState)
                    ReadRegister(REG_DEVICE_STATE);

                if (devState != HWDeviceState.ClampingDone)
                {
                    SystemHost.Journal.AppendLog(ComplexParts.Clamping, LogMessageType.Info, string.Format("Can't update clamping - device state is {0}", devState));
                    return DeviceState.Success;
                    //throw new Exception(string.Format("Can't update clamping - device state is {0}", devState));
                }

                //если заданное в профиле значение усилия зажатия пресса меньше или равно значению параметра LongTimeForce - не надо менять усилие зажатия пресса
                //смысл LongTimeForce - пресс способен продолжительное время удерживать усилие зажатия LongTimeForce без перегрева своего двигателя
                if (_clampingParameters.CustomForce <= _longTimeForce)
                    return DeviceState.Success;

                if (isDefault)
                    WriteRegister(REG_FORCE_VAL, (ushort)(_defaultForce * 10));
                else
                    WriteRegister(REG_FORCE_VAL, (ushort)(_clampingParameters.CustomForce * 10));

                /*
                #if !MME_005
                            if (isDefault)
                                WriteRegister(REG_FORCE_VAL, (ushort)(_defaultForce * 10));
                            else
                                WriteRegister(REG_FORCE_VAL, (ushort)(_clampingParameters.CustomForce * 10));
                #else
                                if (isDefault)
                                    WriteRegister(REG_FORCE_VAL, (ushort)(DefaultForce * 1000));
                                else
                                    WriteRegister(REG_FORCE_VAL, (ushort)(_clampingParameters.CustomForce * 1000));
                #endif
                */

                FireSwitchEvent(SqueezingState.Updating, null, null);
                CallAction(ACT_CLAMPING_UPDATE);

                return WaitForUpdate();
            }
        }

        private DeviceState WaitForUpdate()
        {
            var timeStamp = Environment.TickCount + m_Timeout;

            while (Environment.TickCount < timeStamp)
            {
                if (m_Stop)
                    return DeviceState.Stopped;

                var devState = (HWDeviceState)ReadRegister(REG_DEVICE_STATE, true);
                var warning = (HWWarningReason)ReadRegister(REG_WARNING, true);
                var problem = (HWProblemReason)ReadRegister(REG_PROBLEM, true);

                if (warning != HWWarningReason.None)
                {
                    FireNotificationEvent(warning, HWProblemReason.None, HWFaultReason.None);
                    CallAction(ACT_CLEAR_WARNING);
                }

                if (problem != HWProblemReason.None)
                {
                    FireNotificationEvent(HWWarningReason.None, problem, HWFaultReason.None);

                    return DeviceState.Problem;
                }

                if (devState == HWDeviceState.Fault)
                {
                    var fault = (HWFaultReason)ReadRegister(REG_FAULT_REASON);

                    FireNotificationEvent(HWWarningReason.None, HWProblemReason.None, fault);
                    return DeviceState.Fault;
                }

                if (devState == HWDeviceState.ClampingDone)
                    break;

            }

            if (Environment.TickCount > timeStamp)
            {
                FireExceptionEvent("Timeout while waiting for Clamping to end operation");
                throw new Exception("Timeout while waiting for Clamping to end operation");
            }
            return DeviceState.Success;
        }

        internal DeviceState Unsqueeze(TestParameters ClampingParameters)
        {
            if (ClampingParameters.SkipClamping)
                return DeviceState.Success;

            if (m_IsClampingEmulation)
            {
                FireSwitchEvent(SqueezingState.Unsqueezing, null, null);
                Thread.Sleep(500);
                FireSwitchEvent(SqueezingState.Down, null, null);

                return DeviceState.Success;
            }

            m_Stop = false;

            try
            {
                var warning = (HWWarningReason)
                              ReadRegister(REG_WARNING);

                if (warning != HWWarningReason.None)
                {
                    FireNotificationEvent(warning, HWProblemReason.None, HWFaultReason.None);
                    ClearWarning();
                }

                var devState = (HWDeviceState)ReadRegister(REG_DEVICE_STATE);

                if (devState == HWDeviceState.Fault)
                {
                    FireNotificationEvent(HWWarningReason.None, HWProblemReason.None,
                                          (HWFaultReason)ReadRegister(REG_FAULT_REASON));
                    return DeviceState.Fault;
                }

                //если сработала система безопасности - прижим остановится и перейдёт в состояние Halt
                if (devState == HWDeviceState.Halt)
                    return DeviceState.Stopped;

                //перед выдачей команды на разжатие убедимся, что пресс готов обработать эту команду, а если не готов - подождём пока пресс станет готов
                if ((devState == HWDeviceState.Ready) || (ReadDeviceState(Types.Clamping.HWDeviceState.ClampingDone, 30000) == (ushort)Types.Clamping.HWDeviceState.ClampingDone))
                {
                    FireSwitchEvent(SqueezingState.Unsqueezing, null, null);
                    CallAction(ACT_RELEASE_CLAMPING);
                }

                var result = WaitForEndOfTest(false);

                FireSwitchEvent(SqueezingState.Down, null, null);

                return result;
            }
            catch (Exception ex)
            {
                FireExceptionEvent(ex.Message);
                return DeviceState.Fault;
            }
        }

        internal void WriteCalibrationParams(CalibrationParams Parameters)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Clamping, LogMessageType.Info,
                                         "Clamping @WriteCalibrationParams begin");

            WriteRegister(REG_FORCE_FINE_N, Parameters.ForceFineN, true);
            WriteRegister(REG_FORCE_FINE_D, Parameters.ForceFineD, true);
            WriteRegisterS(REG_FORCE_OFFSET, Parameters.ForceOffset, true);

            if (!m_IsClampingEmulation)
                m_IOAdapter.Call(m_Node, ACT_SAVE_TO_ROM);

            SystemHost.Journal.AppendLog(ComplexParts.Clamping, LogMessageType.Info,
                                         "Clamping @WriteCalibrationParams end");
        }

        internal CalibrationParams ReadCalibrationParams()
        {
            SystemHost.Journal.AppendLog(ComplexParts.Clamping, LogMessageType.Info,
                                         "Clamping @ReadCalibrationParams begin");

            var parameters = new CalibrationParams
            {
                ForceOffset = ReadRegisterS(REG_FORCE_OFFSET, true),

                ForceFineN = ReadRegister(REG_FORCE_FINE_N, true),
                ForceFineD = ReadRegister(REG_FORCE_FINE_D, true)
            };

            SystemHost.Journal.AppendLog(ComplexParts.Clamping, LogMessageType.Info,
                                         "Clamping @ReadCalibrationParams end");

            return parameters;
        }

        private ushort GetDeviceState()
        {
            return ReadRegister(REG_DEVICE_STATE);
        }

        private ushort ReadDeviceState(Types.Clamping.HWDeviceState WaitedState, int Timeout)
        //реализация чтения состояния конечного автомата
        //в WaitedState принимается состояние, в которое должен перейти конечный автомат
        //реализация ожидает перехода конечного автомата в состояние WaitedState в течении времени Timeout
        //реализация возвращает считанный номер состояния конечного автомата
        {
            ushort State = GetDeviceState();

            if (State == (ushort)WaitedState) return State;
            else
            {
                //пока не истёк таймаут - будем перечитывать состояние блока через каждые 100 мс до тех пор, пока не окажемся в ожидаемом состоянии WaitedState
                var timeStamp = Environment.TickCount + Timeout;

                while (Environment.TickCount < timeStamp)
                {
                    Thread.Sleep(100);

                    State = GetDeviceState();

                    //считано состояние State, равное ожидаемому состоянию WaitedState - прерываем цикл ожидания
                    if (State == (ushort)WaitedState) return State;
                }

                //раз мы здесь - значит наступил таймаут, а состояния WaitedState мы так и не дождались
                return State;
            }
        }

        private DeviceState WaitForEndOfTest(bool Up)
        {
            var timeStamp = Environment.TickCount + m_Timeout;

            while (Environment.TickCount < timeStamp)
            {
                if (m_Stop)
                    return DeviceState.Stopped;

                var devState = (HWDeviceState)ReadRegister(REG_DEVICE_STATE, true);

                //если сработала система безопасности - прижим остановится и перейдёт в состояние Halt
                if (devState == HWDeviceState.Halt)
                    return DeviceState.Stopped;

                var warning = (HWWarningReason)ReadRegister(REG_WARNING, true);
                var problem = (HWProblemReason)ReadRegister(REG_PROBLEM, true);

                if (warning != HWWarningReason.None)
                {
                    FireNotificationEvent(warning, HWProblemReason.None, HWFaultReason.None);
                    CallAction(ACT_CLEAR_WARNING);
                }

                if (problem != HWProblemReason.None)
                {
                    FireNotificationEvent(HWWarningReason.None, problem, HWFaultReason.None);

                    return DeviceState.Problem;
                }

                if (devState == HWDeviceState.Fault)
                {
                    var fault = (HWFaultReason)ReadRegister(REG_FAULT_REASON);

                    FireNotificationEvent(HWWarningReason.None, HWProblemReason.None, fault);
                    return DeviceState.Fault;
                }

                if (Up)
                {
                    if (devState == HWDeviceState.ClampingDone)
                        break;
                }
                else
                {
                    if (devState == HWDeviceState.Ready)
                        break;
                }

                Thread.Sleep(100);
            }

            if (Environment.TickCount > timeStamp)
            {
                FireExceptionEvent("Timeout while waiting for Clamping to end test");
                throw new Exception("Timeout while waiting for Clamping to end test");
            }

            return DeviceState.Success;
        }

        #region Events

        private void FireConnectionEvent(DeviceConnectionState State, string Message)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Clamping, LogMessageType.Info, Message);

            m_Communication.PostDeviceConnectionEvent(ComplexParts.Clamping, State, Message);
        }

        private void FireNotificationEvent(HWWarningReason Warning, HWProblemReason Problem, HWFaultReason Fault)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Clamping, LogMessageType.Warning, string.Format("Clamping device notification: warning {0}, problem {1}, fault {2}", Warning, Problem, Fault));

            m_Communication.PostClampingNotificationEvent(Warning, Problem, Fault);
        }

        private void FireSwitchEvent(SqueezingState SQState, IList<float> ArrayF, IList<float> ArrayFd)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Clamping, LogMessageType.Info, string.Format("Squeezing state {0}", SQState));

            m_Communication.PostClampingSwitchEvent(SQState, ArrayF, ArrayFd);
        }

        private void FireExceptionEvent(string Message)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Clamping, LogMessageType.Error, Message);

            m_Communication.PostExceptionEvent(ComplexParts.Clamping, Message);
        }

        #endregion

        private const int REQUEST_DELAY_MS = 2000;
        private const int ROOM_TEMP = 25;
    }
}