using System;
using System.Data;
using System.Windows.Forms;
using SCME.InterfaceImplementations;
using SCME.Types.Interfaces;

namespace SCME.DatabaseManager
{
    public partial class MainForm : Form
    {
        private IDatabaseService _mDbService;
        
        public MainForm()
        {
            InitializeComponent();
        }

        private void BtnBrowseDb_Click(object Sender, EventArgs E)
        {
            if (ofdDatabase.ShowDialog() == DialogResult.OK)
                tbDatabase.Text = ofdDatabase.FileName;
        }

        private void CbConnect_CheckedChanged(object Sender, EventArgs E)
        {
            if (cbConnect.Checked)
            {
                try
                {
                    _mDbService = new DatabaseService(tbDatabase.Text);
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
                MessageBox.Show(ex.Message, @"Connection error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnBrowseProfiles_Click(object sender, EventArgs e)
        {
            if (ofdProfiles.ShowDialog() == DialogResult.OK)
            {
                tbProfilesPath.Text = ofdProfiles.FileName;
            }
        }

        private void btnImportProfiles_Click(object sender, EventArgs e)
        {
            try
            {
                if (_mDbService != null && _mDbService.State == ConnectionState.Open)
                {
                    _mDbService.ImportProfiles(tbProfilesPath.Text);
                    MessageBox.Show(@"Profiles imported", @"Profiles imported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                    MessageBox.Show(@"No connection to database", @"Operation error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, @"Connection error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
