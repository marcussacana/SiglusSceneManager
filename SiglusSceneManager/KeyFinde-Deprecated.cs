using System;
using System.IO;

namespace SiglusSceneManager {
    public class KeyFinder {
        private Stream Memory;
        private int Base;
        public KeyFinder(string ProcessDump, int DB) {
            Memory = new FileStream(ProcessDump, FileMode.Open, FileAccess.Read, FileShare.Read, 255);
            Base = DB;
        }
        public KeyFinder(Stream ProcessDump, int DB) {
            Memory = ProcessDump;
            Base = DB;
        }
        public byte[] key { get; private set; }

        const byte MOV_AL = 0xA0;
        const int Length_MOV_AL = 0x5;

        const byte MOV_BYTE_PTR = 0x88;
        const int Length_MOV_BYTE_PTR = 0x3;

        const byte MOV_ESI = 0x8B;
        const int Length_MOV_ESI = 0x6;

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
        */

        /*
         E?? = 32Bits == 0x11223344
         ?X = 16Bits  == 0x1122
         ?H = 8Bits == 0x33
         ?L = 8Bits == 0x44
        */

        /*MOV_Long 88 ??
        84 == AL
        9C == BL
        8C == CL
        94 == DL
        + 24 + DWORD
        MOV OFFSET, Register;
        */

