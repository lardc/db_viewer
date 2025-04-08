using System;
using System.Runtime.Serialization;
using SCME.Types.BaseTestParams;
using System.Collections.Generic;
using System.Linq;

namespace SCME.Types.IH
{
    //параметры, задающие режим работы
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class TestParameters : BaseTestParametersAndNormatives, ICloneable
    {
        [DataMember]
        public bool IsEnabled { get; set; }

        //Форсирующий ток Itm, А
        [DataMember]
        public ushort Itm { get; set; }

        //Ток удержания Ih, мА
        [DataMember]
        public ushort Ih { get; set; }

        public TestParameters()
        {
            TestParametersType = TestParametersType.IH;
            IsEnabled = true;
            Itm = 500;
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

            TestParameters IHOldParameters = (TestParameters)oldParameters;

            if (Itm != IHOldParameters.Itm)
                return true;            

            return false;
        }
    }

    //параметры измеренных значений
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class TestResults : BaseTestResults
    {
        //Ток удержания Ih, мА
        [DataMember]
        public ushort Ih { get; set; }
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
