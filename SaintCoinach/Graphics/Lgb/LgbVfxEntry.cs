using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SaintCoinach.Graphics.Lgb {
    [StructLayout(LayoutKind.Sequential)]
    public struct Color {
        public byte Red;
        public byte Green;
        public byte Blue;
        public byte Alpha;
    }
    public class LgbVfxEntry : ILgbEntry {
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
            public float SoftParticleFadeRange;
            public uint Reserved2;
            public Color Color;
            public byte AutoPlay;
            public byte NoFarClip;
            public ushort Padding;
            public float NearFadeStart;
            public float NearFadeEnd;
            public float FarFadeStart;
            public float FarFadeEnd;
            public float ZCorrect;
            // 24 bytes of unknowns
        }
        #endregion

        #region Properties
        public LgbEntryType Type => Header.Type;
        public HeaderData Header { get; private set; }
        public string Name { get; private set; }
        public string FilePath { get; private set; }
        #endregion

        #region Constructor
        public LgbVfxEntry(IO.PackCollection packs, byte[] buffer, int offset) {
            this.Header = buffer.ToStructure<HeaderData>(offset);
            this.Name = buffer.ReadString(offset + Header.NameOffset);
            this.FilePath = buffer.ReadString(offset + Header.ResourceOffset);
        }
        #endregion

    }
}
