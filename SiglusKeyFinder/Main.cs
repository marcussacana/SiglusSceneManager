using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SiglusKeyFinder
{
    public class KeyFinder {
        //A0 ? ? ? ?
        //MOV AL, OFF;

        /*
        MOVZX 0F B6 + 
        0D = ECX
        15 = EDX
        1D = EBX
        25 = ESP
        2D = EBP
        35 = ESI
        3D = EDI
        + DWORD;

         E?? = 32Bits == 0x11223344
         ?X = 16Bits  == 0x1122
         ?H = 8Bits == 0x33
         ?L = 8Bits == 0x44
        
        MOV_Long 88 ??
        84 == AL
        9C == BL
        8C == CL
        94 == DL
        + 24 + DWORD
        MOV OFFSET, Register;
              ^-------
        */

        public static Key[] ReadProcess(int PID) {
            Stream Memory = ProcessDump.OpenProcess(PID);
            Key[] Keys = ReadManually(Memory, (int)ProcessDump.BASEADDRESS);
            Memory.Close();
            File.Delete(ProcessDump.FILE);
            return Keys;
        }

        public static Key[] ReadManually(Stream Stream, int baseAddress) {
            return Process(Stream, baseAddress);
        }
        public static Key[] ReadFromPD(string ProcessDump) {
            string[] arr = Path.GetFileNameWithoutExtension(ProcessDump).Split('_');
            int Base;
            try {
                Base = int.Parse(arr[arr.Length - 1], System.Globalization.NumberStyles.HexNumber);
            }
            catch { return null; }
            return Process(new StreamReader(ProcessDump).BaseStream, Base);
        }
        private static Key[] Process(Stream Memory, int BASEOFFSET) {
            Result[] Results = Scan(Memory, Alg1Cmds, Algorithm.Type1);
            Result[] Results2 = Scan(Memory, Alg2Cmds, Algorithm.Type2);
            List<Key> Keys = new List<Key>();
            foreach (Result Result in Results.Concat(Results2))
                switch (Result.Type) {
                    case Algorithm.Type1:
                        Keys.Add(Interpreter(Result.Buffer, new BinaryReader(Memory), Alg1Cmds, BASEOFFSET));
                        break;
                    case Algorithm.Type2:
                        Keys.Add(Interpreter(Result.Buffer, new BinaryReader(Memory), Alg2Cmds, BASEOFFSET));
                        break;
                }
            return Keys.ToArray();
        }

        private static Key Interpreter(byte[] buffer, BinaryReader Mem, Command[] Cmds, int BASEOFFSET) {
            RegisterEmu Reg = new RegisterEmu();
            Dictionary<int, byte> Stack = new Dictionary<int, byte>();
            for (int i = 0, StepPos = 0; StepPos < Cmds.Length; StepPos++) {
                Command Step = Cmds[StepPos];
                Register REG;
                int Pointer;
                switch (Step) {
                    case Command.MOVXZ:
                        REG = ((Register)buffer[i + 2]);
                        Pointer = GetDW(buffer, i + 3) - BASEOFFSET;
                        Mem.BaseStream.Position = Pointer;
                        Mem.BaseStream.Flush();
                        Reg[REG] = Mem.ReadInt32();
                        break;
                    case Command.MOV_L:
                        REG = ((Register)buffer[i + 1]);
                        Pointer = GetDW(buffer, i + 3);
                        Stack.Add(Pointer, (byte)Reg[REG]);
                        break;
                    case Command.MOV_M:
                        REG = Register.AL;
                        Pointer = GetDW(buffer, i + 1) - BASEOFFSET;
                        Mem.BaseStream.Position = Pointer;
                        Reg[REG] = Mem.ReadByte();
                        break;
                    case Command.MOV_S:
                        REG = Register.AL;
                        int Pos = Tools.int8(buffer[i + 2]);
                        Stack.Add(Pos, (byte)Reg[REG]);
                        break;
                    case Command.MOV_ESI:
                        //We can ignore the MOV ESI, DS.
                        break;
                }
                i += GetLen(Step);
            }
            int[] Keys = Stack.Keys.ToArray();
            byte[] Values = Stack.Values.ToArray();
            Array.Sort(Keys, Values);
            return new Key(Values);
        }

        private static int GetDW(byte[] Buff, int pos) => Tools.int32(new byte[] { Buff[pos], Buff[pos + 1], Buff[pos + 2], Buff[pos + 3] });
        enum Command {
            MOVXZ, MOV_L, MOV_M, MOV_S, MOV_ESI
        }

        private static int GetLen(Command Type) { 
            switch (Type){
                case Command.MOV_L:
                case Command.MOVXZ:
                    return 7;
                case Command.MOV_M:
                    return 5;
                case Command.MOV_ESI:
                    return 6;
                case Command.MOV_S:
                    return 3;
                default:
                    throw new Exception("Unsupported Command");
            }
        }

        static Command[] Alg1Cmds = new Command[] {
                Command.MOVXZ, Command.MOVXZ,
                Command.MOVXZ, Command.MOV_L,
                Command.MOVXZ, Command.MOV_L,
                Command.MOVXZ, Command.MOV_L,
                Command.MOVXZ, Command.MOV_L,
                Command.MOVXZ, Command.MOV_L,
                Command.MOVXZ, Command.MOV_L,
                Command.MOVXZ, Command.MOV_L,
                Command.MOVXZ, Command.MOV_L,
                Command.MOVXZ, Command.MOV_L,
                Command.MOVXZ, Command.MOV_L,
                Command.MOVXZ, Command.MOV_L,
                Command.MOVXZ, Command.MOV_L,
                Command.MOVXZ, Command.MOV_L,
                Command.MOVXZ, Command.MOV_L,
                Command.MOV_L, Command.MOV_L
            };

        static Command[] Alg2Cmds = new Command[] {
            Command.MOV_M, Command.MOV_S,
            Command.MOV_M, Command.MOV_S,
            Command.MOV_M, Command.MOV_S,
            Command.MOV_M, Command.MOV_S,
            Command.MOV_M, Command.MOV_S,
            Command.MOV_M, Command.MOV_S,
            Command.MOV_M, Command.MOV_S,
            Command.MOV_M, Command.MOV_S,
            Command.MOV_M, Command.MOV_S,
            Command.MOV_M, Command.MOV_S,
            Command.MOV_M, Command.MOV_S,
            Command.MOV_M, Command.MOV_S,
            Command.MOV_M, Command.MOV_S,
            Command.MOV_M, Command.MOV_S,
            Command.MOV_M, Command.MOV_S,
            Command.MOV_M, Command.MOV_ESI,
            Command.MOV_S
        };

        private static Result[] Scan(Stream Mem, Command[] Cmds, Algorithm Type) {
            List<Result> Results = new List<Result>();
            byte Header = GetCmdByte(Cmds[0]);
            for (long i = 0; i < Mem.Length - 1024; i = Mem.Position) {
                byte[] Buffer = new byte[1024];
                Mem.Read(Buffer, 0, Buffer.Length);
                bool Found = SearchByte(ref Mem, Buffer, Header);

                if (Found) {
                    Buffer = new byte[300];
                    Peek(ref Buffer, Mem);
                    if (AlgValidate(Buffer, Cmds)) {
                        Results.Add(new Result() {
                            Buffer = Buffer,
                            Type = Type
                        });
                        Mem.Position += GetAlgLen(Cmds);
                    } else
                        Mem.Position++;
                }
            }
            Mem.Position = 0;
            return Results.ToArray();
        }

        private static void Peek(ref byte[] Buffer, Stream Mem) {
            long pos = Mem.Position;
            Mem.Read(Buffer, 0, Buffer.Length);
            Mem.Position = pos;
        }

        private static bool SearchByte(ref Stream Mem, byte[] Buffer, byte Byte) {
            for (int x = 0; x < Buffer.Length; x++) {
                if (Buffer[x] == Byte) {
                    Mem.Position -= Buffer.Length;
                    Mem.Position += x;
                    Mem.Flush();
                    return true;
                }
            }
            return false;
        }

        private static byte GetCmdByte(Command Cmd) {
            switch (Cmd) {
                case Command.MOVXZ:
                    return 0x0F;
                case Command.MOV_L:
                case Command.MOV_S:
                    return 0x88;
                case Command.MOV_ESI:
                    return 0x8B;
                case Command.MOV_M:
                    return 0xA0;
                default:
                    throw new Exception("Unsupported Command");
            }
        }
        private static int GetAlgLen(Command[] Cmds) {
            int LEN = 0;
            foreach (Command cmd in Cmds)
                LEN += GetLen(cmd);
            return LEN;
        }

        private static bool AlgType1Test(byte[] Buffer) {
            int StepPos = 0;
            for (int Pos = 0; StepPos < Alg1Cmds.Length; StepPos++) {
                Command Step = Alg1Cmds[StepPos];
                if (Step == Command.MOVXZ ? !Tools.isMOVZX(Buffer, Pos) : !Tools.isLongMOV(Buffer, Pos))
                    return false;
                Pos += GetLen(Step);
            }
            return true;
        }

        private static bool AlgValidate(byte[] Buffer, Command[] Cmds) {
            int StepPos = 0;
            for (int Pos = 0; StepPos < Cmds.Length; StepPos++) {
                Command Step = Cmds[StepPos];
                switch (Step) {
                    case Command.MOVXZ:
                        if (!Tools.isMOVZX(Buffer, Pos))
                            return false;
                        break;
                    case Command.MOV_L:
                        if (!Tools.isLongMOV(Buffer, Pos))
                            return false;
                        break;
                    case Command.MOV_M:
                        if (!Tools.IsMOVAL(Buffer, Pos))
                            return false;
                        break;
                    case Command.MOV_ESI:
                        if (!Tools.IsMOVESI(Buffer, Pos))
                            return false;
                        break;
                    case Command.MOV_S:
                        if (!Tools.IsMOVDS(Buffer, Pos))
                            return false;
                        break;
                }
                Pos += GetLen(Step);
            }
            return true;
        }

        public class Key {
            byte[] KEY;
            internal Key(byte[] KEY) {
                this.KEY = KEY;
            }

            public string KeyStr {
                get {
                    string Hex = string.Empty;
                    for (int i = 0; i < KEY.Length; i++) {
                        Hex += "0x" + KEY[i].ToString("X") + ", ";
                    }
                    return Hex.Substring(0, Hex.Length - 2);
                }
            }
        }
        

        private struct Result {
            public byte[] Buffer;
            public Algorithm Type; 
        }
        private enum Algorithm {
            Type1, Type2
        }
    }


    internal static class Tools {
        internal static bool isMOVZX(byte[] Buff, int Pos) {
            byte b = Buff[Pos], b2 = Buff[Pos + 1], b3 = Buff[Pos + 2];
            return b == 0x0F && b2 == 0xB6 && (b3 == 0x5 || b3 == 0xD || b3 == 0x15);
        }
        internal static bool isLongMOV(byte[] Buffer, int Pos) {
            byte b = Buffer[Pos + 1];
            int h = b >> 4;
            int l = b & 0xF;
            return ((h >= 8 && h <= 0xB) && (l == 0x4 || l == 0xC)) && Buffer[Pos] == 0x88;
        }
        internal static int int8(byte b) {
            return sbyte.Parse(b.ToString("x"), System.Globalization.NumberStyles.HexNumber);
        }
        internal static int int32(byte[] arr) {
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(arr, 0, arr.Length);
            int p = BitConverter.ToInt32(arr, 0);
            return p;
        }

        internal static bool IsMOVAL(byte[] Buffer, int pos) {
            return Buffer[pos] == 0xA0;
        }
        internal static bool IsMOVDS(byte[] Buffer, int pos) {
            return Buffer[pos] == 0x88 && !Tools.isLongMOV(Buffer, pos);
        }
        internal static bool IsMOVESI(byte[] Buffer, int pos) {
            return Buffer[pos] == 0x8B;
        }
    }
    internal class RegisterEmu {
        int EAX, ECX, EDX, EBX, ESP, EBP, ESI, EDI, EXTRA;
        internal int this[Register Reg] {
            get {
                switch (Reg) {
                    case Register.EAX:
                        return EAX;
                    case Register.EBP:
                        return EBP;
                    case Register.EBX:
                        return EBX;
                    case Register.ECX:
                        return ECX;
                    case Register.EDI:
                        return EDI;
                    case Register.EDX:
                        return EDX;
                    case Register.ESI:
                        return ESI;
                    case Register.ESP:
                        return ESP;
                    case Register.Unk:
                        return EXTRA;
                    case Register.AL:
                        return EAX & 0x000000FF;
                    case Register.BL:
                        return EBX & 0x000000FF;
                    case Register.CL:
                        return ECX & 0x000000FF;
                    case Register.DL:
                        return EDX & 0x000000FF;
                    default:
                        throw new Exception("Unsupported Register");
                }
            }
            set {
                switch (Reg) {
                    case Register.EAX:
                        EAX = value;
                        break;
                    case Register.EBP:
                        EBP = value;
                        break;
                    case Register.EBX:
                        EBX = value;
                        break;
                    case Register.ECX:
                        ECX = value;
                        break;
                    case Register.EDI:
                        EDI = value;
                        break;
                    case Register.EDX:
                        EDX = value;
                        break;
                    case Register.ESI:
                        ESI = value;
                        break;
                    case Register.ESP:
                        ESP = value;
                        break;
                    case Register.AL:
                        EAX = value & 0x000000FF;
                        break;
                    case Register.BL:
                        EBX = value & 0x000000FF;
                        break;
                    case Register.CL:
                        ECX = value & 0x000000FF;
                        break;
                    case Register.DL:
                        EDX = value & 0x000000FF;
                        break;
                    case Register.Unk:
                        EXTRA = value;
                        break;
                    default:
                        throw new Exception("Unsupported Register");
                }
            }
        }
    }
    internal enum Register {
        //MovZX
        EAX = 0x05, ECX = 0x0D,
        EDX = 0x15, EBX = 0x1D,
        ESP = 0x25, EBP = 0x2D,
        ESI = 0x35, EDI = 0x3D,
        //Mov Long
        AL = 0x84, BL = 0x9C,
        CL = 0x8C, DL = 0x94,
        //Unknowk
        Unk = -1
    }
}
