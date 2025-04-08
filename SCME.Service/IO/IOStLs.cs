using System;
using System.Collections.Generic;
using System.Threading;
using SCME.Service.Properties;
using SCME.Types;

namespace SCME.Service.IO
{
    internal class IOStLs
    {
        private const float EMU_DEFAULT_VTM = 1200.0f;
        private const ushort EMU_DEFAULT_ITM = 500;
        private const int REQUEST_DELAY_MS = 50;

        private readonly IOAdapter m_IOAdapter;
        private readonly BroadcastCommunication m_Communication;
        private readonly ushort m_Node;
        private readonly bool m_IsSLEmulationHard;
        private readonly bool m_ReadGraph;
        private IOCommutation m_IOCommutation;
        private bool m_IsSLEmulation;
        private Types.SL.TestParameters m_Parameter;
        private DeviceConnectionState m_ConnectionState;
        private volatile Types.SL.TestResults m_Result;
        private volatile DeviceState m_State;
        private volatile bool m_Stop;

        private int m_Timeout = 25000;

        internal IOStLs(IOAdapter Adapter, BroadcastCommunication Communication)
        {
            m_IOAdapter = Adapter;
            m_Communication = Communication;
            m_IsSLEmulationHard = Settings.Default.SLEmulation;
            m_IsSLEmulation = m_IsSLEmulationHard;
            m_ReadGraph = Settings.Default.SLReadGraph;

            m_Node = (ushort)Settings.Default.SLNode;
            m_Result = new Types.SL.TestResults();

            SystemHost.Journal.AppendLog(ComplexParts.SL, LogMessageType.Info,
                                         String.Format("SL created. Emulation mode: {0}", m_IsSLEmulation));
        }

        internal IOCommutation ActiveCommutation
        {
            get { return m_IOCommutation; }
            set { m_IOCommutation = value; }
        }

        internal DeviceConnectionState Initialize(bool Enable, int Timeout)
        {
            m_Timeout = Timeout;
            m_IsSLEmulation = m_IsSLEmulationHard || !Enable;

            m_ConnectionState = DeviceConnectionState.ConnectionInProcess;
            FireConnectionEvent(m_ConnectionState, "SL initializing");

            if (m_IsSLEmulation)
            {
                m_ConnectionState = DeviceConnectionState.ConnectionSuccess;

                FireConnectionEvent(m_ConnectionState, "SL initialized");

                return m_ConnectionState;
            }

            try
            {
                var timeStamp = Environment.TickCount + m_Timeout;

                ClearWarning();

                var devState = (Types.SL.HWDeviceState)ReadRegister(REG_DEVICE_STATE);
                if (devState != Types.SL.HWDeviceState.PowerReady)
                {
                    if (devState == Types.SL.HWDeviceState.Fault)
                    {
                        ClearFault();
                        Thread.Sleep(100);

                        devState = (Types.SL.HWDeviceState)ReadRegister(REG_DEVICE_STATE);

                        if (devState == Types.SL.HWDeviceState.Fault)
                            throw new Exception(string.Format("SL is in fault state, reason: {0}",
                                (Types.SL.HWFaultReason)ReadRegister(REG_FAULT_REASON)));
                    }

                    if (devState == Types.SL.HWDeviceState.Disabled)
                        throw new Exception(string.Format("SL is in disabled state, reason: {0}",
                            (Types.SL.HWDisableReason)ReadRegister(REG_DISABLE_REASON)));

                    CallAction(ACT_ENABLE_POWER);
                }

                while (Environment.TickCount < timeStamp)
                {
                    Thread.Sleep(100);

                    devState = (Types.SL.HWDeviceState)ReadRegister(REG_DEVICE_STATE, true);

                    if (devState == Types.SL.HWDeviceState.PowerReady)
                        break;

                    if (devState == Types.SL.HWDeviceState.Fault)
                        throw new Exception(string.Format("SL is in fault state, reason: {0}",
                            (Types.SL.HWFaultReason)ReadRegister(REG_FAULT_REASON)));
                    if (devState == Types.SL.HWDeviceState.Disabled)
                        throw new Exception(string.Format("SL is in disabled state, reason: {0}",
                            (Types.SL.HWDisableReason)ReadRegister(REG_DISABLE_REASON)));
                }

                if (Environment.TickCount > timeStamp)
                    throw new Exception("Timeout while waiting for device to power up");

                m_ConnectionState = DeviceConnectionState.ConnectionSuccess;

                FireConnectionEvent(DeviceConnectionState.ConnectionSuccess, "SL initialized");
            }
            catch (Exception ex)
            {
                m_ConnectionState = DeviceConnectionState.ConnectionFailed;
                FireConnectionEvent(m_ConnectionState, String.Format("SL initialization error: {0}", ex.Message));
            }

            return m_ConnectionState;
        }

