using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SCME.Common
{
    public class IntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((int)value == 0) ? Visibility.Hidden : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack method is not implemented in IntToVisibilityConverter");
        }
    }

    public class ObjectToBoolConverter : IValueConverter
    {
        //используем данный Converter только для того, чтобы CheckBox не получал через механизм binding значения null - нам надо оперировать только значениями true, false 
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if (value is bool b)
                    return b;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if (value is bool b)
                    return b;
            }

            return false;
        }
    }

    public class AssemblyProtocolModeToColumnSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? 1 : 2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack method is not implemented in AssemblyProtocolModeToColumnSpanConverter");
        }
    }

    public class AssemblyProtocolModeToGridMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? "0,60,0,0" : "240,-9,15,0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack method is not implemented in AssemblyProtocolModeToGridMarginConverter");
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack method is not implemented in BoolToVisibilityConverter");
        }
    }

    public class BitValueByNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((value is ulong permissionsLo) && byte.TryParse(parameter.ToString(), out byte bitNumber))
            {
                bool b = Routines.CheckBit(permissionsLo, bitNumber);// ? Visibility.Visible : Visibility.Collapsed;

                return b;
            }

            return false; // Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack method is not implemented in PermissionsLoByBitNumToVisibility");
        }
    }

    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type ttargetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack method is not implemented in InverseBoolToVisibilityConverter");
        }
    }

    public class DoubleValueConverter : IValueConverter
    {
        public object Convert(object value, Type TargetType, object parameter, CultureInfo culture)
        {
            //чтение данных
            //во входном  parameter принимает значение по умолчанию, которое будет возвращено если конвертируемое значение равно DBNull.Value
            object defaultValue = parameter ?? DBNull.Value;

            return (value == DBNull.Value) ? defaultValue : (object)Routines.DoubleToStr((double)value);
        }

        public object ConvertBack(object value, Type TargetType, object parameter, CultureInfo culture)
        {
            //запись данных
            bool isParced = Routines.TryStringToDouble(value.ToString(), out double d);

            return isParced ? d : 0;
        }
    }

    /*
    public class StringExistToBoolConverter : IValueConverter
    {
        public object Convert(object Value, Type TargetType, object Parameter, CultureInfo Culture)
        {
            if ((Value == null) || (Value.ToString() == string.Empty))
            {
                return true;
            }
            else
                return false;
        }

        public object ConvertBack(object Value, Type TargetType, object Parameter, CultureInfo Culture)
        {
            throw new NotImplementedException("ConvertBack method is not implemented in StringExistToBoolConverter");
        }
    }
    */
}
