using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using SCME.Types.BaseTestParams;

namespace SCME.Types.SL
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
        Charging = 3,
        [EnumMember]
        PowerReady = 4,
        [EnumMember]
        InProcess = 5
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWFaultReason
    {
        [EnumMember]
        None = 0,
        [EnumMember]
        ThermalCell1 = 201,
        [EnumMember]
        ThermalCell2 = 202,
        [EnumMember]
        ThermalCell3 = 203,
        [EnumMember]
        ThermalCell4 = 204,
        [EnumMember]
        ThermalCell5 = 205,
        [EnumMember]
        NoCurrent = 210,
        [EnumMember]
        StCell1 = 211,
        [EnumMember]
        StCell2 = 212,
        [EnumMember]
        StCell3 = 213,
        [EnumMember]
        StCell4 = 214,
        [EnumMember]
        StCell5 = 215,
        [EnumMember]
        Timeout = 220
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWDisableReason
    {
        [EnumMember]
        None = 0,
        [EnumMember]
        OverloadUnknonw = 200,
        [EnumMember]
        OverloadCell1 = 201,
        [EnumMember]
        OverloadCell2 = 202,
        [EnumMember]
        OverloadCell3 = 203,
        [EnumMember]
        OverloadCell4 = 204,
        [EnumMember]
        OverloadCell5 = 205,
        [EnumMember]
        NoChargeCell1 = 211,
        [EnumMember]
        NoChargeCell2 = 212,
        [EnumMember]
        NoChargeCell3 = 213,
        [EnumMember]
        NoChargeCell4 = 214,
        [EnumMember]
        NoChargeCell5 = 215,
        [EnumMember]
        PowerOnCell1 = 221,
        [EnumMember]
        PowerOnCell2 = 222,
        [EnumMember]
        PowerOnCell3 = 223,
        [EnumMember]
        PowerOnCell4 = 224,
        [EnumMember]
        PowerOnCell5 = 225,
        [EnumMember]
        RejectorDrop = 231,
        [EnumMember]
        BadClock = 1001
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWWarningReason
    {
        [EnumMember]
        None = 0,
        [EnumMember]
        VTMUndervoltage = 201,
        [EnumMember]
        UnstableCapacitors = 202,
        [EnumMember]
        RateFast = 203,
        [EnumMember]
        UnstableVTM = 204,
        [EnumMember]
        WatchdogReset = 1001
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWProblemReason
    {
        [EnumMember]
        None = 0,
        [EnumMember]
        NoCurrent = 201,
        [EnumMember]
        FollowingError = 202,
        [EnumMember]
        VTMOverload = 203,
        [EnumMember]
        WrongIdentification = 204,
        [EnumMember]
        SCurveRateIsTooLarge = 205,
        [EnumMember]
        Stopped = 206
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
        public bool IsSelfTest { get; set; }

        [DataMember]
        public VTMTestType TestType { get; set; }

        [DataMember]
        public ushort RampCurrent { get; set; }

        [DataMember]
        public ushort RampTime { get; set; }

        [DataMember]
        public bool IsRampOpeningEnabled { get; set; }

        [DataMember]
        public ushort RampOpeningCurrent { get; set; }

        [DataMember]
        public ushort RampOpeningTime { get; set; }

        [DataMember]
        public ushort SinusCurrent { get; set; }

        [DataMember]
        public ushort SinusTime { get; set; }

        [DataMember]
        public ushort CurveCurrent { get; set; }

        [DataMember]
        public ushort CurveTime { get; set; }

        [DataMember]
        public ushort CurveFactor { get; set; }

        [DataMember]
        public ushort CurveAddTime { get; set; }

        [DataMember]
        public bool UseFullScale { get; set; }

        [DataMember]
        public bool UseLsqMethod { get; set; }

        [DataMember]
        public ushort Count { get; set; }

        [DataMember]
        public float VTM { get; set; }

        public override bool IsHasChanges(BaseTestParametersAndNormatives oldParametersBase)
        {
            var oldParameters = oldParametersBase as TestParameters;

            if (oldParameters == null)
                throw new InvalidCastException("oldParameters must be slOldParameters");

            if (IsSelfTest != oldParameters.IsSelfTest)
                return true;
            if (TestType != oldParameters.TestType)
                return true;
            if (RampCurrent != oldParameters.RampCurrent)
                return true;
            if (RampTime != oldParameters.RampTime)
                return true;
            if (IsRampOpeningEnabled != oldParameters.IsRampOpeningEnabled)
                return true;
            if (RampOpeningCurrent != oldParameters.RampOpeningCurrent)
                return true;
            if (RampOpeningTime != oldParameters.RampOpeningTime)
                return true;
            if (SinusCurrent != oldParameters.SinusCurrent)
                return true;
            if (SinusTime != oldParameters.SinusTime)
                return true;
            if (CurveCurrent != oldParameters.CurveCurrent)
                return true;
            if (CurveTime != oldParameters.CurveTime)
                return true;
            if (CurveFactor != oldParameters.CurveFactor)
                return true;
            if (CurveAddTime != oldParameters.CurveAddTime)
                return true;
            if (UseFullScale != oldParameters.UseFullScale)
                return true;
            if (UseLsqMethod != oldParameters.UseLsqMethod)
                return true;
            if(Count != oldParameters.Count)
                return true;
            if (VTM.CompareTo(oldParameters.VTM) != 0)
                return true;

            return false;
        }


        public TestParameters()
        {
            IsEnabled = false;
            IsSelfTest = false;
            TestType = VTMTestType.Sinus;
            RampCurrent = 500;
            RampTime = 1000;
            RampOpeningCurrent = 100;
            RampOpeningTime = 50;
            IsRampOpeningEnabled = false;
            SinusCurrent = 500;
            SinusTime = 10000;
            CurveCurrent = 500;
            CurveTime = 4000;
            CurveFactor = 50;
            CurveAddTime = 0;
            Count = 1;
            VTM = 2.5f;
            TestParametersType = TestParametersType.StaticLoses;
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
        public float VTM { get; set; }

        public ResultNormatives()
        {
            VTM = 2.5f;
        }
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class TestResults: BaseTestResults
    {
        [DataMember]
        public float Voltage { get; set; }

        [DataMember]
        public ushort Current{ get; set; }

        [DataMember]
        public IList<short> ITMArray { get; set; }

        [DataMember]
        public IList<short> DesiredArray { get; set; }

        [DataMember]
        public IList<short> VTMArray { get; set; }

        [DataMember]
        public IList<short> SelfTestArray { get; set; }

        [DataMember]
        public IList<float> CapacitorsArray { get; set; }

        [DataMember]
        public bool IsSelftest { get; set; }

        public TestResults()
        {
            ITMArray = new List<short>();
            DesiredArray = new List<short>();
            VTMArray = new List<short>();
            SelfTestArray = new List<short>();
            CapacitorsArray = new List<float>();
        }
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class CalibrationParameters
    {
        [DataMember]
        public ushort ItmFineN { get; set; }

        [DataMember]
        public ushort ItmFineD { get; set; }

        [DataMember]
        public ushort VtmFineN { get; set; }

        [DataMember]
        public ushort VtmFineD { get; set; }

        [DataMember]
        public short VtmOffset { get; set; }

        [DataMember]
        public ushort PredictParamK1 { get; set; }

        [DataMember]
        public ushort PredictParamK2 { get; set; }

        [DataMember]
        public ushort VtmFine2N { get; set; }

        [DataMember]
        public ushort VtmFine2D { get; set; }
    }
}