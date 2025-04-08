using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Threading;
using SCME.Service.IO;
using SCME.Service.Properties;
using SCME.Types;
using SCME.Types.BaseTestParams;
using SCME.Types.Commutation;
using SCME.Types.Profiles;
using SCME.Types.SCTU;

namespace SCME.Service
{
    internal class LogicContainer
    {
        private readonly BroadcastCommunication m_Communication;

        private readonly IOAdapter m_IOAdapter;
        private readonly IOGateway m_IOGateway;
        private readonly IOCommutation m_IOCommutation, m_IOCommutationEx;
        private readonly IOGate m_IOGate;
        private readonly IOStLs m_IOStls;
        private readonly IOBvt m_IOBvt;
        private readonly IOClamping m_IOClamping;
        private readonly IOdVdt m_IOdVdt;
        private readonly IOATU m_IOAtu;
        private readonly IOQrrTq m_IOQrrTq;
        private readonly IORAC m_IORAC;
        private readonly IOIH m_IOIH;
        private readonly IORCC m_IORCC;
        private readonly IoSctu _ioSctu;
        private readonly ThreadService m_Thread;
        private readonly bool m_ClampingSystemConnected;

        private Types.Gate.TestParameters m_ParametersGate;
        private Types.SL.TestParameters m_ParametersSL;
        private Types.BVT.TestParameters m_ParametersBvt;
        private Types.dVdt.TestParameters m_ParametersdVdt;
        private Types.ATU.TestParameters m_ParametersAtu;
        private Types.QrrTq.TestParameters m_ParametersQrrTq;
        private Types.RAC.TestParameters m_ParametersRac;
        private Types.IH.TestParameters m_ParametersIH;
        private Types.RCC.TestParameters m_ParametersRCC;

        private Types.Gate.TestParameters[] m_ParametersGateDyn;
        private Types.SL.TestParameters[] m_ParametersSLDyn;
        private Types.BVT.TestParameters[] m_ParametersBvtDyn;
        private Types.dVdt.TestParameters[] m_ParametersdVdtDyn;
        private Types.ATU.TestParameters[] m_ParametersAtuDyn;
        private Types.QrrTq.TestParameters[] m_ParametersQrrTqDyn;
        private Types.RAC.TestParameters[] m_ParametersRacDyn;
        private SctuTestParameters[] _sctuTestParameters;

        private TypeCommon.InitParams m_Param;
        private DeviceState m_State = DeviceState.None;
        private DeviceConnectionState m_ConnectionState = DeviceConnectionState.None;
        private Boolean m_Stop;
        private ComplexSafety m_SafetyType;

        public LogicContainer(BroadcastCommunication Communication)
        {
            m_ClampingSystemConnected = Settings.Default.IsClampingSystemConnected;

            if (!Enum.TryParse(Settings.Default.SafetyType, out m_SafetyType))
                SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Error, "LogicContainer. Unrecognised value on config parameter 'SafetyType'.");

            m_Communication = Communication;

            m_Thread = new ThreadService();
            m_Thread.FinishedHandler += Thread_FinishedHandler;

            m_ParametersGate = new Types.Gate.TestParameters { IsEnabled = false };
            m_ParametersSL = new Types.SL.TestParameters { IsEnabled = false };
            m_ParametersBvt = new Types.BVT.TestParameters { IsEnabled = false };
            m_ParametersdVdt = new Types.dVdt.TestParameters { IsEnabled = false };
            m_ParametersAtu = new Types.ATU.TestParameters { IsEnabled = false };
            m_ParametersQrrTq = new Types.QrrTq.TestParameters { IsEnabled = false };
            m_ParametersRac = new Types.RAC.TestParameters { IsEnabled = false };
            m_ParametersIH = new Types.IH.TestParameters { IsEnabled = false };
            m_ParametersRCC = new Types.RCC.TestParameters { IsEnabled = false };

            m_IOAdapter = new IOAdapter(m_Communication);
            m_IOGateway = new IOGateway(m_IOAdapter, m_Communication);
            m_IOCommutation = new IOCommutation(m_IOAdapter, m_Communication, Settings.Default.CommutationEmulation,
                                                Settings.Default.CommutationNode, Settings.Default.IsCommutationType6, ComplexParts.Commutation);
            m_IOCommutationEx = new IOCommutation(m_IOAdapter, m_Communication, Settings.Default.CommutationExEmulation,
                                                  Settings.Default.CommutationExNode,
                                                  Settings.Default.IsCommutationExType6, ComplexParts.CommutationEx);

            m_IOClamping = new IOClamping(m_IOAdapter, m_Communication);
            m_IOGate = new IOGate(m_IOAdapter, m_Communication);
            m_IOStls = new IOStLs(m_IOAdapter, m_Communication);
            m_IOBvt = new IOBvt(m_IOAdapter, m_Communication);
            m_IOdVdt = new IOdVdt(m_IOAdapter, m_Communication);
            m_IOAtu = new IOATU(m_IOAdapter, m_Communication);
            m_IOQrrTq = new IOQrrTq(m_IOAdapter, m_Communication);
            m_IORAC = new IORAC(m_IOAdapter, m_Communication);
            m_IOIH = new IOIH(m_IOGate, m_IOStls, m_Communication);
            m_IORCC = new IORCC(m_IOGate, m_Communication);
            _ioSctu = new IoSctu(m_IOAdapter, m_Communication);

            m_IOGate.ActiveCommutation = m_IOCommutation;
            m_IOStls.ActiveCommutation = m_IOCommutation;
            m_IOBvt.ActiveCommutation = m_IOCommutation;
            m_IOdVdt.ActiveCommutation = m_IOCommutation;
            m_IOClamping.ActiveCommutation = m_IOCommutation;
            m_IOAtu.ActiveCommutation = m_IOCommutation;
            m_IOQrrTq.ActiveCommutation = m_IOCommutation;
            m_IORAC.ActiveCommutation = m_IOCommutation;
            m_IOIH.ActiveCommutation = m_IOCommutation;
            m_IORCC.ActiveCommutation = m_IOCommutation;
        }

        void Thread_FinishedHandler(object Sender, ThreadFinishedEventArgs E)
        {
            if (E.Error != null)
                m_Communication.PostExceptionEvent(ComplexParts.Service, E.Error.Message);
        }

        internal void Initialize(TypeCommon.InitParams Params)
        {
            try
            {
                Deinitialize();
            }
            catch (Exception ex)
            {
                SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Error, string.Format(Resources.Error_LogicContainer_Exception, ex.Message));
                m_Communication.PostCommonConnectionEvent(DeviceConnectionState.DisconnectionError, ex.Message);

                return;
            }

            m_Param = Params;
            var state = DeviceConnectionState.ConnectionInProcess;

            SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Info,
                                         string.Format(Resources.Log_LogicContainer_Requested_block_modes, m_Param.IsGateEnabled, m_Param.IsSLEnabled, m_Param.IsBVTEnabled, m_Param.IsClampEnabled, m_Param.IsdVdtEnabled, m_Param.IsATUEnabled, m_Param.IsQrrTqEnabled, m_Param.IsRACEnabled, m_Param.IsIHEnabled));

            try
            {
                state = m_IOAdapter.Initialize(m_Param.TimeoutAdapter);

                if (state == DeviceConnectionState.ConnectionSuccess)
                    state = m_IOGateway.Initialize();

                if (state == DeviceConnectionState.ConnectionSuccess)
                    state = m_IOCommutation.Initialize();

                if (state == DeviceConnectionState.ConnectionSuccess)
                    state = m_IOCommutationEx.Initialize();

                if (state == DeviceConnectionState.ConnectionSuccess)
                    state = m_IOGate.Initialize(m_Param.IsGateEnabled, m_Param.TimeoutGate);

                if (state == DeviceConnectionState.ConnectionSuccess)
                    state = m_IOStls.Initialize(m_Param.IsSLEnabled, m_Param.TimeoutSL);

                if (state == DeviceConnectionState.ConnectionSuccess)
                    state = m_IOBvt.Initialize(m_Param.IsBVTEnabled, m_Param.TimeoutBVT);

                if (state == DeviceConnectionState.ConnectionSuccess && m_ClampingSystemConnected)
                    state = m_IOClamping.Initialize(m_Param.IsClampEnabled, m_Param.TimeoutClamp);

                if (state == DeviceConnectionState.ConnectionSuccess)
                    state = m_IOdVdt.Initialize(m_Param.IsdVdtEnabled, m_Param.TimeoutdVdt);

                if (state == DeviceConnectionState.ConnectionSuccess)
                    state = m_IOAtu.Initialize(m_Param.IsATUEnabled, m_Param.TimeoutATU);

                if (state == DeviceConnectionState.ConnectionSuccess)
                    state = m_IOQrrTq.Initialize(m_Param.IsQrrTqEnabled, m_Param.TimeoutQrrTq);

                if (state == DeviceConnectionState.ConnectionSuccess)
                    state = m_IORAC.Initialize(m_Param.IsRACEnabled, m_Param.TimeoutRAC);

                if (state == DeviceConnectionState.ConnectionSuccess)
                    state = m_IOIH.Initialize(m_Param.IsIHEnabled, m_Param.TimeoutIH);

                if (state == DeviceConnectionState.ConnectionSuccess)
                    state = m_IORCC.Initialize(m_Param.IsRCCEnabled, m_Param.TimeoutRCC);

                if (state == DeviceConnectionState.ConnectionSuccess)
                    state = _ioSctu.Initialize(m_Param.IsSctuEnabled, m_Param.TimeoutSctu);

                //при инициализации оборудования необходимо включить зеленый светодиод (запись 1 в регистр 128 gateway)
                if (state == DeviceConnectionState.ConnectionSuccess)
                    m_IOGateway.SetGreenLed(true);

                SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Info,
                string.Format(Resources.Log_LogicContainer_Connection_state,
                    state == DeviceConnectionState.ConnectionSuccess
                        ? Resources.Log_LogicContainer_Connected
                        : Resources.Log_LogicContainer_Failed));
            }
            catch (Exception ex)
            {
                SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Error, string.Format(Resources.Error_LogicContainer_Exception, ex.Message));
            }

            m_ConnectionState = state;
            m_Communication.PostCommonConnectionEvent(state, state == DeviceConnectionState.ConnectionSuccess ? Resources.Log_LogicContainer_Connected : Resources.Log_LogicContainer_Failed);

            IsInitialized = true;
        }

        internal void Deinitialize()
        {
            if (m_ConnectionState != DeviceConnectionState.ConnectionSuccess && m_ConnectionState != DeviceConnectionState.ConnectionFailed)
                return;

            if (m_ConnectionState == DeviceConnectionState.ConnectionInProcess)
                throw new ApplicationException("Initialization is in process");

            //завершаем работу потока
            m_Thread.StopCycle(true);

            Exception savedEx = null;

            try
            {
                m_IOdVdt.Deinitialize();
            }
            catch (Exception ex)
            {
                var message = string.Format(Resources.Error_LogicContainer_Exception, ex.Message);
                SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Error, message);
                savedEx = ex;
            }

            try
            {
                if (Settings.Default.IsClampingSystemConnected)
                    m_IOClamping.Deinitialize();
            }
            catch (Exception ex)
            {
                var message = string.Format(Resources.Error_LogicContainer_Exception, ex.Message);
                SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Error, message);
                savedEx = ex;
            }

            try
            {
                m_IOBvt.Deinitialize();
            }
            catch (Exception ex)
            {
                var message = string.Format(Resources.Error_LogicContainer_Exception, ex.Message);
                SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Error, message);
                savedEx = ex;
            }

            try
            {
                m_IOStls.Deinitialize();
            }
            catch (Exception ex)
            {
                var message = string.Format(Resources.Error_LogicContainer_Exception, ex.Message);
                SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Error, message);
                savedEx = ex;
            }

            try
            {
                m_IOGate.Deinitialize();
            }
            catch (Exception ex)
            {
                var message = string.Format(Resources.Error_LogicContainer_Exception, ex.Message);
                SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Error, message);
                savedEx = ex;
            }

            try
            {
                m_IOCommutation.Deinitialize();
            }
            catch (Exception ex)
            {
                var message = string.Format(Resources.Error_LogicContainer_Exception, ex.Message);
                SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Error, message);
                savedEx = ex;
            }

            try
            {
                m_IOCommutationEx.Deinitialize();
            }
            catch (Exception ex)
            {
                var message = string.Format(Resources.Error_LogicContainer_Exception, ex.Message);
                SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Error, message);
                savedEx = ex;
            }

            try
            {
                m_IOGateway.Deinitialize();
            }
            catch (Exception ex)
            {
                var message = string.Format(Resources.Error_LogicContainer_Exception, ex.Message);
                SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Error, message);
                savedEx = ex;
            }

            try
            {
                m_IOAdapter.Deinitialize();
            }
            catch (Exception ex)
            {
                var message = string.Format(Resources.Error_LogicContainer_Exception, ex.Message);
                SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Error, message);
                savedEx = ex;
            }

            try
            {
                m_IOAtu.Deinitialize();
            }
            catch (Exception ex)
            {
                var message = string.Format(Resources.Error_LogicContainer_Exception, ex.Message);
                SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Error, message);
                savedEx = ex;
            }

            try
            {
                m_IOQrrTq.Deinitialize();
            }
            catch (Exception ex)
            {
                var message = string.Format(Resources.Error_LogicContainer_Exception, ex.Message);
                SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Error, message);
                savedEx = ex;
            }

            try
            {
                m_IORAC.Deinitialize();
            }
            catch (Exception ex)
            {
                var message = string.Format(Resources.Error_LogicContainer_Exception, ex.Message);
                SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Error, message);
                savedEx = ex;
            }

            try
            {
                m_IOIH.Deinitialize();
            }
            catch (Exception ex)
            {
                var message = string.Format(Resources.Error_LogicContainer_Exception, ex.Message);
                SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Error, message);
                savedEx = ex;
            }

            try
            {
                m_IORCC.Deinitialize();
            }
            catch (Exception ex)
            {
                var message = string.Format(Resources.Error_LogicContainer_Exception, ex.Message);
                SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Error, message);
                savedEx = ex;
            }

            try
            {
                _ioSctu.Deinitialize();
            }
            catch (Exception ex)
            {
                var message = string.Format(Resources.Error_LogicContainer_Exception, ex.Message);
                SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Error, message);
                savedEx = ex;
            }

            GC.Collect();

            SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Info, Resources.Log_LogicContainer_All_disconnected);

            if (savedEx != null)
                m_Communication.PostCommonConnectionEvent(DeviceConnectionState.DisconnectionError, string.Format(Resources.Error_LogicContainer_Exception, savedEx.Message));

            IsInitialized = false;
        }

        internal bool IsInitialized { get; private set; }

        private void SetSafetyState(IOCommutation Commutation, bool Safety)
        {
            try
            {
                if (Safety)
                {
                    switch (m_SafetyType)
                    {
                        //случай оптической шторки
                        case (ComplexSafety.Optical):
                            Commutation.SetSafetyOn();
                            break;

                        //случай механической шторки и ни на что не похожей шторки в установке ударного тока - оптическая шторка, подключенная как аппаратная кнопка 'Стоп'                           
                        case (ComplexSafety.Mechanical):
                        case (ComplexSafety.AsButtonStop):
                            m_IOGateway.SetSafetyOn();
                            break;
                    }

                    //при включении мониторинга состояния шторки безопасности нужно выключить зеленый светодиод и включить красный
                    m_IOGateway.SetGreenLed(false);
                    m_IOGateway.SetRedLed(true);
                }
                else
                {
                    switch (m_SafetyType)
                    {
                        //случай оптической шторки
                        case (ComplexSafety.Optical):
                            Commutation.SetSafetyOff();
                            break;

                        //случай механической шторки и шторки установленной в установке ударного тока
                        case (ComplexSafety.Mechanical):
                        case (ComplexSafety.AsButtonStop):
                            m_IOGateway.SetSafetyOff();
                            break;
                    }

                    //при выключении мониторинга состояния шторки безопасности (завершения процесса измерения) нужно включить зеленый светодиод и выключить красный светодиод
                    m_IOGateway.SetGreenLed(true);
                    m_IOGateway.SetRedLed(false);
                }
            }
            catch (Exception ex)
            {
                switch (m_SafetyType)
                {
                    case (ComplexSafety.Optical):
                        ThrowFaultException(ComplexParts.Commutation, ex.ToString(), String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name), false);

                        break;

                    case (ComplexSafety.Mechanical):
                    case (ComplexSafety.AsButtonStop):
                        ThrowFaultException(ComplexParts.Gateway, ex.ToString(), String.Format("LogicContainer.{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name), false);
                        break;
                }
            }
        }

        public void ClearSafetyTrig()
        {
            //сброс состояния SafetyTrig
            switch (m_SafetyType)
            {
                case (ComplexSafety.Optical):
                    //определяемся где мы имеем состояние SafetyTrig, чтобы его сбросить
                    if (m_IOCommutation.IsSafetyAlarm())
                    {
                        //сбрасываем состояние SafetyTrig
                        SetSafetyState(m_IOCommutation, false);
                    }

                    break;

                case (ComplexSafety.Mechanical):
                case (ComplexSafety.AsButtonStop):
                    SetSafetyState(null, false);
                    break;
            }

            //сбрасываем флаг m_Stop ибо он был установлен в реализации Stop(), которая вызывалась с уровня UI при обработке события срабатывания шторки
            m_Stop = false;

            //сбрасываем состояние Halt пресса. Если пресс стоял в состоянии Halt, то данный вызов изменит его состояние на Ready
            m_IOClamping.Clear_Halt();
        }

        public void SafetySystemOn()
        {
            //включение системы безопасности
            if (m_IOCommutation != null)
                SetSafetyState(m_IOCommutation, true);
        }

        public void SafetySystemOff()
        {
            //выключение системы безопасности
            if (m_IOCommutation != null)
                SetSafetyState(m_IOCommutation, false);
        }

        public string NotReadyDevicesToStart()
        {
            //опрашиваем состояние измерительных блоков на предмет их готовности и формируем строку из имён не готовых блоков
            string res = "";

            if (m_ParametersGate.IsEnabled && m_Param.IsGateEnabled)
            {
                if (!m_IOGate.IsReadyToStart())
                    res = "Gate";
            }

            if (m_ParametersSL.IsEnabled && m_Param.IsSLEnabled)
            {
                if (!m_IOStls.IsReadyToStart())
                {
                    if (res != "")
                        res = res + ", ";

                    res = res + "SL";
                }
            }

            if (m_ParametersBvt.IsEnabled && m_Param.IsBVTEnabled)
            {
                if (!m_IOBvt.IsReadyToStart())
                {
                    if (res != "")
                        res = res + ", ";

                    res = res + "Bvt";
                }
            }

            if (m_ParametersAtu.IsEnabled && m_Param.IsATUEnabled)
            {
                if (!m_IOAtu.IsReadyToStart())
                {
                    if (res != "")
                        res = res + ", ";

                    res = res + "Atu";
                }
            }

            if (m_ParametersQrrTq.IsEnabled && m_Param.IsQRREnabled)
            {
                if (!m_IOQrrTq.IsReadyToStart())
                {
                    if (res != "")
                        res = res + ", ";

                    res = res + "QrrTq";
                }
            }

            if (m_ParametersRac.IsEnabled && m_Param.IsRACEnabled)
            {
                if (!m_IORAC.IsReadyToStart())
                {
                    if (res != "")
                        res = res + ", ";

                    res = res + "RAC";
                }
            }

            if (m_ParametersIH.IsEnabled && m_Param.IsIHEnabled)
            {
                if (!m_IOIH.IsReadyToStart())
                {
                    if (res != "")
                        res = res + ", ";

                    res = res + "IH";
                }
            }

            if (m_ParametersRCC.IsEnabled && m_Param.IsRCCEnabled)
            {
                if (!m_IORCC.IsReadyToStart())
                {
                    if (res != "")
                        res = res + ", ";

                    res = res + "RCC";
                }
            }

            return res;
        }

        public string NotReadyDevicesToStartDynamic()
        {
            string res = "";

            var orderedParameters = new List<BaseTestParametersAndNormatives>(m_ParametersGateDyn.Length + m_ParametersSLDyn.Length + m_ParametersBvtDyn.Length + m_ParametersdVdtDyn.Length + m_ParametersAtuDyn.Length + m_ParametersQrrTqDyn.Length + m_ParametersRacDyn.Length + _sctuTestParameters.Length);
            orderedParameters.AddRange(m_ParametersGateDyn);
            orderedParameters.AddRange(m_ParametersSLDyn);
            orderedParameters.AddRange(m_ParametersBvtDyn);
            orderedParameters.AddRange(m_ParametersdVdtDyn);
            orderedParameters.AddRange(m_ParametersAtuDyn);
            orderedParameters.AddRange(m_ParametersQrrTqDyn);
            orderedParameters.AddRange(m_ParametersRacDyn);
            orderedParameters.AddRange(_sctuTestParameters);
            orderedParameters = orderedParameters.OrderBy(o => o.Order).ToList();

            foreach (var baseTestParametersAndNormativese in orderedParameters)
            {
                var gateParams = baseTestParametersAndNormativese as Types.Gate.TestParameters;
                if (!ReferenceEquals(gateParams, null))
                    if (!m_IOGate.IsReadyToStart())
                        res = "Gate";

                var slParameters = baseTestParametersAndNormativese as Types.SL.TestParameters;
                if (!ReferenceEquals(slParameters, null))
                    if (!m_IOStls.IsReadyToStart())
                    {
                        if (res != "")
                            res = res + ", ";

                        res = res + "SL";
                    }

                var bvtParameters = baseTestParametersAndNormativese as Types.BVT.TestParameters;
                if (!ReferenceEquals(bvtParameters, null))
                {
                    if (!m_IOBvt.IsReadyToStart())
                    {
                        if (res != "")
                            res = res + ", ";

                        res = res + "Bvt";
                    }
                }

                var dvDtParameters = baseTestParametersAndNormativese as Types.dVdt.TestParameters;
                if (!ReferenceEquals(dvDtParameters, null))
                {
                    if (!m_IOdVdt.IsReadyToStart())
                    {
                        if (res != "")
                            res = res + ", ";

                        res = res + "dVdt";
                    }
                }

                var AtuParameters = baseTestParametersAndNormativese as Types.ATU.TestParameters;
                if (!ReferenceEquals(AtuParameters, null))
                {
                    if (!m_IOAtu.IsReadyToStart())
                    {
                        if (res != "")
                            res = res + ", ";

                        res = res + "Atu";
                    }
                }

                var QrrTqParameters = baseTestParametersAndNormativese as Types.QrrTq.TestParameters;
                if (!ReferenceEquals(QrrTqParameters, null))
                {
                    if (!m_IOQrrTq.IsReadyToStart())
                    {
                        if (res != "")
                            res = res + ", ";

                        res = res + "QrrTq";
                    }
                }

                var RACParameters = baseTestParametersAndNormativese as Types.RAC.TestParameters;
                if (!ReferenceEquals(RACParameters, null))
                {
                    if (!m_IORAC.IsReadyToStart())
                    {
                        if (res != "")
                            res = res + ", ";

                        res = res + "RAC";
                    }
                }
            }

            return res;
        }

        private bool buttonStateByGateway(ComplexButtons Button)
        {
            try
            {
                return m_IOGateway.GetButtonState(Button);
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.Gateway, ex.ToString(), String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name), false);
            }

            return false;
        }

        private void ProvocationButtonResponseByGateway(ComplexButtons Button)
        {
            try
            {
                m_IOGateway.ProvocationButtonResponse(Button);
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.Gateway, ex.ToString(), String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name), false);
            }
        }

        private bool buttonStateByCommutation()
        {
            try
            {
                return m_IOCommutation.GetButtonState();
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.Commutation, ex.ToString(), String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name), false);
            }

            return false;
        }

        private bool buttonStateByClamping(ComplexButtons Button)
        {
            try
            {
                return m_IOClamping.GetButtonState(Button);
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.Clamping, ex.ToString(), String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name), false);
            }

            return false;
        }

        internal bool GetButtonState(ComplexButtons Button)
        {
            switch (Button)
            {
                //датчик срабатывания шторки безопасности
                case ComplexButtons.ButtonSC1:
                    switch (m_SafetyType)
                    {
                        case ComplexSafety.None:
                            return true;

                        case ComplexSafety.Mechanical:
                        case ComplexSafety.AsButtonStop:
                            return buttonStateByGateway(Button);

                        case ComplexSafety.Optical:
                            return buttonStateByCommutation();

                        default:
                            string message = String.Format("Realization does not know how to process safetyType={0}.", m_SafetyType);
                            ThrowFaultException(ComplexParts.Service, message, String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name), false);
                            break;
                    }

                    break;

                case ComplexButtons.None:
                case ComplexButtons.ButtonStart:
                case ComplexButtons.ButtonStop:
                case ComplexButtons.ButtonSC2:
                case ComplexButtons.ButtonStartFTDI:
                case ComplexButtons.ButtonStopFTDI:
                    return buttonStateByGateway(Button);

                case ComplexButtons.ClampSlidingSensor:
                    return buttonStateByClamping(Button);

                default:
                    string mess = String.Format("Realization does not know how to process Button={0}.", Button.ToString());
                    ThrowFaultException(ComplexParts.Service, mess, String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name), false);
                    break;
            }

            return false;
        }

        internal void ProvocationButtonResponse(ComplexButtons Button)
        {
            switch (Button)
            {
                case (ComplexButtons.ButtonStop):
                    ProvocationButtonResponseByGateway(Button);
                    break;
            }
        }

        internal ComplexSafety GetSafetyType()
        {
            return m_SafetyType;
        }

        #region Test sequence

        public bool Start(Types.Gate.TestParameters ParametersGate, Types.SL.TestParameters ParametersSL, Types.BVT.TestParameters ParametersBvt, Types.ATU.TestParameters ParametersAtu, Types.QrrTq.TestParameters ParametersQrrTq, Types.RAC.TestParameters ParametersRac, Types.IH.TestParameters ParametersIH, Types.RCC.TestParameters ParametersRCC, Types.Commutation.TestParameters ParametersComm, Types.Clamping.TestParameters ParametersClamp)
        {
            m_Stop = false;

            if (m_State == DeviceState.InProcess)
                ThrowFaultException(ComplexParts.Service, "Test is already started", String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name), false);

            IOCommutation m_IOActiveCommutation = null;

            if (ParametersComm.BlockIndex == Types.Commutation.HWBlockIndex.Block1)
                m_IOActiveCommutation = m_IOCommutation;
            else m_IOActiveCommutation = m_IOCommutationEx;

            m_IOGate.ActiveCommutation = m_IOActiveCommutation;
            m_IOStls.ActiveCommutation = m_IOActiveCommutation;
            m_IOBvt.ActiveCommutation = m_IOActiveCommutation;
            m_IOClamping.ActiveCommutation = m_IOActiveCommutation;
            m_IOAtu.ActiveCommutation = m_IOActiveCommutation;
            m_IOQrrTq.ActiveCommutation = m_IOActiveCommutation;
            m_IORAC.ActiveCommutation = m_IOActiveCommutation;
            m_IOIH.ActiveCommutation = m_IOActiveCommutation;
            m_IORCC.ActiveCommutation = m_IOActiveCommutation;

            m_ParametersGate = ParametersGate;
            m_ParametersSL = ParametersSL;
            m_ParametersBvt = ParametersBvt;
            m_ParametersAtu = ParametersAtu;
            m_ParametersQrrTq = ParametersQrrTq;
            m_ParametersRac = ParametersRac;
            m_ParametersIH = ParametersIH;
            m_ParametersRCC = ParametersRCC;

            m_State = DeviceState.InProcess;

            var message = string.Format("Start main test, state {0}; test enabled: Gate - {1}, SL, - {2}, BVT - {3}, ATU - {4}, QrrTq - {5}, RAC - {6}, IH - {7}, RCC - {8}",
                                        m_State, m_ParametersGate.IsEnabled, m_ParametersSL.IsEnabled, m_ParametersBvt.IsEnabled, m_ParametersAtu.IsEnabled, m_ParametersQrrTq.IsEnabled, m_ParametersRac.IsEnabled, m_ParametersIH.IsEnabled, m_ParametersRCC.IsEnabled);
            SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Info, message);

            m_Communication.PostTestAllEvent(m_State, "Starting tests");

            try
            {
                m_Thread.StartSingle(Dummy => MeasurementLogicRoutine(ParametersComm, ParametersClamp));
            }
            catch (Exception e)
            {
                ThrowFaultException(ComplexParts.None, e.Message, String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name), false);
            }

            return true;
        }

        public bool Start(TestParameters parametersCommutation, Types.Clamping.TestParameters parametersClamp, Types.Gate.TestParameters[] parametersGate, Types.SL.TestParameters[] parametersSl, Types.BVT.TestParameters[] parametersBvt, Types.dVdt.TestParameters[] parametersDvDt, Types.ATU.TestParameters[] parametersAtu, Types.QrrTq.TestParameters[] parametersQrrTq, Types.RAC.TestParameters[] parametersRac, SctuTestParameters[] parametersSctu)
        {
            m_Stop = false;

            //наладчик в принципе не может запускать данную реализацию, но забыть включить систему безопасности он может, поэтому спасём оператора
            SafetySystemOn();

            if (m_State == DeviceState.InProcess)
                ThrowFaultException(ComplexParts.Service, "Test is already started", String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name), false);

            IOCommutation m_IOActiveCommutation = null;

            if (parametersCommutation.BlockIndex == HWBlockIndex.Block1)
                m_IOActiveCommutation = m_IOCommutation;
            else m_IOActiveCommutation = m_IOCommutationEx;

            m_IOGate.ActiveCommutation = m_IOActiveCommutation;
            m_IOStls.ActiveCommutation = m_IOActiveCommutation;
            m_IOBvt.ActiveCommutation = m_IOActiveCommutation;
            m_IOdVdt.ActiveCommutation = m_IOActiveCommutation;
            m_IOAtu.ActiveCommutation = m_IOCommutation;
            m_IOQrrTq.ActiveCommutation = m_IOCommutation;
            m_IORAC.ActiveCommutation = m_IOCommutation;
            m_IOClamping.ActiveCommutation = m_IOActiveCommutation;

            m_ParametersGateDyn = parametersGate;
            m_ParametersBvtDyn = parametersBvt;
            m_ParametersSLDyn = parametersSl;
            m_ParametersdVdtDyn = parametersDvDt;
            m_ParametersAtuDyn = parametersAtu;
            m_ParametersQrrTqDyn = parametersQrrTq;
            m_ParametersRacDyn = parametersRac;
            _sctuTestParameters = parametersSctu;

            m_State = DeviceState.InProcess;

            var message = string.Format("Start main test, state {0}; test enabled:", m_State);
            SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Info, message);

            m_Communication.PostTestAllEvent(m_State, "Starting tests");

            try
            {
                m_Thread.StartSingle(Dummy => MeasurementDynamicLogicRoutine(parametersCommutation, parametersClamp));
            }
            catch (Exception e)
            {
                ThrowFaultException(ComplexParts.None, e.Message, String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name), false);
            }

            return true;
        }

        public bool StartHeating(int temperature)
        {
            m_Communication.PostTestAllEvent(DeviceState.Heating, "Starting heating");

            try
            {
                m_Thread.StartSingle(Dummy => HeatingRoutine(temperature));
            }
            catch (Exception e)
            {
                ThrowFaultException(ComplexParts.None, e.Message, String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name), false);
            }

            return true;
        }

        public void StopHeating()
        {
            m_IOClamping.StopHeating();
        }

        public void SetPermissionToUseCanDataBus(bool PermissionToUseCanDataBus)
        {
            //управление разрешением использовать шину CAN фоновыми потоками Clamping (пресса) и Gateway (шлюза)
            try
            {
                if (m_IOClamping != null)
                    m_IOClamping.SetPermissionToScan(PermissionToUseCanDataBus);
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.Clamping, ex.Message, "m_IOClamping.SetPermissionScan");
            }

            try
            {
                if (m_IOGateway != null)
                    m_IOGateway.SetPermissionToScan(PermissionToUseCanDataBus);
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.Clamping, ex.Message, "m_IOGateway.SetPermissionScan");
            }
        }

        private void AfterUnsqueezeRoutine()
        {
            //реализация того, что должно выполнятся после разжатия пресса
            try
            {
                SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Info, "Start after unsqueeze routine");

                var orderedParameters = new List<BaseTestParametersAndNormatives>(m_ParametersGateDyn.Length + m_ParametersSLDyn.Length + m_ParametersBvtDyn.Length + m_ParametersdVdtDyn.Length + m_ParametersAtuDyn.Length + m_ParametersQrrTqDyn.Length + _sctuTestParameters.Length);
                orderedParameters.AddRange(m_ParametersGateDyn);
                orderedParameters.AddRange(m_ParametersSLDyn);
                orderedParameters.AddRange(m_ParametersBvtDyn);
                orderedParameters.AddRange(m_ParametersdVdtDyn);
                orderedParameters.AddRange(m_ParametersAtuDyn);
                orderedParameters.AddRange(m_ParametersQrrTqDyn);
                orderedParameters.AddRange(_sctuTestParameters);
                orderedParameters = orderedParameters.OrderBy(o => o.Order).ToList();

                foreach (var baseTestParametersAndNormativese in orderedParameters)
                {
                    var sctuParameters = baseTestParametersAndNormativese as SctuTestParameters;

                    if (!ReferenceEquals(sctuParameters, null))
                        _ioSctu.SCTUWaitReady();
                }
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.Sctu, ex.Message, "AfterUnsqueezeRoutine");
            }
        }

        private void MeasurementDynamicLogicRoutine(TestParameters parametersCommutation, Types.Clamping.TestParameters parametersClamp)
        {
            var res = DeviceState.Success;

            IOCommutation m_IOActiveCommutation = null;

            if (parametersCommutation.BlockIndex == HWBlockIndex.Block1)
                m_IOActiveCommutation = m_IOCommutation;
            else m_IOActiveCommutation = m_IOCommutationEx;

            SetSafetyState(m_IOCommutation, true);

            try
            {
                try
                {
                    if (m_ClampingSystemConnected && m_Param.IsClampEnabled && !m_Stop)
                    {
                        //пресс может оказаться в Halt не только потому, что данный софт выдал ему команду аварийной остановки, но и потому, что он был остановлен аппаратно, поэтому проверим, не стоит ли пресс в состоянии Halt и если это так - сбросим это состояние. если этого не делать - пресс в состоянии Halt не будет исполнять команду зажатия
                        try
                        {
                            m_IOClamping.CheckAndClearHalt();
                        }
                        catch (Exception ex)
                        {
                            ThrowFaultException(ComplexParts.Clamping, ex.Message, "Reset clamp Halt state (m_IOClamping.CheckAndClearHalt)");
                        }

                        try
                        {
                            res = m_IOClamping.Squeeze(parametersClamp);
                        }
                        catch (Exception ex)
                        {
                            ThrowFaultException(ComplexParts.Clamping, ex.Message, "Start squeezing (m_IOClamping.Squeeze)");
                        }
                    }

                    if (res == DeviceState.Success)
                    {
                        var orderedParameters = new List<BaseTestParametersAndNormatives>(m_ParametersGateDyn.Length + m_ParametersSLDyn.Length + m_ParametersBvtDyn.Length + m_ParametersdVdtDyn.Length + m_ParametersAtuDyn.Length + m_ParametersQrrTqDyn.Length + m_ParametersRacDyn.Length + _sctuTestParameters.Length);
                        orderedParameters.AddRange(m_ParametersGateDyn);
                        orderedParameters.AddRange(m_ParametersSLDyn);
                        orderedParameters.AddRange(m_ParametersBvtDyn);
                        orderedParameters.AddRange(m_ParametersdVdtDyn);
                        orderedParameters.AddRange(m_ParametersAtuDyn);
                        orderedParameters.AddRange(m_ParametersQrrTqDyn);
                        orderedParameters.AddRange(m_ParametersRacDyn);
                        orderedParameters.AddRange(_sctuTestParameters);
                        orderedParameters = orderedParameters.OrderBy(o => o.Order).ToList();

                        foreach (var baseTestParametersAndNormativese in orderedParameters)
                        {
                            if (m_Stop) continue;

                            var gateParams = baseTestParametersAndNormativese as Types.Gate.TestParameters;
                            if (!ReferenceEquals(gateParams, null))
                                try
                                {
                                    if (m_ClampingSystemConnected && m_Param.IsClampEnabled && !m_Stop)
                                        m_IOClamping.ReturnForceToDefault();

                                    res = m_IOGate.Start(gateParams, parametersCommutation);
                                }
                                catch (Exception ex)
                                {
                                    ThrowFaultException(ComplexParts.Gate, ex.Message, "Start Gate test");
                                }

                            var slParameters = baseTestParametersAndNormativese as Types.SL.TestParameters;
                            if (!ReferenceEquals(slParameters, null))
                                try
                                {
                                    if (m_ClampingSystemConnected && m_Param.IsClampEnabled && !m_Stop)
                                        m_IOClamping.SetCustomForce();

                                    res = m_IOStls.Start(slParameters, parametersCommutation);
                                }
                                catch (Exception ex)
                                {
                                    ThrowFaultException(ComplexParts.SL, ex.Message, "Start SL test");
                                }

                            var bvtParameters = baseTestParametersAndNormativese as Types.BVT.TestParameters;
                            if (!ReferenceEquals(bvtParameters, null))
                            {
                                try
                                {
                                    if (m_ClampingSystemConnected && m_Param.IsClampEnabled && !m_Stop)
                                        m_IOClamping.ReturnForceToDefault();

                                    res = m_IOBvt.Start(bvtParameters, parametersCommutation);
                                }
                                catch (Exception ex)
                                {
                                    ThrowFaultException(ComplexParts.BVT, ex.Message, "Start BVT test");
                                }
                            }

                            var dvDtParameters = baseTestParametersAndNormativese as Types.dVdt.TestParameters;
                            if (!ReferenceEquals(dvDtParameters, null))
                            {
                                try
                                {
                                    if (m_ClampingSystemConnected && m_Param.IsClampEnabled && !m_Stop)
                                        m_IOClamping.SetCustomForce();

                                    res = m_IOdVdt.Start(dvDtParameters, parametersCommutation);
                                }
                                catch (Exception ex)
                                {
                                    ThrowFaultException(ComplexParts.BVT, ex.Message, "Start dVdt test");
                                }
                            }

                            var atuParameters = baseTestParametersAndNormativese as Types.ATU.TestParameters;
                            if (!ReferenceEquals(atuParameters, null))
                            {
                                try
                                {
                                    if (m_ClampingSystemConnected && m_Param.IsClampEnabled && !m_Stop)
                                        m_IOClamping.SetCustomForce();

                                    res = m_IOAtu.Start(atuParameters, parametersCommutation);
                                }
                                catch (Exception ex)
                                {
                                    ThrowFaultException(ComplexParts.ATU, ex.Message, "Start ATU test");
                                }
                            }

                            var qrrTqParameters = baseTestParametersAndNormativese as Types.QrrTq.TestParameters;
                            if (!ReferenceEquals(qrrTqParameters, null))
                            {
                                try
                                {
                                    if (m_ClampingSystemConnected && m_Param.IsClampEnabled && !m_Stop)
                                        m_IOClamping.SetCustomForce();

                                    res = m_IOQrrTq.Start(qrrTqParameters, parametersCommutation);
                                }
                                catch (Exception ex)
                                {
                                    ThrowFaultException(ComplexParts.QrrTq, ex.Message, "Start QrrTq test");
                                }
                            }

                            var racParameters = baseTestParametersAndNormativese as Types.RAC.TestParameters;
                            if (!ReferenceEquals(racParameters, null))
                            {
                                try
                                {
                                    if (m_ClampingSystemConnected && m_Param.IsClampEnabled && !m_Stop)
                                        m_IOClamping.SetCustomForce();

                                    res = m_IORAC.Start(racParameters, parametersCommutation);
                                }
                                catch (Exception ex)
                                {
                                    ThrowFaultException(ComplexParts.RAC, ex.Message, "Start RAC test");
                                }
                            }

                            var sctuParameters = baseTestParametersAndNormativese as SctuTestParameters;
                            if (!ReferenceEquals(sctuParameters, null))
                            {
                                try
                                {
                                    if (m_ClampingSystemConnected && m_Param.IsClampEnabled && !m_Stop)
                                        m_IOClamping.ReturnForceToDefault();

                                    _ioSctu.Start(sctuParameters, m_IOClamping, m_IOGateway);
                                }
                                catch (Exception ex)
                                {
                                    ThrowFaultException(ComplexParts.Sctu, ex.Message, "Start SCTU test");
                                }
                            }
                        }
                    }
                }
                finally
                {
                    try
                    {
                        try
                        {
                            if (m_ClampingSystemConnected && m_Param.IsClampEnabled && !parametersClamp.IsHeightMeasureEnabled)
                                res = m_IOClamping.Unsqueeze(parametersClamp);
                        }
                        finally
                        {
                            //после разжатия пресса выполняем ряд действий, которые необходимо выполнить именно после его разжатия
                            AfterUnsqueezeRoutine();
                        }

                        if (parametersClamp.IsHeightMeasureEnabled)
                            SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Info, "Height measure enabled");
                    }
                    catch (Exception ex)
                    {
                        ThrowFaultException(ComplexParts.Clamping, ex.Message, "Start unsqueezing");
                    }
                }
            }
            finally
            {
                SetSafetyState(m_IOActiveCommutation, false);
            }

            var msg = string.Format("Main testing, state - {0}", res);
            SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Info, msg);

            m_State = DeviceState.Success;
            m_Communication.PostTestAllEvent(m_State, "Tests are done");
        }

        private void HeatingRoutine(int temperature)
        {
            try
            {
                var res = DeviceState.Heating;

                if ((m_ClampingSystemConnected || m_IOClamping.m_IsClampingEmulation) && m_Param.IsClampEnabled && !m_Stop)
                    res = m_IOClamping.StartHeating(temperature);

                var msg = string.Format("Heating, state - {0}", res);
                SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Info, msg);
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.Clamping, ex.Message, "Start heating");
            }
        }

        private void MeasurementLogicRoutine(Types.Commutation.TestParameters ParametersComm, Types.Clamping.TestParameters ParametersClamp)
        {
            var res = DeviceState.Success;

            IOCommutation m_IOActiveCommutation = null;

            if (ParametersComm.BlockIndex == Types.Commutation.HWBlockIndex.Block1)
                m_IOActiveCommutation = m_IOCommutation;
            else m_IOActiveCommutation = m_IOCommutationEx;

            SetSafetyState(m_IOCommutation, true);

            try
            {
                try
                {
                    try
                    {
                        if (m_ClampingSystemConnected && m_Param.IsClampEnabled && !m_Stop)
                            res = m_IOClamping.Squeeze(ParametersClamp);
                    }
                    catch (Exception ex)
                    {
                        ThrowFaultException(ComplexParts.Clamping, ex.Message, "Start squeezing");
                    }

                    if (res == DeviceState.Success)
                    {
                        Types.RCC.RCCResult resultRCC = Types.RCC.RCCResult.OPRESULT_NONE;

                        if (m_ParametersRCC.IsEnabled && m_Param.IsRCCEnabled && !m_Stop)
                        {
                            try
                            {
                                m_IORCC.Start(m_ParametersRCC, ParametersComm, out resultRCC);
                            }
                            catch (Exception ex)
                            {
                                ThrowFaultException(ComplexParts.RCC, ex.Message, "Start RCC test");
                            }
                        }

                        bool RCCTestPassed = (resultRCC == Types.RCC.RCCResult.OPRESULT_OK);

                        //если RCC показало замыкание цепи катод-катод - прибор бракованный и дальше нет смысла проверять его другими тестами
                        if (RCCTestPassed || !(m_ParametersRCC.IsEnabled && m_Param.IsRCCEnabled))
                        {
                            //в цепи катод-катод нет КЗ, либо данный тест не исполнялся - в обоих указанных случаях имеет смысл проводить тесты данного изделия дальше
                            bool DefaultForce = false;

                            if (m_ParametersGate.IsEnabled && m_Param.IsGateEnabled && !m_Stop)
                            {
                                try
                                {
                                    //чтобы двигатель зажимного не перегревался - зажимаем прибор перед измерениями Gate (только для Gate это допускается) не с тем усилием, что задано в профиле, а с Default значением 
                                    if (m_ClampingSystemConnected && m_Param.IsClampEnabled && !m_Stop)
                                    {
                                        m_IOClamping.ReturnForceToDefault();
                                        DefaultForce = true;
                                    }

                                    res = m_IOGate.Start(m_ParametersGate, ParametersComm);
                                }
                                catch (Exception ex)
                                {
                                    ThrowFaultException(ComplexParts.Gate, ex.Message, "Start Gate test");
                                }
                            }

                            //если усилие зажатия было default - дожимаем зажимное до заданного в профиле значения усилия зажатия
                            if (DefaultForce && m_ClampingSystemConnected && m_Param.IsClampEnabled && !m_Stop)
                                m_IOClamping.SetCustomForce();

                            if (m_ParametersSL.IsEnabled && m_Param.IsSLEnabled && !m_Stop)
                            {
                                try
                                {
                                    res = m_IOStls.Start(m_ParametersSL, ParametersComm);
                                }
                                catch (Exception ex)
                                {
                                    ThrowFaultException(ComplexParts.SL, ex.Message, "Start SL test");
                                }
                            }

                            if (m_ParametersBvt.IsEnabled && m_Param.IsBVTEnabled && !m_Stop)
                            {
                                try
                                {
                                    res = m_IOBvt.Start(m_ParametersBvt, ParametersComm);
                                }
                                catch (Exception ex)
                                {
                                    ThrowFaultException(ComplexParts.BVT, ex.Message, "Start BVT test");
                                }
                            }

                            if (m_ParametersAtu.IsEnabled && m_Param.IsATUEnabled && !m_Stop)
                            {
                                try
                                {
                                    res = m_IOAtu.Start(m_ParametersAtu, ParametersComm);
                                }
                                catch (Exception ex)
                                {
                                    ThrowFaultException(ComplexParts.ATU, ex.Message, "Start ATU test");
                                }
                            }

                            if (m_ParametersQrrTq.IsEnabled && m_Param.IsQrrTqEnabled && !m_Stop)
                            {
                                try
                                {
                                    res = m_IOQrrTq.Start(m_ParametersQrrTq, ParametersComm);
                                }
                                catch (Exception ex)
                                {
                                    ThrowFaultException(ComplexParts.QrrTq, ex.Message, "Start QrrTq test");
                                }
                            }

                            if (m_ParametersRac.IsEnabled && m_Param.IsRACEnabled && !m_Stop)
                            {
                                try
                                {
                                    res = m_IORAC.Start(m_ParametersRac, ParametersComm);
                                }
                                catch (Exception ex)
                                {
                                    ThrowFaultException(ComplexParts.RAC, ex.Message, "Start RAC test");
                                }
                            }

                            if (m_ParametersIH.IsEnabled && m_Param.IsIHEnabled && !m_Stop)
                            {
                                try
                                {
                                    res = m_IOIH.Start(m_ParametersIH, ParametersComm);
                                }
                                catch (Exception ex)
                                {
                                    ThrowFaultException(ComplexParts.IH, ex.Message, "Start IH test");
                                }
                            }
                        }
                    }
                }
                finally
                {
                    try
                    {
                        if (m_ClampingSystemConnected && m_Param.IsClampEnabled && !ParametersClamp.IsHeightMeasureEnabled)
                            res = m_IOClamping.Unsqueeze(ParametersClamp);

                        if (ParametersClamp.IsHeightMeasureEnabled)
                            SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Info, "Height measure enabled");
                    }
                    catch (Exception ex)
                    {
                        ThrowFaultException(ComplexParts.Clamping, ex.Message, "Start unsqueezing");
                    }
                }
            }

            finally
            {
                SetSafetyState(m_IOActiveCommutation, false);
            }

            var msg = string.Format("Main testing, state - {0}", res);
            SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Info, msg);

            m_State = DeviceState.Success;
            m_Communication.PostTestAllEvent(m_State, "Tests are done");
        }


        #endregion


        internal void Stop()
        {
            m_Stop = true;

            if (m_ParametersGate.IsEnabled)
                m_IOGate.Stop();

            if (m_ParametersSL.IsEnabled)
                m_IOStls.Stop();

            if (m_ParametersBvt.IsEnabled)
                m_IOBvt.Stop();

            if (m_ClampingSystemConnected)
                m_IOClamping.Stop();

            if (m_ParametersdVdt.IsEnabled)
                m_IOdVdt.Stop();

            if (m_ParametersAtu.IsEnabled)
                m_IOAtu.Stop();

            if (m_ParametersQrrTq.IsEnabled)
                m_IOQrrTq.Stop();

            if (m_ParametersRac.IsEnabled)
                m_IORAC.Stop();

            if (m_ParametersIH.IsEnabled)
                m_IOIH.Stop();

            SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Info, Resources.Log_LogicContainer_Main_test_manual_stop);
        }

        internal void StopByButtonStop()
        {
            //данный метод вызывать по факту нажатия аппаратной или экранной кнопки 'Стоп'
            Stop();

            //уведомляем UI о прошедшем вызове Stop
            FireStopEvent();
        }

        internal void Squeeze(Types.Clamping.TestParameters ClampingParameters)
        {
            try
            {
                m_Thread.StartSingle(Dummy => m_IOClamping.Squeeze(ClampingParameters, false));
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.Clamping, ex.ToString(), String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name), false);
            }
        }

        internal void Unsqueeze(Types.Clamping.TestParameters ClampingParameters)
        {
            try
            {
                if (m_ClampingSystemConnected && m_Param.IsClampEnabled)
                    m_Thread.StartSingle(Dummy => m_IOClamping.Unsqueeze(ClampingParameters));
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.Clamping, ex.ToString(), String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name), false);
            }
        }

        internal void WriteResults(ResultItem Item, List<string> Errors)
        {
            try
            {
                ThreadPool.QueueUserWorkItem(delegate
                    {
                        try
                        {
                            SystemHost.Results.WriteResult(Item, Errors);
                        }
                        catch (Exception ex)
                        {
                            SystemHost.Journal.AppendLog(ComplexParts.Database, LogMessageType.Error, ex.Message);
                        }
                    });
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.Database, ex.ToString(), String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name), false);
            }
        }

        internal void SaveProfiles(List<ProfileItem> profileItems)
        {
            try
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    try
                    {
                        SystemHost.Results.SaveProfiles(profileItems, Settings.Default.MMECode);
                    }
                    catch (Exception ex)
                    {
                        SystemHost.Journal.AppendLog(ComplexParts.Database, LogMessageType.Error, ex.Message);
                    }
                });
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.Database, ex.ToString(), String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name), false);
            }
        }

        #region Standart API
        internal ushort ActivationWorkPlace(ComplexParts Device, ChannelByClumpType ChByClumpType, SctuWorkPlaceActivationStatuses ActivationStatus)
        {
            ushort res = 0;

            try
            {
                switch (Device)
                {
                    case ComplexParts.Sctu:
                        res = _ioSctu.ActivationWorkPlace(ChByClumpType, ActivationStatus);
                        break;
                }
            }
            catch (Exception ex)
            {
                ThrowFaultException(Device, ex.Message, String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }

            return res;
        }

        internal ushort ReadRegister(ComplexParts Device, ushort Address)
        {
            ushort res = 0;

            try
            {
                switch (Device)
                {
                    case ComplexParts.Gateway:
                        res = m_IOGateway.ReadRegister(Address);
                        break;
                    case ComplexParts.Commutation:
                        res = m_IOCommutation.ReadRegister(Address);
                        break;
                    case ComplexParts.CommutationEx:
                        res = m_IOCommutationEx.ReadRegister(Address);
                        break;
                    case ComplexParts.Gate:
                        res = m_IOGate.ReadRegister(Address);
                        break;
                    case ComplexParts.SL:
                        res = m_IOStls.ReadRegister(Address);
                        break;
                    case ComplexParts.BVT:
                        res = m_IOBvt.ReadRegister(Address);
                        break;
                    case ComplexParts.Clamping:
                        res = m_IOClamping.ReadRegister(Address);
                        break;
                    case ComplexParts.DvDt:
                        res = m_IOdVdt.ReadRegister(Address);
                        break;
                    case ComplexParts.ATU:
                        res = m_IOAtu.ReadRegister(Address);
                        break;
                    case ComplexParts.QrrTq:
                        res = m_IOQrrTq.ReadRegister(Address);
                        break;
                    case ComplexParts.RAC:
                        res = m_IORAC.ReadRegister(Address);
                        break;
                    case ComplexParts.IH:
                        //блок IH физически не существует
                        break;

                    case ComplexParts.Sctu:
                        res = _ioSctu.ReadRegister(Address);
                        break;

                    //если обработка для Device не предусмотрена
                    default: throw new Exception(string.Format("Processing for Device={0} is not specified", Device.ToString()));
                }
            }
            catch (Exception ex)
            {
                ThrowFaultException(Device, ex.Message, String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }

            return res;
        }

        internal void WriteRegister(ComplexParts Device, ushort Address, ushort Value)
        {
            try
            {
                switch (Device)
                {
                    case ComplexParts.Gateway:
                        m_IOGateway.WriteRegister(Address, Value);
                        break;
                    case ComplexParts.Commutation:
                        m_IOCommutation.WriteRegister(Address, Value);
                        break;
                    case ComplexParts.CommutationEx:
                        m_IOCommutationEx.WriteRegister(Address, Value);
                        break;
                    case ComplexParts.Gate:
                        m_IOGate.WriteRegister(Address, Value);
                        break;
                    case ComplexParts.SL:
                        m_IOStls.WriteRegister(Address, Value);
                        break;
                    case ComplexParts.BVT:
                        m_IOBvt.WriteRegister(Address, Value);
                        break;
                    case ComplexParts.Clamping:
                        m_IOClamping.WriteRegister(Address, Value);
                        break;
                    case ComplexParts.DvDt:
                        m_IOdVdt.WriteRegister(Address, Value);
                        break;
                    case ComplexParts.ATU:
                        m_IOAtu.WriteRegister(Address, Value);
                        break;
                    case ComplexParts.QrrTq:
                        m_IOQrrTq.WriteRegister(Address, Value);
                        break;
                    case ComplexParts.RAC:
                        m_IORAC.WriteRegister(Address, Value);
                        break;
                    case ComplexParts.IH:
                        //блок IH физически не существует
                        break;
                    case ComplexParts.Sctu:
                        _ioSctu.WriteRegister(Address, Value);
                        break;

                    //если обработка для Device не предусмотрена
                    default: throw new Exception(string.Format("Processing for Device={0} is not specified", Device.ToString()));
                }
            }
            catch (Exception ex)
            {
                ThrowFaultException(Device, ex.Message, String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }
        }

        internal void CallAction(ComplexParts Device, ushort Address)
        {
            try
            {
                switch (Device)
                {
                    case ComplexParts.Commutation:
                        m_IOCommutation.CallAction(Address);
                        break;
                    case ComplexParts.CommutationEx:
                        m_IOCommutationEx.CallAction(Address);
                        break;
                    case ComplexParts.Gate:
                        m_IOGate.CallAction(Address);
                        break;
                    case ComplexParts.SL:
                        m_IOStls.CallAction(Address);
                        break;
                    case ComplexParts.BVT:
                        m_IOBvt.CallAction(Address);
                        break;
                    case ComplexParts.Clamping:
                        m_IOClamping.CallAction(Address);
                        break;
                    case ComplexParts.DvDt:
                        m_IOdVdt.CallAction(Address);
                        break;
                    case ComplexParts.ATU:
                        m_IOAtu.CallAction(Address);
                        break;
                    case ComplexParts.QrrTq:
                        m_IOQrrTq.CallAction(Address);
                        break;
                    case ComplexParts.RAC:
                        m_IORAC.CallAction(Address);
                        break;
                    case ComplexParts.IH:
                        //блок IH физически не существует
                        break;

                    case ComplexParts.Sctu:
                        _ioSctu.CallAction(Address);
                        break;

                    //если обработка для Device не предусмотрена
                    default: throw new Exception(string.Format("Processing for Device={0} if not specified", Device));
                }
            }
            catch (Exception ex)
            {
                ThrowFaultException(Device, ex.Message, String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }
        }

        internal void ClearFault(ComplexParts Device)
        {
            try
            {
                switch (Device)
                {
                    case ComplexParts.Gateway:
                        m_IOGateway.ClearFault();
                        break;
                    case ComplexParts.Commutation:
                        m_IOCommutation.ClearFault();
                        break;
                    case ComplexParts.CommutationEx:
                        m_IOCommutationEx.ClearFault();
                        break;
                    case ComplexParts.Gate:
                        m_IOGate.ClearFault();
                        break;
                    case ComplexParts.SL:
                        m_IOStls.ClearFault();
                        break;
                    case ComplexParts.BVT:
                        m_IOBvt.ClearFault();
                        break;
                    case ComplexParts.Clamping:
                        m_IOClamping.ClearFault();
                        break;
                    case ComplexParts.DvDt:
                        m_IOdVdt.ClearFault();
                        break;
                    case ComplexParts.ATU:
                        m_IOAtu.ClearFault();
                        break;
                    case ComplexParts.QrrTq:
                        m_IOQrrTq.ClearFault();
                        break;
                    case ComplexParts.RAC:
                        m_IORAC.ClearFault();
                        break;
                    case ComplexParts.IH:
                        m_IOIH.ClearFault();
                        break;

                    //если обработка для Device не предусмотрена
                    default: throw new Exception(string.Format("Processing for Device={0} if not specified", Device));
                }
            }
            catch (Exception ex)
            {
                ThrowFaultException(Device, ex.Message, String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }
        }

        private void FireStopEvent()
        {
            SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Info, "StopEvent");
            m_Communication.PostStopEvent();
        }

        #endregion


        #region Calibration


        internal Types.Gate.CalibrationResultGate GatePulseCalibrationGate(ushort Current)
        {
            try
            {
                var res = m_IOGate.PulseCalibrationGate(Current);

                return new Types.Gate.CalibrationResultGate { Current = res.Item1, Voltage = res.Item2 };
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.Gate, ex.Message, String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
                return new Types.Gate.CalibrationResultGate();
            }
        }


        internal ushort GatePulseCalibrationMain(ushort Current)
        {
            try
            {
                return m_IOGate.PulseCalibrationMain(Current);
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.Gate, ex.Message, String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
                return 0;
            }
        }


        internal void GateWriteCalibrationParams(Types.Gate.CalibrationParameters Parameters)
        {
            try
            {
                m_IOGate.WriteCalibrationParams(Parameters);
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.Gate, ex.Message, String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }
        }


        internal Types.Gate.CalibrationParameters GateReadCalibrationParams()
        {
            var parameters = new Types.Gate.CalibrationParameters();

            try
            {
                parameters = m_IOGate.ReadCalibrationParams();
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.Gate, ex.Message, String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }

            return parameters;
        }


        internal void SLWriteCalibrationParams(Types.SL.CalibrationParameters Parameters)
        {
            try
            {
                m_IOStls.WriteCalibrationParams(Parameters);
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.SL, ex.Message, String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }
        }


        internal Types.SL.CalibrationParameters SLReadCalibrationParams()
        {
            var parameters = new Types.SL.CalibrationParameters();

            try
            {
                parameters = m_IOStls.ReadCalibrationParams();
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.SL, ex.Message, String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }

            return parameters;
        }


        internal void BVTWriteCalibrationParams(Types.BVT.CalibrationParams Parameters)
        {
            try
            {
                m_IOBvt.WriteCalibrationParams(Parameters);
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.BVT, ex.Message, String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }
        }


        internal Types.BVT.CalibrationParams BVTReadCalibrationParams()
        {
            var parameters = new Types.BVT.CalibrationParams();

            try
            {
                parameters = m_IOBvt.ReadCalibrationParams();
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.BVT, ex.Message, String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }

            return parameters;
        }


        internal void CSWriteCalibrationParams(Types.Clamping.CalibrationParams Parameters)
        {
            try
            {
                m_IOClamping.WriteCalibrationParams(Parameters);
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.Clamping, ex.Message, String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }
        }


        internal Types.Clamping.CalibrationParams CSReadCalibrationParams()
        {
            var parameters = new Types.Clamping.CalibrationParams();

            try
            {
                parameters = m_IOClamping.ReadCalibrationParams();
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.Clamping, ex.Message, String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }

            return parameters;
        }


        internal void DvDtWriteCalibrationParams(Types.dVdt.CalibrationParams Parameters)
        {
            try
            {
                m_IOdVdt.WriteCalibrationParams(Parameters);
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.DvDt, ex.Message, String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }
        }


        internal Types.dVdt.CalibrationParams DvDtReadCalibrationParams()
        {
            var parameters = new Types.dVdt.CalibrationParams();

            try
            {
                parameters = m_IOdVdt.ReadCalibrationParams();
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.DvDt, ex.Message, String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }

            return parameters;
        }

        internal void ATUWriteCalibrationParams(Types.ATU.CalibrationParams Parameters)
        {
            try
            {
                m_IOAtu.WriteCalibrationParams(Parameters);
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.ATU, ex.Message, String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }
        }

        internal Types.ATU.CalibrationParams ATUReadCalibrationParams()
        {
            var parameters = new Types.ATU.CalibrationParams();

            try
            {
                parameters = m_IOAtu.ReadCalibrationParams();
            }
            catch (Exception ex)
            {
                ThrowFaultException(ComplexParts.DvDt, ex.Message, String.Format(@"{0}.{1}", GetType().Name, MethodBase.GetCurrentMethod().Name));
            }

            return parameters;
        }

        #endregion


        private void ThrowFaultException(ComplexParts Device, string Exception, string Func, bool SwitchToFault = true)
        {
            if (SwitchToFault)
                m_State = DeviceState.Fault;

            var message = string.Format(Resources.Error_LogicContainer_Main_testing_state_exception, m_State, Exception);
            SystemHost.Journal.AppendLog(ComplexParts.Service, LogMessageType.Error, message);

            var fd = new FaultData { Device = Device, Message = message, TimeStamp = DateTime.Now };
            throw new FaultException<FaultData>(fd, Func);
        }
    }
}