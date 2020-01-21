using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SaintCoinach.Graphics.Lgb {
    public enum eLightTypeLayer {
        LightTypeNone = 0x0,
        LightTypeDirectional = 0x1,
        LightTypePoint = 0x2,
        LightTypeSpot = 0x3,
        LightTypePlane = 0x4,
        LightTypeLine = 0x5,
        LightTypeFakeSpecular = 0x6,
    };

    public enum ePointLightTypeLayer {
        PointLightTypeSphere = 0x0,
        PointLightTypeHalfSphere = 0x1,
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct ColorHDRI {
        public byte Red;
        public byte Green;
        public byte Blue;
        public byte Alpha;
        public float Intensity;
    }
    public class LgbLightEntry : ILgbEntry {
        #region Struct
        [StructLayout(LayoutKind.Sequential)]
        public struct HeaderData {
            public LgbEntryType Type;
            public uint UnknownId;
            public int NameOffset;
            public Vector3 Translation;
            public Vector3 Rotation;
            public Vector3 Scale;
            public eLightTypeLayer LightType;
            public float Attenuation;
            public float RangeRate;
            public ePointLightTypeLayer PointLightType;
            public float AttenuationConeCoefficient;
            public float ConeDegree;

            public int TexturePath;

            public ColorHDRI DiffuseColorHDRI;
            public byte FollowsDirectionalLight;
            public byte Padding00;
            public short Reserved2;
            public byte SpecularEnabled;
            public byte BGShadowEnabled;
            public byte CharacterShadowEnabled;
            public byte Padding01;
            public float ShadowClipRange;
            public float PlaneLightRotationX;
            public float PlaneLightRotationY;
            public ushort MergeGroupID;
            public ushort Padding02;
            // + unknowns
        }
        #endregion

        #region Properties
        LgbEntryType ILgbEntry.Type { get { return Header.Type; } }
        public HeaderData Header { get; private set; }
        public string Name { get; private set; }
        public string TexturePath { get; private set; }
        public Sgb.SgbFile Gimmick { get; private set; }
        #endregion

        #region Constructor
        public LgbLightEntry(IO.PackCollection packs, byte[] buffer, int offset) {
            this.Header = buffer.ToStructure<HeaderData>(offset);
            this.Name = buffer.ReadString(offset + Header.NameOffset);
            this.TexturePath = buffer.ReadString(offset + Header.TexturePath);

            int x = 0;
        }
        #endregion
    }
}
