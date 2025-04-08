using System;
using System.Runtime.Serialization;
using SCME.Types.BaseTestParams;
using System.Collections.Generic;
using System.Linq;

namespace SCME.Types.RCC
{
    //параметры, задающие режим работы
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class TestParameters : BaseTestParametersAndNormatives, ICloneable
    {
        [DataMember]
        public bool IsEnabled { get; set; }

        public TestParameters()
        {
            TestParametersType = TestParametersType.RCC;
            IsEnabled = true;
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

            return false;
        }
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum RCCResult
    {
        [EnumMember]
        OPRESULT_NONE = 0,
        [EnumMember]
        OPRESULT_OK =   1,   //нет КЗ в цепи катод-катод, данный прибор имеет смысл тестировать
        [EnumMember]
        OPRESULT_FAIL = 2  //КЗ в цепи катод-катод, данный прибор есть брак
    }

    //параметры измеренных значений
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class TestResults : BaseTestResults
    {
        [DataMember]
        public RCCResult RCC { get; set; }

        public TestResults()
        {
            RCC = RCCResult.OPRESULT_NONE;
        }
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWProblemReason
    {
        [EnumMember]
        None = 0
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
