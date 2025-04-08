using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SCME.Types.DataContracts
{
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class MmeCode
    {
        [DataMember]
        public long Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public List<ProfileMme> ProfileMmes { get; set; }
    }
}
