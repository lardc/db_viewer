using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace SCME.Types.Profiles
{
    [DataContract(Namespace = "http://proton-electrotex.com/SCME")]
    public enum TemperatureCondition
    {
        [EnumMember]
        None = 0,
        [EnumMember]
        RT = 1,
        [EnumMember]
        TM = 2
    }

    public static class ProfileRoutines
    {
        public static SubjectForMeasure CalcSubjectForMeasure(string profileName)
        {
            //вычисляет по коду профиля profileName предмет измерения (см. описание SubjectForMeasure)
            var result = Types.SubjectForMeasure.Unknown;

            if (profileName != null)
            {
                profileName = profileName.Trim();

                if (profileName != string.Empty)
                {
                    string subjectForMeasure = profileName.Substring(0, 3);
                    Enum.TryParse(subjectForMeasure, true, out result);
                }
            }

            return result;
        }

        public static string StringTemperatureConditionByProfileName(string profileName)
        {
            //'PSERT Т123 320 16 A2'
            if ((profileName == null) || (profileName == string.Empty))
                return null;

            return profileName.Substring(3, 2);
        }

        public static TemperatureCondition TemperatureConditionByProfileName(string profileName)
        {
            TemperatureCondition result = TemperatureCondition.None;

            if (profileName.Trim() != string.Empty)
            {
                string profileNameUpper = profileName.ToUpper();

                result = profileNameUpper.Contains("RT") ? TemperatureCondition.RT : profileNameUpper.Contains("TM") ? TemperatureCondition.TM : TemperatureCondition.None;
            }

            return result;
        }      

        /*
        public static string SpecialMarkByProfileBody(string profileBody)
        {
            //извлекает обозначение спецмаркировки из обозначения тела профиля profileBody - оно располагается сразу за второй * в profileBody
            int start = profileBody.IndexOf("*", 0);
            if (start == -1)
                return string.Empty;

            start = profileBody.IndexOf("*", start + 1);
            if (start == -1)
            {
                return string.Empty;
            }
            else return profileBody.Substring(start);
        }
        */

        /*
        public static string SpecialMarkByProfileName(string profileName)
        {
            //извлекает обозначение спецмаркировки из обозначения профиля ProfileName. она начинается после 4-го пробела
            //PSETm Т253 1390 24 GvA 03 08
            return string.Empty;
        }
        */

        public static string MakeRT(string profileName)
        {
            //меняет в принятом обозначении профиля profileName либо TM на RT, либо Th на RT - что найдёт, то и заменит на RT
            return System.Text.RegularExpressions.Regex.Replace(profileName, "TM", "RT", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        public static string MakeTM(string profileName)
        {
            //меняет в принятом обозначении профиля profileName либо RT на TM, либо Th на TM - что найдёт, то и заменит на TM
            return System.Text.RegularExpressions.Regex.Replace(profileName, "(RT)", "TM", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        /*
        public static string ProfileRTBodyByProfileBody(string profileBody)
        {
            string result = string.Empty;

            switch (TemperatureConditionByProfileName(profileBody))
            {
                case TemperatureCondition.TM:
                    //меняем в profileBody TM на RT
                    result = MakeRT(profileBody);
                    break;
            }

            return result;
        }

        public static string ProfileTMBodyByProfileBody(string profileBody)
        {
            string result = string.Empty;

            switch (TemperatureConditionByProfileName(profileBody))
            {
                case TemperatureCondition.RT:
                    //меняем в profileBody RT на TM
                    result = MakeTM(profileBody);
                    break;
            }

            return result;
        }
        */


        /*
        public static string PairProfileBodyByProfileName(string profileName, out string profileBody)
        {
            //пара всегда есть связка RT-TM или TM-RT. порядок внутри пары (двойки) может быть любым
            //вычисляет тело пары по принятому profileName. в out profileBody возвращает тело профиля с тем же температурным режимом, что и у принятого profileName
            profileBody = ProfileBodyByProfileName(profileName);

            switch (TemperatureConditionByProfileName(profileName))
            {
                case TemperatureCondition.RT:
                    //меняем в вычисленном profileBody RT на TM
                    return ProfileTMBodyByProfileBody(profileBody);

                case TemperatureCondition.TM:
                    //меняем в вычисленном profileBody TM на RT
                    return ProfileRTBodyByProfileBody(profileBody); ;

                default:
                    return profileBody;
            }
        }
        */

        public static int? DeviceClassByProfileName(string profileName)
        {
            //извлечение класса из наименования профиля. класс в наименовании профиля записан целым числом начиная с символа, следующего за 3-ым пробелом до 4-го пробела при его наличии, если же 4-го пробела нет - то до конца строки
            string profileNameUpper = profileName.ToUpper();
            const string delimeter = " ";

            int start = 0;
            for (int i = 1; i <= 3; i++)
            {
                start = profileNameUpper.IndexOf(delimeter, start);

                if (start == -1)
                    return null;
                else start++;
            }

            int end = profileNameUpper.IndexOf(delimeter, start);

            string cl = (end == -1) ? profileNameUpper.Substring(start) : profileNameUpper.Substring(start, end - start);

            int result;

            switch (int.TryParse(cl, out result))
            {
                case true:
                    return result;

                default:
                    return null;
            }
        }
    }
}
