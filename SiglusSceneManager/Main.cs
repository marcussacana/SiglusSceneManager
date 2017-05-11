using System;
using System.IO;
using System.Text;

namespace SiglusSceneManager {
    public class SSManager {
        byte[] Scene;
        private uint FirstPos;
        public SSManager(byte[] Script) {
            Scene = Script;
            FirstPos = (uint)Scene.Length;
        }
        public string[] Import() {
            Entry OffsetTable = GetEntry(0xC);
            Entry StringTable = GetEntry(0x14);
            string[] Content = new string[StringTable.Length];
            bool SaveInfo = HaveSignature;
            for (uint i = 0; i < Content.Length; i++) {
                uint Off = OffsetTable.Position + (8 * i); //A entry have 8 bytes length, 8 * i to jump the specified entry
                Entry StrEntry = GetEntry(Off);
                uint pos = (StrEntry.Position * 2) + StringTable.Position; //*2 Because is UFT-16
                if (pos < FirstPos)
                    FirstPos = pos;
                byte[] Str = GetRegion(pos, StrEntry.Length * 2); //*2 Because is UFT-16
                Content[i] = Encoding.Unicode.GetString(XOR(Str, i));
            }
            return Content;
        }

        private byte[] Signature = new byte[] { 0x00, 0x53, 0x69, 0x67, 0x6C, 0x75, 0x73, 0x53, 0x63, 0x72, 0x69, 0x70, 0x74, 0x4D, 0x61, 0x6E, 0x61, 0x67, 0x65, 0x72 };

        public byte[] Export(string[] Strings) {
            byte[] Base = HaveSignature ? RemoveTable(Scene) : Scene;
            Entry OffsetTable = GetEntry(0xC);
            GetDWord((uint)Base.Length).CopyTo(Base, 0x14);
            Stream StrTable = new MemoryStream();
            for (uint i = 0u; i < OffsetTable.Length; i++) {
                uint offpos = (i * 8) + OffsetTable.Position;
                uint lenpos = offpos + 4;
                byte[] Content = Encoding.Unicode.GetBytes(Strings[i]);
                GetDWord((uint)StrTable.Length / 2).CopyTo(Base, offpos);
                GetDWord((uint)Content.Length / 2).CopyTo(Base, lenpos);
                StrTable.Write(XOR(Content, i), 0, Content.Length);
            }
            StrTable.Seek(0, SeekOrigin.Begin);
            byte[] outscript = new byte[Base.Length + StrTable.Length + Signature.Length];
            Base.CopyTo(outscript, 0);
            byte[] strtable = new byte[StrTable.Length];
            StrTable.Read(strtable, 0, strtable.Length);
            strtable.CopyTo(outscript, Base.Length);
            Signature.CopyTo(outscript, Base.Length + strtable.Length);
            return outscript;
        }

        private byte[] GetDWord(uint val) {
            byte[] arr = BitConverter.GetBytes(val);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(arr);
            return arr;
        }
        private byte[] RemoveTable(byte[] scene) {
            byte[] rst = new byte[FirstPos];
            for (uint i = 0u; i < rst.Length; i++)
                rst[i] = scene[i];
            return rst;
        }

        private bool HaveSignature { get {
                int pos = Scene.Length - Signature.Length;
                if (pos < 0)
                    return false;
                for (uint i = 0u; i < Signature.Length; i++)
                    if (Scene[pos + i] != Signature[i])
                        return false;
                return true;
            } }

        private byte[] XOR(byte[] Content, uint Key) {
            Key *= 0x7087;
            byte[] Rst = new byte[Content.Length];
            for (uint i = 0; i < Content.Length - 1; i += 2) {
                byte v2 = (byte)((Key >> 8) & 0xFF);
                byte v1 = (byte)(Key & 0xFF);
                Rst[i] = (byte)(Content[i] ^ v1);
                Rst[i + 1] = (byte)(Content[i + 1] ^ v2);
            }
            return Rst;
        }
        private byte[] GetRegion(uint Pos, uint Length) {
            byte[] Reg = new byte[Length];
            for (int i = 0; i < Length; i++)
                Reg[i] = Scene[Pos + i];
            return Reg;
        }
        private Entry GetEntry(uint Pos) {
            return new Entry {
                Position = GetUint(Pos),
                Length = GetUint(Pos + 4)
            };
        }
        
        private uint GetUint(uint Pos) {
            byte[] DW = new byte[] { Scene[Pos], Scene[Pos + 1u], Scene[Pos + 2u], Scene[Pos + 3u] };
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(DW, 0, DW.Length);
            return BitConverter.ToUInt32(DW, 0);
        }
    }

    internal struct Entry {
        internal uint Position;
        internal uint Length;
    }
}
