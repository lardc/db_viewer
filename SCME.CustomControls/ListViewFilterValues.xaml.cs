using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace SCME.CustomControls
{
    /// <summary>
    /// Interaction logic for ListViewFilterValues.xaml
    /// </summary>
    public partial class ListViewFilterValues : ListView
    {
        public ListViewFilterValues()
        {
            InitializeComponent();
        }

        private void BtDeleteSelectedItem_Click(object sender, RoutedEventArgs e)
        {
            //удаление выбранного элемента из множества значений поля по которому выполняется фильтрация
            if ((sender is Button button) && (button.CommandParameter is ListViewItem item) && (item.Content is FilterValue filterValue))
            {
                if (this.ItemsSource is FilterValues filterValues)
                    filterValues.Remove(filterValue);
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

    public class FilterValuesDataTemplateDictionary : Dictionary<object, DataTemplate>
    {
    }

    public class FilterValuesTemplateProvider : DataTemplateSelector
    {
        private readonly FilterValuesTemplateSelectorExt FExtension;

        public FilterValuesTemplateProvider(FilterValuesTemplateSelectorExt extension) : base()
        {
            this.FExtension = extension;
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if ((item != null) && (container is FrameworkElement))
            {
                if (item is FilterValue filterValue)
                {
                    string templateName = null;
                    FilterDescription filterDescription = filterValue.FilterDescription;
                    string typeName = filterDescription.Type;

                    switch (typeName)
                    {
                        case "System.String":
                            templateName = (filterDescription.ListOfValues.Count == 0) ? "listOfStringValuesTemplate" : "listOfComboBoxStringValuesTemplate";
                            break;

                        case "System.DateOnly":
                            templateName = "listOfDateValuesTemplate";
                            break;

                        case "System.DateTime":
                            templateName = "listOfDateTimeValuesTemplate";
                            break;

                        case "System.Int32":
                            templateName = "listOfIntValuesTemplate";
                            break;

                        case "System.Double":
                            templateName = "listOfDoubleValuesTemplate";
                            break;

                        case "System.Boolean":
                            templateName = "listOfBoolValuesTemplate";
                            break;
                    }

                    if (this.FExtension.TemplateDictionary.ContainsKey(templateName))
                    {
                        bool founded = FExtension.TemplateDictionary.TryGetValue(templateName, out DataTemplate dataTemplate);

                        if (founded)
                            return dataTemplate;
                    }
                }
            }

            return null;
        }
    }

    [MarkupExtensionReturnType(typeof(DataTemplateSelector))]
    public class FilterValuesTemplateSelectorExt : MarkupExtension
    {
        public FilterValuesDataTemplateDictionary TemplateDictionary { get; set; }

        public FilterValuesTemplateSelectorExt()
        {
        }

        public FilterValuesTemplateSelectorExt(FilterValuesDataTemplateDictionary templateDictionary) : this()
        {
            TemplateDictionary = templateDictionary;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new FilterValuesTemplateProvider(this);
        }
    }
}