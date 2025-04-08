using System.Runtime.Serialization;

namespace SCME.Types.Gateway
{
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWDeviceState
    {
        [EnumMember] None = 0,
        [EnumMember] Fault = 1,
        [EnumMember] Disabled = 2,
        [EnumMember] PowerReady = 3
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWFaultReason
    {
        [EnumMember] None = 0,
        [EnumMember] SafetyCircuit = 301,
        [EnumMember] LowPressure = 302
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWDisableReason
    {
        [EnumMember] None = 0,
        [EnumMember] BadClock = 1001
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWWarningReason
    {
        [EnumMember] None = 0,
        [EnumMember] WatchdogReset = 1001
    };
}