using System.Runtime.Serialization;

namespace SCME.Types.SL
{
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum VTMTestType
    {
        [EnumMember] Ramp,
        [EnumMember] Sinus,
        [EnumMember] Curve
    };
}