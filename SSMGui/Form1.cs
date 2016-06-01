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
            Script.Import();
            listBox1.Items.Clear();
            foreach (string str in Script.Strings)
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
            Script.Strings = strs;
            byte[] newscript = Script.Export();
            System.IO.File.WriteAllBytes(saveFileDialog1.FileName, newscript);
            MessageBox.Show("Script Saved.", "SSMGui");
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e) {
            try {
                this.Text = "id: " + listBox1.SelectedIndex;
                textBox1.Text = listBox1.Items[listBox1.SelectedIndex].ToString();
            }
            catch {

            }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e) {
            if (e.KeyChar == '\r' || e.KeyChar == '\n') {
                listBox1.Items[listBox1.SelectedIndex] = textBox1.Text;
            }
        }

        private void getSiglusKey2ToolStripMenuItem_Click(object sender, EventArgs e) {
            MessageBox.Show("Select a memory dump from the game, use the \"ProcessDump\" to dump a compatible binary and you can't rename dumped files...\n\nhttp://split-code.com/processdump.html\n\nCompatible only with SiglusEngine v1.1 or better \nPress CTRL + C to copy", "Unstable - Siglus Key Discovery Tool");
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "All Executables|*.exe";
            fd.Title = "Select a memory dump from siglus engine game";
            DialogResult dr = fd.ShowDialog();
            if (dr == DialogResult.OK) {
                string fname = System.IO.Path.GetFileNameWithoutExtension(fd.FileName);
                int Base = 0;
                //SiglusEngine_exe_SiglusEngine.exe_BaseOffset
                if (fname.Contains(".") && fname.Contains("_")) {
                    string[] arr = fname.Split('_');
                    try {
                        Base = int.Parse(arr[arr.Length - 1], System.Globalization.NumberStyles.HexNumber);
                    }
                    catch { goto erro; }

                } else goto erro;
                KeyFinder KF = new KeyFinder(fd.FileName, Base);
                int total = KF.FindKeys();
                string MSG = "Valid Keys to this dump:\n";
                for (int i = 0; i < total; i++) {
                    Key Pass = KF.GetKey(i);
                    if (Pass.Corrupted)
                        continue;
                    string key = Pass.String;
                    MSG += Pass.Main ? "Looks Correct: " + key : "Try: " + key;
                    MSG += "\n---------------------------\n";
                }
                MSG += "Press CTRL + C to Copy";
                MessageBox.Show(MSG, "Test your lucky - Unstable SiglusEngine Key Finder - Work only with newer Games", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return;
        erro:;
            MessageBox.Show("ERROR - You rename the process dump??", "Unstable - Srry");
        }
    }
}
