using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SaintCoinach.Graphics.Sgb {
    public class SGSettings {
        public enum SGAnimType : int {
            Unknown = 0x0,
            Door = 0x1,
            Rotation = 0x2,
            Unknown3 = 0x3,
            Unknown4 = 0x4,
            Transform = 0x5,
            Colour = 0x6,

            Reserved
        };

        public enum SGRotationAxis : int {
            X = 0,
            Y = 1,
            Z = 2
        }

        #region Struct
        [StructLayout(LayoutKind.Sequential)]
        public struct AnimContainer {
            public int Unknown;
            public int EntryCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HeaderData {
            public uint Unknown0;
            public uint Unknown4;
            public uint Unknown8;
            public uint UnknownC;
            public uint Unknown10;
            public uint Unknown14;
            public uint Unknown18;
            public uint Unknown1C;
            public int AnimContainerOffset;
            public uint Unknown24;
            public int Unknown28;

        }

        // todo:
        // cba with other shit, rotation only for now for aetherytes
        [StructLayout(LayoutKind.Sequential)]
        public struct SGAnimRotation {
            public uint TargetSGEntryID;
            public SGRotationAxis Axis;
            public float FullRotationTime;
            public float UnknownFloat;
            public byte TargetSGVfx;
            public byte TargetSGVfxRotationEnabled; // true/false
            public byte SomeSoundLink;
            public byte SomeSoundLink2;
            public byte SomeSoundLink3;
            public byte TargetSGVfx2;
            public byte TargetSGVfx2RotationEnabled;
            public byte Padding;
        }
        #endregion

        #region Properties
        public HeaderData Header { get; private set; }
        public SgbFile Parent { get; private set; }
        public List<SGAnimRotation> Rotations { get; private set; }
        #endregion

        #region Constructor
        public SGSettings(SgbFile parent, byte[] buffer, int offset) {
            this.Parent = parent;
            this.Rotations = new List<SGAnimRotation>();

            this.Header = buffer.ToStructure<HeaderData>(offset);
            int animContainerOffset = offset + Header.AnimContainerOffset;

            AnimContainer animContainer = buffer.ToStructure<AnimContainer>(ref animContainerOffset);
            int entryCount = animContainer.EntryCount;

            for (int i = 0; i < entryCount; ++i) {
                int entryOffset = animContainerOffset + buffer.ToStructure<int>(animContainerOffset + (i * 4));
                SGAnimType type = (SGAnimType)buffer.ToStructure<int>(ref entryOffset);
                // uint, uint, uint
                entryOffset += 12;
                switch (type) {
                    case SGAnimType.Rotation:
                        var anim = buffer.ToStructure<SGAnimRotation>(entryOffset);
                        Rotations.Add(anim);
                        if (parent.SGAnimRotationTargetMap.ContainsKey(anim.TargetSGEntryID)) {
                            parent.SGAnimRotationTargetMap[anim.TargetSGEntryID].Add(anim);
                        }
                        else {
                            parent.SGAnimRotationTargetMap.Add(anim.TargetSGEntryID, new List<SGAnimRotation>() { anim });
                        }

                        break;
                    default:
                        System.Diagnostics.Debug.WriteLine($"SGAnimType {type.ToString()} not implemented!");
                        break;
                }
            }
        }
        #endregion
    }
}
