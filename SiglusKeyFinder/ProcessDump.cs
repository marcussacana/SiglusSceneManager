using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace SiglusKeyFinder {
    class ProcessDump {
        internal static string FILE;
        internal static uint BASEADDRESS;
        internal static Stream OpenProcess(int PID, bool CrashIfCantDump = false) {
            while (!CheckRequeriments()) {
                if (CrashIfCantDump)
                    throw new Exception("VS15 C++ Redist not installed.");

                DialogResult Result = MessageBox.Show("Install the VS2015 Redist Packget before continue", "Requried Library Not Installed", MessageBoxButtons.RetryCancel);
                if (Result == DialogResult.Cancel)
                    throw new Exception("VS15 C++ Redist not installed.");
            }
            string WorkDir = Path.GetTempPath() + "PDWDIR-" + new Random().Next(100, 999);
            if (!Directory.Exists(WorkDir))
                Directory.CreateDirectory(WorkDir);
            string PD = GetTmp(Environment.Is64BitOperatingSystem ? "pd64" : "pd32");
            string Arguments = string.Format("-pid {0} -o \"{1}\"", PID, WorkDir);
            ProcessStartInfo SI = new ProcessStartInfo() {
                FileName = PD,
                Arguments = Arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            Process Proc = Process.Start(SI);
            Proc.StandardOutput.ReadToEnd();
            Proc.WaitForExit();
            Proc.Close();
            File.Delete(PD);
            string EXE = Path.GetFileName(Process.GetProcessById(PID).ProcessName) + ".exe";

            string TMP = Path.GetTempFileName();
            string Filter = EXE.Replace(".", "_") + "*" + EXE + "_*.exe";
            string[] Modules = Directory.GetFiles(WorkDir, Filter);
            if (Modules.Length == 0)
                throw new Exception("Failed to dump the process");
            string Dump = Modules[0];
            string[] Parts = Path.GetFileNameWithoutExtension(Dump).Split('_');
            BASEADDRESS = Convert.ToUInt32(Parts[Parts.Length - 2], 16);
            if (File.Exists(TMP))
                File.Delete(TMP);
            File.Move(Dump, TMP);
            Directory.Delete(WorkDir, true);
            FILE = TMP;
            return new StreamReader(TMP).BaseStream;
        }

        private static bool CheckRequeriments() {
            const string DLL = "msvcp140.dll";
            string Dir = Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\";
            if (!File.Exists(Dir + DLL))
                return false;
            return true;
        }

        private static string GetTmp(string Resource) {
            Stream Reader = Assembly.GetExecutingAssembly().GetManifestResourceStream("SiglusKeyFinder." + Resource);
            string tmp = Path.GetTempFileName();
            Stream Out = new StreamWriter(tmp).BaseStream;
            int count = 0;
            byte[] Buffer = new byte[1024];
            do {
                count = Reader.Read(Buffer, 0, Buffer.Length);
                Out.Write(Buffer, 0, count);
            } while (count > 0);
            Out.Close();
            Reader.Close();
            return tmp;
        }
    }
}
