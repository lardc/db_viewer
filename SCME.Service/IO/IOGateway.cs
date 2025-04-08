using System;
using SCME.Service.Properties;
using SCME.Types;

namespace SCME.Service.IO
{
    internal class IOGateway
    {
        private readonly IOAdapter m_IOAdapter;
        private readonly BroadcastCommunication m_Communication;
        private readonly ThreadService m_Thread = null;
        private readonly bool m_IsGatewayEmulation;
        private readonly ushort m_Node;
        internal readonly ComplexSafety m_SafetyType = ComplexSafety.None;
        private readonly bool?[] m_ButtonStates = { null, null, null, null };
        private DeviceConnectionState m_ConnectionState;
        private volatile bool m_PermissionGatewayScan = true;

        private bool Sensor1
        {
            get
            {
                //состояния концевых датчиков и кнопок опрашивает m_Thread. поэтому если он работает - в m_ButtonStates[] будут актуальные данные, иначе надо прочитать запрошенные данные из соответствующего регистра
                bool NeedRead = false;

                if (m_Thread == null)
                    NeedRead = true;
                else
                {
                    if (!m_Thread.IsRunning)
                        NeedRead = true;
                }

                if ((NeedRead) || (m_ButtonStates[0] == null))
                    m_ButtonStates[0] = ReadRegister(REG_SENSOR1, true) != 0;

                return (bool)m_ButtonStates[0];
            }
        }

        private bool Sensor2
        {
            get
            {
                bool NeedRead = false;

                if (m_Thread == null)
                    NeedRead = true;
                else
                {
                    if (!m_Thread.IsRunning)
                        NeedRead = true;
                }

                if ((NeedRead) || (m_ButtonStates[1] == null))
                    m_ButtonStates[1] = ReadRegister(REG_SENSOR2, true) != 0;

                return (bool)m_ButtonStates[1];
            }
        }

        private bool Sensor3
        {
            get
            {
                bool NeedRead = false;

                if (m_Thread == null)
                    NeedRead = true;
                else
                {
                    if (!m_Thread.IsRunning)
                        NeedRead = true;
                }

                if ((NeedRead) || (m_ButtonStates[2] == null))
                    m_ButtonStates[2] = ReadRegister(REG_SENSOR3, true) != 0;

                return (bool)m_ButtonStates[2];
            }
        }

        private bool Sensor4
        {
            get
            {
                bool NeedRead = false;

                if (m_Thread == null)
                    NeedRead = true;
                else
                {
                    if (!m_Thread.IsRunning)
                        NeedRead = true;
                }

                if ((NeedRead) || (m_ButtonStates[3] == null))
                    m_ButtonStates[3] = ReadRegister(REG_SENSOR4, true) != 0;

                return (bool)m_ButtonStates[3];
            }
        }

        private bool m_SafetyOn = false;

        internal IOGateway(IOAdapter Adapter, BroadcastCommunication Communication)
        {
            m_IOAdapter = Adapter;
            m_Communication = Communication;
            m_IsGatewayEmulation = Settings.Default.GatewayEmulation;
            m_Node = (ushort)Settings.Default.GatewayNode;

            if (!Enum.TryParse(Settings.Default.SafetyType, out m_SafetyType))
                SystemHost.Journal.AppendLog(ComplexParts.Gateway, LogMessageType.Error, String.Format("Unrecognised value on config parameter '{0}' ", "SafetyType"));

            m_Thread = new ThreadService();
            m_Thread.FinishedHandler += Thread_FinishedHandler;

            SystemHost.Journal.AppendLog(ComplexParts.Gateway, LogMessageType.Info,
                                         String.Format("Gateway created. Emulation mode: {0}", m_IsGatewayEmulation));
        }

