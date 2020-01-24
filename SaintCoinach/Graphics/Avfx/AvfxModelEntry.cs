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

    public struct ConvertedVertex {
        public Vector4 Position;
        public Vector4 Normal;
        public Vector4 Tangent;
        public Vector4 Color;
        public Vector4 UV1;
        public Vector4 UV2;
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

        ConvertedVertex[] ConvertedVertexes;
        Avfx.Indices[] Indices;
        AvfxVertex[] AvfxVertexes;

        #endregion

        #region Constructor
        public AvfxModelEntry(IO.File file, byte[] buffer, int offset) {
            this.Header = buffer.ToStructure<HeaderData>(ref offset);
            int initialOffset = offset;
            ConvertedVertexes = new List<ConvertedVertex>().ToArray();
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
                        ConvertedVertexes = new ConvertedVertex[numV];

                        for (int i = 0; i < numV; ++i) {
                            var v = buffer.ToStructure<AvfxVertex>(ref offset);
                            AvfxVertexes[i] = v;

                            ConvertedVertex converted = new ConvertedVertex();
                            converted.Position = new Vector4();
                            converted.Position.X = (float)(v.Position.X / 32768.0f);
                            converted.Position.Y = (float)(v.Position.Y / 32768.0f);
                            converted.Position.Z = (float)(v.Position.Z / 32768.0f);
                            converted.Position.W = (float)(v.Position.W / 32768.0f);

                            converted.Normal = new Vector4();
                            converted.Normal.X = (float)(v.Normal.X / 255.0f);
                            converted.Normal.Y = (float)(v.Normal.Y / 255.0f);
                            converted.Normal.Z = (float)(v.Normal.Z / 255.0f);
                            converted.Normal.W = (float)(v.Normal.W / 255.0f);

                            converted.Tangent = new Vector4();
                            converted.Tangent.X = (float)(v.Tangent.X / 255.0f);
                            converted.Tangent.Y = (float)(v.Tangent.Y / 255.0f);
                            converted.Tangent.Z = (float)(v.Tangent.Z / 255.0f);
                            converted.Tangent.W = (float)(v.Tangent.W / 255.0f);

                            converted.Color = new Vector4();
                            converted.Color.X = (float)(v.Color.X / 255.0f);
                            converted.Color.Y = (float)(v.Color.Y / 255.0f);
                            converted.Color.Z = (float)(v.Color.Z / 255.0f);
                            converted.Color.W = (float)(v.Color.W / 255.0f);


                            converted.UV1 = new Vector4();
                            converted.UV1.X = (float)(v.UV1.X / 32768.0f);
                            converted.UV1.Y = (float)(v.UV1.Y / 32768.0f);
                            converted.UV1.Z = (float)(v.UV1.Z / 32768.0f);
                            converted.UV1.W = (float)(v.UV1.W / 32768.0f);


                            converted.UV2 = new Vector4();
                            converted.UV2.X = (float)(v.UV2.X / 32768.0f);
                            converted.UV2.Y = (float)(v.UV2.Y / 32768.0f);
                            converted.UV2.Z = (float)(v.UV2.Z / 32768.0f);
                            converted.UV2.W = (float)(v.UV2.W / 32768.0f);

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
