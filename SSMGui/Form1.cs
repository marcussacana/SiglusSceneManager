using System;
using System.ComponentModel;
using SiglusSceneManager;
using System.Windows.Forms;

namespace SSMGui {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
            MessageBox.Show("This GUI don't is a stable translation tool, this program is a Demo for my dll, the \"SiglusSceneManager.dll\" it's a opensoruce project to allow you make your program to edit any script from SiglusEngine\n\nHow to use:\n*Rigth Click in the window to open or save the file\n*Select the string in listbox and edit in the text box\n*Press enter to update the string\n\nThis program is unstable!");
        }
        public SSManager Script;
        private void openFileDialog1_FileOk(object sender, CancelEventArgs e) {
            byte[] file = System.IO.File.ReadAllBytes(openFileDialog1.FileName);
            Script = new SSManager(file);
            listBox1.Items.Clear();
            foreach (string str in Script.Import())
                listBox1.Items.Add(str);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            openFileDialog1.ShowDialog();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
            saveFileDialog1.ShowDialog();
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e) {
            string[] strs = new string[listBox1.Items.Count];
            for (int i = 0; i < strs.Length; i++)
                strs[i] = listBox1.Items[i].ToString();
            byte[] newscript = Script.Export(strs);
            System.IO.File.WriteAllBytes(saveFileDialog1.FileName, newscript);
            MessageBox.Show("Script Saved.", "SSMGui");
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e) {
            try {
                this.Text = "id: " + listBox1.SelectedIndex;
                textBox1.Text = listBox1.Items[listBox1.SelectedIndex].ToString().Replace("\n", "\\n");
            }
            catch {

            }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e) {
            if (e.KeyChar == '\r' || e.KeyChar == '\n') {
                listBox1.Items[listBox1.SelectedIndex] = textBox1.Text.Replace("\\n", "\n");
            }
        }    
        private void findSiglusEngineKeyToolStripMenuItem_Click(object sender, EventArgs e) {
            int PID = GetPID();
            if (PID != -1) {
                SiglusKeyFinder.KeyFinder.Key[] Keys = SiglusKeyFinder.KeyFinder.ReadProcess(PID);
                string MSG = "Keys found:\n";
                foreach (SiglusKeyFinder.KeyFinder.Key Key in Keys)
                    MSG += Key.KeyStr + "\n---------------------------\n";
                MSG += "Press CTRL + C to Copy";
                MessageBox.Show(MSG, "SiglusKeyFinder - By Marcussacana", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private int GetPID() {
            ProcSel form = new ProcSel();
            form.ShowDialog();
            return form.PID;
        }

        private void textBox1_TextChanged(object sender, EventArgs e) {

        }
    }
}
