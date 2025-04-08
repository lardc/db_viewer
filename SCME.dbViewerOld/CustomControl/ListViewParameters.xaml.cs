using SCME.dbViewer.ForParameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SCME.dbViewer.CustomControl
{
    /// <summary>
    /// Interaction logic for ListViewParameters.xaml
    /// </summary>
    public partial class ListViewParameters : ListView
    {
        public ListViewParameters()
        {
            InitializeComponent();
        }
    }

    public class ParamTemplateDictionary : Dictionary<object, DataTemplate>
    {
    }

    public class ParamTemplateProvider : DataTemplateSelector
    {
        private readonly TemplateParamSelectorExt _extension;

        public ParamTemplateProvider(TemplateParamSelectorExt extension) : base()
        {
            _extension = extension;
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null)
                return null;

            if (container as FrameworkElement != null)
            {
                string templateName = null;

                if (item.GetType() == typeof(TParameters))
                    templateName = "T_ParametersTemplate";

                if (item.GetType() == typeof(TLParameters))
                    templateName = "TL_ParametersTemplate";

                if (item.GetType() == typeof(TBParameters))
                    templateName = "TB_ParametersTemplate";
                
                if (item.GetType() == typeof(TBIParameters))
                    templateName = "TBI_ParametersTemplate";

                if (item.GetType() == typeof(TBHParameters))
                    templateName = "TBH_ParametersTemplate";

                if (item.GetType() == typeof(DParameters))
                    templateName = "D_ParametersTemplate";

                if (item.GetType() == typeof(DLParameters))
                    templateName = "DL_ParametersTemplate";

                if (item.GetType() == typeof(DHParameters))
                    templateName = "DH_ParametersTemplate";
                
                bool Founded = false;
                DataTemplate dataTemplate = null;

                if (templateName != null)
                {
                    if (_extension.TemplateDictionary.ContainsKey(templateName))
                        Founded = (_extension.TemplateDictionary.TryGetValue(templateName, out dataTemplate));
                }

                if (Founded)
                    return dataTemplate;
            }

            return null;
        }
    }

    [MarkupExtensionReturnType(typeof(DataTemplateSelector))]
    public class TemplateParamSelectorExt : MarkupExtension
    {
        public ParamTemplateDictionary TemplateDictionary { get; set; }

        public TemplateParamSelectorExt()
        {
        }

        public TemplateParamSelectorExt(ParamTemplateDictionary templateDictionary) : this()
        {
            TemplateDictionary = templateDictionary;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new ParamTemplateProvider(this);
        }
    }
}
