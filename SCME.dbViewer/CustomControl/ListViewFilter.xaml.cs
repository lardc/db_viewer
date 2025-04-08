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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Markup;
using SCME.dbViewer.ForFilters;

namespace SCME.dbViewer.CustomControl
{
    /// <summary>
    /// Interaction logic for ListViewFilters.xaml
    /// </summary>
    public partial class ListViewFilters : ListView
    {
        public ListViewFilters()
        {
            InitializeComponent();
        }

        private void btDeleteSelectedFilter_Click(object sender, RoutedEventArgs e)
        {
            //удаление выбранного фильтра
            if (!(sender is Button button))
                return;

            if (!(button.CommandParameter is ListViewItem lvi))
                return;

            if(!(lvi.Content is FilterDescription filter))
                return;

            var collection = ItemsSource as ActiveFilters;
            collection?.Remove(filter);
        }
    }

    public class FilterDataTemplateDictionary : Dictionary<object, DataTemplate>
    {
    }

    public class FilterTemplateProvider : DataTemplateSelector
    {
        private readonly TemplateSelectorExt _extension;

        public FilterTemplateProvider(TemplateSelectorExt extension) : base()
        {
            _extension = extension;
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null)
                return null;

            if (container as FrameworkElement != null)
            {
                bool Founded = false;
                FilterDescription filterDescription = (FilterDescription)item;
                DataTemplate dataTemplate = null;

                if (filterDescription.type == typeof(string))
                {
                    string templateName = "stringFilterTemplate";
                    if (_extension.TemplateDictionary.ContainsKey(templateName))
                        Founded = (_extension.TemplateDictionary.TryGetValue(templateName, out dataTemplate));
                }

                if (filterDescription.type == typeof(DateTime))
                {
                    string templateName = "dateFilterTemplate";
                    if (_extension.TemplateDictionary.ContainsKey(templateName))
                        Founded = (_extension.TemplateDictionary.TryGetValue(templateName, out dataTemplate));
                }

                if (filterDescription.type == typeof(int))
                {
                    string templateName = "intFilterTemplate";
                    if (_extension.TemplateDictionary.ContainsKey(templateName))
                        Founded = (_extension.TemplateDictionary.TryGetValue(templateName, out dataTemplate));
                }

                if (filterDescription.type == typeof(double))
                {
                    string templateName = "doubleFilterTemplate";
                    if (_extension.TemplateDictionary.ContainsKey(templateName))
                        Founded = (_extension.TemplateDictionary.TryGetValue(templateName, out dataTemplate));
                }

                if (filterDescription.type == typeof(decimal))
                {
                    string templateName = "decimalFilterTemplate";
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
    public class TemplateSelectorExt : MarkupExtension
    {
        public FilterDataTemplateDictionary TemplateDictionary { get; set; }

        public TemplateSelectorExt()
        {
        }

        public TemplateSelectorExt(FilterDataTemplateDictionary templateDictionary) : this()
        {
            TemplateDictionary = templateDictionary;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new FilterTemplateProvider(this);
        }
    }
}
