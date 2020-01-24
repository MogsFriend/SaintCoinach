using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SaintCoinach.Graphics.Lgb {
    public class LgbEnvLocationEntry : ILgbEntry {
        #region Struct
        [StructLayout(LayoutKind.Sequential)]
        public struct HeaderData {
            public LgbEntryType Type;
            public uint UnknownId;
            public int NameOffset;
            public Vector3 Translation;
            public Vector3 Rotation;
            public Vector3 Scale;
            public int ResourceOffset;
            public int MapAssetOffset;
            // 24 bytes of unknowns
        }
        #endregion

        #region Properties
        public LgbEntryType Type => Header.Type;
        public HeaderData Header { get; private set; }
        public string Name { get; private set; }
        public string AmbientLightPath { get; private set; }
        public string MapAssetOffset { get; private set; }
        #endregion

        #region Constructor
        public LgbEnvLocationEntry(IO.PackCollection packs, byte[] buffer, int offset) {
            this.Header = buffer.ToStructure<HeaderData>(offset);
            this.Name = buffer.ReadString(offset + Header.NameOffset);
            this.AmbientLightPath = buffer.ReadString(offset + Header.ResourceOffset);
            this.MapAssetOffset = buffer.ReadString(offset + Header.MapAssetOffset);

            int x = 0;
        }
        #endregion

    }
}
