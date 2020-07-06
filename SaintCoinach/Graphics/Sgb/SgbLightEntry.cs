using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SaintCoinach.Graphics.Sgb {
    public class SgbLightEntry : ISgbGroupEntry {

        #region Struct
        [StructLayout(LayoutKind.Sequential)]
        public struct HeaderData {
            public SgbGroupEntryType Type;
            public uint UnknownId;
            public int NameOffset;
            public Vector3 Translation;
            public Vector3 Rotation;
            public Vector3 Scale;
            public Lgb.eLightTypeLayer LightType;
            public float Attenuation;
            public float RangeRate;
            public Lgb.ePointLightTypeLayer PointLightType;
            public float AttenuationConeCoefficient;
            public float ConeDegree;

            public int TexturePath;

            public Lgb.ColorHDRI DiffuseColorHDRI;
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
        SgbGroupEntryType ISgbGroupEntry.Type { get { return Header.Type; } }
        public HeaderData Header { get; private set; }
        public string Name { get; private set; }
        public string TexturePath { get; private set; }
        public Sgb.SgbFile Parent { get; private set; }
        #endregion

        #region Constructor
        public SgbLightEntry(SgbFile parent, byte[] buffer, int offset) {
            this.Header = buffer.ToStructure<HeaderData>(offset);
            this.Name = buffer.ReadString(offset + Header.NameOffset);
            this.TexturePath = buffer.ReadString(offset + Header.TexturePath);

            this.Parent = parent;
            int x = 0;
        }
        #endregion
    }
}
