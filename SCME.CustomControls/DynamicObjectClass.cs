using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Dynamic;
using System.Windows;

namespace SCME.CustomControls
{
    public class DynamicObj : DynamicObject, INotifyPropertyChanged
    {
        #region Private data

        private readonly Dictionary<string, object> FMembers;
        private readonly ObservableCollection<object> FItemsCollection;

        #endregion Private data

        #region DynamicObject overrides

        public DynamicObj()
        {
            this.FMembers = new Dictionary<string, object>();
            this.FItemsCollection = new ObservableCollection<object>();
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return this.FMembers.TryGetValue(binder.Name.ToLower(), out result);
        }

        public bool GetMember(string name, out object result)
        {
            return this.FMembers.TryGetValue(name.ToLower(), out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            string name = binder.Name.ToLower();
            this.FMembers[name] = value;

            OnPropertyChanged(name);

            return true;
        }

        public void SetMember(string name, object value)
        {
            string n = name.ToLower();
            this.FMembers[n] = value;

            OnPropertyChanged(n);
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            //все имена будут в нижнем регистре
            return this.FMembers.Keys;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            int index = (int)indexes[0];

            try
            {
                result = this.FItemsCollection[index];
            }
            catch (ArgumentOutOfRangeException)
            {
                result = null;

                return false;
            }

            return true;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            int index = (int)indexes[0];
            this.FItemsCollection[index] = value;

            OnPropertyChanged(System.Windows.Data.Binding.IndexerName);

            return true;
        }

        public override bool TryDeleteMember(DeleteMemberBinder binder)
        {
            if (this.FMembers.ContainsKey(binder.Name))
            {
                this.FMembers.Remove(binder.Name);

                return true;
            }

            return false;
        }

        public override bool TryDeleteIndex(DeleteIndexBinder binder, object[] indexes)
        {
            int index = (int)indexes[0];
            this.FItemsCollection.RemoveAt(index);

            return true;
        }

        #endregion DynamicObj overrides

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged

        #region Public methods

        public object AddItem(object item)
        {
            this.FItemsCollection.Add(item);
            OnPropertyChanged(Binding.IndexerName);

            return null;
        }

        #endregion Public methods
    }

    public class SetPropertyBinder : SetMemberBinder
    {
        public SetPropertyBinder(string propertyName) : base(propertyName, false)
        {
        }

        public override DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
        {
            System.Linq.Expressions.NewExpression newExpression = System.Linq.Expressions.Expression.New(typeof(InvalidOperationException).GetConstructor(new Type[] { typeof(string) }), new System.Linq.Expressions.Expression[] { System.Linq.Expressions.Expression.Constant("Error") });
            System.Linq.Expressions.UnaryExpression unaryExpression = System.Linq.Expressions.Expression.Throw(newExpression);

            return new DynamicMetaObject(unaryExpression, BindingRestrictions.Empty);
        }
    }
}

