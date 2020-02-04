using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SaintCoinach.Graphics.Sgb {
    public class SgbSoundEntry : ISgbGroupEntry {
        #region Struct
        [StructLayout(LayoutKind.Sequential)]
        public struct HeaderData {
            public Lgb.LgbEntryType Type;
            public uint UnknownId;
            public int NameOffset;
            public Vector3 Translation;
            public Vector3 Rotation;
            public Vector3 Scale;
            public int ResourceOffset;
            public int AssetOffset;
            // 24 bytes of unknowns
        }
        #endregion

        #region Properties
        public SgbGroupEntryType Type => SgbGroupEntryType.Sound;
        public HeaderData Header { get; private set; }
        public string Name { get; private set; }
        public string ShcdFilePath { get; private set; }
        #endregion

        #region Constructor
        public SgbSoundEntry(IO.PackCollection packs, byte[] buffer, int offset) {
            this.Header = buffer.ToStructure<HeaderData>(offset);
            this.Name = buffer.ReadString(offset + Header.NameOffset);
            this.ShcdFilePath = buffer.ReadString(offset + Header.AssetOffset);
            int x = 0;
        }
        #endregion

    }
}
