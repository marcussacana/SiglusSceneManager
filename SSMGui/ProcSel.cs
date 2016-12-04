using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SSMGui {
    public partial class ProcSel : Form {
        public ProcSel() {
            InitializeComponent();
            Update_Tick(null, null);
        }

        private void Update_Tick(object sender, EventArgs e) {
            ProcLst.Items.Clear();
            Process[] Procs = Process.GetProcesses();
            foreach (Process Proc in Procs){
                string Name = Proc.ProcessName, ID = Proc.Id.ToString();
                if (Name.ToLower().Contains(ProcName.Text.ToLower()))
                    ProcLst.Items.Add(new ListViewItem(new string[] { Name, ID }));
            } 
        }


        public int PID = -1;
        private void ProcLst_DoubleClick(object sender, EventArgs e) {
            try {
                string ID = ProcLst.SelectedItems[0].SubItems[1].Text;
                PID = int.Parse(ID);
                Close(); 
            } catch { }
        }

        private void ProcName_TextChanged(object sender, EventArgs e) {
            Update_Tick(null, null);
        }
    }
}
