using System;
using System.Runtime.Serialization;
using SCME.Types.BaseTestParams;

namespace SCME.Types.SCTU
{
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class SctuTestParameters : BaseTestParametersAndNormatives, ICloneable
    {
        public SctuTestParameters()
        {
            TestParametersType = TestParametersType.Sctu;
            Type = SctuDutType.Diode;
            WaveFormType = SctuWaveFormType.Sinusoidal;
            TrapezeEdgeTime = 100;
            Value = 100;
            ShuntResistance = 1;
        }

        [DataMember]
        public SctuDutType Type { get; set; }

        [DataMember]
        public SctuWaveFormType WaveFormType { get; set; }

        [DataMember]
        public ushort TrapezeEdgeTime { get; set; }

        [DataMember]
        public int Value { get; set; }

        [DataMember]
        public ushort ShuntResistance { get; set; }

        public override bool IsHasChanges(BaseTestParametersAndNormatives oldParametersBase)
        {
            return false;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }


    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum SctuDutType
    {
        [EnumMember]
        Diode = 1234,

        [EnumMember]
        Thyristor = 5678
    };


    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum SctuWaveFormType
    {
        [EnumMember]
        Sinusoidal = 43690,

        [EnumMember]
        Trapezium = 48059
    };
}
