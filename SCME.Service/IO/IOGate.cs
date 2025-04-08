using System;
using System.Collections.Generic;
using System.Threading;
using SCME.Service.Properties;
using SCME.Types;

namespace SCME.Service.IO
{
    internal class IOGate
    {
        private const int CAL_WAIT_TIME_MS = 5000;
        private const int DEFAULT_TIMEOUT = 50000;
        private const int REQUEST_DELAY_MS = 50;

        private const bool EMU_DEFAULT_KELVIN = true;
        private const float EMU_DEFAULT_RESISTANCE = 5.0f;
        private const float EMU_DEFAULT_IGT = 170;
        private const float EMU_DEFAULT_VGT = 990;
        private const short EMU_DEFAULT_IH = 32;
        private const short EMU_DEFAULT_IL = 270;

        private readonly IOAdapter m_IOAdapter;
        private readonly BroadcastCommunication m_Communication;
        private readonly ushort m_Node;
        private readonly bool m_IsGateEmulationHard;
        private readonly bool m_ReadGraph;
        private IOCommutation m_IOCommutation;
        private bool m_IsGateEmulation;
        private Types.Gate.TestParameters m_Parameter;
        private DeviceConnectionState m_ConnectionState;
        private volatile Types.Gate.TestResults m_Result;
        private volatile DeviceState m_State;
        private volatile bool m_Stop;

        private int m_Timeout = DEFAULT_TIMEOUT;

        internal IOGate(IOAdapter Adapter, BroadcastCommunication Communication)
        {
            m_IOAdapter = Adapter;
            m_Communication = Communication;
            m_IsGateEmulationHard = Settings.Default.GateEmulation;
            m_IsGateEmulation = m_IsGateEmulationHard;
            m_ReadGraph = Settings.Default.GateReadGraph;

            m_Node = (ushort)Settings.Default.GateNode;
            m_Result = new Types.Gate.TestResults();

            SystemHost.Journal.AppendLog(ComplexParts.Gate, LogMessageType.Info,
                                         String.Format("Gate created. Emulation mode: {0}", m_IsGateEmulation));
        }

        internal IOCommutation ActiveCommutation
        {
            get { return m_IOCommutation; }
            set { m_IOCommutation = value; }
        }

        internal DeviceConnectionState Initialize(bool Enable, int Timeout)
        {
            m_Timeout = Timeout;
            m_IsGateEmulation = m_IsGateEmulationHard || !Enable;

            m_ConnectionState = DeviceConnectionState.ConnectionInProcess;
            FireConnectionEvent(m_ConnectionState, "Gate initializing");

            if (m_IsGateEmulation)
            {
                m_ConnectionState = DeviceConnectionState.ConnectionSuccess;

                FireConnectionEvent(m_ConnectionState, "Gate initialized");

                return m_ConnectionState;
            }

            try
            {
                ClearWarning();

                m_ConnectionState = DeviceConnectionState.ConnectionSuccess;
                FireConnectionEvent(m_ConnectionState, "Gate initialized");
            }
            catch (Exception ex)
            {
                m_ConnectionState = DeviceConnectionState.ConnectionFailed;
                FireConnectionEvent(m_ConnectionState, String.Format("Gate initialization error: {0}", ex.Message));
            }

            return m_ConnectionState;
        }

        internal void Deinitialize()
        {
            var oldState = m_ConnectionState;

            m_ConnectionState = DeviceConnectionState.DisconnectionInProcess;
            FireConnectionEvent(DeviceConnectionState.DisconnectionInProcess, "Gate disconnecting");

            if (!m_IsGateEmulation && oldState == DeviceConnectionState.ConnectionSuccess)
                Stop();

            m_ConnectionState = DeviceConnectionState.DisconnectionSuccess;
            FireConnectionEvent(DeviceConnectionState.DisconnectionSuccess, "Gate disconnected");
        }

