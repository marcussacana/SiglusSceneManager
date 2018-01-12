//Compression Level:
//Valid Values: MAX/COMP1/COMP2/COMP3/COMP4/COMP5/COMP6/COMP7/COMP8
//MAX == SLOWEST/BEST
//COMP8 == FAST/TRASH
//Remove the Define to disable the compression
#define MAX

using AdvancedBinary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SiglusSceneManager {
    public class DBS {
        List<bool> DataFormat = new List<bool>();
        bool Unicode = false;
        byte[] Script;

        public DBS(byte[] Script) {
            XOR(ref Script, Begin: 1);
            Script = Decompress(Script);
            this.Script = Decrypt(Script);
            DiscoveryEncoding();
        }

        #region Obfuscation
        private void XOR(ref byte[] Data, uint Key = 0x89f4622d, int Begin = 0, int Len = -1) {
            if (Len < 0)
                Len = Data.Length / 4;

            for (int i = Begin; i < Len; i++) {
                uint DW = BitConverter.ToUInt32(Data, i * 4) ^ Key;
                BitConverter.GetBytes(DW).CopyTo(Data, i * 4);
            }
        }

        byte[] DWTBL = new byte[1024];
        byte[] XORTBL = new byte[] { 0xFF, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0xFF, 0xFF, 0xFF, 0x00, 0xFF, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF };
        const uint K1 = 0x7190c70e, K2 = 0x499bf135;

        private byte[] Encrypt(byte[] Data) => Decrypt(Data);

        private byte[] Decrypt(byte[] Data) {
            byte[] Out = new byte[Data.Length];
            Data.CopyTo(Out, 0);

            uint Key = K1, SwitchTable = 0;

            XOR(ref Out,K2);

            uint EAX, EBX;
            uint BuffPtr = 0;            

            for (uint i = 0; i < Data.LongLength / 64; i++) {
                for (uint y = 0, Ind = 0; y < 0x10; y++, BuffPtr += 4, Ind++) {
                    if (Ind >= 5)
                        Ind = 0;

                    EBX = (uint)XORTBL[SwitchTable + Ind] << 2;

                    if (EBX != 0x00) {
                        SelectDWTable(ref EBX);
                        EBX = BitConverter.ToUInt32(DWTBL, (int)EBX + 0x3FC) & 0xFF;

                        if (EBX != 0x00) {
                            if (EBX != 0xFF) {
                                System.Diagnostics.Debugger.Log(1, "Warning", "Untested Algorithm, Please Report a issue");

                                EBX <<= 2;
                                SelectDWTable(ref EBX);
                                uint DW = BitConverter.ToUInt32(Data, (int)EBX);

                                for (int x = 0; x < 3; x++) {
                                    EBX = (Data[BuffPtr + x]);
                                    EAX = (Out[BuffPtr + x]);
                                    EBX = ((EBX - EAX) << 2) + DW;
                                    EAX += BitConverter.ToUInt32(Data, (int)EBX);
                                    Out[BuffPtr + x] = (byte)(EAX & 0xFF);
                                }

                            } else {
                                EAX = BitConverter.ToUInt32(Data, (int)BuffPtr) ^ Key;
                                BitConverter.GetBytes(EAX).CopyTo(Out, (int)BuffPtr);
                            }
                        }
                    }                 
                }

                SwitchTable += 5;
                if (SwitchTable >= XORTBL.Length)
                    SwitchTable = 0;
            }

            return Out;
        }

        private int GetSigned(uint Val) {
            return BitConverter.ToInt32(BitConverter.GetBytes(Val), 0);
        }
        private void SelectDWTable(ref uint Seed) {
            Seed /= 4;

            DWTBL = new byte[DWTBL.Length];
            for (uint i = ((uint)DWTBL.LongLength/4) - 1; i > 0; i--) {
                BitConverter.GetBytes(Seed).CopyTo(DWTBL, i * 4);
                if (Seed > 0)
                    Seed--;
            }
        }
        #endregion

        #region Compression
        private byte[] Decompress(byte[] Data) {
            uint DecLen = BitConverter.ToUInt32(Data, 0x8);
            byte[] Buffer = new byte[DecLen];
            if (Buffer.Length == 0) {
                byte[] Dummy = new byte[Data.Length - 0xC];
                for (int i = 0; i < Data.Length - 0xC; i++)
                    Dummy[i] = Data[i + 0xC];
                return Dummy;
            }

            byte Flags = 0;
            byte BitCnt = 0;
            int BuffPtr = 0;
            for (int i = 0xC; i < Data.Length; i++) {
                if (BitCnt == 0x00) {
                    Flags = Data[i];
                    BitCnt = 8;
                    continue;
                }

                if ((Flags & 1) != 0) {
                    Buffer[BuffPtr++] = Data[i];
                } else {
                    uint Reptions = Data[i++];
                    uint Offset = (uint)(Data[i] << 4) | (Reptions >> 4);
                    Reptions = (Reptions & 0xF) + 2;

                    for (int x = 0; x < Reptions; x++, BuffPtr++) {
                        Buffer[BuffPtr] = Buffer[BuffPtr - Offset];
                    }
                }

                BitCnt--;
                Flags >>= 1;
            }

            return Buffer;
        }

        private void FakeCompress(ref byte[] Data) {
            CompressionHeader Header = new CompressionHeader() {
                Magic = 1,
                dLen = (uint)Data.LongLength
            };

            List<byte> Buffer = new List<byte>();

            for (uint i = 0, x = 0; i < Data.Length; i++, x--) {
                if (x == 0) {
                    x = 8;
                    Buffer.Add(0xFF);
                }
                Buffer.Add(Data[i]);
            }

            Header.cLen = (uint)Buffer.LongCount() + 0x8;

            Buffer.InsertRange(0, Tools.BuildStruct(ref Header));

            Data = null;
            Data = Buffer.ToArray();
        }

#if MAX
        const byte MinCompressMatch = 2;
#elif COMP1
        const byte MinCompressMatch = 3;
#elif COMP2
        const byte MinCompressMatch = 4;
#elif COMP3
        const byte MinCompressMatch = 5;
#elif COMP4
        const byte MinCompressMatch = 6;
#elif COMP5
        const byte MinCompressMatch = 7;
#elif COMP6
        const byte MinCompressMatch = 8;
#elif COMP7
        const byte MinCompressMatch = 9;
#elif COMP8
        const byte MinCompressMatch = 10;
#else
        const byte MinCompressMatch = 0xFF;
#endif
        private void Compress(ref byte[] Data) {
            CompressionHeader Header = new CompressionHeader() {
                Magic = 1,
                dLen = (uint)Data.LongLength
            };
            List<byte> Output = new List<byte>();
            List<byte> Buffer = new List<byte>();
            byte f = 0;
            
            for (uint i = 0, x = 8; i < Data.LongLength; i++) {
                for (byte Cnt = 0x11; Cnt > MinCompressMatch && i + Cnt < Data.LongLength; Cnt--) {
                    byte[] Tmp = GetRange(Data, i, Cnt);
                    for (ushort r = Cnt; r < 0xFFF && r <= i; r++) {
                        if (Data[i - r] == Tmp[0] && EqualsAt(Data, Tmp, i - r)) {
                            Buffer.Add((byte)(((r & 0xF) << 4) | (Cnt - 2)));
                            Buffer.Add((byte)(r >> 4));
                            i += (uint)Cnt - 1;
                            goto Cont;
                        }
                    }
                }

                f |= (byte)(1 << (int)(8 - x));
                Buffer.Add(Data[i]);

                Cont:;
                x--;
                if (x == 0) {
                    Output.Add(f);
                    Output.AddRange(Buffer);

                    Buffer = new List<byte>();
                    x = 8;
                    f = 0;
                }
            }

            if (Buffer.Count != 0) {
                Output.Add(f);
                Output.AddRange(Buffer);
            }

            Header.cLen = (uint)Output.LongCount() + 0x8;

            Output.InsertRange(0, Tools.BuildStruct(ref Header));

            Data = null;
            Data = Output.ToArray();
        }

        private byte[] GetRange(byte[] Data, uint Start, uint Length) {
            byte[] Result = new byte[Length];
            for (uint i = 0; i < Length; i++) {
                Result[i] = Data[Start + i];
            }

            return Result;
        }
        private bool EqualsAt(byte[] Data, byte[] Content, uint At) {
            if (At + Content.Length >= Data.Length)
                return false;

            for (uint i = 0; i < Content.Length; i++) {
                if (Data[i + At] != Content[i])
                    return false;
            }
            return true;
        }

        #endregion

        public string[] Import() {
            DataFormat = new List<bool>();
            int DFID = 0;

            Database Header = new Database();
            Tools.ReadStruct(Script, ref Header);
            List<string> Strings = new List<string>();
            
            
            using (Stream tmp = new MemoryStream(Script))
            using (StructReader Reader = new StructReader(tmp, false, Encoding: GetEncoding())) {

                //List All Data Types
                Reader.Seek(Header.ValueTypeList, SeekOrigin.Begin);
                while (Reader.BaseStream.Position < Header.ValueList) {
                    TypeHeader THeader = new TypeHeader();
                    Reader.ReadStruct(ref THeader);
                    DataFormat.Add(THeader.IsString);
                }

                //Foreach Inside all Data Values
                Reader.Seek(Header.ValueList, SeekOrigin.Begin);
                while (Reader.BaseStream.Position < Header.StringTable) {
                    uint Value = Reader.ReadUInt32();

                    //Check if is a string
                    if (DataFormat[DFID]) {
                        uint StrPos = Value + Header.StringTable;
                        long Pos = Reader.BaseStream.Position;

                        Reader.Seek(StrPos, SeekOrigin.Begin);
                        Strings.Add(Reader.ReadString(GetStrEnd()));

                        Reader.Seek(Pos, SeekOrigin.Begin);
                    }

                    //Adv Type Pointer
                    DFID++;
                    if (DFID >= DataFormat.Count)
                        DFID = 0;
                }
            }

            return Strings.ToArray();
        }
        public byte[] Export(string[] Strings) {
            uint[] Offsets = new uint[Strings.LongLength];
            Database Header = new Database();
            Tools.ReadStruct(Script, ref Header);
            byte[] Data;
            using (MemoryStream Output = new MemoryStream()) {
                Output.Write(Script, 0, (int)Header.StringTable);
                using (StructWriter Writer = new StructWriter(Output, Encoding: GetEncoding())) {
                    //Write Strings and Generate Offset Table
                    for (uint i = 0; i < Strings.LongLength; i++) {
                        Offsets[i] = (uint)(Writer.BaseStream.Position) - Header.StringTable;
                        Writer.Write(Strings[i], GetStrEnd());
                        Writer.Flush();
                    }
                    
                    //Update StrEnd Entry of the Header
                    uint StrEnd = 0;
                    while ((StrEnd = (uint)Output.Position) % 4 != Header.StringEnd % 4)
                        Output.WriteByte(0x00);


                    //Copy Content After the String Table
                    Output.Write(Script, (int)Header.StringEnd, (int)(Script.LongLength - Header.StringEnd));

                    Header.StringEnd = StrEnd;
                    Data = Output.ToArray();

                    //Update Offsets
                    for (uint i = Header.ValueList, OID = 0, DFID = 0; i < Header.StringTable; i += 4) { 
                        if (DataFormat[(int)DFID]) {
                            BitConverter.GetBytes(Offsets[OID++]).CopyTo(Data, i);
                        }

                        DFID++;
                        if (DFID >= DataFormat.Count)
                            DFID = 0;
                    }
                }
            }

            Tools.BuildStruct(ref Header).CopyTo(Data, 0);
            
            Data = Encrypt(Data);
#if !MAX && !COMP1 && !COMP2 && !COMP3 && !COMP4 && !COMP5 && !COMP6 && !COMP7 && !COMP8
            FakeCompress(ref Data);
#else
            Compress(ref Data);
#endif

            XOR(ref Data, Begin: 1);

            return Data;
        }    
       
        /// <summary>
        /// Try discovery the DB Encoding by the string sufix (unicode ends with 0x0000 and utf8 0x00)
        /// </summary>
        private void DiscoveryEncoding() {
            Database Header = new Database();
            Tools.ReadStruct(Script, ref Header);

            for (uint i = Header.StringTable; i < Header.StringEnd - 1; ) {
                if (Script[i] == 0x00 && i == Header.StringTable) {
                    while (Script[i++] == 0x00 && i < Header.StringEnd - 1)
                        continue;
                }
                if (Script[i++] == 0x00) {
                    if (Script[i++] == 0x00 && i < Header.StringEnd - 1) {
                        Unicode = true;
                        return;
                    }
                }
            }

            Unicode = false;
        }

        private Encoding GetEncoding() {
            if (Unicode)
                return Encoding.Unicode;
            return Encoding.UTF8;
        }
        private StringStyle GetStrEnd() {
            if (Unicode)
                return StringStyle.UCString;
            return StringStyle.CString;
        }


    }

#pragma warning disable 649
    internal struct Database {
        public uint StringEnd;//Columns Checksum Offset?
        public uint Unk1;
        public uint Unk2;//Flags?
        public uint Unk3;

        public uint ValueTypeList;
        public uint ValueList;
        public uint StringTable;
    }

    internal struct CompressionHeader {
        public uint Magic;
        public uint cLen;
        public uint dLen;
    }

    internal struct TypeHeader {
        public uint ID;
        public uint Type;

        [Ignore]
        public bool IsString { get {
                return Type == 0x53;
            }
        }
    }
#pragma warning restore 169
}
