using System;
using System.Runtime.Serialization;

namespace SCME.Types.QRR
{
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWDeviceState
    {
        [EnumMember] None = 0,
        [EnumMember] Fault = 1,
        [EnumMember] Disabled = 2,
        [EnumMember] PowerReady = 3,
        [EnumMember] InProcess = 4
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWFaultReason
    {
        [EnumMember] None = 0
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWWarningReason
    {
        [EnumMember] None = 0,
        [EnumMember] WatchdogReset = 1001
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWOperationResult
    {
        [EnumMember] InProcess = 0,
        [EnumMember] Success = 1,
        [EnumMember] Fail = 2
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class TestParameters : ICloneable
    {
        [DataMember]
        public bool IsEnabled { get; set; }

        public TestParameters()
        {
            IsEnabled = false;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class TestResults
    {
        public TestResults()
        {
        }
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class CalibrationParams
    {
    }
}