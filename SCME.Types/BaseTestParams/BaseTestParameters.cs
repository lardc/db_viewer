using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace SCME.Types.BaseTestParams
{
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum TestParametersType
    {
        [EnumMember]
        Gate = 1,

        [EnumMember]
        StaticLoses = 2,

        [EnumMember]
        Bvt = 3,

        [EnumMember]
        Commutation = 4,

        [EnumMember]
        Clamping = 5,

        [EnumMember]
        Dvdt = 6,

        [EnumMember]
        Sctu = 7,

        [EnumMember]
        ATU = 8,

        [EnumMember]
        QrrTq = 9,

        [EnumMember]
        RAC = 10,

        [EnumMember]
        IH = 11,

        [EnumMember]
        RCC = 12
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public abstract class BaseTestParametersAndNormatives
    {
        [DataMember]
        public TestParametersType TestParametersType { get; set; }

        [DataMember]
        public int Order { get; set; }

        [DataMember]
        public long TestTypeId { get; set; }

        public abstract bool IsHasChanges(BaseTestParametersAndNormatives oldParametersBase);
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public abstract class BaseTestResults
    {
        [DataMember]
        public long TestTypeId { get; set; }
    }
}