        internal DeviceState Start(Types.Gate.TestParameters parameters, Types.Commutation.TestParameters commParameters)
        {
            m_Parameter = parameters;

            if (m_State == DeviceState.InProcess)
                throw new Exception("Gate test is already started");

            m_Stop = false;
            m_Result = new Types.Gate.TestResults { TestTypeId = parameters.TestTypeId };

            ClearWarning();

            if (!m_IsGateEmulation)
            {
                var devState = (Types.Gate.HWDeviceState)ReadRegister(REG_DEVICE_STATE);

                if (devState == Types.Gate.HWDeviceState.Fault)
                {
                    var faultReason = (Types.Gate.HWFaultReason)ReadRegister(REG_FAULT_REASON);
                    FireNotificationEvent(Types.Gate.HWProblemReason.None, Types.Gate.HWWarningReason.None, faultReason,
                                          Types.Gate.HWDisableReason.None);

                    throw new Exception(string.Format("Gate is in fault state, reason: {0}", faultReason));
                }

                if (devState == Types.Gate.HWDeviceState.Disabled)
                {
                    var disableReason = (Types.Gate.HWDisableReason)ReadRegister(REG_DISABLE_REASON);
                    FireNotificationEvent(Types.Gate.HWProblemReason.None, Types.Gate.HWWarningReason.None,
                                          Types.Gate.HWFaultReason.None, disableReason);

                    throw new Exception(string.Format("Gate is in disabled state, reason: {0}", disableReason));
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
            //блок готов если он в состоянии отличном от Fault, Disabled или InProcess
            var devState = (Types.Gate.HWDeviceState)ReadRegister(REG_DEVICE_STATE);

            return !((devState == Types.Gate.HWDeviceState.Fault) || (devState == Types.Gate.HWDeviceState.Disabled) || (m_State == DeviceState.InProcess));
        }

        #region Standart API

        internal void ClearFault()
        {
            SystemHost.Journal.AppendLog(ComplexParts.Gate, LogMessageType.Note, "Gate fault cleared");

            CallAction(ACT_CLEAR_FAULT);
        }

        internal void ClearWarning()
        {
            SystemHost.Journal.AppendLog(ComplexParts.Gate, LogMessageType.Note, "Gate warning cleared");

            CallAction(ACT_CLEAR_WARNING);
        }

        internal ushort ReadRegister(ushort Address, bool SkipJournal = false)
        {
            ushort value = 0;

            if (!m_IsGateEmulation)
                value = m_IOAdapter.Read16(m_Node, Address);

            if (!SkipJournal)
                SystemHost.Journal.AppendLog(ComplexParts.Gate, LogMessageType.Note,
                                         string.Format("Gate @ReadRegister, address {0}, value {1}", Address, value));

            return value;
        }

        internal short ReadRegisterS(ushort Address, bool SkipJournal = false)
        {
            short value = 0;

            if (!m_IsGateEmulation)
                value = m_IOAdapter.Read16S(m_Node, Address);

            if (!SkipJournal)
                SystemHost.Journal.AppendLog(ComplexParts.Gate, LogMessageType.Note,
                                         string.Format("Gate @ReadRegisterS, address {0}, value {1}", Address, value));

            return value;
        }

        internal ushort ReadDeviceState(bool SkipJournal = false)
        {
            return ReadRegister(REG_DEVICE_STATE, SkipJournal);
        }

        internal ushort ReadFaultReason(bool SkipJournal = false)
        {
            return ReadRegister(REG_FAULT_REASON, SkipJournal);
        }

        internal ushort ReadDisableReason(bool SkipJournal = false)
        {
            return ReadRegister(REG_DISABLE_REASON, SkipJournal);
        }

        internal ushort ReadFinished(bool SkipJournal = false)
        {
            return ReadRegister(REG_TEST_FINISHED, SkipJournal);
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
                SystemHost.Journal.AppendLog(ComplexParts.Gate, LogMessageType.Note,
                                         string.Format("Gate @WriteRegister, address {0}, value {1}", Address, Value));

            if (m_IsGateEmulation)
                return;

            m_IOAdapter.Write16(m_Node, Address, Value);
        }

        internal void WriteRegisterS(ushort Address, short Value, bool SkipJournal = false)
        {
            if (!SkipJournal)
                SystemHost.Journal.AppendLog(ComplexParts.Gate, LogMessageType.Note,
                                         string.Format("Gate @WriteRegisterS, address {0}, value {1}", Address, Value));

            if (m_IsGateEmulation)
                return;

            m_IOAdapter.Write16S(m_Node, Address, Value);
        }

        private IList<short> ReadArrayFastS(ushort Address)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Gate, LogMessageType.Note,
                                         string.Format("Gate @ReadArrayFastS, endpoint {0}", Address));

            if (m_IsGateEmulation)
                return new List<short>();

            return m_IOAdapter.ReadArrayFast16S(m_Node, Address);
        }

