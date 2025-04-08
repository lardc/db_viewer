using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using SCME.UI.Annotations;
using SCME.UI.CustomControl;
using GateTestParameters = SCME.Types.Gate.TestParameters;
using BvtTestParameters = SCME.Types.BVT.TestParameters;
using VtmTestParameters = SCME.Types.SL.TestParameters;
using DvDtTestParameters = SCME.Types.dVdt.TestParameters;

namespace SCME.UI.CustomControl
{
    /// <summary>
    /// Interaction logic for ListViewResults.xaml
    /// </summary>
    public partial class ListViewResults : INotifyPropertyChanged
    {
        public ListViewResults()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }


    }

    public class ResultsDataTemplateDictionary : Dictionary<object, DataTemplate>
    { }

    public class ResultsDataTemplateSelector : DataTemplateSelector
    {
        private readonly ResultsTemplateSelectorExt _ext;

        public ResultsDataTemplateSelector(ResultsTemplateSelectorExt ext)
        {
            _ext = ext;
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
                    if (_ext.TemplateDictionary.ContainsKey("GateParametersTemplate"))
                        isFound = (_ext.TemplateDictionary.TryGetValue("GateParametersTemplate", out dataTemplate));
                }
                if (item is BvtTestParameters)
                {
                    if (_ext.TemplateDictionary.ContainsKey("BvtParametersTemplate"))
                        isFound = (_ext.TemplateDictionary.TryGetValue("BvtParametersTemplate", out dataTemplate));
                }
                if (item is VtmTestParameters)
                {
                    if (_ext.TemplateDictionary.ContainsKey("VtmParametersTemplate"))
                        isFound = (_ext.TemplateDictionary.TryGetValue("VtmParametersTemplate", out dataTemplate));
                }
                if (item is DvDtTestParameters)
                {
                    if (_ext.TemplateDictionary.ContainsKey("dVdtParametersTemplate"))
                        isFound = (_ext.TemplateDictionary.TryGetValue("dVdtParametersTemplate", out dataTemplate));
                }
                if (isFound) return dataTemplate;
            }
            return null;
        }

    }

    [MarkupExtensionReturnType(typeof(DataTemplateSelector))]
    public class ResultsTemplateSelectorExt : MarkupExtension
    {
        public ResultsDataTemplateDictionary TemplateDictionary { get; set; }

        public ResultsTemplateSelectorExt()
        {

        }

        public ResultsTemplateSelectorExt(ResultsDataTemplateDictionary templateDictionary)
            : this()
        {
            TemplateDictionary = templateDictionary;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new ResultsDataTemplateSelector(this);
        }
    }

}
