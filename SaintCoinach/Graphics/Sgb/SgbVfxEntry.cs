using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SaintCoinach.Graphics.Sgb {
    public class SgbVfxEntry : ISgbGroupEntry {
        #region Struct
        [StructLayout(LayoutKind.Sequential)]
        public struct HeaderData {
            public SgbGroupEntryType Type;
            public uint UnknownId;
            public int NameOffset;
            public Vector3 Translation;
            public Vector3 Rotation;
            public Vector3 Scale;
            public int ResourceOffset;
            public float SoftParticleFadeRange;
            public uint Reserved2;
            public Lgb.Color Color;
            public byte AutoPlay;
            public byte NoFarClip;
            public ushort Padding;
            public float NearFadeStart;
            public float NearFadeEnd;
            public float FarFadeStart;
            public float FarFadeEnd;
            public float ZCorrect;

            //            public int AvfxFileOffset;
            // unknowns
        }
        #endregion

        #region Properties
        public SgbGroupEntryType Type => Header.Type;
        public HeaderData Header { get; private set; }
        public string Name { get; private set; }
        public string FilePath { get; private set; }
        public Avfx.AvfxFile AvfxFile { get; private set; }
        #endregion

        #region Constructor
        public SgbVfxEntry(IO.PackCollection packs, byte[] buffer, int offset) {
            this.Header = buffer.ToStructure<HeaderData>(offset);
            this.Name = buffer.ReadString(offset + Header.NameOffset);
            this.FilePath = buffer.ReadString(offset + Header.ResourceOffset);

            if (!string.IsNullOrEmpty(FilePath))
                this.AvfxFile = new Avfx.AvfxFile(packs.GetFile(FilePath));
        }
        #endregion

    }
}
