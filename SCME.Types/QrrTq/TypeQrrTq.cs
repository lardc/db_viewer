using System;
using System.Runtime.Serialization;
using SCME.Types.BaseTestParams;
using System.Collections.Generic;
using System.Linq;

namespace SCME.Types.QrrTq
{
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWDeviceState
    //список состояний блока QrrTq
    {
        //состояние после включения питания
        [EnumMember]
        DS_None = 0,

        //состояние fault (можно сбросить командой ACT_FAULT_CLEAR)
        [EnumMember]
        DS_Fault = 1,

        //состояние disabled (требуется перезапуск питания)
        [EnumMember]
        DS_Disabled = 2,

        //установка в процессе включения
        [EnumMember]
        DS_PowerOn = 3,

        //состояние готовности к новому измерению
        [EnumMember]
        DS_Ready = 4,

        //в процессе измерения
        [EnumMember]
        DS_InProcess = 5
    };
    /*
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
    */



    //режимы измерений (значения регистра 128)
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum TMode
    {
        [EnumMember]
        //Measure only reverse recovery (Qrr) parameters - измерение только параметров обратного восстановления (применим для диодов и тиристоров)
        Qrr = 0,

        [EnumMember]
        //Measure Qrr-tq parameters - измерение параметров обратного восстановления и времени выключения (применим только для тиристоров)
        QrrTq = 1,
    };


    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum TDcFallRate
    {
        [EnumMember]
        r2 = 2,

        [EnumMember]
        r5 = 5,

        [EnumMember]
        r10 = 10,

        [EnumMember]
        r15 = 15,

        [EnumMember]
        r20 = 20,

        [EnumMember]
        r30 = 30,

        [EnumMember]
        r50 = 50,

        [EnumMember]
        r60 = 60,

        [EnumMember]
        r100 = 100
    };

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class TDcFallRateHelper : Object
    {
        public static Array EnumValues()
        {
            return Enum.GetValues(typeof(TDcFallRate)).Cast<uint>().Select(x => x.ToString()).ToArray();
        }
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum TOsvRate
    {
        [EnumMember]
        r20 = 20,

        [EnumMember]
        r50 = 50,

        [EnumMember]
        r100 = 100,

        [EnumMember]
        r200 = 200
    }

    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class TOsvRateHelper : Object
    {
        public static Array EnumValues()
        {
            return Enum.GetValues(typeof(TOsvRate)).Cast<uint>().Select(x => x.ToString()).ToArray();
        }
    }

    //параметры, задающие режим работы
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class TestParameters : BaseTestParametersAndNormatives, ICloneable
    {
        [DataMember]
        public bool IsEnabled { get; set; }

        //Measurement mode - режим измерения
        [DataMember]
        public TMode Mode { get; set; }

        //Direct current amplitude (in A) – амплитуда прямого тока(в А)
        [DataMember]
        public ushort DirectCurrent { get; set; }

        //Direct current pulse duration (in us) – длительность импульса прямого тока(в мкс)
        [DataMember]
        public ushort DCPulseWidth { get; set; }

        //Direct current rise rate (in A/us) – скорость нарастания прямого тока(в А/мкс)
        [DataMember]
        public float DCRiseRate { get; set; }

        //Direct current fall rate (in A/us) – скорость спада тока(в А/мкс)
        [DataMember]
        public TDcFallRate DCFallRate { get; set; }

        //Off-state voltage (in V) – прямое повторное напряжение(в В)
        [DataMember]
        public ushort OffStateVoltage { get; set; }

        //Off-state voltage rise rate (in V/us) – скорость нарастания прямого повторного напряжения(в В/мкс)
        [DataMember]
        public TOsvRate OsvRate { get; set; }

        //Измерение trr по методу 90%-50%
        [DataMember]
        public bool TrrMeasureBy9050Method { get; set; }


        //Actual DC current value (in A) – фактическое значение прямого тока(в А)
        [DataMember]
        public short Idc { get; set; }

        //Reverse recovery charge (in uC) – заряд обратного восстановления(в мкКл)
        [DataMember]
        public short Qrr { get; set; }

        //Reverse recovery current amplitude (in A) – ток обратного восстановления(в А)
        [DataMember]
        public short Irr { get; set; }

        //Reverse recovery time (in us) – время обратного восстановления(в мкс)
        [DataMember]
        public short Trr { get; set; }

        [DataMember]
        //Фактическая скорость спада тока, А/мкс
        public float DCFactFallRate { get; set; }

        //Turn-off time (in us) – время выключения(в мкс)
        [DataMember]
        public short Tq { get; set; }

        public TestParameters()
        {
            TestParametersType = TestParametersType.QrrTq;
            IsEnabled = true;
            Mode = TMode.Qrr;
            DirectCurrent = 50;
            DCPulseWidth = 500;
            DCRiseRate = 0.2f;
            DCFallRate = TDcFallRate.r2;
            OffStateVoltage = 400;
            OsvRate = TOsvRate.r20;
            TrrMeasureBy9050Method = false;
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

            TestParameters QrrTqOldParameters = (TestParameters)oldParameters;

            if (Mode != QrrTqOldParameters.Mode)
                return true;

            if (DirectCurrent != QrrTqOldParameters.DirectCurrent)
                return true;

            if (DCPulseWidth != QrrTqOldParameters.DCPulseWidth)
                return true;

            if (DCRiseRate != QrrTqOldParameters.DCRiseRate)
                return true;

            if (DCFallRate != QrrTqOldParameters.DCFallRate)
                return true;

            if (OffStateVoltage != QrrTqOldParameters.OffStateVoltage)
                return true;

            if (OsvRate != QrrTqOldParameters.OsvRate)
                return true;

            if (TrrMeasureBy9050Method != QrrTqOldParameters.TrrMeasureBy9050Method)
                return true;

            if (Idc != QrrTqOldParameters.Idc)
                return true;

            if (Qrr != QrrTqOldParameters.Qrr)
                return true;

            if (Irr != QrrTqOldParameters.Irr)
                return true;

            if (Trr != QrrTqOldParameters.Trr)
                return true;

            if (DCFactFallRate != QrrTqOldParameters.DCFactFallRate)
                return true;

            if (Tq != QrrTqOldParameters.Tq)
                return true;

            return false;
        }
    }

    //параметры измеренных значений
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public class TestResults : BaseTestResults
    {
        //режим работы блока QrrTq при котором выполнялись измерения
        [DataMember]
        public TMode Mode { get; set; }

        //Off-state voltage (in V) – прямое повторное напряжение(в В)
        [DataMember]
        public ushort OffStateVoltage { get; set; }

        //Off-state voltage rise rate (in V/us) – скорость нарастания прямого повторного напряжения(в В/мкс)
        [DataMember]
        public ushort OsvRate { get; set; }


        //Actual DC current value (in A) – фактическое значение прямого тока(в А)
        [DataMember]
        public short Idc { get; set; }

        //Reverse recovery charge (in uC) – заряд обратного восстановления(в мкКл)
        [DataMember]
        public float Qrr { get; set; }

        //Reverse recovery current amplitude (in A) – ток обратного восстановления(в А)
        [DataMember]
        public short Irr { get; set; }

        //Reverse recovery time (in us) – время обратного восстановления(в мкс)
        [DataMember]
        public float Trr { get; set; }

        [DataMember]
        //Фактическая скорость спада тока, А/мкс
        public float DCFactFallRate { get; set; }

        //Turn-off time (in us) – время выключения(в мкс)
        [DataMember]
        public float Tq { get; set; }

        //Данные для построения графика тока
        [DataMember]
        public List<short> CurrentData { get; set; }

        //Данные для построения графика напряжения
        [DataMember]
        public List<short> VoltageData { get; set; }

        public TestResults()
        {
            CurrentData = new List<short>();
            VoltageData = new List<short>();
        }
    }

    //код окончания измерений - расшифровка значения регистра 197
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum HWFinishedResult
    {
        //No information or not finished
        [EnumMember]
        None = 0,

        //Operation was successful
        [EnumMember]
        Ok = 1,

        //Operation failed
        [EnumMember]
        Fail = 2
    };

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