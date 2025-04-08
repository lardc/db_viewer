using System.Runtime.Serialization;

namespace SCME.Types.Commutation
{
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum CommutationMode
    {
        [EnumMember]
        None = 0,
        [EnumMember]
        Gate,
        [EnumMember]
        SL,
        [EnumMember]
        BVTD,
        [EnumMember]
        BVTR,
        [EnumMember]
        DVDT,
        [EnumMember]
        ATU,
        [EnumMember]
        RAC
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum ModuleCommutationType
    {
        [EnumMember]
        Direct = 0x00,
        [EnumMember]
        MT1 = 0x01,
        [EnumMember]
        MD1 = 0x02,
        [EnumMember]
        MT3 = 0x03,
        [EnumMember]
        MT4 = 0x04,
        [EnumMember]
        MT5 = 0x05,
        [EnumMember]
        MD3 = 0x06,
        [EnumMember]
        MD4 = 0x07,
        [EnumMember]
        MD5 = 0x08,
        [EnumMember]
        MTD3 = 0x09,
        [EnumMember]
        MDT3 = 0x0A,
        [EnumMember]
        MTD4 = 0x0B,
        [EnumMember]
        MDT4 = 0x0C,
        [EnumMember]
        MTD5 = 0x0D,
        [EnumMember]
        MDT5 = 0x0E,
        [EnumMember]
        Reverse = 0x0F
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum ModulePosition
    {
        [EnumMember]
        P1 = 0,
        [EnumMember]
        P2
    }
}