using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Collections.ObjectModel;
using System.ComponentModel;
using SCME.dbViewer.CustomControl;
using System.Windows.Input;
using System.Globalization;

namespace SCME.dbViewer.ForFilters
{
    /// <summary>
    /// Interaction logic for FiltersInput.xaml
    /// </summary>
    public partial class FiltersInput : Window
    {
        public ActiveFilters Filters { get; set; }

        public FiltersInput(ActiveFilters activeFilters)
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;

            this.Filters = activeFilters;
            this.DataContext = this;
        }

        public bool? Demonstrate(Point position)
        {
            if (position != null)
            {
                this.Left = ((position.X + this.Width) > SystemParameters.WorkArea.Width) ? SystemParameters.WorkArea.Width - this.Width : position.X;
                this.Top = position.Y;
            }

            return this.ShowDialog();
        }

        private void DeleteLastFilter()
        {
            //удаление последнего фильтра
            if (this.lvFilters.ItemsSource != null)
            {
                if (this.lvFilters.ItemsSource is ActiveFilters activeFilters)
                {
                    int index = activeFilters.Count - 1;

                    if (index >= 0)
                        activeFilters?.RemoveAt(index);
                }
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DeleteLastFilter();
                this.Close();
            }
        }

        private void BtCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DeleteLastFilter();
        }

        private void BtOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void BtDeleteAllFilters_Click(object sender, RoutedEventArgs e)
        {
            //удаление всех фильтров
            if (this.lvFilters.ItemsSource is ActiveFilters activeFilters)
                activeFilters.Clear();

            this.DialogResult = true;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}