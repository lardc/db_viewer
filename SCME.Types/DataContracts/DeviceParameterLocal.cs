using System.Runtime.Serialization;

namespace SCME.Types.DataContracts
{
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class DeviceParameterLocal
    {
        [DataMember]
        public long ParameterId { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public float Value { get; set; }

        [DataMember]
        public long TestTypeId { get; set; }
    }
}