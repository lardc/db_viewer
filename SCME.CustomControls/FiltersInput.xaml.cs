using System.Windows;
using System.Windows.Input;

namespace SCME.CustomControls
{
    /// <summary>
    /// Interaction logic for FiltersInput.xaml
    /// </summary>
    public partial class FiltersInput : Window
    {
        public ActiveFilters Filters { get; set; }

        public FiltersInput(ActiveFilters activeFilters, Window owner)
        {
            InitializeComponent();

            this.Owner = owner;

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

        private void DeleteNotAcceptedToUseFilters()
        {
            //удаляет все не подтверждённые пользователем фильтры
            for (int index = this.Filters.Count - 1; index >= 0; index--)
            {
                if (!this.Filters[index].AcceptedToUse)
                    this.Filters.RemoveAt(index);
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DeleteNotAcceptedToUseFilters();
                this.Close();
            }
        }

        private void BtCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DeleteNotAcceptedToUseFilters();
            this.Close();
        }

        private void BtOK_Click(object sender, RoutedEventArgs e)
        {
            //пользователь подтверждает нужность всех фильтров (он всегда видит их все без исключения)
            for (int index = this.Filters.Count - 1; index >= 0; index--)
            {
                this.Filters[index].AcceptedToUse = true;
            }

            //если в момент нажатия экранной кнопки 'OK' нажата кнопка клавиатуры 'Ctrl' - значит пользователь не хочет применения сформированных фильтров - он их формирует с целью применить их все сразу, а не ждать последовательного применения каждого из них
            switch (Common.Routines.IsKeyCtrlPressed())
            {
                case true:
                    this.Close();
                    break;

                default:
                    this.DialogResult = true;
                    break;
            }
        }

        private void BtDeleteAllFilters_Click(object sender, RoutedEventArgs e)
        {
            //удаление всех фильтров
            if (this.LvFilters.ItemsSource is ActiveFilters activeFilters)
                activeFilters.Clear();

            this.DialogResult = true;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}