using System.Runtime.Serialization;

namespace SCME.Types.DataContracts
{

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class ProfileMme
    {
        [DataMember]
        public long Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public bool IsSelected { get; set; }
    }
}
