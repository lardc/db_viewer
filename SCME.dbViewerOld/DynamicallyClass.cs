using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;


namespace SCME.dbViewer
{
    public class TypedField
    {
        public string Name { get; }
        public Type T { get; }

        public TypedField(string name, Type type)
        {
            this.Name = name;
            this.T = type;
        }

        public object Value(SqlDataReader reader)
        {
            if (reader != null)
            {
                int i = reader.GetOrdinal(this.Name);

                if (T == typeof(string))
                {
                    return reader.GetString(i);
                }

                if (T == typeof(int) | T == typeof(Int16) | T == typeof(Int32))
                {
                    return reader.GetInt32(i);
                }

                if (T == typeof(Int64))
                {
                    return reader.GetInt64(i);
                }

                if (T == typeof(DateTime))
                {
                    return reader.GetDateTime(i);
                }

                if (T == typeof(float))
                {
                    return reader.GetFloat(i);
                }

                if (T == typeof(double))
                {
                    return reader.GetDouble(i);
                }
            }

            return null;
        }
    }

    public class DynamicallyClass
    {
        AssemblyName asemblyName;

        public DynamicallyClass(string ClassName)
        {
            this.asemblyName = new AssemblyName(ClassName);
        }

        public object CreateObject(List<TypedField> typedFields)
        {
            TypeBuilder DynamicClass = this.CreateClass();
            this.CreateConstructor(DynamicClass);

            foreach (TypedField typeField in typedFields)
            {
                CreateProperty(DynamicClass, typeField.Name, typeField.T);
            }

            Type type = DynamicClass.CreateType();

            return Activator.CreateInstance(type);
        }

        private TypeBuilder CreateClass()
        {
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(this.asemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicallyClassModule");

            TypeBuilder typeBuilder = moduleBuilder.DefineType(this.asemblyName.FullName, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout, null);

            return typeBuilder;
        }

        private void CreateConstructor(TypeBuilder typeBuilder)
        {
            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
        }

        private void CreateProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType)
        {
            FieldBuilder fieldBuilder = typeBuilder.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            MethodBuilder getPropMthdBldr = typeBuilder.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();

            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            MethodBuilder setPropMthdBldr = typeBuilder.DefineMethod("set_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, new[] { propertyType });

            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
            Label modifyProperty = setIl.DefineLabel();
            Label exitSet = setIl.DefineLabel();

            setIl.MarkLabel(modifyProperty);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);

            setIl.Emit(OpCodes.Nop);
            setIl.MarkLabel(exitSet);
            setIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);
        }
    }
}
