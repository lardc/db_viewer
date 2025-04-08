using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using SCME.SQLDatabaseClient.Annotations;
using SCME.SQLDatabaseClient.EntityData;

namespace SCME.SQLDatabaseClient
{
    public class ParameterAndVisibility: INotifyPropertyChanged
    {
        private bool _isParamVisible;

        public string ParamName { get; set; }

        public string VisibleParamName { get; set; }

        public bool IsParamCheckedForVisible
        {
            get { return _isParamVisible; }
            set
            {
                _isParamVisible = value;
                OnPropertyChanged();
            }
        }

        #region INotifyPorpertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    public class VisibleParameter
    {
        public string ParamName { get; set; }

        public string VisibleParamName { get; set; }

        public VisibleParameter(ParameterAndVisibility source)
        {
            ParamName = source.ParamName;
            VisibleParamName = source.VisibleParamName;
        }
    }

    public class ParameterValueChunk
    {
        public PARAM ParameterEntity { get; set; }

        public ObservableCollection<ParameterValue> ChunkValues { get; set; }

        public bool IsFake { get; set; }
    }

    public class ParameterValue: INotifyPropertyChanged
    {
        private string _parameterValueData;
        public DEV_PARAM ParameterValueEntity { get; set; }

        public string ParameterValueData
        {
            get { return _parameterValueData; }
            set
            {
                decimal val;

                if (!String.IsNullOrWhiteSpace(value) && !decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out val))
                    throw new ApplicationException("Неверный формат вещественного числа");

                _parameterValueData = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasChanges));
            }
        }

        internal void Commit()
        {
            ParameterValueEntity.VALUE = Decimal.Parse(ParameterValueData, CultureInfo.InvariantCulture);
            OnPropertyChanged(nameof(HasChanges));
        }

        internal bool IsValid
        {
            get
            {
                decimal d;
                return !IsEmpty && Decimal.TryParse(ParameterValueData, NumberStyles.Float, CultureInfo.InvariantCulture, out d);
            }
        }

        internal bool IsEmpty => String.IsNullOrWhiteSpace(ParameterValueData);

        public bool HasChanges
        {
            get
            {
                if (ParameterValueEntity == null)
                    return !String.IsNullOrWhiteSpace(ParameterValueData);

                decimal num;
                var parseRes = Decimal.TryParse(ParameterValueData, NumberStyles.Float, CultureInfo.InvariantCulture, out num);

                if (!parseRes)
                    return true;

                return num != ParameterValueEntity.VALUE;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class DeviceAndParametersWithProfile
    {
        public DEVICE Device { get; set; }

        public PROFILE Profile { get; set; }

        public IList<ParameterValueChunk> Parameters { get; set; }

        internal DeviceAndParametersWithProfile FitInternal()
        {
            if (Profile == null)
                Profile = new PROFILE
                {
                    PROF_NAME = "DEFAULT"
                };

            return this;
        }
    }

    public class DeviceReportItemBase
    {
        public DEVICE Properties { get; set; }
    }

    public class ParametersReportItemBase
    {
        public int Number { get; set; }
    }

    internal static class CustomTypeFactory
    {
        public static Type CreateCustomType(string newTypeName, Dictionary<string, Type> properties, Type baseClassType)
        {
            var assemblyBldr = Thread.GetDomain().DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.RunAndCollect);
            var moduleBldr = assemblyBldr.DefineDynamicModule("tmpModule");

            var typeBldr = moduleBldr.DefineType(newTypeName, TypeAttributes.Public | TypeAttributes.Class, baseClassType);

            foreach (var propName in properties.Keys)
            {
                var fldBldr = typeBldr.DefineField("_" + propName, properties[propName], FieldAttributes.Private);

                var prptyBldr = typeBldr.DefineProperty(propName, PropertyAttributes.None, properties[propName], new [] { properties[propName] });

                const MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.HideBySig;

                var currGetPropMthdBldr = typeBldr.DefineMethod("get_value", getSetAttr, properties[propName], null);
                var currGetIL = currGetPropMthdBldr.GetILGenerator();
                currGetIL.Emit(OpCodes.Ldarg_0);
                currGetIL.Emit(OpCodes.Ldfld, fldBldr);
                currGetIL.Emit(OpCodes.Ret);

                var currSetPropMthdBldr = typeBldr.DefineMethod("set_value", getSetAttr, null, new [] { properties[propName] });
                var currSetIL = currSetPropMthdBldr.GetILGenerator();
                currSetIL.Emit(OpCodes.Ldarg_0);
                currSetIL.Emit(OpCodes.Ldarg_1);
                currSetIL.Emit(OpCodes.Stfld, fldBldr);
                currSetIL.Emit(OpCodes.Ret);

                prptyBldr.SetGetMethod(currGetPropMthdBldr);
                prptyBldr.SetSetMethod(currSetPropMthdBldr);
            }

            return typeBldr.CreateType();
        }
    }

    public class DynamicParameterRowReportItem: DynamicObject
    {
        private readonly Dictionary<string, object> _properties;

        public DynamicParameterRowReportItem()
        {
            _properties = new Dictionary<string, object>();
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return _properties.Keys;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (_properties.ContainsKey(binder.Name))
            {
                result = _properties[binder.Name];
                return true;
            }

            result = null;
            return false;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            return false;
        }

        internal void SetMember(string memberName, object value)
        {
            if (_properties.ContainsKey(memberName))
                _properties[memberName] = value;
            else
                _properties.Add(memberName, value);
        }
    }

    public class ReportTemplateInfo
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
    }

    public class RestrictionValues
    {
        public IList<Restriction> Bounds { get; set; }
        public string Name { get; set; }
    }

    public class Restriction
    {
        public double? MaxVal { get; set; }
        public double? MinVal { get; set; }
    }

    public class ConditionValues
    {
        public string Name { get; set; }
        public IList<string> Values { get; set; }
    }

    public class RestrictionsAndConditions
    {
        public IList<ConditionValues> Conditions { get; set; }
        public IList<RestrictionValues> Restrictions { get; set; }
    }
}
