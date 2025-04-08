using System;
using System.Windows.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace SCME.dbViewer
{
    public class PermissionBitToEnableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //в value принимает битовую маску доступа пользователя системы
            //в parameter принимает наименование поля к которому пользователь хочет получить доступ
            //определяет номер бита, соответствующий наименованию поля, считывает состояние этого бита из принятой битовой маски и возвращает в качестве результата
            if ((value is ulong permissionsLo) && (parameter is string fieldName))
            {
                switch (fieldName)
                {
                    //используется для управления свойством IsReadOnly - поэтому возвращаем инверсное значение бита разрешения
                    case Common.Constants.DeviceComments:
                        return !Common.Routines.IsUserCanReadCreateComments(permissionsLo);
                }                
            }
            
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack method is not implemented in PermissionBitToEnableConverter.");
        }
    }

    public class ChoiceEnabledMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            //нулевой параметр - item текущей записи
            //первый параметр - bool флаг 'Включен режим просмотра протокола сборки'
            //второй параметр - битовая маска прав пользователя, из которой читаются права на работу с протоколом сборки
            //таблица согласно которой должен вычисляться возвращаемый результат
            // нулевой     первый     результат
            //    0           0           1
            //    0           1           0
            //    1           0           0
            //    1           1           1
            //как видно f(параметр0, параметр1) есть исключающее или с инверсией
            //результат = f(параметр0, параметр1) & f(параметр2)

            if ((values[0] is CustomControls.DynamicObj item) && item.GetMember(Common.Constants.AssemblyProtocolDescr, out object value))
            {
                if (values[1] is bool assemblyProtocolMode)
                {
                    bool assemblyProtocolExist = false;

                    if (value is string assemblyProtocolDescr)
                        assemblyProtocolExist = ((assemblyProtocolDescr == null) || (assemblyProtocolDescr == string.Empty)) ? false : true;

                    bool res = !(assemblyProtocolMode ^ assemblyProtocolExist);

                    //проверям права на формирование и расформирование протокола сборки только если в этом есть смысл
                    if (res)
                    {
                        if (ulong.TryParse(values[2].ToString(), out ulong permissionsLo))
                        {
                            //устанавливать/сбрасывать выбор записи будем разрешать при наличии права на работу с протоколом сборки
                            bool permissionGranted = Common.Routines.IsUserCanWorkWithAssemblyProtocol(permissionsLo);

                            return res && permissionGranted;
                        }
                    }
                    else
                        return res;
                }
            }

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ToolTipMultiConverter : IMultiValueConverter
    {
        private object ValueByColumnName(string columnName, CustomControls.DynamicObj item)
        {
            if (!string.IsNullOrEmpty(columnName) && (item != null))
            {
                columnName = Routines.NameOfHiddenColumn(columnName);

                return item.GetMember(columnName, out object value) ? ((value == DBNull.Value) ? Constants.noData : value) : Constants.noData;
            }

            return Constants.noData;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if ((values[0] is DataGridCell cell) && (cell.Column is DataGridBoundColumn column) && (values[1] is CustomControls.DynamicObj item))
            {
                string sourceFieldName = Common.Routines.SourceFieldNameByColumn(column);

                return this.ValueByColumnName(sourceFieldName, item);
            }

            return Constants.noData;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DeviceTypeAssemblyProtocolModeToVisibilityMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if ((values[0] is string[] deviceTypeArray) && (values[1] is bool assemblyProtocolMode))
            {
                string deviceTypeRU = deviceTypeArray[1];

                switch (string.IsNullOrEmpty(deviceTypeRU))
                {
                    case true:
                        return Visibility.Hidden;

                    default:
                        return (assemblyProtocolMode && (deviceTypeRU[0].ToString().ToUpper() == "М")) ? Visibility.Visible : Visibility.Hidden;
                }
            }

            return Visibility.Hidden;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ValueToBrushMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if ((values[0] is DataGridCell cell) && (cell.Column is DataGridBoundColumn column) && (values[1] is CustomControls.DynamicObj item) && (values[2] is DataGrid dataGrid))
            {
                string sourceFieldName = Common.Routines.SourceFieldNameByColumn(column);
                NrmStatus nrmStatus = Routines.IsInNrm(item, sourceFieldName);

                switch (nrmStatus)
                {
                    case (NrmStatus.Good):
                        //в норме
                        return dataGrid.FindResource("ValueInNrm");

                    case (NrmStatus.Defective):
                        //за пределами норм
                        return dataGrid.FindResource("ValueOutSideTheNorm");

                    case (NrmStatus.NotSetted):
                        //норма не установлена
                        return dataGrid.FindResource("NrmNotSetted");

                    case (NrmStatus.LegallyAbsent):
                        //норма законно отсутствует
                        return dataGrid.FindResource("NrmLegallyAbsent");

                    case NrmStatus.UnCheckable:
                        //не являетя ревизитом, который надо проверять на соответствие нормам
                        bool? checkResult = Routines.CheckValuesFromRegularFields(item, sourceFieldName);

                        switch (checkResult)
                        {
                            case true:
                                return dataGrid.FindResource("GoodSatatus");

                            case false:
                                return dataGrid.FindResource("FaultSatatus");

                            default:
                                //значение реквизита не подлежит проверке
                                return DependencyProperty.UnsetValue;
                        }
                    default:
                        return DependencyProperty.UnsetValue;
                }
            }

            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /*
    public class DataGridColumnVisibilityConverter : IValueConverter
    {
        //используем данный конвертер для двух разных входных параметров:
        // - для случая комментариев к изделию запускаем его с входным параметром ulong - битовая маска разрешений;
        // - для всех других случаев запускаем его с входным параметром bool - состояние режима просмотра протокола сборки
        public object Convert(object value, Type typeTarget, object param, CultureInfo culture)
        {
            if ((param is string fieldName) && (!string.IsNullOrEmpty(fieldName)))
            {
                switch (fieldName)
                {
                    //комментарии к изделию должны быть видны при любом значении assemblyProtocolMode, но только при наличии прав
                    case Common.Constants.DeviceComments:
                        if (value is ulong permissionsLo)
                            return (Common.Routines.IsUserCanReadCreateComments(permissionsLo) || Common.Routines.IsUserCanReadComments(permissionsLo)) ? Visibility.Visible : Visibility.Collapsed;

                        break;

                    default:
                        //все другие столбцы, видимость которых зависит только от режима просмотра протокола испытаний и не регулируется правами пользователя
                        if (value is bool assemblyProtocolMode)
                            return assemblyProtocolMode ? Visibility.Collapsed : Visibility.Visible;

                        break;
                }
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type typeTarget, object param, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    */

    public class DataGridColumnVisibilityMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            //нулевой параметр - bool флаг 'Включен режим просмотра протокола сборки';
            //первый параметр - битовая маска прав пользователя;
            //второй параметр - имя поля
            if ((values[0] is bool assemblyProtocolMode) && (values[1] is ulong permissionsLo) && (values[2] is string fieldName))
            {
                switch (fieldName)
                {
                    //комментарии к изделию должны быть видны при любом значении assemblyProtocolMode, но только при наличии прав
                    case Common.Constants.DeviceComments:
                        return (Common.Routines.IsUserCanReadCreateComments(permissionsLo) || Common.Routines.IsUserCanReadComments(permissionsLo)) ? Visibility.Visible : Visibility.Collapsed;

                    case Common.Constants.Reason:
                        //в режиме протокола сборки поле 'Reason' всегда скрыто, в режиме просмотра данных видимость этого поля определяется соответствующим битом в битовой маске this.PermissionsLo
                        return assemblyProtocolMode ? Visibility.Collapsed : Common.Routines.IsUserCanReadReason(permissionsLo) ? Visibility.Visible : Visibility.Collapsed;

                    default:
                        //все другие столбцы, видимость которых зависит только от режима просмотра протокола испытаний и не регулируется правами пользователя
                        return assemblyProtocolMode ? Visibility.Collapsed : Visibility.Visible;
                }
            }

            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TextToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type typeTarget, object param, CultureInfo culture)
        {
            string text = (string)value;

            switch (text == Properties.Resources.NotSetted)
            {
                case true:
                    return Brushes.Red;

                default:
                    return Brushes.Black;
            }
        }

        public object ConvertBack(object value, Type typeTarget, object param, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    /*
        public class TemperatureConditionToStrConverter : IValueConverter
        {        
            public object Convert(object Value, Type TargetType, object Parameter, CultureInfo Culture)
            {
                TemperatureCondition tc = (TemperatureCondition)Value;

                switch (tc)
                {
                    case TemperatureCondition.None:
                        return string.Empty;

                    default:
                        return tc.ToString();
                }
            }

            public object ConvertBack(object Value, Type TargetType, object Parameter, CultureInfo Culture)
            {
                throw new InvalidOperationException("ConvertBack method is not implemented in TemperatureConditionToStrConverter");
            }        
        }
    */
}