        private Register[] ParseMOV(byte b) {
            Register[] Result = new Register[2];
            //0 = Dest
            //1 = Source
            #region switch
            switch (b) {
                //MOV Short Table
                case 0x45:
                    Result[0] = Register.EBP;
                    Result[1] = Register.AL;
                    break;
                case 0x46:
                    Result[0] = Register.ESI;
                    Result[1] = Register.AL;
                    break;
                case 0x47:
                    Result[0] = Register.EDI;
                    Result[1] = Register.AL;
                    break;
                case 0x43:
                    Result[0] = Register.EBX;
                    Result[1] = Register.AL;
                    break;
                case 0x42:
                    Result[0] = Register.EDX;
                    Result[1] = Register.AL;
                    break;
                case 0x41:
                    Result[0] = Register.ECX;
                    Result[1] = Register.AL;
                    break;
                case 0x40:
                    Result[0] = Register.EAX;
                    Result[1] = Register.AL;
                    break;
                case 0x48:
                    Result[0] = Register.EAX;
                    Result[1] = Register.CL;
                    break;
                case 0x49:
                    Result[0] = Register.ECX;
                    Result[1] = Register.CL;
                    break;
                case 0x4A:
                    Result[0] = Register.EDX;
                    Result[1] = Register.CL;
                    break;
                case 0x4B:
                    Result[0] = Register.EBX;
                    Result[1] = Register.CL;
                    break;
                case 0x4D:
                    Result[0] = Register.EBP;
                    Result[1] = Register.CL;
                    break;
                case 0x4E:
                    Result[0] = Register.ESI;
                    Result[1] = Register.CL;
                    break;
                case 0x4F:
                    Result[0] = Register.EDI;
                    Result[1] = Register.CL;
                    break;
                case 0x50:
                    Result[0] = Register.EAX;
                    Result[1] = Register.DL;
                    break;
                case 0x51:
                    Result[0] = Register.ECX;
                    Result[1] = Register.DL;
                    break;
                case 0x52:
                    Result[0] = Register.EDX;
                    Result[1] = Register.DL;
                    break;
                case 0x53:
                    Result[0] = Register.EBX;
                    Result[1] = Register.DL;
                    break;
                case 0x55:
                    Result[0] = Register.EBP;
                    Result[1] = Register.DL;
                    break;
                case 0x56:
                    Result[0] = Register.ESI;
                    Result[1] = Register.DL;
                    break;
                case 0x57:
                    Result[0] = Register.EDI;
                    Result[1] = Register.DL;
                    break;
                case 0x58:
                    Result[0] = Register.EAX;
                    Result[1] = Register.BL;
                    break;
                case 0x59:
                    Result[0] = Register.ECX;
                    Result[1] = Register.BL;
                    break;
                case 0x5A:
                    Result[0] = Register.EDX;
                    Result[1] = Register.BL;
                    break;
                case 0x5B:
                    Result[0] = Register.EBX;
                    Result[1] = Register.BL;
                    break;
                case 0x5D:
                    Result[0] = Register.EBP;
                    Result[1] = Register.BL;
                    break;
                case 0x5E:
                    Result[0] = Register.ESI;
                    Result[1] = Register.BL;
                    break;
                case 0x5F:
                    Result[0] = Register.EDI;
                    Result[1] = Register.BL;
                    break;
                default:
                    Result[0] = Register.Unk;
                    Result[1] = Register.Unk;
                    break;
            }
            #endregion
            return Result;
        }
        private Registers REGS = new Registers();
        public Key GetKey(int ID) {
            if (ID > founds.Length)
                throw new Exception("Invalid ID");

            byte[] data = new byte[300];
            int pos = founds[ID];
            Memory.Seek(pos, SeekOrigin.Begin);
            Memory.Read(data, 0, data.Length);

            bool OldCode = false;

            int[] KeyBytes = new int[16];
            int[] KeyOrder = new int[16];
            bool[] processed = new bool[16];
            int ind = 0;

            bool IsMain = data[128] >= 0xFD;
            bool Corrupted = false;

            if (isLongMOV(data[0]) || isMOVZX(data[0], data[1], data[2])) {
                int findStart = pos;
                while (true) {//In this while he try find for the real First MOVZX, my "detection" Algoritm can return a wrong position
                    findStart -= 7;
                    Memory.Seek(findStart, SeekOrigin.Begin);
                    byte[] buff = new byte[3];
                    Memory.Read(buff, 0, buff.Length);
                    Memory.Seek(findStart, SeekOrigin.Begin);
                    if (!isMOVZX(buff[0], buff[1], buff[2]))
                        break;
                }
                Memory.Seek(7, SeekOrigin.Current);
                Memory.Read(data, 0, data.Length);
                OldCode = true;
                for (int i = 0; ;) {
                    if (i >= 224 || ind > 0xF) {
                        IsMain = data[i] == MOV_ESI;
                        break;
                    }
                    if (isLongMOV(data[i+1])) {
                        int val = int32(new byte[] { data[i + 3], data[i + 4], data[i + 5], data[i + 6] });
                        Register Register = (Register)Enum.ToObject(typeof(Register), data[i + 1]);
                        KeyOrder[ind] = val;
                        byte[] DW = REGS[Register];
                        KeyBytes[ind] = DW[0];
                        ind++;
                        i += 7;
                    } else if (isMOVZX(data[i], data[i + 1], data[i + 2])) {
                        Register Register = (Register)Enum.ToObject(typeof(Register), data[i + 2]);
                        int off = int32(new byte[] { data[i + 3], data[i + 4], data[i + 5], data[i + 6] });
                        if (off > 0)
                            off -= Base;
                        int b = -1;
                        if (!(off < 0 || off > Memory.Length)) {
                            long back = Memory.Position;
                            Memory.Seek(off, SeekOrigin.Begin);
                            b = Memory.ReadByte();
                            Memory.Seek(back, SeekOrigin.Begin);
                        }
                        REGS[Register] = new byte[] { 0x0, 0x0, 0x0, b < 0 ? (byte)0 : (byte)b};
                        i += 7;
                    } else if (data[i] == MOV_BYTE_PTR){
                        Register[] regs = ParseMOV(data[i + 1]);
                        Register Source = regs[1];
                        //Register Target = regs[0];
                        KeyBytes[ind] = REGS[Source][0];
                        KeyOrder[ind] = int8(data[i + 2]);
                        ind++;
                        i += Length_MOV_BYTE_PTR;
                    } else {
                        IsMain = data[i] == MOV_ESI;
                            
                        break;
                    }
                }
            }
            else
                for (int fnd = 0, Postion = 0; ;) {
                    if (Postion >= 128) {
                        break;
                    }
                    if (fnd == 0) {
                        fnd = 1;
                        KeyBytes[ind] = int32(new byte[] { data[Postion + 1], data[Postion + 2], data[Postion + 3], data[Postion + 4] });
                        Postion += Length_MOV_AL;
                    }
                    else {
                        fnd = 0;
                        if (data[Postion] == MOV_ESI) {
                            Postion += Length_MOV_ESI;
                            fnd = data[Postion] == MOV_AL ? 0 : 1;
                        }
                        KeyOrder[ind] = int8(data[Postion + 2]);
                        ind++;
                        Postion += Length_MOV_BYTE_PTR;
                    }
                }
            int[] Offsets = new int[16];
            for (int i = 0; i < processed.Length; i++) {
                int minval = -1;
                for (ind = 0; ind < KeyOrder.Length; ind++) {
                    if (minval < 0) {
                        minval = 0;
                        while (processed[minval])
                            minval++;
                    }
                    else if (KeyOrder[ind] < KeyOrder[minval] && !processed[ind])
                        minval = ind;
                }
                if (!Corrupted && KeyBytes[minval] < 0 && OldCode)
                    Corrupted = true;
                Offsets[i] = KeyBytes[minval];
                processed[minval] = true;
            }

            byte[] key = new byte[16];
            for (ind = 0; ind < key.Length; ind++) {
                int off = Offsets[ind];
                if (!OldCode) {//My Algoritm to detect the old key algoritm Auto-Read all values...
                    if (off > 0)
                        off -= Base;
                    int b = -1;
                    if (!(off < 0 || off > Memory.Length)) {
                        Memory.Seek(off, SeekOrigin.Begin);
                        b = Memory.ReadByte();
                    }
                    if (!Corrupted && b < 0)
                        Corrupted = true;
                    key[ind] = b < 0 ? (byte)0 : (byte)b;//dont allow invalid bytes
                }
                else
                    key[ind] = (byte)off;
            }
            return new Key() { Bytes = key, Main = IsMain, Corrupted = Corrupted };
        }

