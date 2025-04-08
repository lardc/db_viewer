using SCME.Types.Profiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SCME.dbViewer
{
    public static class Dictionaries
    {
        private static readonly Dictionary<string, string> RTConditionsNames;
        private static readonly Dictionary<string, string> TMConditionsNames;
        private static readonly Dictionary<string, string> ConditionsUnitMeasure;
        private static readonly Dictionary<string, string> ParametersName;
        private static readonly Dictionary<string, string> ParametersFormat;

        static Dictionaries()
        {
            //имена условий зависят от температурного режима. здесь хранятся соответствия имён условий базы данных именам условий RT, которые хочет видеть пользователь приложения
            RTConditionsNames = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                {"CLAMP_Temperature", "T"},

                {"SL_ITM", "ITM"},

                {"BVT_VD", "UDRM"},
                {"BVT_VR", "UBRmax"},
                {"BVT_UdsmUrsm_VD", "UDSM"},
                {"BVT_UdsmUrsm_VR", "URSM"},

                {"DVDT_VoltageRate", "dU/dt"},

                {"QrrTq_DCFallRate", "dIdt"}
            };

            //имена условий зависят от температурного режима. здесь хранятся соответствия имён условий базы данных именам условий TM, которые хочет видеть пользователь приложения
            TMConditionsNames = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                {"CLAMP_Temperature", "T"},

                {"SL_ITM", "ITM"},

                {"BVT_VD", "UDRM"},
                {"BVT_VR", "URRM"},
                {"BVT_UdsmUrsm_VD", "UDSM"},
                {"BVT_UdsmUrsm_VR", "URSM"},

                {"DVDT_VoltageRate", "dU/dt"},

                {"QrrTq_DCFallRate", "dIdt"}
            };

            //здесь храним значения единиц измерения условий
            ConditionsUnitMeasure = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                {"SL_ITM", "А"},

                {"BVT_I", "мА"},
                {"BVT_VD", "В"},
                {"BVT_VR", "В"},
                {"BVT_UdsmUrsm_VD", "В"},
                {"BVT_UdsmUrsm_VR", "В"},

                {"DVDT_VoltageRate", "В/мкс"},

                {"QrrTq_DCFallRate", "А/мкс"}
            };

            //имена параметров не зависят от температурного режима. здесь хранятся соответствия имён измеряемых параметров базы данных именам измеряемых параметров, которые хочет видеть пользователь приложения
            ParametersName = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                {"VDRM", "UBO"},
                {"VRRM", "UBR"}
            };

            //здесь храним форматы отображения измеряемых параметров
            ParametersFormat = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                {"VTM", "F"}, //0.00
                {"VFM", "F"}  //0.00
            };
        }

        public static string ConditionName(TemperatureCondition temperatureCondition, string conditionName)
        {
            Dictionary<string, string> dictionary = (temperatureCondition == TemperatureCondition.None) ? null : (temperatureCondition == TemperatureCondition.RT) ? RTConditionsNames : TMConditionsNames;

            if (dictionary == null)
                return conditionName;

            switch (dictionary.ContainsKey(conditionName))
            {
                case true:
                    return dictionary[conditionName];

                default:
                    return conditionName;
            }
        }

        public static string ConditionUnitMeasure(string conditionName)
        {
            switch (ConditionsUnitMeasure.ContainsKey(conditionName))
            {
                case true:
                    return ConditionsUnitMeasure[conditionName];

                default:
                    return null;
            }
        }

        public static string ParameterName(string parameterName)
        {
            string result;

            switch (ParametersName.ContainsKey(parameterName))
            {
                case true:
                    result = ParametersName[parameterName];
                    break;

                default:
                    result = parameterName;
                    break;
            }

            //если первый символ параметра начинается на V - заменяем его на U
            result = Routines.VtoU(result);

            return result;
        }

        public static string ParameterName(string parameterName, out string formatValue)
        {
            //отличается от предыдущей реализации наличием out параметра formatValue
            formatValue = ParameterFormat(parameterName);

            return ParameterName(parameterName);
        }

        public static string ParameterFormat(string parameterName)
        {
            switch (ParametersFormat.ContainsKey(parameterName))
            {
                case true:
                    return ParametersFormat[parameterName];

                default:
                    return "F";
            }
        }
    }
}
