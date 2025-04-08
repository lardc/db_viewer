using System;
using System.Data;
using System.Windows.Forms;
using SCME.InterfaceImplementations;

namespace SCME.DatabaseManager
{
    public partial class MainForm : Form
    {
        private SQLDatabaseService _mDbService;
        
        public MainForm()
        {
            InitializeComponent();
        }

        private void CbConnect_CheckedChanged(object Sender, EventArgs E)
        {
            if (cbConnect.Checked)
            {
                try
                {
                    var connectionString = chbIntSec.Checked
                        ? $"Server={tbServer.Text}; Database={tbDBName.Text}; Integrated Security=true;"
                        : $"Server={tbServer.Text}; Database={tbDBName.Text}; User Id={tbUserName.Text}; Password={tbPassword.Text};";

                    _mDbService = new SQLDatabaseService(connectionString);
                    _mDbService.Open();

                    tsslConnectStatus.Text = @"Connected";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, @"Connection error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    cbConnect.Checked = false;
                }
            }
            else
            {
                if (_mDbService != null)
                {
                    _mDbService.Close();
                    _mDbService = null;
                }

                tsslConnectStatus.Text = @"Disconnected";
            }
        }

        private void BtnResetDatabase_Click(object Sender, EventArgs E)
        {
            try
            {
                if (_mDbService != null && _mDbService.State == ConnectionState.Open)
                {
                    _mDbService.ResetContent();
                    MessageBox.Show(@"Content reseted", @"Content reseted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                    MessageBox.Show(@"No connection to database", @"Operation error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, @"Operation error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnBrowseProfiles_Click(object sender, EventArgs e)
        {
            if (ofdDB.ShowDialog() == DialogResult.OK)
            {
                tbSQliteDBPath.Text = ofdDB.FileName;
            }
        }

        private void btnImportProfiles_Click(object sender, EventArgs e)
        {
            try
            {
                const string dbSettings = "synchronous=Full;journal mode=Truncate;failifmissing=True";
                var connStr = $"data source={tbSQliteDBPath.Text};{dbSettings}";
    
                _mDbService.ImportData(connStr, cbImportProfiles.Checked, cbImportResults.Checked);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, @"Operation error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void chbIntSec_CheckedChanged(object sender, EventArgs e)
        {
            tbUserName.Enabled = !chbIntSec.Checked;
            tbPassword.Enabled = !chbIntSec.Checked;
        }
    }
}
