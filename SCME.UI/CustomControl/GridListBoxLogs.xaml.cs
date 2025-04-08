using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SCME.Types;

namespace SCME.UI.CustomControl
{
    /// <summary>
    /// Interaction logic for GridListBox.xaml
    /// </summary>
    public partial class GridListBoxLogs
    {
        public GridListBoxLogs()
        {
            InitializeComponent();
        }

        public event RoutedEventHandler EndOfData;

        protected void InvokeEndOfData(RoutedEventArgs Args)
        {
            if (EndOfData != null)
                EndOfData(this, Args);
        }

        private void ScrollViewer_OnScrollChanged(object Sender, ScrollChangedEventArgs E)
        {
            var scrollViewer = (ScrollViewer) Sender;

            if (E.VerticalOffset >= scrollViewer.ScrollableHeight)
                InvokeEndOfData(new RoutedEventArgs());
        }
    }
}
