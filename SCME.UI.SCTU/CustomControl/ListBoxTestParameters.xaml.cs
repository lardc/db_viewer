using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using SCME.Types.BaseTestParams;
using SCME.UI.Annotations;
using GateTestParameters = SCME.Types.Gate.TestParameters;
using BvtTestParameters = SCME.Types.BVT.TestParameters;
using VtmTestParameters = SCME.Types.SL.TestParameters;
using dVdtTestParameters = SCME.Types.dVdt.TestParameters;

namespace SCME.UI.CustomControl
{
    /// <summary>
    /// Interaction logic for ListBoxTestParameters.xaml
    /// </summary>
    public partial class ListBoxTestParameters : INotifyPropertyChanged
    {
        public ListBoxTestParameters()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsButtonDeleteVisible { get; set; }
        
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }


        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
                return;

            var lvi = button.CommandParameter as ListViewItem;
            if (lvi == null)
                return;

            var parametersAndNormatives = lvi.Content as BaseTestParametersAndNormatives;
            if (parametersAndNormatives == null)
                return;

            var collection = ItemsSource as ObservableCollection<BaseTestParametersAndNormatives>;

            if (collection != null)
                collection.Remove(parametersAndNormatives);
        }
    }

  
    public class MyDataTemplateDictionary : Dictionary<object, DataTemplate>
    {
    }

    public class MyTemplateProvider : DataTemplateSelector
    {
        private readonly TemplateSelectorExt _extension;

        public MyTemplateProvider(TemplateSelectorExt extension):base()
        {
            _extension = extension;
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null) return null;
            if (container as FrameworkElement != null)
            {
                bool isFound = false;
                DataTemplate dataTemplate = null;
                if (item is GateTestParameters)
                {
                    if (_extension.TemplateDictionary.ContainsKey("GateParametersTemplate"))
                        isFound = (_extension.TemplateDictionary.TryGetValue("GateParametersTemplate", out dataTemplate));
                }
                if (item is BvtTestParameters)
                {
                    if (_extension.TemplateDictionary.ContainsKey("BvtParametersTemplate"))
                        isFound = (_extension.TemplateDictionary.TryGetValue("BvtParametersTemplate", out dataTemplate));
                }
                if (item is VtmTestParameters)
                {
                    if (_extension.TemplateDictionary.ContainsKey("VtmParametersTemplate"))
                        isFound = (_extension.TemplateDictionary.TryGetValue("VtmParametersTemplate", out dataTemplate));
                }
                if (item is dVdtTestParameters)
                {
                    if (_extension.TemplateDictionary.ContainsKey("dVdtParametersTemplate"))
                        isFound = (_extension.TemplateDictionary.TryGetValue("dVdtParametersTemplate", out dataTemplate));
                }
                if (isFound) return dataTemplate;
            }
            return null;
        }
    }
    
    [MarkupExtensionReturnType(typeof(DataTemplateSelector))]
    public class TemplateSelectorExt : MarkupExtension
    {
        public MyDataTemplateDictionary TemplateDictionary { get; set; }
       
        public TemplateSelectorExt()
        {
            
        }

        public TemplateSelectorExt(MyDataTemplateDictionary templateDictionary):this()
        {
            TemplateDictionary = templateDictionary;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new MyTemplateProvider(this);
        }
    }
}
