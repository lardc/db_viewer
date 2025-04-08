using System.Runtime.Serialization;

namespace SCME.Types.Clamping
{
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum SqueezingState
    {
        [EnumMember] Down = 0,
        [EnumMember] Squeezing,
        [EnumMember] Up,
        [EnumMember] Unsqueezing,
        [EnumMember] Undeterminated,
        [EnumMember] Heating,
        [EnumMember] Updating,
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum ClampingForce
    {
        [EnumMember] Contact = 0,
        [EnumMember] Custom
    }
}