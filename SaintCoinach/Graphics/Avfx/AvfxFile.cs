using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SaintCoinach.Graphics.Avfx {

    public class AvfxFile {
        public static Dictionary<string, AvfxEntryType> tagTypeMap = new Dictionary<string, AvfxEntryType> {

            { "dhcS", AvfxEntryType.Scheduler},
            { "nLmT", AvfxEntryType.Timeline },
            { "lctP", AvfxEntryType.Particle },
            { "tcfE", AvfxEntryType.Effector },
            { "dniB", AvfxEntryType.Binder },
            { "xeT", AvfxEntryType.Texture },
            { "ldoM", AvfxEntryType.Model }
        };

        #region Struct
        [StructLayout(LayoutKind.Sequential)]
        public struct HeaderData {
            public uint Magic1;     // Avfx1
            public int FileSize;
            public uint Unknown1;
        }
        #endregion

        #region Properties
        public HeaderData Header { get; private set; }
        public IO.File File { get; private set; }

        List<AvfxModelEntry> Models;
        List<string> TexturePaths;
        List<SaintCoinach.Imaging.ImageFile> Textures;

        #endregion

        #region Constructor
        public AvfxFile(IO.File file) {
            this.File = file;

            Build();
        }
        #endregion

        #region Build
        private void Build() {
            const int BaseOffset = 0x14;

            var buffer = File.GetData();

            Models = new List<AvfxModelEntry>();
            Textures = new List<SaintCoinach.Imaging.ImageFile>();
            TexturePaths = new List<string>();

            this.Header = buffer.ToStructure<HeaderData>(0);
            //if (Header.Magic1 != "AVFX" || Header.Magic2 != 0x314E4353)     // LGB1 & SCN1
            //    throw new System.IO.InvalidDataException();

            int offset = BaseOffset;
            int remaining = offset - Header.FileSize - BaseOffset;
            while (offset < buffer.Length - BaseOffset) {

                int tagInt = buffer.ToStructure<int>(ref offset);
                string tag = System.Text.ASCIIEncoding.ASCII.GetString(BitConverter.GetBytes(tagInt));

                switch (tag) {
                    case "xeT\0":
                        int length = buffer.ToStructure<int>(ref offset);
                        if (length + offset < buffer.Length - BaseOffset) {

                            string texPath = buffer.ReadString(offset);

                            var img = File.Pack.GetFile(texPath);
                            TexturePaths.Add(texPath);
                            //Textures.Add(img);
                        }
                        break;
                    case "ldoM":
                        Models.Add(new AvfxModelEntry(File, buffer, offset));
                        break;
                    default:
                        break;

                }

            }
            //this.Data = data.ToArray();
        }
        #endregion
    }
}
