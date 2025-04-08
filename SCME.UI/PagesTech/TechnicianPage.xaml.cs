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
            btndVdt.IsEnabled = Param.IsdVdtEnabled;
            btnATU.IsEnabled = Param.IsATUEnabled;
            btnQrrTq.IsEnabled = Param.IsQrrTqEnabled;
            btnRAC.IsEnabled = Param.IsRACEnabled;
            btnIH.IsEnabled = Param.IsIHEnabled;
        }

        private void Button_Click(object Sender, RoutedEventArgs E)
        {
            var btn = (Button)Sender;

            Page page = null;

            switch (Convert.ToUInt16(btn.CommandParameter))
            {
                case 1:
                    Cache.Gate = new GatePage();
                    page = Cache.Gate;
                    break;
                case 2:
                    Cache.SL = new SLPage();
                    page = Cache.SL;
                    break;
                case 3:
                    Cache.Bvt = new BvtPage();
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
                    Cache.ProfileSelection.ClearFilter();
                    Cache.ProfileEdit.InitFilter();
                    page = Cache.ProfileEdit;
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
                case 12:
                    Cache.DVdt = new DVdtPage();
                    page = Cache.DVdt;
                    break;
                case 13:
                    Cache.ATU = new ATUPage();
                    page = Cache.ATU;
                    break;
                case 14:
                    Cache.QrrTq = new QrrTqPage();
                    page = Cache.QrrTq;
                    break;
                case 15:
                    Cache.RAC = new RACPage();
                    page = Cache.RAC;
                    break;
                case 16:
                    Cache.IH = new IHPage();
                    page = Cache.IH;
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

                Cache.ProfileEdit.ClearFilter();
                Cache.ProfileSelection.InitFilter();
                Cache.ProfileSelection.InitSorting();
                if (PreviousPage is PagesUser.ProfileSelectionPage)
                {
                    NavigationService.Navigate(Cache.ProfileSelection);
                    return;
                }

                NavigationService.Navigate(PreviousPage);
            }
        }
    }
}