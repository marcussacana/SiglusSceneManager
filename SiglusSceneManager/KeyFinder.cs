using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
        const int L_MOV_AL = 0x5;

        const byte MOV_BYTE_PTR = 0x88;
        const int L_MOV_BYTE_PTR = 0x3;

        const byte MOV_ESI = 0x8B;
        const int L_MOV_ESI = 0x6;

        //A0 ? ? ? ?
        //MOV AL, OFF;

        //A0 ? ? ? ? 8B ? ? ? ? ?
        //MOV AL, OFF, 8B Order
        //-0x400000

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
        private string ParseRegister(byte b) {
            switch (b) {
                //MOVZX - Table
                case 0x5:
                    return "EAX";
                case 0xD:
                    return "ECX";
                case 0x15:
                    return "EDX";
                case 0x1D:
                    return "EBX";
                case 0x25:
                    return "ESP";
                case 0x2D:
                    return "EBP";
                case 0x35:
                    return "ESI";
                case 0x3D:
                    return "EDI";
                //MOV Long Table
                case 0x84:
                    return "AL";
                case 0x9C:
                    return "BL";
                case 0x8C:
                    return "CL";
                case 0x94:
                    return "DL";
                default:
                    return "Other";
            }
        }

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

        private string ParseMOV(byte b) {
            switch (b) {
                //MOV Shor Table
                case 0x45:
                    return "EBP,AL";
                case 0x46:
                    return "ESI,AL";
                case 0x47:
                    return "EDI,AL";
                case 0x43:
                    return "EBX,AL";
                case 0x42:
                    return "EDX,AL";
                case 0x41:
                    return "ECX,AL";
                case 0x40:
                    return "EAX,AL";
                case 0x48:
                    return "EAX,CL";
                case 0x49:
                    return "ECX,CL";
                case 0x4A:
                    return "EDX,CL";
                case 0x4B:
                    return "EBX,CL";
                case 0x4D:
                    return "EBP,CL";
                case 0x4E:
                    return "ESI,CL";
                case 0x4F:
                    return "EDI,CL";
                case 0x50:
                    return "EAX,DL";
                case 0x51:
                    return "ECX,DL";
                case 0x52:
                    return "EDX,DL";
                case 0x53:
                    return "EBX,DL";
                case 0x55:
                    return "EBP,DL";
                case 0x56:
                    return "ESI,DL";
                case 0x57:
                    return "EDI,DL";
                case 0x58:
                    return "EAX,BL";
                case 0x59:
                    return "ECX,BL";
                case 0x5A:
                    return "EDX,BL";
                case 0x5B:
                    return "EBX,BL";
                case 0x5D:
                    return "EBP,BL";
                case 0x5E:
                    return "ESI,BL";
                case 0x5F:
                    return "EDI,BL";
                default:
                    return "Unk,Unk";
            }
        }
        private Registers REGS = new Registers();
        public Key GetKey(int ID) {
            if (ID > founds.Length)
                throw new Exception("Invalid ID");
            byte[] Alg = new byte[300];
            int pos = founds[ID];
            Memory.Seek(pos, SeekOrigin.Begin);
            Memory.Read(Alg, 0, Alg.Length);
            bool OldCode = false;
            int[] ALVAL = new int[16];
            int[] EBPVAL = new int[16];
            bool[] processed = new bool[16];
            int ind = 0;
            if (isLongMOV(Alg[0]) || isMOVZX(Alg[0], Alg[1], Alg[2])) {
                int findStart = pos;
                while (true) {
                    findStart -= 7;
                    Memory.Seek(findStart, SeekOrigin.Begin);
                    byte[] buff = new byte[3];
                    Memory.Read(buff, 0, buff.Length);
                    Memory.Seek(findStart, SeekOrigin.Begin);
                    if (!isMOVZX(buff[0], buff[1], buff[2]))
                        break;
                }
                Memory.Seek(7, SeekOrigin.Current);
                Memory.Read(Alg, 0, Alg.Length);
                OldCode = true;
                for (int i = 0; ;) {
                    if (i >= 224 || ind > 0xF)
                        break;
                    if (isLongMOV(Alg[i+1])) {
                        int val = int32(new byte[] { Alg[i + 3], Alg[i + 4], Alg[i + 5], Alg[i + 6] });
                        string Register = ParseRegister(Alg[i + 1]);
                        EBPVAL[ind] = val;
                        byte[] DW = REGS[Register];
                        ALVAL[ind] = DW[0];
                        ind++;
                        i += 7;
                    } else if (isMOVZX(Alg[i], Alg[i + 1], Alg[i + 2])) {
                        string Register = ParseRegister(Alg[i + 2]);
                        int off = int32(new byte[] { Alg[i + 3], Alg[i + 4], Alg[i + 5], Alg[i + 6] });
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
                    } else if (Alg[i] == MOV_BYTE_PTR){
                        string[] reg = ParseMOV(Alg[i + 1]).Split(',');
                        string Source = reg[1];
                        //string Target = reg[0];
                        ALVAL[ind] = REGS[Source][0];
                        EBPVAL[ind] = int8(Alg[i + 2]);
                        ind++;
                        i += L_MOV_BYTE_PTR;
                    } else {
                        break;
                    }
                }
            }
            else
                for (int fnd = 0, pt = 0; ;) {
                    if (pt >= 128) {
                        break;
                    }
                    if (fnd == 0) {
                        fnd = 1;
                        ALVAL[ind] = int32(new byte[] { Alg[pt + 1], Alg[pt + 2], Alg[pt + 3], Alg[pt + 4] });
                        pt += L_MOV_AL;
                    }
                    else {
                        fnd = 0;
                        if (Alg[pt] == MOV_ESI) {
                            pt += L_MOV_ESI;
                            fnd = Alg[pt] == MOV_AL ? 0 : 1;
                        }
                        EBPVAL[ind] = int8(Alg[pt + 2]);
                        ind++;
                        pt += L_MOV_BYTE_PTR;
                    }
                }
            int[] Offsets = new int[16];
            for (int i = 0; i < processed.Length; i++) {
                int minval = -1;
                for (ind = 0; ind < EBPVAL.Length; ind++) {
                    if (minval < 0) {
                        minval = 0;
                        while (processed[minval])
                            minval++;
                    }
                    else if (EBPVAL[ind] < EBPVAL[minval] && !processed[ind])
                        minval = ind;
                }
                Offsets[i] = ALVAL[minval];
                processed[minval] = true;
            }

            byte[] key = new byte[16];
            for (ind = 0; ind < key.Length; ind++) {
                int off = Offsets[ind];
                if (!OldCode) {
                    if (off > 0)
                        off -= Base;
                    int b = -1;
                    if (!(off < 0 || off > Memory.Length)) {
                        Memory.Seek(off, SeekOrigin.Begin);
                        b = Memory.ReadByte();
                    }
                    key[ind] = b < 0 ? (byte)0 : (byte)b;
                }
                else
                    key[ind] = (byte)off;
            }
            return new Key(key, Alg[128] >= 0xFD);
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
            byte[] buff = new byte[300];
            while (true) {
                if (Memory.Position >= Memory.Length)
                    break;
                //1248855 - hana
                //1247333 - ab
                int pointer = (int)Memory.Position;
                Memory.Read(buff, 0, buff.Length);
                for (int i = 0; i < buff.Length; i++)
                    if (buff[i] == MOV_AL) {
                        if (i > 0) {
                            Memory.Seek(pointer + i, SeekOrigin.Begin);
                            goto resume;
                        }
                        for (int fnd = 0, pt = 0; ;) {
                            if (i + pt >= 128) {
                                Register((int)pointer + i);
                                i += 128;
                                break;
                            }
                            if (fnd == 0) {
                                fnd = 1;
                                pt += L_MOV_AL;
                                if (!(buff[i + pt] != MOV_BYTE_PTR || buff[i + pt] != MOV_ESI) && i + pt < 128)
                                    break;
                            }
                            else {
                                fnd = 0;
                                pt += buff[i + pt] == MOV_BYTE_PTR ? L_MOV_BYTE_PTR : L_MOV_ESI;
                                if (buff[i + pt] != MOV_AL && i + pt < 128)
                                    break;
                            }
                        }
                    }
                    else {
                        if (buff[i] == MOV_BYTE_PTR || buff[i] == 0x0F) {
                            if (i > 0) {
                                Memory.Seek(pointer + i, SeekOrigin.Begin);
                                goto resume;
                            }
                            for (int fnd = 0, pt = 0; ;) {
                                if (pt >= 204 || fnd >= 0xF) {
                                    Register(pointer + i);
                                    i += (int)pt;
                                    break;
                                }
                                if (isLongMOV(buff[i + pt + 1])) {
                                    pt += 7;
                                    fnd++;
                                }
                                else if (isMOVZX(buff[i + pt], buff[i + pt + 1], buff[i + pt + 2])) {
                                    pt += 7;
                                }
                                else if (buff[i + pt] == MOV_BYTE_PTR) {
                                    pt += L_MOV_BYTE_PTR;
                                    fnd++;
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
        private void Register(int pos) {
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
        internal Key(byte[] key, bool MainKey) {
            Bytes = key;
            Main = MainKey;
        }

        public bool Main = false;
        public byte[] Bytes { get; private set; }
        public string String { get { return KeyFinder.ParseBytes(Bytes); } }
    }

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