        internal static string ParseBytes(byte[] arr) {
            string outstr = "0x";
            foreach (byte b in arr) {
                string hex = b.ToString("x").ToUpper();
                outstr += (hex.Length < 2 ? "0" + hex : hex) + ", 0x";
            }
            outstr = outstr.Substring(0, outstr.Length - ", 0x".Length);
            return outstr;
        }
        public int FindKeys() {
            Memory.Seek(0, SeekOrigin.Begin);
            byte[] Data = new byte[300];
            while (true) {
                if (Memory.Position >= Memory.Length)
                    break;
                //1248855 - hana
                //1247333 - ab
                int Position = (int)Memory.Position;
                Memory.Read(Data, 0, Data.Length);
                for (int i = 0; i < Data.Length; i++)
                    if (Data[i] == MOV_AL) {
                        if (i > 0) {//To don't register a wrong position update the stream to for position
                            Memory.Seek(Position + i, SeekOrigin.Begin);
                            goto resume;
                        }
                        for (int Switch = 0, pos = 0; ;) {
                            if (pos >= 128) {//Algoritm have 128bytes length... if is bigger than and the this for don't break, register as possible key...
                                Save(Position);
                                i += 128;
                                break;
                            }
                            if (Switch == 0) {
                                Switch = 1;
                                pos += Length_MOV_AL;
                                if (!(Data[pos] != MOV_BYTE_PTR || Data[pos] != MOV_ESI) && pos < 128) //If don't is a MOV command and the position is less than 128 break
                                    break;//Only can have another command if the position is bigger than 128 (if end the algoritm)
                            }
                            else {
                                Switch = 0;
                                pos += Data[pos] == MOV_BYTE_PTR ? Length_MOV_BYTE_PTR : Length_MOV_ESI;//jump the correct position
                                if (Data[pos] != MOV_AL && pos < 128)
                                    break;
                            }
                        }
                    }
                    else {
                        if (Data[i] == MOV_BYTE_PTR || Data[i] == 0x0F) {
                            if (i > 0) {//To don't register a wrong position update the stream to for position
                                Memory.Seek(Position + i, SeekOrigin.Begin);
                                goto resume;
                            }
                            for (int MovCnt = 0, pos = 0; ;) {
                                if (pos >= 204 || MovCnt >= 0xF) {//if the Algoritm have more than 204 bytes or have more than 15 MOV's command, register as possible key...
                                    Save(Position);
                                    i += pos;
                                    break;
                                }
                                if (isLongMOV(Data[pos + 1])) { //if is a MOV with 7 bytes length
                                    pos += 7;
                                    MovCnt++;
                                }
                                else if (isMOVZX(Data[pos], Data[pos + 1], Data[pos + 2])) { //if is a MOVZX (7 bytes length)
                                    pos += 7;
                                }
                                else if (Data[pos] == MOV_BYTE_PTR) { //if is a small MOV
                                    pos += Length_MOV_BYTE_PTR;
                                    MovCnt++;
                                }
                                else
                                    break;
                            }
                        }
                    }
                resume:
                ;
            }

            Memory.Seek(0, SeekOrigin.Begin);
            return founds.Length;
        }

