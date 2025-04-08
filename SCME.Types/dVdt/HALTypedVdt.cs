using System.Runtime.Serialization;

namespace SCME.Types.dVdt
{
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum DvdtMode
    {
        [EnumMember]
        Confirmation = 0,
        [EnumMember]
        Detection = 1
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum VoltageRate
    {
        [EnumMember]
        V500 = 500,

        [EnumMember]
        V1000 = 1000,

        [EnumMember]
        V1600 = 1600,

        [EnumMember]
        V2000 = 2000,

        [EnumMember]
        V2500 = 2500
    };
}