        internal DeviceConnectionState Initialize()
        {
            m_ConnectionState = DeviceConnectionState.ConnectionInProcess;
            FireConnectionEvent(m_ConnectionState, "Gateway initializing");

            if (m_IsGatewayEmulation)
            {
                m_ConnectionState = DeviceConnectionState.ConnectionSuccess;
                FireConnectionEvent(m_ConnectionState, "Gateway initialized");

                return m_ConnectionState;
            }

            try
            {
                ReadRegister(REG_DEVICE_STATE);

                m_Thread.StartCycle(ThreadWorker, REQUEST_DELAY_MS);

                m_ConnectionState = DeviceConnectionState.ConnectionSuccess;
                FireConnectionEvent(m_ConnectionState, "Gateway initialized");
            }
            catch (Exception ex)
            {
                m_ConnectionState = DeviceConnectionState.ConnectionFailed;

                FireConnectionEvent(m_ConnectionState, String.Format("Gateway initialization error: {0}", ex.Message));
            }

            return m_ConnectionState;
        }

        internal void Deinitialize()
        {
            var oldState = m_ConnectionState;

            m_ConnectionState = DeviceConnectionState.DisconnectionInProcess;
            FireConnectionEvent(DeviceConnectionState.DisconnectionInProcess, "Gateway disconnecting");

            if (!m_IsGatewayEmulation && oldState == DeviceConnectionState.ConnectionSuccess)
                m_Thread.StopCycle(true);

            m_ConnectionState = DeviceConnectionState.DisconnectionSuccess;
            FireConnectionEvent(DeviceConnectionState.DisconnectionSuccess, "Gateway disconnected");
        }

        #region Standart API

        internal void ClearFault()
        {
            SystemHost.Journal.AppendLog(ComplexParts.Gate, LogMessageType.Note, "Gateway fault cleared");

            CallAction(ACT_CLEAR_FAULT);
        }


        internal ushort ReadRegister(ushort Address, bool SkipJournal = false)
        {
            ushort value = 0;

            if (!m_IsGatewayEmulation)
                value = m_IOAdapter.Read16(m_Node, Address);

            if (!SkipJournal)
                SystemHost.Journal.AppendLog(ComplexParts.Gateway, LogMessageType.Note,
                                         string.Format("Gateway @ReadRegister, address {0}, value {1}", Address, value));

            return value;
        }

        internal void WriteRegister(ushort Address, ushort Value, bool SkipJournal = false)
        {
            if (!SkipJournal)
                SystemHost.Journal.AppendLog(ComplexParts.Gateway, LogMessageType.Note,
                                         string.Format("Gateway @WriteRegister, address {0}, value {1}", Address, Value));

            if (m_IsGatewayEmulation)
                return;

            m_IOAdapter.Write16(m_Node, Address, Value);
        }

        internal void CallAction(ushort Action, bool SkipJournal = false)
        {
            if (!SkipJournal)
                SystemHost.Journal.AppendLog(ComplexParts.Gateway, LogMessageType.Note,
                                         string.Format("Gateway @Call, action {0}", Action));

            if (m_IsGatewayEmulation)
                return;

            m_IOAdapter.Call(m_Node, Action);
        }

        #endregion

        internal bool GetButtonState(ComplexButtons Button)
        {
            switch (Button)
            {
                case ComplexButtons.ButtonSC1:
                    return m_IsGatewayEmulation || Sensor1;

                case ComplexButtons.ButtonSC2:
                    return m_IsGatewayEmulation || Sensor2;

                case ComplexButtons.ButtonStart:
                    return !m_IsGatewayEmulation && Sensor3;

                case ComplexButtons.ButtonStop:
                    return !m_IsGatewayEmulation && Sensor4;

                default:
                    throw new ArgumentOutOfRangeException("Button");
            }
        }

        internal void SetLamps(bool Lamp1, bool Lamp2, bool Lamp3)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Gateway, LogMessageType.Note,
                                         String.Format("Set lamps: lamp 1 - {0}, lamp 2 - {1}, lamp 3 - {2}", Lamp1,
                                                       Lamp2, Lamp3));

            if (m_IsGatewayEmulation)
                return;

