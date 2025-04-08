using System;
using System.Runtime.Serialization;
using SCME.Types.BaseTestParams;


namespace SCME.Types.ATU
{
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWDeviceState
    //список состояний блока ATU
    {
        //блок в неопределенном состоянии (после включения питания)
        [EnumMember]
        DS_None = 0,

        //блок в состоянии Fault
        [EnumMember]
        DS_Fault = 1,

        //блок в состоянии Disabled
        [EnumMember]
        DS_Disabled = 2,

        //блок в состоянии ожидания заряда конденсаторной батареи
        [EnumMember]
        DS_BatteryCharge = 3,

        //блок в состоянии готовности
        [EnumMember]
        DS_Ready = 4,

        //блок в процессе работы
        [EnumMember]
        DS_InProcess = 5,
    };


    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWOperationResult
    {
        [EnumMember]
        InProcess = 0,

        [EnumMember]
        Success = 1,

        [EnumMember]
        Fail = 2
    };


    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class TestParameters : BaseTestParametersAndNormatives, ICloneable
    {
        [DataMember]
        public bool IsEnabled { get; set; }

        [DataMember]
        public ushort PrePulseValue { get; set; }

        [DataMember]
        public float PowerValue { get; set; }

        [DataMember]
        public short UBR { get; set; }

        [DataMember]
        public short UPRSM { get; set; }

        [DataMember]
        public float IPRSM { get; set; }

        [DataMember]
        public float PRSM { get; set; }

        public TestParameters()
        {
            TestParametersType = TestParametersType.ATU;
            IsEnabled = true;
            PrePulseValue = 100; //мА
            PowerValue = 16;     //кВт
        }


        public object Clone()
        {
            return MemberwiseClone();
        }


        public override bool IsHasChanges(BaseTestParametersAndNormatives oldParameters)
        {
            if (oldParameters == null) throw new ArgumentNullException("Метод '" + System.Reflection.MethodBase.GetCurrentMethod().Name + "' получил на вход параметр 'oldParameters' равный Null.");

            //сравниваем свой хеш с хешем принятого oldParameters и если они одинаковы - значит мы имеем дело с одним и тем же экземпляром
            if (this.GetHashCode() == oldParameters.GetHashCode()) return false;

            //раз мы сюда добрались - имеем дело с разными экземплярами, необходимо сравнение их содержимого
            string typeName = oldParameters.GetType().Name;

            if (typeName != "TestParameters") throw new InvalidCastException("Method '" + System.Reflection.MethodBase.GetCurrentMethod().Name + "' получил на вход параметр 'oldParameters' тип которого '" + typeName + "'. Ожидался тип параметра 'TestParameters'.");

            TestParameters aTUOldParameters = (TestParameters)oldParameters;

            if (PrePulseValue != aTUOldParameters.PrePulseValue)
                return true;

            if (PowerValue != aTUOldParameters.PowerValue)
                return true;

            return false;
        }
    }


    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class TestResults : BaseTestResults
    {
        [DataMember]
        public short UBR { get; set; }

        [DataMember]
        public short UPRSM { get; set; }

        [DataMember]
        public float IPRSM { get; set; }

        [DataMember]
        public float PRSM { get; set; }
    }


    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class CalibrationParams
    {

    }


    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWWarningReason
    {
        [EnumMember]
        None = 0,

        [EnumMember]
        Idle = 1,          //ХХ на выходе

        [EnumMember]
        Short = 2,         //КЗ на выходе

        [EnumMember]
        PowerAccuracy = 3, //Погрешность полученной мощности велика

        [EnumMember]
        BreakDUT = 4,      //Пробой прибора

        [EnumMember]
        FacetBreak = 5,    //Краевой пробой прибора
    };


    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWFaultReason
    {
        [EnumMember]
        None = 0,

        [EnumMember]
        ChargeError = 1, //Ошибка заряда батареи

        [EnumMember]
        ADCError = 2,    //Ошибка оцифровки значений напряжения/тока
    };


    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWDisableReason
    {
        [EnumMember]
        None = 0,
    }

}