        internal void Deinitialize()
        {
            var oldState = m_ConnectionState;

            m_ConnectionState = DeviceConnectionState.DisconnectionInProcess;
            FireConnectionEvent(DeviceConnectionState.DisconnectionInProcess, "SL disconnecting");

            try
            {
                if (!m_IsSLEmulation && oldState == DeviceConnectionState.ConnectionSuccess)
                {
                    Stop();
                    CallAction(ACT_DISABLE_POWER);
                }

                m_ConnectionState = DeviceConnectionState.DisconnectionSuccess;
                FireConnectionEvent(DeviceConnectionState.DisconnectionSuccess, "SL disconnected");
            }
            catch (Exception)
            {
                m_ConnectionState = DeviceConnectionState.DisconnectionError;
                FireConnectionEvent(DeviceConnectionState.DisconnectionError, "SL disconnection error");
            }
        }

        internal DeviceState Start(Types.SL.TestParameters parameters, Types.Commutation.TestParameters commParameters)
        {
            m_Parameter = parameters;

            if (m_State == DeviceState.InProcess)
                throw new Exception("SL test is already started");

            m_Result = new Types.SL.TestResults { TestTypeId = parameters.TestTypeId };
            m_Stop = false;

            ClearWarning();

            if (!m_IsSLEmulation)
            {
                var devState = (Types.SL.HWDeviceState)ReadRegister(REG_DEVICE_STATE);
                if (devState == Types.SL.HWDeviceState.Fault)
                {
                    var faultReason = (Types.SL.HWFaultReason)ReadRegister(REG_FAULT_REASON);
                    FireNotificationEvent(Types.SL.HWProblemReason.None, Types.SL.HWWarningReason.None, faultReason,
                                          Types.SL.HWDisableReason.None);

                    throw new Exception(string.Format("SL is in fault state, reason: {0}", faultReason));
                }

                if (devState == Types.SL.HWDeviceState.Disabled)
                {
                    var disableReason = (Types.SL.HWDisableReason)ReadRegister(REG_DISABLE_REASON);
                    FireNotificationEvent(Types.SL.HWProblemReason.None, Types.SL.HWWarningReason.None,
                                          Types.SL.HWFaultReason.None, disableReason);

                    throw new Exception(string.Format("SL is in disabled state, reason: {0}", disableReason));
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
            var devState = (Types.SL.HWDeviceState)ReadRegister(REG_DEVICE_STATE);

            return !((devState == Types.SL.HWDeviceState.Fault) || (devState == Types.SL.HWDeviceState.Disabled) || (m_State == DeviceState.InProcess));
        }

        #region Standart API

        internal void ClearFault()
        {
            SystemHost.Journal.AppendLog(ComplexParts.SL, LogMessageType.Note, "SL fault cleared");

            CallAction(ACT_CLEAR_FAULT);
        }

        internal void ClearWarning()
        {
            SystemHost.Journal.AppendLog(ComplexParts.SL, LogMessageType.Note, "SL warning cleared");

            CallAction(ACT_CLEAR_WARNING);
        }

        internal ushort ReadRegister(ushort Address, bool SkipJournal = false)
        {
            ushort value = 0;

            if (!m_IsSLEmulation)
                value = m_IOAdapter.Read16(m_Node, Address);

            if (!SkipJournal)
                SystemHost.Journal.AppendLog(ComplexParts.SL, LogMessageType.Note, string.Format("SL @ReadRegister, address {0}, value {1}", Address, value));

            return value;
        }

        internal short ReadRegisterS(ushort Address, bool SkipJournal = false)
        {
            short value = 0;

            if (!m_IsSLEmulation)
                value = m_IOAdapter.Read16S(m_Node, Address);

            if (!SkipJournal)
                SystemHost.Journal.AppendLog(ComplexParts.SL, LogMessageType.Note, string.Format("SL @ReadRegisterS, address {0}, value {1}", Address, value));

            return value;
        }

        internal ushort ReadDeviceState(bool SkipJournal = false)
        {
            return ReadRegister(REG_DEVICE_STATE, SkipJournal);
        }

        internal ushort ReadFinished(bool SkipJournal = false)
        {
            return ReadRegister(REG_TEST_FINISHED, SkipJournal);
        }

        internal ushort ReadFaultReason(bool SkipJournal = false)
        {
            return ReadRegister(REG_FAULT_REASON, SkipJournal);
        }

        internal ushort ReadDisableReason(bool SkipJournal = false)
        {
            return ReadRegister(REG_DISABLE_REASON, SkipJournal);
        }

        internal ushort ReadWarning(bool SkipJournal = false)
        {
            return ReadRegister(REG_WARNING, SkipJournal);
        }

        internal ushort ReadProblem(bool SkipJournal = false)
        {
            return ReadRegister(REG_PROBLEM, SkipJournal);
        }

        internal void WriteRegister(ushort Address, ushort Value, bool SkipJournal = false)
        {
            if (!SkipJournal)
                SystemHost.Journal.AppendLog(ComplexParts.SL, LogMessageType.Note, string.Format("SL @WriteRegister, address {0}, value {1}", Address, Value));

            if (m_IsSLEmulation)
                return;

            m_IOAdapter.Write16(m_Node, Address, Value);
        }

        internal void WriteRegisterS(ushort Address, short Value, bool SkipJournal = false)
        {
            if (!SkipJournal)
                SystemHost.Journal.AppendLog(ComplexParts.SL, LogMessageType.Note, string.Format("SL @WriteRegisterS, address {0}, value {1}", Address, Value));

            if (m_IsSLEmulation)
                return;

            m_IOAdapter.Write16S(m_Node, Address, Value);
        }

        internal void CallAction(ushort Action)
        {
            SystemHost.Journal.AppendLog(ComplexParts.SL, LogMessageType.Note, string.Format("SL @Call, action {0}", Action));

            if (m_IsSLEmulation)
                return;

            m_IOAdapter.Call(m_Node, Action);
        }

        internal IList<ushort> ReadArrayFast(ushort Address)
        {
            SystemHost.Journal.AppendLog(ComplexParts.SL, LogMessageType.Note, string.Format("SL @ReadArrayFast, endpoint {0}", Address));

            if (m_IsSLEmulation)
                return new List<ushort>();

            return m_IOAdapter.ReadArrayFast16(m_Node, Address);
        }

        internal IList<short> ReadArrayFastS(ushort Address)
        {
            SystemHost.Journal.AppendLog(ComplexParts.SL, LogMessageType.Note, string.Format("SL @ReadArrayFastS, endpoint {0}", Address));

            if (m_IsSLEmulation)
                return new List<short>();

            return m_IOAdapter.ReadArrayFast16S(m_Node, Address);
        }

        #endregion

        internal void WriteCalibrationParams(Types.SL.CalibrationParameters Parameters)
        {
            SystemHost.Journal.AppendLog(ComplexParts.SL, LogMessageType.Note, "SL @WriteCalibrationParams begin");

            WriteRegisterS(REG_VTM_OFFSET, Parameters.VtmOffset, true);

            WriteRegister(REG_ITM_FINE_N, Parameters.ItmFineN, true);
            WriteRegister(REG_ITM_FINE_D, Parameters.ItmFineD, true);
            WriteRegister(REG_VTM_FINE_N, Parameters.VtmFineN, true);
            WriteRegister(REG_VTM_FINE_D, Parameters.VtmFineD, true);
            WriteRegister(REG_PREDICT_PARAM_K1, Parameters.PredictParamK1, true);
            WriteRegister(REG_PREDICT_PARAM_K2, Parameters.PredictParamK2, true);
            WriteRegister(REG_VTM_FINE2_N, Parameters.VtmFine2N, true);
            WriteRegister(REG_VTM_FINE2_D, Parameters.VtmFine2D, true);

            if (!m_IsSLEmulation)
                m_IOAdapter.Call(m_Node, ACT_SAVE_TO_ROM);

            SystemHost.Journal.AppendLog(ComplexParts.SL, LogMessageType.Note, "SL @WriteCalibrationParams end");

        }

        internal Types.SL.CalibrationParameters ReadCalibrationParams()
        {
            SystemHost.Journal.AppendLog(ComplexParts.SL, LogMessageType.Note, "SL @ReadCalibrationParams begin");

            var parameters = new Types.SL.CalibrationParameters
            {
                VtmOffset = ReadRegisterS(REG_VTM_OFFSET, true),

                ItmFineN = ReadRegister(REG_ITM_FINE_N, true),
                ItmFineD = ReadRegister(REG_ITM_FINE_D, true),
                VtmFineN = ReadRegister(REG_VTM_FINE_N, true),
                VtmFineD = ReadRegister(REG_VTM_FINE_D, true),
                PredictParamK1 = ReadRegister(REG_PREDICT_PARAM_K1, true),
                PredictParamK2 = ReadRegister(REG_PREDICT_PARAM_K2, true),
                VtmFine2N = ReadRegister(REG_VTM_FINE2_N, true),
                VtmFine2D = ReadRegister(REG_VTM_FINE2_D, true)
            };

            SystemHost.Journal.AppendLog(ComplexParts.SL, LogMessageType.Note, "SL @ReadCalibrationParams end");

            return parameters;
        }

        private void MeasurementLogicRoutine(Types.Commutation.TestParameters Commutation)
        {
            try
            {
                m_State = DeviceState.InProcess;
                FireVTMEvent(m_State, m_Result, 0);

                var timeStamp = Environment.TickCount + m_Timeout;

                if (!m_IsSLEmulation)
                {
                    while (Environment.TickCount < timeStamp)
                    {
                        var devState = (Types.SL.HWDeviceState)ReadRegister(REG_DEVICE_STATE, true);
                        if (devState == Types.SL.HWDeviceState.PowerReady)
                            break;

                        Thread.Sleep(50);
                    }
                }

                if (Environment.TickCount > timeStamp)
                {
                    m_State = DeviceState.Fault;
                    return;
                }

                if (!m_Parameter.IsSelfTest)
                    if (m_IOCommutation.Switch(Types.Commutation.CommutationMode.SL, Commutation.CommutationType, Commutation.Position) == DeviceState.Fault)
                    {
                        m_State = DeviceState.Fault;
                        FireVTMEvent(m_State, m_Result, 0);
                        return;
                    }

                var current = 0;

                if (!m_IsSLEmulation)
                {
                    if (m_Parameter.IsSelfTest)
                    {
                        m_Result.CapacitorsArray = new List<float>
                            {
                                ReadRegister(REG_V11, true)/10.0f,
                                ReadRegister(REG_V12, true)/10.0f,
                                ReadRegister(REG_V21, true)/10.0f,
                                ReadRegister(REG_V22, true)/10.0f,
                                ReadRegister(REG_V31, true)/10.0f,
                                ReadRegister(REG_V32, true)/10.0f,
                                ReadRegister(REG_V41, true)/10.0f,
                                ReadRegister(REG_V42, true)/10.0f,
                                ReadRegister(REG_V51, true)/10.0f,
                                ReadRegister(REG_V52, true)/10.0f
                            };

                        CallAction(ACT_RUN_SELF_TEST);

                        for (var i = 0; i < m_Result.CapacitorsArray.Count; i++)
                            if (m_Result.CapacitorsArray[i] > 100)
                                m_Result.CapacitorsArray[i] = 100;
                    }
                    else
                    {
                        switch (m_Parameter.TestType)
                        {
                            case Types.SL.VTMTestType.Ramp:
                                {
                                    WriteRegister(REG_MEASUREMENT_TYPE, MEASURE_RAMP);
                                    WriteRegister(REG_RAMP_DESIRED_CURRENT, m_Parameter.RampCurrent);
                                    WriteRegister(REG_RAMP_DESIRED_TIME, m_Parameter.RampTime);
                                    WriteRegister(REG_HEATING_ENABLE, (ushort)(m_Parameter.IsRampOpeningEnabled ? 1 : 0));

                                    if (m_Parameter.IsRampOpeningEnabled)
                                    {
                                        WriteRegister(REG_HEATING_CURRENT, m_Parameter.RampOpeningCurrent);
                                        WriteRegister(REG_HEATING_TIME, m_Parameter.RampOpeningTime);
                                    }

                                    current = m_Parameter.RampCurrent;
                                }
                                break;

                            case Types.SL.VTMTestType.Sinus:
                                {
                                    WriteRegister(REG_MEASUREMENT_TYPE, MEASURE_SIN);
                                    WriteRegister(REG_SIN_DESIRED_CURRENT, m_Parameter.SinusCurrent);
                                    WriteRegister(REG_SIN_DESIRED_TIME, m_Parameter.SinusTime);

                                    current = m_Parameter.SinusCurrent;
                                }
                                break;

                            case Types.SL.VTMTestType.Curve:
                                {
                                    WriteRegister(REG_MEASUREMENT_TYPE, MEASURE_S_CURVE);
                                    WriteRegister(REG_SCURVE_DESIRED_CURRENT, m_Parameter.CurveCurrent);
                                    WriteRegister(REG_SCURVE_DESIRED_TIME, m_Parameter.CurveTime);
                                    WriteRegister(REG_SCURVE_ADD_TIME, m_Parameter.CurveAddTime);
                                    WriteRegister(REG_SCURVE_FACTOR, m_Parameter.CurveFactor);

                                    current = m_Parameter.CurveCurrent;
                                }
                                break;
                        }

                        WriteRegister(REG_USE_FULL_SCALE, (ushort)(m_Parameter.UseFullScale ? 1 : 0));
                        WriteRegister(REG_DISABLE_VTM_PP, (ushort)(m_Parameter.UseLsqMethod ? 0 : 1));

                        WriteRegister(REG_REPETITION_COUNT, m_Parameter.Count);
                        CallAction(ACT_START_TEST);
                    }

                    m_State = WaitForEndOfTest();
                }
                else
                    m_State = DeviceState.Success;

                if (!m_Parameter.IsSelfTest)
                    if (m_IOCommutation.Switch(Types.Commutation.CommutationMode.None) == DeviceState.Fault)
                    {
                        m_State = DeviceState.Fault;
                        FireVTMEvent(m_State, m_Result, 0);
                        return;
                    }

                m_Result.IsSelftest = m_Parameter.IsSelfTest;

                if (m_IsSLEmulation)
                {
                    m_Result.Voltage = EMU_DEFAULT_VTM;
                    m_Result.Current = EMU_DEFAULT_ITM;
                }
                else
                {
                    m_Result.Voltage = ReadRegister(REG_TEST_RESULT);
                    m_Result.Current = ReadRegister(REG_TEST_CURRENT_RESULT);

                    if (m_ReadGraph)
                    {
                        m_Result.DesiredArray = ReadArrayFastS(ARR_SCOPE_DESIRED);
                        m_Result.ITMArray = ReadArrayFastS(ARR_SCOPE_ITM);
                        m_Result.VTMArray = ReadArrayFastS(ARR_SCOPE_VTM);
                    }

                    if (m_Parameter.IsSelfTest)
                        m_Result.SelfTestArray = ReadArrayFastS(ARR_SELF_TEST);

                }

                FireVTMEvent(m_State, m_Result, current);
            }
            catch (Exception ex)
            {
                if (!m_Parameter.IsSelfTest)
                    m_IOCommutation.Switch(Types.Commutation.CommutationMode.None);

                m_State = DeviceState.Fault;
                FireVTMEvent(DeviceState.Fault, m_Result, 0);
                FireExceptionEvent(ex.Message);

                throw;
            }
        }

        private DeviceState WaitForEndOfTest()
        {
            var timeStamp = Environment.TickCount + m_Timeout;

            while (Environment.TickCount < timeStamp)
            {
                if (m_Stop)
                {
                    CallAction(ACT_STOP);
                    return DeviceState.Stopped;
                }

                var devState = (Types.SL.HWDeviceState)ReadRegister(REG_DEVICE_STATE, true);
                var opResult = (Types.SL.HWOperationResult)ReadRegister(REG_TEST_FINISHED, true);
                var stResult = (Types.SL.HWOperationResult)ReadRegister(REG_SELF_TEST_RESULT, true);

                if (devState == Types.SL.HWDeviceState.Fault)
                {
                    var faultReason = (Types.SL.HWFaultReason)ReadRegister(REG_FAULT_REASON);

                    FireNotificationEvent(Types.SL.HWProblemReason.None, Types.SL.HWWarningReason.None, faultReason,
                                          Types.SL.HWDisableReason.None);

                    throw new Exception(string.Format("SL device is in fault state, reason: {0}", faultReason));
                }

                if (devState == Types.SL.HWDeviceState.Disabled)
                {
                    var disableReason = (Types.SL.HWDisableReason)ReadRegister(REG_DISABLE_REASON);

                    FireNotificationEvent(Types.SL.HWProblemReason.None, Types.SL.HWWarningReason.None,
                                          Types.SL.HWFaultReason.None, disableReason);

                    throw new Exception(string.Format("SL device is in disabled state, reason: {0}", disableReason));
                }

                if (m_Parameter.IsSelfTest && stResult != Types.SL.HWOperationResult.InProcess)
                    break;

                if (!m_Parameter.IsSelfTest && opResult != Types.SL.HWOperationResult.InProcess)
                {
                    var warning = (Types.SL.HWWarningReason)ReadRegister(REG_WARNING);
                    var problem = (Types.SL.HWProblemReason)ReadRegister(REG_PROBLEM);

                    if (problem != Types.SL.HWProblemReason.None)
                    {
                        FireNotificationEvent(problem, Types.SL.HWWarningReason.None, Types.SL.HWFaultReason.None,
                                              Types.SL.HWDisableReason.None);
                    }

                    if (warning != Types.SL.HWWarningReason.None)
                    {
                        FireNotificationEvent(Types.SL.HWProblemReason.None, warning, Types.SL.HWFaultReason.None,
                                              Types.SL.HWDisableReason.None);

                        ClearWarning();
                    }

                    break;
                }

                Thread.Sleep(REQUEST_DELAY_MS);
            }

            if (Environment.TickCount > timeStamp)
            {
                FireExceptionEvent("Timeout while waiting for SL test to end");
                throw new Exception("Timeout while waiting for SL test to end");
            }

            return DeviceState.Success;
        }

        #region Events

        private void FireConnectionEvent(DeviceConnectionState State, string Message)
        {
            SystemHost.Journal.AppendLog(ComplexParts.SL, LogMessageType.Info, Message);
            m_Communication.PostDeviceConnectionEvent(ComplexParts.SL, State, Message);
        }

        private void FireVTMEvent(DeviceState State, Types.SL.TestResults Result, int ITM)
        {
            var message = string.Format("{0} state {1}", !m_Parameter.IsSelfTest ? "SL test" : "SL self-test", State);

            if (State == DeviceState.Success)
            {
                message = !m_Parameter.IsSelfTest
                              ? String.Format("Test result VTM - {0} mV on ITM - {1} A", Result.Voltage, ITM)
                              : "Self-test OK";
            }

            SystemHost.Journal.AppendLog(ComplexParts.SL, LogMessageType.Info, message);
            m_Communication.PostSLEvent(State, Result);
        }

        private void FireNotificationEvent(Types.SL.HWProblemReason Problem, Types.SL.HWWarningReason Warning,
                                           Types.SL.HWFaultReason Fault, Types.SL.HWDisableReason Disable)
        {
            SystemHost.Journal.AppendLog(ComplexParts.SL, LogMessageType.Warning,
                                         String.Format(
                                             "SL device notification: problem {0}, warning {1}, fault {2}, disable {3}",
                                             Problem, Warning, Fault, Disable));

            m_Communication.PostSLNotificationEvent(Problem, Warning, Fault, Disable);
        }

        private void FireExceptionEvent(string Message)
        {
            SystemHost.Journal.AppendLog(ComplexParts.SL, LogMessageType.Error, Message);
            m_Communication.PostExceptionEvent(ComplexParts.SL, Message);
        }

        #endregion

        #region Registers

        private const ushort
            ACT_ENABLE_POWER = 1,
            ACT_DISABLE_POWER = 2,
            ACT_CLEAR_FAULT = 3,
            ACT_CLEAR_WARNING = 4,
            ACT_START_TEST = 100,
            ACT_STOP = 101,
            ACT_RUN_SELF_TEST = 102,
            ACT_SAVE_TO_ROM = 200,
            MEASURE_RAMP = 0,
            MEASURE_SIN = 1,
            MEASURE_S_CURVE = 2,
            REG_ITM_FINE_N = 18,
            REG_ITM_FINE_D = 19,
            REG_VTM_FINE_N = 22,
            REG_VTM_FINE_D = 23,
            REG_PREDICT_PARAM_K1 = 29,
            REG_PREDICT_PARAM_K2 = 30,
            REG_VTM_OFFSET = 32,
            REG_VTM_FINE2_N = 39,
            REG_VTM_FINE2_D = 40,
            REG_RAMP_DESIRED_CURRENT = 129,
            REG_RAMP_DESIRED_TIME = 130,
            REG_HEATING_CURRENT = 131,
            REG_HEATING_TIME = 132,
            REG_HEATING_ENABLE = 133,
            REG_SIN_DESIRED_CURRENT = 140,
            REG_SIN_DESIRED_TIME = 141,
            REG_SCURVE_DESIRED_CURRENT = 150,
            REG_SCURVE_DESIRED_TIME = 151,
            REG_SCURVE_FACTOR = 152,
            REG_SCURVE_ADD_TIME = 153,
            REG_USE_FULL_SCALE = 162,
            REG_DISABLE_VTM_PP = 161,
            REG_REPETITION_COUNT = 160,
            REG_MEASUREMENT_TYPE = 128,
            REG_DEVICE_STATE = 192,
            REG_FAULT_REASON = 193,
            REG_DISABLE_REASON = 194,
            REG_WARNING = 195,
            REG_PROBLEM = 196,
            REG_TEST_FINISHED = 197,
            REG_TEST_RESULT = 198,
            REG_SELF_TEST_RESULT = 200,
            REG_TEST_CURRENT_RESULT = 206,
            REG_V11 = 210,
            REG_V12 = 211,
            REG_V21 = 212,
            REG_V22 = 213,
            REG_V31 = 214,
            REG_V32 = 215,
            REG_V41 = 216,
            REG_V42 = 217,
            REG_V51 = 218,
            REG_V52 = 219,
            ARR_SCOPE_ITM = 1,
            ARR_SCOPE_VTM = 2,
            ARR_SCOPE_DESIRED = 4,
            ARR_SELF_TEST = 7;

        #endregion
    }
}