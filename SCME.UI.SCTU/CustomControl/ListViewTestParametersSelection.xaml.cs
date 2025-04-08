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

namespace SCME.UI.CustomControl
{
    /// <summary>
    /// Interaction logic for ListViewTestParametersSelection.xaml
    /// </summary>
    public partial class ListViewTestParametersSelection : INotifyPropertyChanged
    {
        public ListViewTestParametersSelection()
        {
            InitializeComponent();
        }

        public bool IsButtonDeleteVisible { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

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


}
