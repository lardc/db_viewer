using SCME.UI.IO;
using SCME.UI.PagesCommon;
using SCME.UI.PagesTech;
using SCME.UI.PagesUser;

namespace SCME.UI
{
    enum UserWorkMode : byte { Operator, ServiceMan, SpecialMeasure }

    internal static class Cache
    {
        private static HardwareStatusPage ms_WelcomeScreen;
        private static PasswordPage ms_PasswordPage;
        private static UserWorkModePage ms_UserWorkModePage;
        private static LoginPage ms_LoginPage;
        private static UserTestPage ms_UserTestPage;
        private static TechnicianPage ms_TechnicianPage;
        private static CalibrationPage ms_CalibrationPage;
        private static SLPage ms_SLPage;
        private static GatePage ms_GatePage;
        private static BvtPage ms_BVTPage;
        private static SelftestPage ms_SelftestPage;
        private static ResultsPage ms_ResultsPage;
        private static LogsPage ms_LogsPage;
        private static ConsolePage ms_ConsolePage;
        private static ClampPage ms_ClampPage;
        private static DVdtPage ms_DVdtPage;
        private static ATUPage ms_ATUPage;
        private static QrrTqPage ms_QrrTqPage;
        private static RACPage ms_RACPage;
        private static IHPage ms_IHPage;

        internal static MainWindow Main { get; set; }

        internal static ControlLogic Net { get; set; }

        internal static KeyboardLayouts Keyboards { get; set; }

        internal static FtdiLibrary FTDI { get; set; }

        internal static LocalStorage Storage { get; set; }

        #region User pages

        internal static HardwareStatusPage Welcome
        {
            get { return ms_WelcomeScreen ?? (ms_WelcomeScreen = new HardwareStatusPage()); }
            set { ms_WelcomeScreen = value; }
        }

        internal static UserWorkModePage UserWorkMode
        {
            get { return ms_UserWorkModePage ?? (ms_UserWorkModePage = new UserWorkModePage()); }
            set { ms_UserWorkModePage = value; }
        }

        internal static LoginPage Login
        {
            get { return ms_LoginPage ?? (ms_LoginPage = new LoginPage()); }
            set { ms_LoginPage = value; }
        }

        internal static ProfileSelectionPage ProfileSelection { get; set; }

        internal static UserTestPage UserTest
        {
            get { return ms_UserTestPage ?? (ms_UserTestPage = new UserTestPage()); }
            set { ms_UserTestPage = value; }
        }

        #endregion

        #region Tech pages

        internal static TechnicianPage Technician
        {
            get { return ms_TechnicianPage ?? (ms_TechnicianPage = new TechnicianPage()); }
            set { ms_TechnicianPage = value; }
        }

        internal static PasswordPage Password
        {
            get { return ms_PasswordPage ?? (ms_PasswordPage = new PasswordPage()); }
            set { ms_PasswordPage = value; }
        }

        internal static ProfilePage ProfileEdit { get; set; }

        internal static CalibrationPage Calibration
        {
            get { return ms_CalibrationPage ?? (ms_CalibrationPage = new CalibrationPage()); }
            set { ms_CalibrationPage = value; }
        }

        internal static SLPage SL
        {
            get { return ms_SLPage ?? (ms_SLPage = new SLPage()); }
            set { ms_SLPage = value; }
        }

        internal static GatePage Gate
        {
            get { return ms_GatePage ?? (ms_GatePage = new GatePage()); }
            set { ms_GatePage = value; }
        }

        internal static BvtPage Bvt
        {
            get { return ms_BVTPage ?? (ms_BVTPage = new BvtPage()); }
            set { ms_BVTPage = value; }
        }

        internal static SelftestPage Selftest
        {
            get { return ms_SelftestPage ?? (ms_SelftestPage = new SelftestPage()); }
            set { ms_SelftestPage = value; }
        }

        internal static ResultsPage Results
        {
            get { return ms_ResultsPage ?? (ms_ResultsPage = new ResultsPage()); }
            set { ms_ResultsPage = value; }
        }

        internal static LogsPage Logs
        {
            get { return ms_LogsPage ?? (ms_LogsPage = new LogsPage()); }
            set { ms_LogsPage = value; }
        }

        internal static ConsolePage Console
        {
            get { return ms_ConsolePage ?? (ms_ConsolePage = new ConsolePage()); }
            set { ms_ConsolePage = value; }
        }

        internal static ClampPage Clamp
        {
            get { return ms_ClampPage ?? (ms_ClampPage = new ClampPage()); }
            set { ms_ClampPage = value; }
        }

        internal static DVdtPage DVdt
        {
            get { return ms_DVdtPage ?? (ms_DVdtPage = new DVdtPage()); }
            set { ms_DVdtPage = value; }
        }

        internal static ATUPage ATU
        {
            get { return ms_ATUPage ?? (ms_ATUPage = new ATUPage()); }
            set { ms_ATUPage = value; }
        }

        internal static QrrTqPage QrrTq
        {
            get { return ms_QrrTqPage ?? (ms_QrrTqPage = new QrrTqPage()); }
            set { ms_QrrTqPage = value; }
        }

        internal static RACPage RAC
        {
            get { return ms_RACPage ?? (ms_RACPage = new RACPage()); }
            set { ms_RACPage = value; }
        }

        internal static IHPage IH
        {
            get { return ms_IHPage ?? (ms_IHPage = new IHPage()); }
            set { ms_IHPage = value; }
        }
        #endregion

        #region Flags
        //данный флаг хранит значение режима работы, который выбран пользователем
        internal static UserWorkMode WorkMode { get; set; }
        #endregion
    }
}