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
        };

        public enum eSGAnimTransformCurveType {
            CurveLinear = 0x0,
            CurveSpline = 0x1,
            CurveAcceleration = 0x2,
            CurveDeceleration = 0x3,
        };

        public enum eSGAnimTransformMovementType {
            MovementOneWay = 0x0,
            MovementRoundTrip = 0x1,
            MovementRepetition = 0x2,
        };

        public struct SGAnimTransformItem {
            public byte Enabled;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] Padding00;
            public Vector3 Offset;
            public float RandomRate;
            public uint Time;
            public uint StartEndTime;
            public eSGAnimTransformCurveType CurveType;
            public eSGAnimTransformMovementType MovementType;
        };

        public struct SGAnimTransform// : SGAction
        {
            public int TargetSGMemberIDs;
            public int TargetSGMemberIDCount;
            public byte Loop;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] Padding01;
            public uint Reserved2;
            public int Translation;
            public int Rotation;
            public int Scale;
            public SGAnimTransformItem ItemTransform;
        };

        public struct SGAnimTransform2
        {
            public uint[] TargetSGMemberIDs;
            public byte Loop;
            public Vector3 Translation;
            public Vector3 Rotation;
            public Vector3 Scale;

            public byte Enabled;
            public Vector3 Offset;
            public float RandomRate;
            public uint Time;
            public uint StartEndTime;
            public eSGAnimTransformCurveType CurveType;
            public eSGAnimTransformMovementType MovementType;
        }

        public enum eOpenStyle {
            Rotation_0 = 0x0,
            HorizontalSlide = 0x1,
            VerticalSlide = 0x2,
        };

        public enum eCurveType {
            Spline_0 = 0x1,
            Linear = 0x2,
            Acceleration1 = 0x3,
            Deceleration1 = 0x4,
        };

        public struct SGAnimDoor {
            public byte DoorInstanceID1;
            public byte DoorInstanceID2;
            public short Padding01;
            public eOpenStyle OpenStyle;
            public float TimeLength;
            public float OpenAngle;
            public float OpenDistance;
            public byte SoundAtOpening;
            public byte SoundAtClosing;
            public byte DoorInstanceID3;
            public byte DoorInstanceID4;
            public eCurveType CurveType;
            public SGRotationAxis RotationAxis;
        };

        #endregion

        #region Properties
        public HeaderData Header { get; private set; }
        public SgbFile Parent { get; private set; }
        public List<SGAnimRotation> Rotations { get; private set; }
        public List<SGAnimTransform2> Transformations { get; private set; }

        public List<SGAnimDoor> Doors { get; private set; }
        #endregion

        #region Constructor
        public SGSettings(SgbFile parent, byte[] buffer, int offset) {
            this.Parent = parent;
            this.Rotations = new List<SGAnimRotation>();
            this.Transformations = new List<SGAnimTransform2>();
            this.Doors = new List<SGAnimDoor>();

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
                        {
                            var anim = buffer.ToStructure<SGAnimRotation>(entryOffset);
                            Rotations.Add(anim);
                            if (parent.SGAnimRotationTargetMap.ContainsKey(anim.TargetSGEntryID))
                                parent.SGAnimRotationTargetMap[anim.TargetSGEntryID].Add(anim);
                            else 
                                parent.SGAnimRotationTargetMap.Add(anim.TargetSGEntryID, new List<SGAnimRotation>() { anim });
                        }
                        break;
                    case SGAnimType.Transform:
                        {
                            var anim = buffer.ToStructure<SGAnimTransform>(entryOffset);
                            SGAnimTransform2 anim2 = new SGAnimTransform2 { TargetSGMemberIDs = new uint[anim.TargetSGMemberIDCount], Loop = anim.Loop };
                            anim2.Translation = buffer.ToStructure<Vector3>((entryOffset - 12) + anim.Translation);
                            anim2.Rotation = buffer.ToStructure<Vector3>((entryOffset - 12) + anim.Rotation);
                            anim2.Scale = buffer.ToStructure<Vector3>((entryOffset - 12) + anim.Scale);

                            anim2.Enabled = anim.ItemTransform.Enabled;
                            anim2.Offset = anim.ItemTransform.Offset;
                            anim2.CurveType = anim.ItemTransform.CurveType;
                            anim2.MovementType = anim.ItemTransform.MovementType;
                            anim2.Time = anim.ItemTransform.Time;
                            anim2.StartEndTime = anim.ItemTransform.StartEndTime;
                            anim2.RandomRate = anim.ItemTransform.RandomRate;
                            
                            for(var j = 0; j < anim.TargetSGMemberIDCount; ++j) {
                                byte currTargetSg = buffer.ToStructure<byte>((entryOffset - 16) + anim.TargetSGMemberIDs + j);

                                anim2.TargetSGMemberIDs[j] = currTargetSg;
                                if (parent.SGAnimTransformationTargetMap.ContainsKey(currTargetSg))
                                    parent.SGAnimTransformationTargetMap[currTargetSg].Add(anim2);
                                else
                                    parent.SGAnimTransformationTargetMap.Add(currTargetSg, new List<SGAnimTransform2>() { anim2 });

                            }
                            Transformations.Add(anim2);
                        }
                        break;
                    case SGAnimType.Door:
                        {
                            var anim = buffer.ToStructure<SGAnimDoor>(entryOffset);
                            Doors.Add(anim);

                            if (parent.SGAnimDoorTargetMap.ContainsKey(anim.DoorInstanceID1))
                                parent.SGAnimDoorTargetMap[anim.DoorInstanceID1].Add(anim);
                            else
                                parent.SGAnimDoorTargetMap.Add(anim.DoorInstanceID1, new List<SGAnimDoor>() { anim });


                            if (parent.SGAnimDoorTargetMap.ContainsKey(anim.DoorInstanceID2))
                                parent.SGAnimDoorTargetMap[anim.DoorInstanceID2].Add(anim);
                            else
                                parent.SGAnimDoorTargetMap.Add(anim.DoorInstanceID2, new List<SGAnimDoor>() { anim });

                            if (parent.SGAnimDoorTargetMap.ContainsKey(anim.DoorInstanceID3))
                                parent.SGAnimDoorTargetMap[anim.DoorInstanceID3].Add(anim);
                            else
                                parent.SGAnimDoorTargetMap.Add(anim.DoorInstanceID3, new List<SGAnimDoor>() { anim });

                            if (parent.SGAnimDoorTargetMap.ContainsKey(anim.DoorInstanceID3))
                                parent.SGAnimDoorTargetMap[anim.DoorInstanceID3].Add(anim);
                            else
                                parent.SGAnimDoorTargetMap.Add(anim.DoorInstanceID3, new List<SGAnimDoor>() { anim });


                            if (parent.SGAnimDoorTargetMap.ContainsKey(anim.DoorInstanceID4))
                                parent.SGAnimDoorTargetMap[anim.DoorInstanceID4].Add(anim);
                            else
                                parent.SGAnimDoorTargetMap.Add(anim.DoorInstanceID4, new List<SGAnimDoor>() { anim });
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
