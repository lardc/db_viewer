namespace SCME.DatabaseManager
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panel1 = new System.Windows.Forms.Panel();
            this.tbPassword = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.tbUserName = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.chbIntSec = new System.Windows.Forms.CheckBox();
            this.tbDBName = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cbConnect = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tbServer = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tabControlMain = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.cbImportResults = new System.Windows.Forms.CheckBox();
            this.cbImportProfiles = new System.Windows.Forms.CheckBox();
            this.btnImportData = new System.Windows.Forms.Button();
            this.btnBrowseProfiles = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.tbSQliteDBPath = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnResetDatabase = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tsslConnectStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.ofdDB = new System.Windows.Forms.OpenFileDialog();
            this.panel1.SuspendLayout();
            this.tabControlMain.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.tbPassword);
            this.panel1.Controls.Add(this.label7);
            this.panel1.Controls.Add(this.tbUserName);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.chbIntSec);
            this.panel1.Controls.Add(this.tbDBName);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.cbConnect);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.tbServer);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(654, 209);
            this.panel1.TabIndex = 0;
            // 
            // tbPassword
            // 
            this.tbPassword.Location = new System.Drawing.Point(176, 167);
            this.tbPassword.Margin = new System.Windows.Forms.Padding(4);
            this.tbPassword.Name = "tbPassword";
            this.tbPassword.Size = new System.Drawing.Size(359, 23);
            this.tbPassword.TabIndex = 11;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Verdana", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label7.Location = new System.Drawing.Point(25, 168);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(87, 18);
            this.label7.TabIndex = 10;
            this.label7.Text = "Password:";
            // 
            // tbUserName
            // 
            this.tbUserName.Location = new System.Drawing.Point(176, 136);
            this.tbUserName.Margin = new System.Windows.Forms.Padding(4);
            this.tbUserName.Name = "tbUserName";
            this.tbUserName.Size = new System.Drawing.Size(359, 23);
            this.tbUserName.TabIndex = 9;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Verdana", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label6.Location = new System.Drawing.Point(25, 137);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(96, 18);
            this.label6.TabIndex = 8;
            this.label6.Text = "User name:";
            // 
            // chbIntSec
            // 
            this.chbIntSec.AutoSize = true;
            this.chbIntSec.Checked = true;
            this.chbIntSec.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chbIntSec.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.chbIntSec.Location = new System.Drawing.Point(12, 104);
            this.chbIntSec.Name = "chbIntSec";
            this.chbIntSec.Size = new System.Drawing.Size(219, 22);
            this.chbIntSec.TabIndex = 7;
            this.chbIntSec.Text = "Use integrated security";
            this.chbIntSec.UseVisualStyleBackColor = true;
            this.chbIntSec.CheckedChanged += new System.EventHandler(this.chbIntSec_CheckedChanged);
            // 
            // tbDBName
            // 
            this.tbDBName.Location = new System.Drawing.Point(176, 74);
            this.tbDBName.Margin = new System.Windows.Forms.Padding(4);
            this.tbDBName.Name = "tbDBName";
            this.tbDBName.Size = new System.Drawing.Size(359, 23);
            this.tbDBName.TabIndex = 6;
            this.tbDBName.Text = "SCME_ResultsDB";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label4.Location = new System.Drawing.Point(11, 76);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(89, 18);
            this.label4.TabIndex = 5;
            this.label4.Text = "DB name:";
            // 
            // cbConnect
            // 
            this.cbConnect.Appearance = System.Windows.Forms.Appearance.Button;
            this.cbConnect.Location = new System.Drawing.Point(542, 44);
            this.cbConnect.Name = "cbConnect";
            this.cbConnect.Size = new System.Drawing.Size(90, 22);
            this.cbConnect.TabIndex = 4;
            this.cbConnect.Text = "Connect";
            this.cbConnect.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.cbConnect.UseVisualStyleBackColor = true;
            this.cbConnect.CheckedChanged += new System.EventHandler(this.CbConnect_CheckedChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label2.Location = new System.Drawing.Point(11, 8);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(483, 18);
            this.label2.TabIndex = 2;
            this.label2.Text = "Utility to manage result and profile database structure";
            // 
            // tbServer
            // 
            this.tbServer.Location = new System.Drawing.Point(176, 43);
            this.tbServer.Margin = new System.Windows.Forms.Padding(4);
            this.tbServer.Name = "tbServer";
            this.tbServer.Size = new System.Drawing.Size(359, 23);
            this.tbServer.TabIndex = 1;
            this.tbServer.Text = "LELIKK-PC";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label1.Location = new System.Drawing.Point(11, 44);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(117, 18);
            this.label1.TabIndex = 0;
            this.label1.Text = "Server name:";
            // 
            // tabControlMain
            // 
            this.tabControlMain.Controls.Add(this.tabPage1);
            this.tabControlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlMain.Location = new System.Drawing.Point(0, 209);
            this.tabControlMain.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.tabControlMain.Name = "tabControlMain";
            this.tabControlMain.SelectedIndex = 0;
            this.tabControlMain.Size = new System.Drawing.Size(654, 309);
            this.tabControlMain.TabIndex = 1;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.groupBox2);
            this.tabPage1.Controls.Add(this.groupBox1);
            this.tabPage1.Location = new System.Drawing.Point(4, 25);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.tabPage1.Size = new System.Drawing.Size(646, 280);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Manage DB";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.cbImportResults);
            this.groupBox2.Controls.Add(this.cbImportProfiles);
            this.groupBox2.Controls.Add(this.btnImportData);
            this.groupBox2.Controls.Add(this.btnBrowseProfiles);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.tbSQliteDBPath);
            this.groupBox2.Location = new System.Drawing.Point(5, 119);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.groupBox2.Size = new System.Drawing.Size(636, 102);
            this.groupBox2.TabIndex = 0;
            this.groupBox2.TabStop = false;
            // 
            // cbImportResults
            // 
            this.cbImportResults.AutoSize = true;
            this.cbImportResults.Location = new System.Drawing.Point(391, 63);
            this.cbImportResults.Name = "cbImportResults";
            this.cbImportResults.Size = new System.Drawing.Size(207, 20);
            this.cbImportResults.TabIndex = 6;
            this.cbImportResults.Text = "Импортировать измерения";
            this.cbImportResults.UseVisualStyleBackColor = true;
            // 
            // cbImportProfiles
            // 
            this.cbImportProfiles.AutoSize = true;
            this.cbImportProfiles.Location = new System.Drawing.Point(191, 63);
            this.cbImportProfiles.Name = "cbImportProfiles";
            this.cbImportProfiles.Size = new System.Drawing.Size(194, 20);
            this.cbImportProfiles.TabIndex = 5;
            this.cbImportProfiles.Text = "Импортировать профили";
            this.cbImportProfiles.UseVisualStyleBackColor = true;
            // 
            // btnImportData
            // 
            this.btnImportData.Location = new System.Drawing.Point(9, 58);
            this.btnImportData.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.btnImportData.Name = "btnImportData";
            this.btnImportData.Size = new System.Drawing.Size(150, 28);
            this.btnImportData.TabIndex = 4;
            this.btnImportData.Text = "Import data";
            this.btnImportData.UseVisualStyleBackColor = true;
            this.btnImportData.Click += new System.EventHandler(this.btnImportProfiles_Click);
            // 
            // btnBrowseProfiles
            // 
            this.btnBrowseProfiles.Location = new System.Drawing.Point(556, 20);
            this.btnBrowseProfiles.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.btnBrowseProfiles.Name = "btnBrowseProfiles";
            this.btnBrowseProfiles.Size = new System.Drawing.Size(67, 26);
            this.btnBrowseProfiles.TabIndex = 3;
            this.btnBrowseProfiles.Text = "Browse";
            this.btnBrowseProfiles.UseVisualStyleBackColor = true;
            this.btnBrowseProfiles.Click += new System.EventHandler(this.btnBrowseProfiles_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label5.Location = new System.Drawing.Point(6, 23);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(157, 18);
            this.label5.TabIndex = 0;
            this.label5.Text = "Path to SQlite db:";
            // 
            // tbSQliteDBPath
            // 
            this.tbSQliteDBPath.Location = new System.Drawing.Point(191, 22);
            this.tbSQliteDBPath.Margin = new System.Windows.Forms.Padding(4);
            this.tbSQliteDBPath.Name = "tbSQliteDBPath";
            this.tbSQliteDBPath.Size = new System.Drawing.Size(359, 23);
            this.tbSQliteDBPath.TabIndex = 1;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.btnResetDatabase);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Location = new System.Drawing.Point(5, 5);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.groupBox1.Size = new System.Drawing.Size(636, 108);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            // 
            // btnResetDatabase
            // 
            this.btnResetDatabase.Location = new System.Drawing.Point(9, 68);
            this.btnResetDatabase.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.btnResetDatabase.Name = "btnResetDatabase";
            this.btnResetDatabase.Size = new System.Drawing.Size(150, 28);
            this.btnResetDatabase.TabIndex = 4;
            this.btnResetDatabase.Text = "Reset database";
            this.btnResetDatabase.UseVisualStyleBackColor = true;
            this.btnResetDatabase.Click += new System.EventHandler(this.BtnResetDatabase_Click);
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label3.Location = new System.Drawing.Point(6, 19);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(634, 46);
            this.label3.TabIndex = 1;
            this.label3.Text = "Press button below to reset all database content and populate dictionaries with d" +
    "efault values";
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsslConnectStatus});
            this.statusStrip1.Location = new System.Drawing.Point(0, 496);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(654, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // tsslConnectStatus
            // 
            this.tsslConnectStatus.Name = "tsslConnectStatus";
            this.tsslConnectStatus.Size = new System.Drawing.Size(71, 17);
            this.tsslConnectStatus.Text = "Disconnected";
            // 
            // ofdDB
            // 
            this.ofdDB.Filter = "Database|*.sqlite";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(654, 518);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.tabControlMain);
            this.Controls.Add(this.panel1);
            this.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MainForm";
            this.Text = "SQL Server database manager";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.tabControlMain.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TextBox tbServer;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TabControl tabControlMain;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.CheckBox cbConnect;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnResetDatabase;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel tsslConnectStatus;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnImportData;
        private System.Windows.Forms.Button btnBrowseProfiles;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbSQliteDBPath;
        private System.Windows.Forms.OpenFileDialog ofdDB;
        private System.Windows.Forms.TextBox tbPassword;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox tbUserName;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox chbIntSec;
        private System.Windows.Forms.TextBox tbDBName;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox cbImportResults;
        private System.Windows.Forms.CheckBox cbImportProfiles;
    }
}

