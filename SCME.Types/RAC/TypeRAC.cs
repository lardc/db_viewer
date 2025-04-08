using System;
using System.Runtime.Serialization;
using SCME.Types.BaseTestParams;
using System.Collections.Generic;
using System.Linq;

namespace SCME.Types.RAC
{
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWDeviceState
    //список состояний блока RAC
    {
        //состояние после включения питания
        [EnumMember]
        DS_None = 0,

        //состояние fault (состояние ошибки, которое можно сбросить)
        [EnumMember]
        DS_Fault = 1,

        //состояние disabled (состояние ошибки, требующее перезапуска питания)
        [EnumMember]
        DS_Disabled = 2,

        //заглушка
        [EnumMember]
        DS_Dummy = 3,

        //включён и готов к работе
        [EnumMember]
        DS_Powered = 4,

        //DS_InProcess
        [EnumMember]
        DS_InProcess = 5
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWOperationResult
    {
        [EnumMember]
        //No information or not finished
        None = 0,

        [EnumMember]
        //Operation was successful
        OK = 1,

        [EnumMember]
        //Operation failed
        Fail = 2
    }

    //параметры, задающие режим работы
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class TestParameters : BaseTestParametersAndNormatives, ICloneable
    {
        [DataMember]
        public bool IsEnabled { get; set; }

        //DC voltage value for resistance measurement (in V) - амплитуда напряжения для измерения (в В)
        [DataMember]
        public ushort ResVoltage { get; set; }

        [DataMember]
        public float ResultR { get; set; }

        public TestParameters()
        {
            TestParametersType = TestParametersType.RAC;
            IsEnabled = true;

            ResVoltage = 1000;
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

            TestParameters RACOldParameters = (TestParameters)oldParameters;

            if (ResVoltage != RACOldParameters.ResVoltage)
                return true;

            if (ResultR != RACOldParameters.ResultR)
                return true;

            return false;
        }
    }

    //параметры измеренных значений
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class TestResults : BaseTestResults
    {
        //Resistance result (in MOhm) - сопротивление изоляции (в МОм)
        [DataMember]
        public float ResultR { get; set; }
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWProblemReason
    {
        [EnumMember]
        None = 0,
        StateIsNoGood = 1000 //в описании блока не было такого значения, зарезервировал его себе для случая не корректного состояния блока
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWWarningReason
    {
        [EnumMember]
        None = 0
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWFaultReason
    {
        [EnumMember]
        None = 0
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWDisableReason
    {
        [EnumMember]
        None = 0
    }
}