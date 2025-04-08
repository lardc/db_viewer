using System.Runtime.Serialization;
using SCME.Types.BaseTestParams;
using System.Collections.Generic;

namespace SCME.Types.SCTU
{
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class SctuTestResults : BaseTestResults
    {
        /// <summary>
        /// Напряжение мВ
        /// </summary>
        [DataMember]
        public int VoltageValue { get; set; }

        /// <summary>
        /// Ударный ток А
        /// </summary>
        [DataMember]
        public int CurrentValue { get; set; }

        /// <summary>
        /// Коэффициент усиления
        /// </summary>
        [DataMember]
        public double MeasureGain { get; set; }

        /// <summary>
        /// Данные для построения графика напряжения
        /// </summary>
        [DataMember]
        public List<short> VoltageData { get; set; }

        /// <summary>
        /// Данные для построения графика тока
        /// </summary>
        [DataMember]
        public List<short> CurrentData { get; set; }

        public SctuTestResults()
        {
            VoltageData = new List<short>();
            CurrentData = new List<short>();
        }
    }
}
