using SCME.Types;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SCME.dbViewer
{
    /// <summary>
    /// Interaction logic for BitCalculator.xaml
    /// </summary>
    public partial class BitCalculator : Window
    {
        public BitCalculator(long userID, ulong permissionsLo, string title)
        {
            InitializeComponent();

            this.FUserID = userID;
            this.FValueLo = permissionsLo;

            this.Owner = Application.Current.MainWindow;
            this.Title = title;
        }

        private long FUserID = -1;
        private ulong FValueLo = 0;

        public bool? ShowModal(out ulong permissionsLo)
        {
            this.ShowData();

            bool? result = this.ShowDialog();

            if (result ?? false)
            {
                //сохраняем сформированное значение битовой маски разрешений в базу данных
                DbRoutines.SaveToUsers(this.FUserID, this.FValueLo);
            }

            permissionsLo = this.FValueLo;
            return result;
        }

        private void CheckBoxClick(CheckBox cb, byte numberOfBit)
        {
            this.FValueLo = (cb.IsChecked ?? false) ? Common.Routines.SetBit(this.FValueLo, numberOfBit) : Common.Routines.DropBit(this.FValueLo, numberOfBit);
        }

        private void CbBit0_Click(object sender, RoutedEventArgs e)
        {
            const byte numberOfBit = Common.Constants.cIsUserAdmin;

            this.CheckBoxClick(sender as CheckBox, numberOfBit);
        }

        private void CbBit1_Click(object sender, RoutedEventArgs e)
        {
            const byte numberOfBit = Common.Constants.cIsUserCanReadCreateComments;

            this.CheckBoxClick(sender as CheckBox, numberOfBit);
        }

        private void CbBit2_Click(object sender, RoutedEventArgs e)
        {
            const byte numberOfBit = Common.Constants.cIsUserCanReadComments;

            this.CheckBoxClick(sender as CheckBox, numberOfBit);
        }

        private void CbBit3_Click(object sender, RoutedEventArgs e)
        {
            const byte numberOfBit = Common.Constants.cIsUserCanCreateParameter;

            this.CheckBoxClick(sender as CheckBox, numberOfBit);
        }

        private void CbBit4_Click(object sender, RoutedEventArgs e)
        {
            const byte numberOfBit = Common.Constants.cIsUserCanEditParameter;

            this.CheckBoxClick(sender as CheckBox, numberOfBit);
        }

        private void CbBit5_Click(object sender, RoutedEventArgs e)
        {
            const byte numberOfBit = Common.Constants.cIsUserCanDeleteParameter;

            this.CheckBoxClick(sender as CheckBox, numberOfBit);
        }

        private void CbBit6_Click(object sender, RoutedEventArgs e)
        {
            const byte numberOfBit = Common.Constants.cIsUserCanCreateValueOfManuallyEnteredParameter;

            this.CheckBoxClick(sender as CheckBox, numberOfBit);
        }

        private void CbBit7_Click(object sender, RoutedEventArgs e)
        {
            const byte numberOfBit = Common.Constants.cIsUserCanEditValueOfManuallyEnteredParameter;

            this.CheckBoxClick(sender as CheckBox, numberOfBit);
        }

        private void CbBit8_Click(object sender, RoutedEventArgs e)
        {
            const byte numberOfBit = Common.Constants.cIsUserCanDeleteValueOfManuallyEnteredParameter;

            this.CheckBoxClick(sender as CheckBox, numberOfBit);
        }

        private void CbBit9_Click(object sender, RoutedEventArgs e)
        {
            const byte numberOfBit = Common.Constants.cIsUserCanCreateDevices;

            this.CheckBoxClick(sender as CheckBox, numberOfBit);
        }

        private void CbBit10_Click(object sender, RoutedEventArgs e)
        {
            const byte numberOfBit = Common.Constants.cWorkWithAssemblyProtocol;

            this.CheckBoxClick(sender as CheckBox, numberOfBit);
        }

        private void CbBit11_Click(object sender, RoutedEventArgs e)
        {
            const byte numberOfBit = Common.Constants.cIsUserCanManageDeviceReferences;

            this.CheckBoxClick(sender as CheckBox, numberOfBit);
        }

        private void CbBit12_Click(object sender, RoutedEventArgs e)
        {
            const byte numberOfBit = Common.Constants.cIsUserCanReadReason;

            this.CheckBoxClick(sender as CheckBox, numberOfBit);
        }

        private void CbBit14_Click(object sender, RoutedEventArgs e)
        {
            const byte numberOfBit = Common.Constants.cEditAssembly;

            this.CheckBoxClick(sender as CheckBox, numberOfBit);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    this.DialogResult = false;
                    break;

                case Key.Enter:
                    this.DialogResult = true;
                    break;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void ShowData()
        {
            //перебираем все имеющиеся на форме CheckBox и выставляем их свойства IsChecked
            foreach (CheckBox cb in Common.Routines.FindVisualChildren<CheckBox>(mainGrid))
            {
                cb.IsChecked = Common.Routines.CheckBit(this.FValueLo, Convert.ToByte(cb.Tag));
            }
        }


    }
}
