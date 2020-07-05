using SaintCoinach.Graphics.Lgb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SaintCoinach.Graphics.Sgb {
    public class SGSettings {
        public enum SGActionType : int {
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
        public enum eSGActionColorCurveType {
            SGActionColorCurveLinear = 0x0,
            SGActionColorCurveSpline = 0x1,
        };

        public enum eSGActionColorBlinkType {
            BlinkSineCurve = 0x0,
            BlinkRandom = 0x1,
        };

        public enum eSGActionTransformCurveType {
            CurveLinear = 0x0,
            CurveSpline = 0x1,
            CurveAcceleration = 0x2,
            CurveDeceleration = 0x3,
        };

        public enum eSGActionTransformMovementType {
            MovementOneWay = 0x0,
            MovementRoundTrip = 0x1,
            MovementRepetition = 0x2,
        };

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
        public struct SGActionRotation {
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

        public struct SGActionTransformItem {
            public byte Enabled;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] Padding00;
            public Vector3 Offset;
            public float RandomRate;
            public uint Time;
            public uint StartEndTime;
            public eSGActionTransformCurveType CurveType;
            public eSGActionTransformMovementType MovementType;
        };

        public struct SGActionTransform// : SGAction
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
            public SGActionTransformItem ItemTransform;
        };

        public struct SGActionTransform2
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
            public eSGActionTransformCurveType CurveType;
            public eSGActionTransformMovementType MovementType;
        }

        public struct SGActionDoor {
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

        public struct SGActionColorItem {
            public byte Enabled;
            public byte ColorEnabled;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst =2)]
            public byte[] Padding00;
            public Lgb.ColorHDRI ColorStart;
            public Lgb.ColorHDRI ColorEnd;
            public byte PowerEnabled;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] Padding01;
            public float PowerStart;
            public float PowerEnd;
            public uint Time;
            public eSGActionColorCurveType Curve;
            public byte BlinkEnabled;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] Padding02;
            public float BlinkAmplitude;
            public float BlinkSpeed;
            public eSGActionColorBlinkType BlinkType;
            public byte BlinkSync;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] Padding03;
        };

        public struct SGActionColor// : SGAction
        {
            public int TargetSGMemberIDs;
            public int TargetSGMemberIDCount;
            public byte Loop;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] Padding01;
            public uint Reserved2;
            public int Emissive;
            public int Light;
            public SGActionColorItem ColorItem;
        };

        #endregion

        #region Properties
        public HeaderData Header { get; private set; }
        public SgbFile Parent { get; private set; }
        public List<SGActionRotation> Rotations { get; private set; }
        public List<SGActionTransform2> Transformations { get; private set; }
        public List<SGActionDoor> Doors { get; private set; }
        public List<SGActionColor> ColourActions { get; private set; }
        #endregion

        #region Constructor
        public SGSettings(SgbFile parent, byte[] buffer, int offset) {
            this.Parent = parent;
            this.Rotations = new List<SGActionRotation>();
            this.Transformations = new List<SGActionTransform2>();
            this.Doors = new List<SGActionDoor>();
            this.ColourActions = new List<SGActionColor>();

            this.Header = buffer.ToStructure<HeaderData>(offset);
            int animContainerOffset = offset + Header.AnimContainerOffset;

            AnimContainer animContainer = buffer.ToStructure<AnimContainer>(ref animContainerOffset);
            int entryCount = animContainer.EntryCount;

            for (int i = 0; i < entryCount; ++i) {
                int entryOffset = animContainerOffset + buffer.ToStructure<int>(animContainerOffset + (i * 4));
                SGActionType type = (SGActionType)buffer.ToStructure<int>(ref entryOffset);
                // uint, uint, uint
                entryOffset += 12;
                switch (type) {
                    case SGActionType.Rotation:
                        {
                            var anim = buffer.ToStructure<SGActionRotation>(entryOffset);
                            Rotations.Add(anim);
                            if (parent.SGActionRotationTargetMap.ContainsKey(anim.TargetSGEntryID))
                                parent.SGActionRotationTargetMap[anim.TargetSGEntryID].Add(anim);
                            else 
                                parent.SGActionRotationTargetMap.Add(anim.TargetSGEntryID, new List<SGActionRotation>() { anim });
                        }
                        break;
                    case SGActionType.Transform:
                        {
                            var anim = buffer.ToStructure<SGActionTransform>(entryOffset);
                            SGActionTransform2 anim2 = new SGActionTransform2 { TargetSGMemberIDs = new uint[anim.TargetSGMemberIDCount], Loop = anim.Loop };
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
                                if (parent.SGActionTransformationTargetMap.ContainsKey(currTargetSg))
                                    parent.SGActionTransformationTargetMap[currTargetSg].Add(anim2);
                                else
                                    parent.SGActionTransformationTargetMap.Add(currTargetSg, new List<SGActionTransform2>() { anim2 });

                            }
                            Transformations.Add(anim2);
                        }
                        break;
                    case SGActionType.Door:
                        {
                            var anim = buffer.ToStructure<SGActionDoor>(entryOffset);
                            Doors.Add(anim);

                            if (parent.SGActionDoorTargetMap.ContainsKey(anim.DoorInstanceID1))
                                parent.SGActionDoorTargetMap[anim.DoorInstanceID1].Add(anim);
                            else
                                parent.SGActionDoorTargetMap.Add(anim.DoorInstanceID1, new List<SGActionDoor>() { anim });


                            if (parent.SGActionDoorTargetMap.ContainsKey(anim.DoorInstanceID2))
                                parent.SGActionDoorTargetMap[anim.DoorInstanceID2].Add(anim);
                            else
                                parent.SGActionDoorTargetMap.Add(anim.DoorInstanceID2, new List<SGActionDoor>() { anim });

                            if (parent.SGActionDoorTargetMap.ContainsKey(anim.DoorInstanceID3))
                                parent.SGActionDoorTargetMap[anim.DoorInstanceID3].Add(anim);
                            else
                                parent.SGActionDoorTargetMap.Add(anim.DoorInstanceID3, new List<SGActionDoor>() { anim });

                            if (parent.SGActionDoorTargetMap.ContainsKey(anim.DoorInstanceID3))
                                parent.SGActionDoorTargetMap[anim.DoorInstanceID3].Add(anim);
                            else
                                parent.SGActionDoorTargetMap.Add(anim.DoorInstanceID3, new List<SGActionDoor>() { anim });


                            if (parent.SGActionDoorTargetMap.ContainsKey(anim.DoorInstanceID4))
                                parent.SGActionDoorTargetMap[anim.DoorInstanceID4].Add(anim);
                            else
                                parent.SGActionDoorTargetMap.Add(anim.DoorInstanceID4, new List<SGActionDoor>() { anim });
                        }
                        break;
                    case SGActionType.Colour:
                        {
                            var anim = buffer.ToStructure<SGActionColor>(entryOffset);
                            ColourActions.Add(anim);

                            
                        }
                        break;
                    default:
                        System.Diagnostics.Debug.WriteLine($"SGActionType {type.ToString()} not implemented!");
                        break;
                }
            }
        }
        #endregion
    }
}
