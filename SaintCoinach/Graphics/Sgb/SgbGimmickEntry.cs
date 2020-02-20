using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SaintCoinach.Graphics.Sgb {

    public class SgbGimmickEntry : ISgbGroupEntry {
        #region Struct
        [StructLayout(LayoutKind.Sequential)]
        public struct HeaderData {
            public SgbGroupEntryType Type;
            public uint GimmickId;
            public int NameOffset;
            public Vector3 Translation;
            public Vector3 Rotation;
            public Vector3 Scale;
            public int GimmickFileOffset;
            public int CollisionFileOffset;
            // SgbGimmickEntry size is around 152 bytes?
        }
        #endregion

        #region Properties
        SgbGroupEntryType ISgbGroupEntry.Type { get { return (SgbGroupEntryType)Header.Type; } }
        public Lgb.LgbGimmickEntry.HeaderData Header { get; private set; }
        public string Name { get; private set; }
        public SgbFile Gimmick { get; private set; }
        public Lgb.LgbGimmickEntry.MovePathSettings MovePathSettings { get; private set; }
        #endregion

        #region Constructor
        public SgbGimmickEntry(IO.PackCollection packs, byte[] buffer, int offset) {
            this.Header = buffer.ToStructure<Lgb.LgbGimmickEntry.HeaderData>(offset);

            this.MovePathSettings = buffer.ToStructure<Lgb.LgbGimmickEntry.MovePathSettings>(offset + Header.MovePathSetting);

            this.Name = buffer.ReadString(offset + Header.NameOffset);

            var sgbFileName = buffer.ReadString(offset + Header.GimmickFileOffset);
            if (!string.IsNullOrWhiteSpace(sgbFileName)) {
                SaintCoinach.IO.File file;
                if (packs.TryGetFile(sgbFileName, out file))
                    this.Gimmick = new SgbFile(file);
            }
        }
        #endregion
    }
}
