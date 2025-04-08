using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using SCME.Types.BaseTestParams;

namespace SCME.Types.Gate
{
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWDeviceState
    {
        [EnumMember]
        None = 0,
        [EnumMember]
        Fault = 1,
        [EnumMember]
        Disabled = 2,
        [EnumMember]
        Kelvin = 3,
        [EnumMember]
        Gate = 4,
        [EnumMember]
        IH = 5,
        [EnumMember]
        IL = 6,
        [EnumMember]
        Resistance = 7,
        [EnumMember]
        CalGate = 8,
        [EnumMember]
        CalHolding = 9
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWFaultReason
    {
        [EnumMember]
        None = 0,
        [EnumMember]
        HoldProcessError = 101,
        [EnumMember]
        GateProcessError = 112,
        [EnumMember]
        LatchProcessError = 121,
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWDisableReason
    {
        [EnumMember]
        None = 0,
        [EnumMember]
        BadClock = 1001
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWWarningReason
    {
        [EnumMember]
        None = 0,
        [EnumMember]
        HoldingCurrentSmall = 101,
        [EnumMember]
        WatchdogReset = 1001
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWProblemReason
    {
        [EnumMember]
        None = 0,
        [EnumMember]
        HoldReachTimeout = 101,
        [EnumMember]
        GateCurrentHigh = 111,
        [EnumMember]
        GateFollowingError = 112,
        [EnumMember]
        GateIgtOverload = 113,
        [EnumMember]
        LatchCurrentHigh = 121,
        [EnumMember]
        LatchFollowingError = 122,
        [EnumMember]
        RGateFollowingError = 141,
        [EnumMember]
        RGateOverload = 142
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWOperationResult
    {
        [EnumMember]
        InProcess = 0,
        [EnumMember]
        Success = 1,
        [EnumMember]
        Fail = 2
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    [KnownType(typeof(BaseTestParametersAndNormatives))]
    public class TestParameters : BaseTestParametersAndNormatives, ICloneable
    {
        [DataMember]
        public bool IsEnabled { get; set; }

        [DataMember]
        public bool IsCurrentEnabled { get; set; }

        [DataMember]
        public bool IsIhEnabled { get; set; }

        [DataMember]
        public bool IsIhStrikeCurrentEnabled { get; set; }

        [DataMember]
        public bool IsIlEnabled { get; set; }

        [DataMember]
        public float Resistance { get; set; }

        [DataMember]
        public float IGT { get; set; }

        [DataMember]
        public float VGT { get; set; }

        [DataMember]
        public float IH { get; set; }

        [DataMember]
        public float IL { get; set; }

        public TestParameters()
        {
            IsEnabled = true;
            Resistance = 100;
            IGT = 500;
            VGT = 2.5f;
            IH = 150;
            IL = 1000;
            TestParametersType = TestParametersType.Gate;
        }

        public override bool IsHasChanges(BaseTestParametersAndNormatives oldParametersBase)
        {
            var oldParameters = oldParametersBase as TestParameters;
            if (oldParameters == null)
                throw new InvalidCastException("oldParameters must be gateOldParameters");

            if (IsCurrentEnabled != oldParameters.IsCurrentEnabled)
                return true;
            if (IsIhEnabled != oldParameters.IsIhEnabled)
                return true;
            if (IsIhStrikeCurrentEnabled != oldParameters.IsIhStrikeCurrentEnabled)
                return true;
            if (IsIlEnabled != oldParameters.IsIlEnabled)
                return true;
            if (Resistance.CompareTo(oldParameters.Resistance) != 0)
                return true;
            if (IGT.CompareTo(oldParameters.IGT) != 0)
                return true;
            if (VGT.CompareTo(oldParameters.VGT) != 0)
                return true;
            if (IH.CompareTo(oldParameters.IH) != 0)
                return true;
            if (IL.CompareTo(oldParameters.IL) != 0)
                return true;

            return false;
        }


        public object Clone()
        {
            return MemberwiseClone();
        }


    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class ResultNormatives
    {
        [DataMember]
        public float Resistance { get; set; }

        [DataMember]
        public float IGT { get; set; }

        [DataMember]
        public float VGT { get; set; }

        [DataMember]
        public float IH { get; set; }

        [DataMember]
        public float IL { get; set; }

        public ResultNormatives()
        {
            Resistance = 100;
            IGT = 500;
            VGT = 2.5f;
            IH = 150;
            IL = 1000;
        }
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class TestResults : BaseTestResults
    {
        [DataMember]
        public float Resistance { get; set; }

        [DataMember]
        public float IGT { get; set; }

        [DataMember]
        public float VGT { get; set; }

        [DataMember]
        public float IH { get; set; }

        [DataMember]
        public float IL { get; set; }

        [DataMember]
        public bool IsKelvinOk { get; set; }

        [DataMember]
        public IList<short> ArrayVGT { get; set; }

        [DataMember]
        public IList<short> ArrayIGT { get; set; }

        [DataMember]
        public IList<short> ArrayIH { get; set; }

        [DataMember]
        public IList<short> ArrayKelvin { get; set; }

        public TestResults()
        {
            ArrayVGT = new List<short>();
            ArrayIGT = new List<short>();
            ArrayIH = new List<short>();
            ArrayKelvin = new List<short>();
        }
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class CalibrationResultGate
    {
        [DataMember]
        public ushort Current { get; set; }

        [DataMember]
        public ushort Voltage { get; set; }
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class CalibrationParameters
    {
        [DataMember]
        public short GateIGTOffset { get; set; }

        [DataMember]
        public short GateVGTOffset { get; set; }

        [DataMember]
        public short GateIHLOffset { get; set; }

        [DataMember]
        public ushort RgCurrent { get; set; }

        [DataMember]
        public ushort GateFineIGT_N { get; set; }

        [DataMember]
        public ushort GateFineIGT_D { get; set; }

        [DataMember]
        public ushort GateFineVGT_N { get; set; }

        [DataMember]
        public ushort GateFineVGT_D { get; set; }

        [DataMember]
        public ushort GateFineIHL_N { get; set; }

        [DataMember]
        public ushort GateFineIHL_D { get; set; }
    }
}