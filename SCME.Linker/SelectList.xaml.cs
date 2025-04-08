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

namespace SCME.Linker
{
    /// <summary>
    /// Interaction logic for SelectList.xaml
    /// </summary>
    public partial class SelectList : Window
    {
        public SelectList()
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;
        }

        public void ShowModal()
        {
            this.ShowDialog();
        }

        private void ViewLinks(string selectedGroupName)
        {
            ViewLinks viewLinks = new ViewLinks();
            viewLinks.ShowModal(selectedGroupName);
        }

        private void WorkByJobsWay()
        {
            Jobs jobs = new Jobs();

            if (jobs.ShowModal(out string selectedGroupName))
                this.ViewLinks(selectedGroupName);
        }

        private void btByGroupNameList_Click(object sender, RoutedEventArgs e)
        {
            this.WorkByJobsWay();
        }

        private void btByPackage_Click(object sender, RoutedEventArgs e)
        {
            Packages packages = new Packages();

            if (packages.ShowModal(out string selectedGroupName))
                this.ViewLinks(selectedGroupName);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    e.Handled = true;
                    this.DialogResult = false;
                    break;

                case Key.Enter:
                    e.Handled = true;
                    this.WorkByJobsWay();
                    break;
            }
        }
    }
}