            WriteRegister(REG_LAMP1, (ushort)(Lamp1 ? 1 : 0));
            WriteRegister(REG_LAMP2, (ushort)(Lamp2 ? 1 : 0));
            WriteRegister(REG_LAMP3, (ushort)(Lamp3 ? 1 : 0));
        }

        internal void SetGreenLed(bool value)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Gateway, LogMessageType.Note, string.Format("Set green led: {0}", value));

            if (m_IsGatewayEmulation)
                return;

            WriteRegister(REG_LAMP1, (ushort)(value ? 1 : 0));
        }

        internal void SetRedLed(bool value)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Gateway, LogMessageType.Note, string.Format("Set red led: {0}", value));

            if (m_IsGatewayEmulation)
                return;

            WriteRegister(REG_LAMP2, (ushort)(value ? 1 : 0));
        }

        internal void SetPermissionToScan(bool PermissionToScan)
        {
            //SCTU очень критично к загрузке CAN шины, поэтому понадобилась возможность обеспечения тишины на шине CAN
            m_PermissionGatewayScan = PermissionToScan;
        }

        private void ThreadWorker()
        {
            //если нельзя опрашивать Gateway - то и делать нечего. SCTU очень критично к загрузке CAN шины, поэтому понадобилась возможность обеспечения тишины на шине CAN
            if (!m_PermissionGatewayScan)
                return;

            var devState = (Types.Gateway.HWDeviceState)ReadRegister(REG_DEVICE_STATE, true);

            if (!m_PermissionGatewayScan)
                return;

            var disableReason = (Types.Gateway.HWDisableReason)ReadRegister(REG_DISABLE_REASON, true);

            if (!m_PermissionGatewayScan)
                return;

            var warning = (Types.Gateway.HWWarningReason)ReadRegister(REG_WARNING, true);

            if (!m_PermissionGatewayScan)
                return;

            var faultReason = (Types.Gateway.HWFaultReason)ReadRegister(REG_FAULT_REASON, true);

            if (!m_PermissionGatewayScan)
                return;

            bool nButton1 = ReadRegister(REG_SENSOR1, true) != 0;

            if (!m_PermissionGatewayScan)
                return;

            bool nButton2 = ReadRegister(REG_SENSOR2, true) != 0;

            if (!m_PermissionGatewayScan)
                return;

            bool nButton3 = ReadRegister(REG_SENSOR3, true) != 0;

            if (!m_PermissionGatewayScan)
                return;

            bool nButton4 = ReadRegister(REG_SENSOR4, true) != 0;

            if (!m_PermissionGatewayScan)
                return;

            bool Sensor1Changed = (nButton1 != m_ButtonStates[0]);
            if (Sensor1Changed)
                m_ButtonStates[0] = nButton1;

            bool Sensor2Changed = (nButton2 != m_ButtonStates[1]);
            if (Sensor2Changed)
                m_ButtonStates[1] = nButton2;

            bool Sensor3Changed = (nButton3 != m_ButtonStates[2]);
            if (Sensor3Changed)
                m_ButtonStates[2] = nButton3;

            bool Sensor4Changed = (nButton4 != m_ButtonStates[3]);
            if (Sensor4Changed)
                m_ButtonStates[3] = nButton4;

            if (warning != Types.Gateway.HWWarningReason.None)
            {
                FireNotificationEvent(warning, Types.Gateway.HWFaultReason.None, Types.Gateway.HWDisableReason.None);

                if (!m_PermissionGatewayScan)
                    return;

                CallAction(ACT_CLEAR_WARNING, true);
            }

            if (devState == Types.Gateway.HWDeviceState.Fault)
            {
                FireNotificationEvent(Types.Gateway.HWWarningReason.None, faultReason, Types.Gateway.HWDisableReason.None);

                throw new Exception(string.Format("Device is in fault state, reason - {0}", faultReason));
            }

            if (devState == Types.Gateway.HWDeviceState.Disabled)
            {
                FireNotificationEvent(Types.Gateway.HWWarningReason.None, Types.Gateway.HWFaultReason.None, disableReason);

                throw new Exception(string.Format("Device is in disabled state, reason - {0}", disableReason));
            }

            if (m_SafetyType == ComplexSafety.Mechanical)
            {
                //если тип защиты есть механическая шторка - надо оповестить верхний уровень (UI) об изменении состояний концевиков шторки 
                if (Sensor1Changed)
                {
                    //различаем ситуации: 
                    //                   - зафиксировано изменение состояния концевого датчика во время выполнения теста;
                    //                   - во время ожидания начала теста
                    switch (m_SafetyOn)
                    {
                        case true:
                            //датчик изменил своё состояние во время выполнения теста
                            bool Alarm = !nButton1;
                            FireSafetyEvent(Alarm, ComplexButtons.ButtonSC1);
                            break;

                        default:
                            //датчик сработал во время ожидания начала теста - UI отреагирует только видимостью иконки
                            FireButtonEvent(ComplexButtons.ButtonSC1, nButton1);
                            break;
                    }
                }

                if (Sensor2Changed)
                {
                    //различаем ситуации:
                    //                   - зафиксировано изменение состояния концевого датчика во время выполнения теста;
                    //                   - во время ожидания начала теста
                    switch (m_SafetyOn)
                    {
                        case true:
                            //датчик изменил своё состояние во время выполнения теста
                            bool Alarm = !nButton2;
                            FireSafetyEvent(Alarm, ComplexButtons.ButtonSC2);
                            break;

                        default:
                            //датчик сработал во время ожидания начала теста - UI отреагирует только видимостью иконки
                            FireButtonEvent(ComplexButtons.ButtonSC2, nButton2);
                            break;
                    }
                }
            }

            if (Sensor3Changed)
                FireButtonEvent(ComplexButtons.ButtonStart, nButton3);

            if (Sensor4Changed)
            {
                switch (m_SafetyType)
                {
                    //в случае SCTU шторка подключена как аппаратная кнопка 'Стоп', поэтому уведомляем UI о срабатывании шторки и/или кнопки 'Стоп' только когда выполняется тест, т.е. контролировать состояние кнопки 'Стоп' и шторки безопасности до начала теста и после его окончания нельзя - ибо пользователь даже при установке прибора будет видеть сообщения о нажатой кнопки 'Стоп'. естественно, что событие срабатывания шторки и событие изменения состояния кнопки 'Стоп' для системы при этом не различимы
                    case (ComplexSafety.AsButtonStop):
                        if (m_SafetyOn)
                            FireButtonEvent(ComplexButtons.ButtonStop, nButton4);

                        break;

                    //по умолчанию на всех установках контроль именно кнопки 'Стоп' выполняется до, во время и после теста, а шторка безопасности контролируется только во время теста и события срабатывания шторки безопасности и кнопки 'Стоп' это абсолютно разные события
                    default:
                        FireButtonEvent(ComplexButtons.ButtonStop, nButton4);
                        break;
                }
            }
        }

        private void Thread_FinishedHandler(object Sender, ThreadFinishedEventArgs E)
        {
            if (E.Error != null)
                FireExceptionEvent(E.Error.Message);
        }

        private void FireConnectionEvent(DeviceConnectionState State, string Message)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Gateway, LogMessageType.Info, Message);
            m_Communication.PostDeviceConnectionEvent(ComplexParts.Gateway, State, Message);
        }

        private void FireNotificationEvent(Types.Gateway.HWWarningReason Warning, Types.Gateway.HWFaultReason Fault,
                                           Types.Gateway.HWDisableReason Disable)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Gateway, LogMessageType.Warning,
                                         string.Format(
                                             "Gateway device notification: warning {0}, fault {1}, disable {2}",
                                             Warning, Fault, Disable));

            m_Communication.PostGatewayNotificationEvent(Warning, Fault, Disable);
        }

        private void FireExceptionEvent(string Message)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Gateway, LogMessageType.Error, Message);
            m_Communication.PostExceptionEvent(ComplexParts.Gateway, Message);
        }

        private void FireButtonEvent(ComplexButtons Button, bool State)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Gateway, LogMessageType.Info, string.Format("Gateway button {0}, state {1}", Button, State));
            m_Communication.PostButtonPressedEvent(Button, State);
        }

        internal void SetSafetyOn()
        {
            //установка признака 'тест начался' для случая, когда применяется механический датчик безопасности или SCTU шторка безопасности
            switch (m_SafetyType)
            {
                case (ComplexSafety.Mechanical):
                    m_SafetyOn = true;
                    SystemHost.Journal.AppendLog(ComplexParts.Gateway, LogMessageType.Info, "Mechanical safety system set to on");
                    break;

                case (ComplexSafety.AsButtonStop):
                    m_SafetyOn = true;
                    SystemHost.Journal.AppendLog(ComplexParts.Gateway, LogMessageType.Info, "AsButtonStop safety system set to on");
                    break;
            }
        }

        internal void SetSafetyOff()
        {
            //сброс признака 'тест начался' для случая, когда применяется механический датчик безопасности или SCTU шторка безопасности
            switch (m_SafetyType)
            {
                case (ComplexSafety.Mechanical):
                    m_SafetyOn = false;
                    SystemHost.Journal.AppendLog(ComplexParts.Gateway, LogMessageType.Info, "Mechanical safety system set to off");
                    break;

                case (ComplexSafety.AsButtonStop):
                    m_SafetyOn = false;
                    SystemHost.Journal.AppendLog(ComplexParts.Gateway, LogMessageType.Info, "AsButtonStop safety system set to off");
                    break;
            }
        }

        internal void ProvocationButtonResponse(ComplexButtons Button)
        {
            //провоцируем срабатывание кнопки Button 
            switch (Button)
            {
                //требуется провоцировать срабатывание только кнопки "Стоп" и только ради случая SCTU из-за подключения её системы безопасности как аппаратной кнопки "Стоп"
                case (ComplexButtons.ButtonStop):
                    bool nButton4 = ReadRegister(REG_SENSOR4, true) != 0;
                    FireButtonEvent(ComplexButtons.ButtonStop, nButton4);
                    SystemHost.Journal.AppendLog(ComplexParts.Gateway, LogMessageType.Info, "Provocation button 'Stop' response");
                    break;
            }
        }

        private void FireSafetyEvent(bool Alarm, ComplexButtons Button)
        {
            //уведомляем UI о наступлении события срабатывания механической шторки или SCTU шторки
            switch (m_SafetyType)
            {
                case (ComplexSafety.Mechanical):
                    SystemHost.Journal.AppendLog(ComplexParts.Commutation, LogMessageType.Info, string.Format("Mechanical safety system alarm={0}. Button {1}", Alarm, Button));
                    m_Communication.PostSafetyEvent(Alarm, ComplexSafety.Mechanical, Button);
                    break;

                case (ComplexSafety.AsButtonStop):
                    SystemHost.Journal.AppendLog(ComplexParts.Commutation, LogMessageType.Info, string.Format("AsButtonStop safety system alarm={0}. Button {1}", Alarm, Button));
                    m_Communication.PostSafetyEvent(Alarm, ComplexSafety.AsButtonStop, Button);
                    break;
            }
        }

        #region Registers

        private const ushort
            ACT_CLEAR_FAULT = 3,
            ACT_CLEAR_WARNING = 4,
            REG_LAMP1 = 128,
            REG_LAMP2 = 129,
            REG_LAMP3 = 130,
            REG_SENSOR1 = 197,
            REG_SENSOR2 = 198,
            REG_SENSOR3 = 199,
            REG_SENSOR4 = 200,
            REG_DEVICE_STATE = 192,
            REG_FAULT_REASON = 193,
            REG_DISABLE_REASON = 194,
            REG_WARNING = 195;

        private const int REQUEST_DELAY_MS = 100;

        #endregion
    }
}