        internal void CallAction(ushort Action)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Gate, LogMessageType.Note,
                                         string.Format("Gate @Call, action {0}", Action));

            if (m_IsGateEmulation)
                return;

            m_IOAdapter.Call(m_Node, Action);
        }

        #endregion

        internal Tuple<ushort, ushort> PulseCalibrationGate(ushort Current)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Gate, LogMessageType.Info,
                                         string.Format("Calibrate gate, current - {0}", Current));

            if (m_IsGateEmulation)
                return new Tuple<ushort, ushort>(0, 0);

            if (m_IOCommutation.Switch(Types.Commutation.CommutationMode.Gate) == DeviceState.Fault)
                return new Tuple<ushort, ushort>(0, 0);

            try
            {
                var timeout = Environment.TickCount + CAL_WAIT_TIME_MS;

                WriteRegister(REG_CAL_CURRENT, Current);
                CallAction(ACT_CALIBRATE_GATE);

                var done = false;
                while (timeout > Environment.TickCount && !done)
                    done = ReadRegister(REG_DEVICE_STATE, true) == (ushort)Types.Gate.HWDeviceState.None;

                if (!done)
                    return new Tuple<ushort, ushort>(0, 0);

                var resultCurrent = ReadRegister(REG_RES_CAL_CURRENT);
                var resultVoltage = ReadRegister(REG_RES_CAL_VOLTAGE);

                return new Tuple<ushort, ushort>(resultCurrent, resultVoltage);
            }
            finally
            {
                m_IOCommutation.Switch(Types.Commutation.CommutationMode.None);
            }
        }

        internal ushort PulseCalibrationMain(ushort Current)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Gate, LogMessageType.Info,
                                         string.Format("Calibrate main circuit, current - {0}", Current));

            if (m_IsGateEmulation)
                return 0;

            if (m_IOCommutation.Switch(Types.Commutation.CommutationMode.Gate) == DeviceState.Fault)
                return 0;

            try
            {
                var timeout = Environment.TickCount + CAL_WAIT_TIME_MS;

                WriteRegister(REG_CAL_CURRENT, Current);
                CallAction(ACT_CALIBRATE_HOLDING);

                var done = false;
                while (timeout > Environment.TickCount && !done)
                    done = ReadRegister(REG_DEVICE_STATE, true) == (ushort)Types.Gate.HWDeviceState.None;

                if (!done)
                    return 0;

                var resultCurrent = ReadRegister(REG_RES_CAL_CURRENT);

                return resultCurrent;
            }
            finally
            {
                m_IOCommutation.Switch(Types.Commutation.CommutationMode.None);
            }
        }

        internal void WriteCalibrationParams(Types.Gate.CalibrationParameters Parameters)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Gate, LogMessageType.Note,
                                         "Gate @WriteCalibrationParams begin");


            WriteRegisterS(REG_GATE_VGT_OFFSET, Parameters.GateVGTOffset, true);
            WriteRegisterS(REG_GATE_IGT_OFFSET, Parameters.GateIGTOffset, true);
            WriteRegisterS(REG_GATE_IHL_OFFSET, Parameters.GateIHLOffset, true);

            WriteRegister(REG_RG_CURRENT, Parameters.RgCurrent, true);
            WriteRegister(REG_GATE_FINE_IGT_N, Parameters.GateFineIGT_N, true);
            WriteRegister(REG_GATE_FINE_IGT_D, Parameters.GateFineIGT_D, true);
            WriteRegister(REG_GATE_FINE_VGT_N, Parameters.GateFineVGT_N, true);
            WriteRegister(REG_GATE_FINE_VGT_D, Parameters.GateFineVGT_D, true);
            WriteRegister(REG_GATE_FINE_IHL_N, Parameters.GateFineIHL_N, true);
            WriteRegister(REG_GATE_FINE_IHL_D, Parameters.GateFineIHL_D, true);

            if (!m_IsGateEmulation)
                CallAction(ACT_SAVE_TO_ROM);

            SystemHost.Journal.AppendLog(ComplexParts.Gate, LogMessageType.Note,
                                         "Gate @WriteCalibrationParams end");
        }

        internal Types.Gate.CalibrationParameters ReadCalibrationParams()
        {
            SystemHost.Journal.AppendLog(ComplexParts.Gate, LogMessageType.Note,
                                         "Gate @ReadCalibrationParams begin");

            var parameters = new Types.Gate.CalibrationParameters
            {
                GateIGTOffset = ReadRegisterS(REG_GATE_IGT_OFFSET, true),
                GateVGTOffset = ReadRegisterS(REG_GATE_VGT_OFFSET, true),
                GateIHLOffset = ReadRegisterS(REG_GATE_IHL_OFFSET, true),

                RgCurrent = ReadRegister(REG_RG_CURRENT, true),
                GateFineIGT_N = ReadRegister(REG_GATE_FINE_IGT_N, true),
                GateFineIGT_D = ReadRegister(REG_GATE_FINE_IGT_D, true),
                GateFineVGT_N = ReadRegister(REG_GATE_FINE_VGT_N, true),
                GateFineVGT_D = ReadRegister(REG_GATE_FINE_VGT_D, true),
                GateFineIHL_N = ReadRegister(REG_GATE_FINE_IHL_N, true),
                GateFineIHL_D = ReadRegister(REG_GATE_FINE_IHL_D, true),
            };

            SystemHost.Journal.AppendLog(ComplexParts.Gate, LogMessageType.Note,
                                         "Gate @ReadCalibrationParams end");

            return parameters;
        }

        private void MeasurementLogicRoutine(Types.Commutation.TestParameters Commutation)
        {
            try
            {
                m_State = DeviceState.InProcess;
                FireAllEvent(m_State);

                if (m_IOCommutation.Switch(Types.Commutation.CommutationMode.Gate, Commutation.CommutationType, Commutation.Position) ==
                    DeviceState.Fault)
                {
                    m_State = DeviceState.Fault;
                    FireAllEvent(m_State);
                    return;
                }

                if (Kelvin())
                {
                    Resistance();
                    IgtVgt();
                    Ih();
                    Il();
                }

                if (m_IOCommutation.Switch(Types.Commutation.CommutationMode.None) == DeviceState.Fault)
                {
                    m_State = DeviceState.Fault;
                    FireAllEvent(m_State);
                    return;
                }

                m_State = m_Stop ? DeviceState.Stopped : DeviceState.Success;

                FireAllEvent(m_State);
            }
            catch (Exception ex)
            {
                m_IOCommutation.Switch(Types.Commutation.CommutationMode.None);

                m_State = DeviceState.Fault;
                FireAllEvent(m_State);
                FireExceptionEvent(ex.Message);

                throw;
            }
        }

        private bool Kelvin()
        {
            if (m_Stop)
                return false;

            FireKelvinEvent(DeviceState.InProcess, m_Result);

            CallAction(ACT_START_KELVIN);

            if (!m_IsGateEmulation)
            {
                WaitForEndOfTest();

                m_Result.IsKelvinOk = ReadRegister(REG_RESULT_KELVIN) != 0;

                if (m_ReadGraph)
                {
                    m_Result.ArrayKelvin.Add((short)ReadRegister(REG_KELVIN12));
                    m_Result.ArrayKelvin.Add((short)ReadRegister(REG_KELVIN41));
                    m_Result.ArrayKelvin.Add((short)ReadRegister(REG_KELVIN14));
                    m_Result.ArrayKelvin.Add((short)ReadRegister(REG_KELVIN32));
                }
            }
            else
                m_Result.IsKelvinOk = EMU_DEFAULT_KELVIN;

            FireKelvinEvent(m_Result.IsKelvinOk ? DeviceState.Success : DeviceState.Problem, m_Result);

            return m_Result.IsKelvinOk;
        }

        private void Resistance()
        {
            if (m_Stop)
                return;

            FireResistanceEvent(DeviceState.InProcess, m_Result);

            m_IOAdapter.Call(m_Node, ACT_START_RG);

            if (!m_IsGateEmulation)
            {
                WaitForEndOfTest();
                m_Result.Resistance = ReadRegister(REG_RESULT_RG) / 10.0f;
            }
            else
                m_Result.Resistance = EMU_DEFAULT_RESISTANCE;

            FireResistanceEvent(DeviceState.Success, m_Result);
        }

        private void IgtVgt()
        {
            if (m_Stop) return;

            FireGateEvent(DeviceState.InProcess, m_Result);

            WriteRegister(REG_GATE_VGT_PURE, (ushort)(m_Parameter.IsCurrentEnabled ? 1 : 0));
            WriteRegister(REG_SCOPE1_TYPE, SCOPE_I);
            WriteRegister(REG_SCOPE2_TYPE, SCOPE_V);
            CallAction(ACT_START_GATE);

            if (!m_IsGateEmulation)
            {
                WaitForEndOfTest();

                m_Result.IGT = ReadRegister(REG_RESULT_IGT);
                m_Result.VGT = ReadRegister(REG_RESULT_VGT);

                if (m_ReadGraph)
                {
                    m_Result.ArrayIGT = ReadArrayFastS(ARR_SCOPE1_DATA);
                    m_Result.ArrayVGT = ReadArrayFastS(ARR_SCOPE2_DATA);
                }
            }
            else
            {
                m_Result.IGT = EMU_DEFAULT_IGT;
                m_Result.VGT = EMU_DEFAULT_VGT;
            }

            FireGateEvent(DeviceState.Success, m_Result);
        }

        private void Ih()
        {
            if (m_Stop)
                return;
            if (!m_Parameter.IsIhEnabled)
                return;

            FireIHEvent(DeviceState.InProcess, m_Result);

            WriteRegister(REG_USE_HOLD_STRIKE, (ushort)(m_Parameter.IsIhStrikeCurrentEnabled ? 1 : 0));
            CallAction(ACT_START_IH);

            if (!m_IsGateEmulation)
            {
                WaitForEndOfTest();

                m_Result.IH = ReadRegister(REG_RESULT_IH);

                if (m_ReadGraph)
                    m_Result.ArrayIH = ReadArrayFastS(ARR_SCOPE1_DATA);
            }
            else
                m_Result.IH = EMU_DEFAULT_IH;

            FireIHEvent(DeviceState.Success, m_Result);
        }

        private void Il()
        {
            if (m_Stop)
                return;
            if (!m_Parameter.IsIlEnabled)
                return;

            FireILEvent(DeviceState.InProcess, m_Result);

            CallAction(ACT_START_IL);

            if (!m_IsGateEmulation)
            {
                WaitForEndOfTest();
                m_Result.IL = ReadRegister(REG_RESULT_IL);
            }
            else
                m_Result.IL = EMU_DEFAULT_IL;

            FireILEvent(DeviceState.Success, m_Result);
        }

        private void WaitForEndOfTest()
        {
            var timeStamp = Environment.TickCount + m_Timeout;

            while (Environment.TickCount < timeStamp)
            {
                var devState = (Types.Gate.HWDeviceState)ReadRegister(REG_DEVICE_STATE, true);
                var opResult = (Types.Gate.HWOperationResult)ReadRegister(REG_TEST_FINISHED, true);

                if (devState == Types.Gate.HWDeviceState.Fault)
                {
                    var faultReason = (Types.Gate.HWFaultReason)ReadRegister(REG_FAULT_REASON);

                    FireNotificationEvent(Types.Gate.HWProblemReason.None, Types.Gate.HWWarningReason.None, faultReason,
                                          Types.Gate.HWDisableReason.None);
                    throw new Exception(string.Format("Gate device is in fault state, reason - {0}", faultReason));
                }

                if (devState == Types.Gate.HWDeviceState.Disabled)
                {
                    var disableReason = (Types.Gate.HWDisableReason)ReadRegister(REG_DISABLE_REASON);

                    FireNotificationEvent(Types.Gate.HWProblemReason.None, Types.Gate.HWWarningReason.None,
                                          Types.Gate.HWFaultReason.None, disableReason);
                    throw new Exception(string.Format("Gate device is in disabled state, reason - {0}", disableReason));
                }

                if (opResult != Types.Gate.HWOperationResult.InProcess)
                {
                    var warning = (Types.Gate.HWWarningReason)ReadRegister(REG_WARNING);
                    var problem = (Types.Gate.HWProblemReason)ReadRegister(REG_PROBLEM);

                    if (problem != Types.Gate.HWProblemReason.None)
                    {
                        FireNotificationEvent(problem, Types.Gate.HWWarningReason.None, Types.Gate.HWFaultReason.None,
                                              Types.Gate.HWDisableReason.None);
                    }

                    if (warning != Types.Gate.HWWarningReason.None)
                    {
                        FireNotificationEvent(Types.Gate.HWProblemReason.None, warning, Types.Gate.HWFaultReason.None,
                                              Types.Gate.HWDisableReason.None);
                        CallAction(ACT_CLEAR_WARNING);
                    }

                    break;
                }

                Thread.Sleep(REQUEST_DELAY_MS);
            }

            if (Environment.TickCount > timeStamp)
            {
                FireExceptionEvent("Timeout while waiting for Gate test to end");
                throw new Exception("Timeout while waiting for Gate test to end");
            }
        }

        #region Events

        private void FireConnectionEvent(DeviceConnectionState State, string Message)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Gate, LogMessageType.Info, Message);
            m_Communication.PostDeviceConnectionEvent(ComplexParts.Gate, State, Message);
        }

        private void FireAllEvent(DeviceState State)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Gate, LogMessageType.Info,
                                         string.Format("Gate test state - {0}", State));
            m_Communication.PostGateAllEvent(State);

            if (State == DeviceState.Stopped)
                m_Communication.PostTestAllEvent(State, "Gate manual stop");
        }

        private void FireKelvinEvent(DeviceState state, Types.Gate.TestResults result)
        {
            var message = string.Format("Gate Kelvin state {0}", state);

            if (state == DeviceState.Success)
                message = string.Format("Gate Kelvin is {0}", result.IsKelvinOk);

            SystemHost.Journal.AppendLog(ComplexParts.Gate, LogMessageType.Info, message);

            m_Communication.PostGateKelvinEvent(state, result);
        }

        private void FireResistanceEvent(DeviceState State, Types.Gate.TestResults Result)
        {
            var message = string.Format("Gate resistance state {0}", State);

            if (State == DeviceState.Success)
                message = string.Format("Gate resistance {0} Ohm", Result.Resistance);

            SystemHost.Journal.AppendLog(ComplexParts.Gate, LogMessageType.Info, message);

            m_Communication.PostGateResistanceEvent(State, Result);
        }

        private void FireGateEvent(DeviceState State, Types.Gate.TestResults Result)
        {
            var message = string.Format("Gate gate state {0}", State);

            if (State == DeviceState.Success)
                message = string.Format("Gate IGT {0} mA, VGT {1} mV", Result.IGT, Result.VGT);

            SystemHost.Journal.AppendLog(ComplexParts.Gate, LogMessageType.Info, message);

            m_Communication.PostGateGateEvent(State, Result);
        }

        private void FireIHEvent(DeviceState State, Types.Gate.TestResults Result)
        {
            var message = string.Format("Gate IH state {0}", State);

            if (State == DeviceState.Success)
                message = string.Format("Gate IH {0} mA", Result.IH);

            SystemHost.Journal.AppendLog(ComplexParts.Gate, LogMessageType.Info, message);

            m_Communication.PostGateIhEvent(State, Result);
        }

        private void FireILEvent(DeviceState State, Types.Gate.TestResults Result)
        {
            var message = string.Format("Gate IL state {0}", State);

            if (State == DeviceState.Success)
                message = string.Format("Gate IL {0} mA", Result.IL);

            SystemHost.Journal.AppendLog(ComplexParts.Gate, LogMessageType.Info, message);

            m_Communication.PostGateIlEvent(State, Result);
        }

        private void FireNotificationEvent(Types.Gate.HWProblemReason Problem, Types.Gate.HWWarningReason Warning,
                                           Types.Gate.HWFaultReason Fault, Types.Gate.HWDisableReason Disable)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Gate, LogMessageType.Warning,
                                         string.Format(
                                             "Gate device notification: problem {0}, warning {1}, fault {2}, disable {3}",
                                             Problem, Warning, Fault, Disable));

            m_Communication.PostGateNotificationEvent(Problem, Warning, Fault, Disable);
        }

        private void FireExceptionEvent(string Message)
        {
            SystemHost.Journal.AppendLog(ComplexParts.Gate, LogMessageType.Error, Message);
            m_Communication.PostExceptionEvent(ComplexParts.Gate, Message);
        }

        #endregion

        #region Registers

        internal const ushort
            ACT_CLEAR_FAULT = 3,
            ACT_CLEAR_WARNING = 4,
            ACT_START_KELVIN = 100,
            ACT_START_GATE = 101,
            ACT_START_IH = 102,
            ACT_START_IL = 103,
            ACT_START_RG = 104,
            ACT_STOP = 105,
            ACT_START_RCC = 106,
            ACT_CALIBRATE_GATE = 110,
            ACT_CALIBRATE_HOLDING = 111,
            ACT_SAVE_TO_ROM = 200,
            REG_GATE_VGT_OFFSET = 56,
            REG_GATE_IGT_OFFSET = 57,
            REG_RG_CURRENT = 93,
            REG_GATE_IHL_OFFSET = 35,
            REG_GATE_FINE_IGT_N = 50,
            REG_GATE_FINE_IGT_D = 51,
            REG_GATE_FINE_VGT_N = 52,
            REG_GATE_FINE_VGT_D = 53,
            REG_GATE_FINE_IHL_N = 33,
            REG_GATE_FINE_IHL_D = 34,
            REG_DEVICE_STATE = 192,
            REG_FAULT_REASON = 193,
            REG_DISABLE_REASON = 194,
            REG_WARNING = 195,
            REG_PROBLEM = 196,
            REG_TEST_FINISHED = 197,
            REG_RESULT_KELVIN = 198,
            REG_RESULT_IGT = 199,
            REG_RESULT_VGT = 200,
            REG_RESULT_IH = 201,
            REG_RESULT_IL = 202,
            REG_RESULT_RG = 203,
            REG_KELVIN12 = 211,
            REG_KELVIN41 = 212,
            REG_KELVIN14 = 213,
            REG_KELVIN32 = 214,
            REG_GATE_VGT_PURE = 128,
            REG_USE_HOLD_STRIKE = 129,
            REG_SCOPE1_TYPE = 150,
            REG_SCOPE2_TYPE = 151,
            REG_CAL_CURRENT = 140,
            REG_RES_CAL_CURRENT = 204,
            REG_RES_CAL_VOLTAGE = 205,
            SCOPE_I = 1,
            SCOPE_V = 2,
            ARR_SCOPE1_DATA = 1,
            ARR_SCOPE2_DATA = 2;

        #endregion
    }
}