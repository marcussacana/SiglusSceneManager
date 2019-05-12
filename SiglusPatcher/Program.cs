using SiglusKeyFinder;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace SiglusPatcher {
    class Program {

        static readonly uint[] KeyPointers = new uint[] 
        { 0x6A1416, 0x6A13E1, 0x6A163B, 0x6A1593, 0x6A11D1, 0x6A16F6,
          0x6A15D5, 0x6A17D0, 0x6A17FF, 0x6A1355, 0x6A12DB, 0x6A1788,
          0x6A111D, 0x6A14A1, 0x6A125B, 0x6A10AD };


        //If one don't works, try other....
        static readonly byte[] ValidationData = new byte[] { 0x00, 0x8B, 0x01, 0x3B, 0x02, 0x75, 0xFF, 0x83, 0xC1, 0x04 };
        //static readonly byte[] ValidationData = new byte[] { 0x8B, 0x01, 0x3B, 0x02, 0x75, 0xFF };
        //static readonly byte[] ValidationData = new byte[] { 0x8B, 0x01, 0x3B, 0x02, 0x75, 0xFF, 0x83, 0xC1, 0x04 };

        static readonly byte[] Unks = new byte[] { 0x55, 0x74 };

        private static readonly Version BypVer = new Version(1, 1, 107, 0);

        static void Main(string[] args) {
            Console.Title = "SiglusPatcher v2 - By Marcussacana";
            if (args?.Length == 0) {
                Console.WriteLine("Drag&Drop the SiglusEngine.exe to the \"{0}\" to enable the Debug Mode", Path.GetFileName(Application.ExecutablePath));
                Console.WriteLine("Or execute -WordWrap SiglusEngine.exe if you want enable the WordWrap. (Test before release!)");
                Console.ReadKey();
                return;
            }
            bool Wordwrap = false;
            foreach (string exe in args) {
                if (exe.Trim(' ', '-', '\\').ToLower().StartsWith("wordwrap")) {
                    Wordwrap = true;
                    continue;
                }
                byte[] Executable = File.ReadAllBytes(exe);
                bool NoLoop = false;
                again: ;
                long Pos = -1;
                for (uint i = 0; i < Executable.LongLength; i++) {
                    if (EqualsAt(Executable, ValidationData, i))
                        if (Pos != -1) {
                            Pos = -1;
                            break;
                        } else {
                            uint Rst = IndexOfSequence(ValidationData, new byte[] { 0x75, 0xFF });
                            if (Rst == uint.MaxValue) {
                                Pos = -1;
                                break;
                            }
                            Pos = i + (int)Rst;
                            break;
                        }
                }
                if (Pos == -1 || Wordwrap) {                    
                    if (NoLoop) {
                        Console.WriteLine("Something Looks Wrong...");
                        Console.ReadKey();
                        continue;
                    }
                    if (!Wordwrap)
                        Console.WriteLine("Executable Protected? Trying Brute Patch mode...");
                    else
                        Console.WriteLine("When release the user needs the SiglusEngine.exe and SiglusDRM.dll to Play, the SiglusDebugger3.dll isn't needed.");

                    Wordwrap = false;
                    Version Version = GetFileVersion(exe);
                    if (Version > BypVer) {
                        Console.WriteLine("This executable is too newer, the patch can cause bugs, Continue?");
                        if (Console.ReadKey().KeyChar.ToString().ToUpper()[0] != 'Y')
                            continue;
                    }

                    if (File.Exists(GetDirectory(exe) + "\\SiglusDebugger3.dll"))
                        File.Delete(GetDirectory(exe) + "\\SiglusDebugger3.dll");

                    Console.WriteLine("Detecting Encryption Key...");
                    byte[] OriKey = GetSiglusKey(exe);

                    Console.WriteLine("Extracting Engine...");
                    ExtractResource("SiglusEngine.exe", GetDirectory(exe) + "\\");
                    ExtractResource("SiglusDRM.dll", GetDirectory(exe) + "\\");

                    Console.WriteLine("Updating Encryption Key...");
                    Executable = File.ReadAllBytes(GetDirectory(exe) + "\\SiglusEngine.exe");
                    for (int i = 0; i < OriKey.Length; i++) {
                        Console.WriteLine("Patching at 0x{0:X8} from 0x{1:X2} to 0x{2:X2}", KeyPointers[i], Executable[KeyPointers[i]], OriKey[i]);
                        Executable[KeyPointers[i]] = OriKey[i];
                    }

                    NoLoop = true;
                    goto again;
                }
                Console.WriteLine($"Patching at 0x{Pos:X8} from 0x75{Executable[Pos + 1].ToString("X2")} to 0x9090");
                Executable[Pos++] = 0x90;
                Executable[Pos] = 0x90;

                Console.WriteLine("Extracting SiglusDebugger...");
                ExtractResource("SiglusDebugger3.dll", GetDirectory(exe) + "\\");

                if (!File.Exists(exe + ".bak"))
                    File.Move(exe, exe + ".bak");
                File.WriteAllBytes(exe, Executable);
                Console.WriteLine("Patched Successfully.");
            }
            Console.ReadKey();
        }

        private static string GetDirectory(string File) {
            if (File.Length < 2)
                return Path.GetDirectoryName(File);
            if (File[1] == ':')
                return Path.GetDirectoryName(File);
            return AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
        }
        private static uint IndexOfSequence(byte[] Data, byte[] Sequence) {
            for (uint i = 0; i < Data.Length; i++) {
                if (EqualsAt(Data, Sequence, i, true))
                    return i;
            }
            return uint.MaxValue;
        }

        private static byte[] GetSiglusKey(string Exe) {
            try
            {
                Process Proc = Process.Start(Exe);
                while (Proc.MainWindowHandle == IntPtr.Zero)
                    System.Threading.Thread.Sleep(100);
                KeyFinder.Key Key = KeyFinder.ReadProcess(Proc.Id)[0];
                byte[] KeyB = Key.KEY;
                Proc.Kill();
                Console.WriteLine("Encryption Key Detected:\n{0}", Key.KeyStr);
                return KeyB;
            }
            catch (Exception ex) {
                var Color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed, Please, Report a issue... LOG:\n{0}", ex.ToString());
                Console.ForegroundColor = Color;

                Console.WriteLine("You know the key? If yes, type it...");
                string Reply = Console.ReadLine();
                Reply = Reply.ToUpper().Replace("0X", "").Replace(",", "").Replace(" ", "").Trim();

                if (string.IsNullOrWhiteSpace(Reply) || Reply.Length/2 != KeyPointers.Length)
                    throw new Exception();

                if ((from x in Reply where ((x >= '0' && x <= '9') || (x >= 'A' && x <= 'F')) select x).Count() != Reply.Length)
                    throw new Exception();

                byte[] Key = new byte[KeyPointers.Length];
                for (byte i = 0; i < Key.Length; i++)
                {
                    Key[i] = Convert.ToByte(Reply.Substring(i * 2, 2), 16);
                }

                return Key;
            }
        }

        private static void ExtractResource(string Resource, string Dir) {
            string OutPath = Dir.TrimEnd('\\') + "\\" + Resource;
            if (OutPath.StartsWith("\\"))
                OutPath = '.' + OutPath;

            while (File.Exists(OutPath))
                try { File.Delete(OutPath); } catch {};

            Stream ResStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SiglusPatcher.Resources." + Resource);

            Stream Output = new StreamWriter(OutPath).BaseStream;
            ResStream.CopyTo(Output);
            ResStream.Close();
            Output.Close();
        }

        private static Version GetFileVersion(string exe) {
            FileVersionInfo Info = FileVersionInfo.GetVersionInfo(exe);
            string[] Version = Info.FileVersion.Replace(",", ".").Split('.');
            return new Version(int.Parse(Version[0].Trim()), int.Parse(Version[1].Trim()), int.Parse(Version[2].Trim()), int.Parse(Version[3].Trim()));
        }

        private static bool EqualsAt(byte[] Data, byte[] DataToCompare, uint At, bool NoMask = false) {
            if (DataToCompare.LongLength + At > Data.LongLength)
                return false;
            for (uint i = 0; i < DataToCompare.LongLength; i++){
                byte Byte = Data[i + At];
                if (DataToCompare[i] == 0xFF && Unks.Contains(Byte) && !NoMask)
					continue;
                if (Byte != DataToCompare[i])
                    return false;
			}
            return true;
        }
    }
}
