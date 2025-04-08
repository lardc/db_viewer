using System;
using System.Windows;
using System.Windows.Controls;
using SCME.Types;
using SCME.UI.IO;

namespace SCME.UI.PagesTech
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class TechnicianPage
    {
        internal object PreviousPage;

        public TechnicianPage()
        {
            InitializeComponent();
        }

        internal void AreButtonsEnabled(TypeCommon.InitParams Param)
        {
            btnGate.IsEnabled = Param.IsGateEnabled;
            btnVtm.IsEnabled = Param.IsSLEnabled;
            btnBvt.IsEnabled = Param.IsBVTEnabled;
        }

        private void Button_Click(object Sender, RoutedEventArgs E)
        {
            var btn = (Button)Sender;

            Page page = null;

            switch (Convert.ToUInt16(btn.CommandParameter))
            {
                case 1:
                    page = Cache.Gate;
                    break;
                case 2:
                    page = Cache.SL;
                    break;
                case 3:
                    page = Cache.Bvt;
                    break;
                case 4:
                    page = Cache.Selftest;
                    break;
                case 5:
                    page = Cache.Calibration;
                    break;
                case 6:
                    page = Cache.Console;
                    break;
                case 7:
                    var ProfileSelection = Cache.ProfileSelection;
                    if (ProfileSelection != null)
                        Cache.ProfileSelection.ClearFilter();

                    var ProfileEdit = Cache.ProfileEdit;
                    if (ProfileEdit != null)
                    {
                        Cache.ProfileEdit.InitFilter();
                        page = Cache.ProfileEdit;
                    }
                    break;

                case 8:
                    page = Cache.Logs;
                    break;
                case 9:
                    page = Cache.Results;
                    break;
                case 10:
                    Cache.Welcome.IsBackEnable = true;
                    Cache.Welcome.IsRestartEnable = true;
                    page = Cache.Welcome;
                    break;
                case 11:
                    page = Cache.Clamp;
                    break;
            }

            if (page != null && NavigationService != null)
                NavigationService.Navigate(page);
        }

        private void BtnBack_Click(object Sender, RoutedEventArgs E)
        {
            if (NavigationService != null)
            {
                if (PreviousPage == null)
                    PreviousPage = Cache.Login;

                if (Cache.ProfileEdit != null)
                    Cache.ProfileEdit.ClearFilter();

                if (Cache.ProfileSelection != null)
                {
                    Cache.ProfileSelection.InitFilter();
                    Cache.ProfileSelection.InitSorting();
                }

                if (PreviousPage is PagesUser.ProfileSelectionPage)
                {
                    if (Cache.ProfileSelection != null)
                    {
                        NavigationService.Navigate(Cache.ProfileSelection);
                        return;
                    }
                }

                NavigationService.Navigate(PreviousPage);
            }
        }
    }
}