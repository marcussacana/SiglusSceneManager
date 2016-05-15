using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SiglusSceneManager {
    public class KeyFinder {
        private Stream Memory;
        private uint Base;
        public KeyFinder(string ProcessDump, uint DB) {
            Memory = new FileStream(ProcessDump, FileMode.Open, FileAccess.Read, FileShare.Read, 255);
            Base = DB;
        }
        public KeyFinder(Stream ProcessDump, uint DB) {
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

        //A0 ? ? ? ? 88 ? ?
        //MOV AL, OFF;

        //A0 ? ? ? ? 8B ? ? ? ? ?
        //MOV AL, OFF, 8B Order
        //-0x400000
        
        public Key GetKey(int ID) {
            if (ID > founds.Length)
                throw new Exception("Invalid ID");
            byte[] Alg = new byte[200];
            uint pos = founds[ID];
            Memory.Seek(pos, SeekOrigin.Begin);
            Memory.Read(Alg, 0, Alg.Length);
            uint[] ALVAL = new uint[16];
            sbyte[] EBPVAL = new sbyte[16];
            bool[] processed = new bool[16];
            int ind = 0;
            for (int fnd = 0, pt = 0; ;) {
                if (pt >= 128) {
                    break;
                }
                if (fnd == 0) {
                    fnd = 1;
                    ALVAL[ind] = uint32(new byte[] { Alg[pt + 1], Alg[pt + 2], Alg[pt + 3], Alg[pt + 4] });
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
            uint[] Offsets = new uint[16];
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
                Memory.Seek(Offsets[ind] - Base, SeekOrigin.Begin);
                int b = Memory.ReadByte();
                key[ind] = b < 0 ? (byte)0 : (byte)b;
            }
            return new Key(key, Alg[128] >= 0xFD);
        }

        internal static string ParseBytes(byte[] arr) {
            string outstr = "0x";
            foreach (byte b in arr) {
                string hex = b.ToString("x").ToUpper() ;
                outstr += (hex.Length < 2 ? "0" + hex : hex) + ", 0x";
            }
            outstr = outstr.Substring(0, outstr.Length - ", 0x".Length);
            return outstr;
        }
        public int FindKeys() {
            Memory.Seek(0, SeekOrigin.Begin);
            byte[] buff = new byte[200];
            while (true) {
                if (Memory.Position >= Memory.Length)
                    break;
                //1248855 - hana
                //1247333 - ab
                uint pointer = (uint)Memory.Position;
                Memory.Read(buff, 0, buff.Length);
                for (uint i = 0; i < buff.Length; i++)
                    if (buff[i] == MOV_AL) {
                        if (i > 0) {
                            Memory.Seek(pointer + i, SeekOrigin.Begin);
                            goto resume;
                        }
                        for (int fnd = 0, pt = 0; ;) {
                            if (i + pt >= 128) {
                                Register((uint)pointer + i);
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
                resume:
                ;
            }
            Memory.Seek(0, SeekOrigin.Begin);
            return founds.Length;
        }
        private uint[] founds = new uint[0];
        private void Register(uint pos) {
            uint[] tmp = new uint[founds.Length + 1];
            founds.CopyTo(tmp, 0);
            tmp[founds.Length] = pos;
            founds = tmp;
        }
        private sbyte int8(byte b) {
            return sbyte.Parse(b.ToString("x"), System.Globalization.NumberStyles.HexNumber);
        }
        private uint uint32(byte[] arr) {
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(arr, 0, arr.Length);
            uint p = BitConverter.ToUInt32(arr, 0);
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

}
