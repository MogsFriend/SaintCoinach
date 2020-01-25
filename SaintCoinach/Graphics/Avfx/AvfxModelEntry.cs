using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SaintCoinach.Graphics.Avfx {

    [StructLayout(LayoutKind.Sequential)]
    public struct AvfxVec4Half {
        public ushort X;
        public ushort Y;
        public ushort Z;
        public ushort W;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AvfxVec4U8 {
        public byte X;
        public byte Y;
        public byte Z;
        public byte W;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AvfxVertex {
        public Avfx.AvfxVec4Half Position;
        public Avfx.AvfxVec4U8 Normal;
        public Avfx.AvfxVec4U8 Tangent;
        public Avfx.AvfxVec4U8 Color;
        public Avfx.AvfxVec4Half UV1;
        public Avfx.AvfxVec4Half UV2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Indices {
        public ushort I1;
        public ushort I2;
        public ushort I3;
    }

    public class AvfxModelEntry : IAvfxEntry {
        #region Struct
        [StructLayout(LayoutKind.Sequential)]
        public struct HeaderData {
            public int Length;
        }
        #endregion

        #region Properties
        AvfxEntryType IAvfxEntry.Type { get { return AvfxEntryType.Model; } }
        public HeaderData Header { get; private set; }
        public string Name { get; private set; }
        public string ModelFilePath { get; private set; }

        public Vertex[] ConvertedVertexes;
        public Avfx.Indices[] Indices;
        public AvfxVertex[] AvfxVertexes;

        #endregion

        #region Constructor
        public AvfxModelEntry(IO.File file, byte[] buffer, int offset) {
            this.Header = buffer.ToStructure<HeaderData>(ref offset);
            int initialOffset = offset;

            AvfxVertexes = new List<AvfxVertex>().ToArray();
            Indices = new List<Indices>().ToArray();

            this.Name = file.Path.Substring(file.Path.LastIndexOf("/") + 1);

            string tag = "";

            while (offset < this.Header.Length + initialOffset) {
                try {
                    int tagInt = buffer.ToStructure<int>(ref offset);

                    // lmao
                    tag = System.Text.ASCIIEncoding.ASCII.GetString(BitConverter.GetBytes(tagInt));


                    if (tag.Length < 4)
                        continue;
                    tag = tag.Substring(0, 4);

                    // skip 4 bytes for tag

                    if (tag == "wrDV") {
                        //offset += 4;
                        int numV = buffer.ToStructure<int>(ref offset) / System.Runtime.InteropServices.Marshal.SizeOf(new AvfxVertex());
                        AvfxVertexes = new AvfxVertex[numV];
                        ConvertedVertexes = new Vertex[numV];

                        for (int i = 0; i < numV; ++i) {
                            var v = buffer.ToStructure<AvfxVertex>(ref offset);
                            AvfxVertexes[i] = v;

                            Vector4 Position = new Vector4();
                            Position.X = (float)(v.Position.X / 32768.0f);
                            Position.Y = (float)(v.Position.Y / 32768.0f);
                            Position.Z = (float)(v.Position.Z / 32768.0f);
                            Position.W = (float)(v.Position.W / 32768.0f);

                            Vector3 Normal = new Vector3();
                            Normal.X = (float)(v.Normal.X / 255.0f);
                            Normal.Y = (float)(v.Normal.Y / 255.0f);
                            Normal.Z = (float)(v.Normal.Z / 255.0f);
                            //Normal.W = (float)(v.Normal.W / 255.0f);

                            Vector4 Tangent1 = new Vector4();
                            Tangent1.X = (float)(v.Tangent.X / 255.0f);
                            Tangent1.Y = (float)(v.Tangent.Y / 255.0f);
                            Tangent1.Z = (float)(v.Tangent.Z / 255.0f);
                            Tangent1.W = (float)(v.Tangent.W / 255.0f);


                            Vector4 Color = new Vector4();
                            Color.X = (float)(v.Color.X / 255.0f);
                            Color.Y = (float)(v.Color.Y / 255.0f);
                            Color.Z = (float)(v.Color.Z / 255.0f);
                            Color.W = (float)(v.Color.W / 255.0f);


                            Vector4 UV = new Vector4();
                            UV.X = (float)(v.UV1.X / 32768.0f);
                            UV.Y = (float)(v.UV1.Y / 32768.0f);
                            UV.Z = (float)(v.UV2.Z / 32768.0f);
                            UV.W = (float)(v.UV2.W / 32768.0f);

                            Vertex converted = new Vertex();
                            converted.Position = Position;
                            converted.Normal = Normal;
                            converted.Tangent1 = Tangent1;
                            converted.Color = Color;
                            converted.UV = UV;

                            ConvertedVertexes[i] = converted;

                            //System.Diagnostics.Debug.WriteLine("");
                        }
                    }
                    else if (tag == "xdIV") {
                        int numI = buffer.ToStructure<int>(ref offset) / System.Runtime.InteropServices.Marshal.SizeOf(new Indices());
                        Indices = new Indices[numI];
                        for (int i = 0; i < numI; ++i) {
                            Indices[i] = buffer.ToStructure<Indices>(ref offset);
                        }
                    }
                    else if (buffer.Length - offset < 4) {
                        offset += buffer.Length - offset;
                    }

                    int x = 0;
                }
                catch (Exception e) {
                    System.Diagnostics.Debug.WriteLine($"unable to read tag {tag} buffer offset {offset} {e.Message}");
                }
            }
        }
        #endregion
    }
}
