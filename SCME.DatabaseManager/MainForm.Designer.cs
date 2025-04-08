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
            this.cbConnect = new System.Windows.Forms.CheckBox();
            this.btnBrowseDB = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.tbDatabase = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.ofdDatabase = new System.Windows.Forms.OpenFileDialog();
            this.tabControlMain = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btnImportProfiles = new System.Windows.Forms.Button();
            this.btnBrowseProfiles = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.tbProfilesPath = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnResetDatabase = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tsslConnectStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.ofdProfiles = new System.Windows.Forms.OpenFileDialog();
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
            this.panel1.Controls.Add(this.cbConnect);
            this.panel1.Controls.Add(this.btnBrowseDB);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.tbDatabase);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(723, 77);
            this.panel1.TabIndex = 0;
            // 
            // cbConnect
            // 
            this.cbConnect.Appearance = System.Windows.Forms.Appearance.Button;
            this.cbConnect.AutoSize = true;
            this.cbConnect.Location = new System.Drawing.Point(613, 41);
            this.cbConnect.Name = "cbConnect";
            this.cbConnect.Size = new System.Drawing.Size(73, 26);
            this.cbConnect.TabIndex = 4;
            this.cbConnect.Text = "Connect";
            this.cbConnect.UseVisualStyleBackColor = true;
            this.cbConnect.CheckedChanged += new System.EventHandler(this.CbConnect_CheckedChanged);
            // 
            // btnBrowseDB
            // 
            this.btnBrowseDB.Location = new System.Drawing.Point(541, 41);
            this.btnBrowseDB.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.btnBrowseDB.Name = "btnBrowseDB";
            this.btnBrowseDB.Size = new System.Drawing.Size(67, 26);
            this.btnBrowseDB.TabIndex = 3;
            this.btnBrowseDB.Text = "Browse";
            this.btnBrowseDB.UseVisualStyleBackColor = true;
            this.btnBrowseDB.Click += new System.EventHandler(this.BtnBrowseDb_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label2.Location = new System.Drawing.Point(11, 8);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(384, 18);
            this.label2.TabIndex = 2;
            this.label2.Text = "Utility to manage result database structure";
            // 
            // tbDatabase
            // 
            this.tbDatabase.Location = new System.Drawing.Point(176, 43);
            this.tbDatabase.Margin = new System.Windows.Forms.Padding(4);
            this.tbDatabase.Name = "tbDatabase";
            this.tbDatabase.Size = new System.Drawing.Size(359, 23);
            this.tbDatabase.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label1.Location = new System.Drawing.Point(11, 44);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(157, 18);
            this.label1.TabIndex = 0;
            this.label1.Text = "Path to database:";
            // 
            // ofdDatabase
            // 
            this.ofdDatabase.Filter = "SQLite database|*.sqlite";
            // 
            // tabControlMain
            // 
            this.tabControlMain.Controls.Add(this.tabPage1);
            this.tabControlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlMain.Location = new System.Drawing.Point(0, 77);
            this.tabControlMain.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.tabControlMain.Name = "tabControlMain";
            this.tabControlMain.SelectedIndex = 0;
            this.tabControlMain.Size = new System.Drawing.Size(723, 441);
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
            this.tabPage1.Size = new System.Drawing.Size(715, 412);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Manage DB";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.btnImportProfiles);
            this.groupBox2.Controls.Add(this.btnBrowseProfiles);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.tbProfilesPath);
            this.groupBox2.Location = new System.Drawing.Point(5, 119);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.groupBox2.Size = new System.Drawing.Size(705, 102);
            this.groupBox2.TabIndex = 0;
            this.groupBox2.TabStop = false;
            this.groupBox2.Visible = false;
            // 
            // btnImportProfiles
            // 
            this.btnImportProfiles.Location = new System.Drawing.Point(9, 58);
            this.btnImportProfiles.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.btnImportProfiles.Name = "btnImportProfiles";
            this.btnImportProfiles.Size = new System.Drawing.Size(150, 28);
            this.btnImportProfiles.TabIndex = 4;
            this.btnImportProfiles.Text = "Import profiles";
            this.btnImportProfiles.UseVisualStyleBackColor = true;
            this.btnImportProfiles.Click += new System.EventHandler(this.btnImportProfiles_Click);
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
            this.label5.Size = new System.Drawing.Size(177, 18);
            this.label5.TabIndex = 0;
            this.label5.Text = "Path to profiles xml:";
            // 
            // tbProfilesPath
            // 
            this.tbProfilesPath.Location = new System.Drawing.Point(191, 22);
            this.tbProfilesPath.Margin = new System.Windows.Forms.Padding(4);
            this.tbProfilesPath.Name = "tbProfilesPath";
            this.tbProfilesPath.Size = new System.Drawing.Size(359, 23);
            this.tbProfilesPath.TabIndex = 1;
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
            this.groupBox1.Size = new System.Drawing.Size(705, 108);
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
            this.statusStrip1.Size = new System.Drawing.Size(723, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // tsslConnectStatus
            // 
            this.tsslConnectStatus.Name = "tsslConnectStatus";
            this.tsslConnectStatus.Size = new System.Drawing.Size(71, 17);
            this.tsslConnectStatus.Text = "Disconnected";
            // 
            // ofdProfiles
            // 
            this.ofdProfiles.Filter = "XML|*.xml";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(723, 518);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.tabControlMain);
            this.Controls.Add(this.panel1);
            this.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MainForm";
            this.Text = "Database manager";
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
        private System.Windows.Forms.TextBox tbDatabase;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnBrowseDB;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.OpenFileDialog ofdDatabase;
        private System.Windows.Forms.TabControl tabControlMain;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.CheckBox cbConnect;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnResetDatabase;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel tsslConnectStatus;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnImportProfiles;
        private System.Windows.Forms.Button btnBrowseProfiles;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbProfilesPath;
        private System.Windows.Forms.OpenFileDialog ofdProfiles;
    }
}

