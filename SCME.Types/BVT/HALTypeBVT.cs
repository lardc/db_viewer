using System.Runtime.Serialization;

namespace SCME.Types.BVT
{
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum BVTTestType
    {
        [EnumMember] Both,
        [EnumMember] Direct,
        [EnumMember] Reverse
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum BVTMeasurementMode
    {
        [EnumMember] ModeI,
        [EnumMember] ModeV
    };

    public static class BVTRates
    {
        public static string[] GetRates()
        {
            return new [] {"5", "50"};
        }
    }
}