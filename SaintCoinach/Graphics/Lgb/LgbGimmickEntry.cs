using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SaintCoinach.Graphics.Lgb {
    public class LgbGimmickEntry : ILgbEntry {
        public enum eRotationTypeLayer {
            NoRotate = 0x0,
            AllAxis = 0x1,
            YAxisOnly = 0x2,
        };

        public enum eRotationStateLayer {
            Rounding = 0x1,
            Stopped = 0x2,
        };

        public enum eMovePathModeLayer {
            None_4 = 0x0,
            SGAction = 0x1,
            Timeline_1 = 0x2,
        };

        public enum eDoorStateLayer {
            Auto_0 = 0x1,
            Open = 0x2,
            Closed = 0x3,
        };

        public enum eTransformStateLayer {
            TransformStatePlay = 0x0,
            TransformStateStop = 0x1,
            TransformStateReplay = 0x2,
            TransformStateReset = 0x3,
        };

        public enum eColorStateLayer {
            ColorStatePlay = 0x0,
            ColorStateStop = 0x1,
            ColorStateReplay = 0x2,
            ColorStateReset = 0x3,
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct MovePathSettings {
            public eMovePathModeLayer Mode;
            public byte AutoPlay;
            public byte Padding00;
            public ushort Time;
            public byte Loop;
            public byte Reverse;
            public ushort Padding01;
            public eRotationTypeLayer Rotation;
            public ushort AccelerateTime;
            public ushort DecelerateTime;

            // .net man
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.AsAny, SizeConst = 2)]
            public float[] VerticalSwingRange;
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.AsAny, SizeConst = 2)]
            public float[] HorizontalSwingRange;
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.AsAny, SizeConst = 2)]
            public float[] SwingMoveSpeedRange;
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.AsAny, SizeConst = 2)]
            public float[] SwingRotation;
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.AsAny, SizeConst = 2)]
            public float[] SwingRotationSpeedRange;
        };

        #region Struct
        [StructLayout(LayoutKind.Sequential)]
        public struct HeaderData {
            public LgbEntryType Type;
            public uint GimmickId;
            public int NameOffset;
            public Vector3 Translation;
            public Vector3 Rotation;
            public Vector3 Scale;
            public int GimmickFileOffset;

            public eDoorStateLayer InitialDoorState;
            public int OverriddenMembers;
            public int OverriddenMembersCount;
            public eRotationStateLayer InitialRotationState;
            public byte RandomTimelineAutoPlay;
            public byte RandomTimelineLoopPlayback;
            public byte IsCollisionControllableWithoutEObj;
            public byte Padding00;
            public uint BoundCLientPathInstanceID;
            public int MovePathSetting;
            public byte NotCreateNavimeshDoor;
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.AsAny, SizeConst = 3)]
            public byte[] Padding01;
            public eTransformStateLayer InitialTransformState;
            public eColorStateLayer InitialColorState;
            // + 100 bytes of unknowns
        }
        #endregion

        #region Properties
        LgbEntryType ILgbEntry.Type { get { return Header.Type; } }
        public HeaderData Header { get; private set; }
        public string Name { get; private set; }
        public Sgb.SgbFile Gimmick { get; private set; }

        public MovePathSettings MoveSettings { get; private set; }
        #endregion

        #region Constructor
        public LgbGimmickEntry(IO.PackCollection packs, byte[] buffer, int offset) {
            this.Header = buffer.ToStructure<HeaderData>(offset);

            this.MoveSettings = buffer.ToStructure<MovePathSettings>(offset + Header.MovePathSetting);

            this.Name = buffer.ReadString(offset + Header.NameOffset);

            var gimmickFilePath = buffer.ReadString(offset + Header.GimmickFileOffset);
            if (!string.IsNullOrWhiteSpace(gimmickFilePath)) {
                SaintCoinach.IO.File file;
                if (packs.TryGetFile(gimmickFilePath, out file))
                    this.Gimmick = new Sgb.SgbFile(file);
            }
        }
        #endregion
    }
}
