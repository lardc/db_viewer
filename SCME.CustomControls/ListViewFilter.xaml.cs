using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace SCME.CustomControls
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

        private void BtDeleteSelectedFilter_Click(object sender, RoutedEventArgs e)
        {
            //удаление выбранного фильтра
            if ((sender is Button button) && (button.CommandParameter is ListViewItem item) && (item.Content is FilterDescription filter))
            {
                if (this.ItemsSource is ActiveFilters filters)
                    filters.Remove(filter);
            }
        }

        private void BtNewValue_Click(object sender, RoutedEventArgs e)
        {
            //добавление нового значения в список значений выбранного фильтра
            if ((sender is Button button) && (button.CommandParameter is ListViewItem item) && (item.Content is FilterDescription filter))
            {
                filter.Values.NewFilterValue();
            }
        }

        private void TbOnlyNumeric_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Common.Routines.TextBoxOnlyNumeric_PreviewTextInput(sender, e);
        }

        private void TbOnlyNumericPaste(object sender, DataObjectPastingEventArgs e)
        {
            Common.Routines.TextBoxOnlyNumericPaste(sender, e);
        }

        private void TbDisableSpace_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Common.Routines.TextBoxDisableSpace_PreviewKeyDown(sender, e);
        }

        private void TbOnlyDouble_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Common.Routines.TextBoxOnlyDouble_PreviewTextInput(sender, e);
        }

        private void TbOnlyDoublePaste(object sender, DataObjectPastingEventArgs e)
        {
            Common.Routines.TextBoxOnlyDoublePaste(sender, e);
        }

        private void BtDateTimePickerClick(object sender, RoutedEventArgs e)
        {
            if ((sender is Button bt) && (bt.Tag is Xceed.Wpf.Toolkit.DateTimePicker dt))
            {
                dt.IsOpen = true;
            }
        }
    }

    public class FilterDataTemplateDictionary : Dictionary<object, DataTemplate>
    {
    }

    public class FilterTemplateProvider : DataTemplateSelector
    {
        private readonly FilterTemplateSelectorExt FExtension;

        public FilterTemplateProvider(FilterTemplateSelectorExt extension) : base()
        {
            this.FExtension = extension;
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if ((item != null) && (container is FrameworkElement))
            {
                if (item is FilterDescription filterDescription)
                {
                    string templateName = null;

                    switch (filterDescription.Values.Count > 1)
                    {
                        case true:
                            templateName = "listOfValuesFilterTemplate";
                            break;

                        default:
                            switch (filterDescription.Type)
                            {
                                case "System.String":
                                    //если список filterDescription.ListOfValues пуст - имеем дело с одним единственным строковым значением иначе - имеем дело со списком строковых значений в ComboBox
                                    templateName = (filterDescription.ListOfValues.Count == 0) ? "stringFilterTemplate" : "valuesInComboBoxFilterTemplate";
                                    break;

                                case "System.DateOnly":
                                    templateName = "dateFilterTemplate";
                                    break;

                                case "System.DateTime":
                                    templateName = "dateTimeFilterTemplate";
                                    break;

                                case "System.Int32":
                                    templateName = "intFilterTemplate";
                                    break;

                                case "System.Double":
                                    templateName = "doubleFilterTemplate";
                                    break;

                                case "System.Boolean":
                                    templateName = "boolFilterTemplate";
                                    break;
                            }
                            break;
                    }

                    if (templateName != null)
                    {
                        if (this.FExtension.FilterTemplateDictionary.ContainsKey(templateName))
                        {
                            bool founded = FExtension.FilterTemplateDictionary.TryGetValue(templateName, out DataTemplate dataTemplate);

                            if (founded)
                                return dataTemplate;
                        }
                    }
                }
            }

            return null;
        }
    }

    [MarkupExtensionReturnType(typeof(DataTemplateSelector))]
    public class FilterTemplateSelectorExt : MarkupExtension
    {
        public FilterDataTemplateDictionary FilterTemplateDictionary { get; set; }

        public FilterTemplateSelectorExt()
        {
        }

        public FilterTemplateSelectorExt(FilterDataTemplateDictionary templateDictionary) : this()
        {
            FilterTemplateDictionary = templateDictionary;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new FilterTemplateProvider(this);
        }
    }
}