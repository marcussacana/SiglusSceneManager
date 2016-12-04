namespace SSMGui {
    partial class ProcSel {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            this.ProcLst = new System.Windows.Forms.ListView();
            this.Name = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Update = new System.Windows.Forms.Timer(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.ProcName = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // ProcLst
            // 
            this.ProcLst.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ProcLst.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Name,
            this.ID});
            this.ProcLst.Location = new System.Drawing.Point(0, 0);
            this.ProcLst.Name = "ProcLst";
            this.ProcLst.Size = new System.Drawing.Size(494, 357);
            this.ProcLst.TabIndex = 0;
            this.ProcLst.UseCompatibleStateImageBehavior = false;
            this.ProcLst.View = System.Windows.Forms.View.Details;
            this.ProcLst.DoubleClick += new System.EventHandler(this.ProcLst_DoubleClick);
            // 
            // Name
            // 
            this.Name.Text = "Name";
            this.Name.Width = 392;
            // 
            // ID
            // 
            this.ID.Text = "PID";
            this.ID.Width = 93;
            // 
            // Update
            // 
            this.Update.Enabled = true;
            this.Update.Interval = 10000;
            this.Update.Tick += new System.EventHandler(this.Update_Tick);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 368);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(39, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "Find:";
            // 
            // ProcName
            // 
            this.ProcName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ProcName.Location = new System.Drawing.Point(57, 363);
            this.ProcName.Name = "ProcName";
            this.ProcName.Size = new System.Drawing.Size(425, 22);
            this.ProcName.TabIndex = 2;
            this.ProcName.Text = "SiglusEngine";
            this.ProcName.TextChanged += new System.EventHandler(this.ProcName_TextChanged);
            // 
            // ProcSel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(494, 394);
            this.Controls.Add(this.ProcName);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ProcLst);
            this.Text = "Double Click a Process";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView ProcLst;
        private System.Windows.Forms.Timer Update;
        private System.Windows.Forms.ColumnHeader Name;
        private System.Windows.Forms.ColumnHeader ID;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox ProcName;
    }
}