        private bool isMOVZX(byte b, byte b2, byte b3) {
            return b == 0x0F && b2 == 0xB6 && (b3 == 0x5 || b3 == 0xD || b3 == 0x15);
        }
        private bool isLongMOV(byte b) {
            int h = b >> 4;
            int l = b & 0xF;
            return ((h >= 8 && h <= 0xB) && (l == 0x4 || l == 0xC));
        }
        private int[] founds = new int[0];
        private void Save(int pos) {
            int[] tmp = new int[founds.Length + 1];
            founds.CopyTo(tmp, 0);
            tmp[founds.Length] = pos;
            founds = tmp;
        }
        private int int8(byte b) {
            return sbyte.Parse(b.ToString("x"), System.Globalization.NumberStyles.HexNumber);
        }
        private int int32(byte[] arr) {
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(arr, 0, arr.Length);
            int p = BitConverter.ToInt32(arr, 0);
            return p;
        }
    }

    public class Key {
        public bool Corrupted { get; internal set; } = false;
        public bool Main { get; internal set; } = false;
        public byte[] Bytes { get; internal set; }
        public string String { get { return KeyFinder.ParseBytes(Bytes); } }
    }


    //My fake 32bits Register Algoritm (work with any name.... but i use original ASM Registers)
    internal class Registers {
        internal byte[] this[string reg] {
            get
            {
                return getData(reg);
            }
            set
            {
                setData(reg, value);
            }
        }
        internal byte[] this[Register reg] {
            get
            {
                return getData(reg.ToString());
            }
            set
            {
                setData(reg.ToString(), value);
            }
        }
        private void setData(string Register, byte[] DW) {
            Register = Register.ToUpper();
            for (int i = 0; i < MEM.Length; i++) {
                string REG = (string)(((object[])MEM[i])[0]);
                byte[] Value = (byte[])((object[])MEM[i])[1];
                if (REG == Register) {
                    ((object[])MEM[i])[1] = DW;
                    return;
                }
            }
            object[] tmp = new object[MEM.Length + 1];
            MEM.CopyTo(tmp, 0);
            tmp[MEM.Length] = new object[] { Register, DW };
            MEM = tmp;
        }
        private byte[] getData(string Register) {
            Register = Register.ToUpper();
            string br = Register;
            for (int i = 0; i < Register.Length; i++) {
                Register = br;
                string REG = (string)(((object[])MEM[i])[0]);
                byte[] Value = (byte[])((object[])MEM[i])[1];
                switch (Register) {
                    case "AL":
                        Register = "EAX";
                        goto case "ReadLow";
                    case "BL":
                        Register = "EBX";
                        goto case "ReadLow";
                    case "CL":
                        Register = "ECX";
                        goto case "ReadLow";
                    case "DL":
                        Register = "EDX";
                        goto case "ReadLow";
                    case "AH":
                        Register = "EAX";
                        goto case "ReadHigh";
                    case "BH":
                        Register = "EBX";
                        goto case "ReadHigh";
                    case "CH":
                        Register = "ECX";
                        goto case "ReadHigh";
                    case "DH":
                        Register = "EDX";
                        goto case "ReadHigh";
                    case "ReadLow":
                        Value = new byte[] { Value[3] };
                        break;
                    case "ReadHigh":
                        Value = new byte[] { Value[2] };
                        break;
                }
                if (REG == Register)
                    return Value;
            }
            return new byte[] { 0x00, 0x00, 0x00, 0x00 };
        }
        private object[] MEM = new object[0];
    }


}
