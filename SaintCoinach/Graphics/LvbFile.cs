using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SaintCoinach.Graphics {
    public class LvbFile {
        public struct HeaderData {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public char[] FileID;
            public int FileSize;
            public int TotalChunkCount;
        };

        public HeaderData Header;
        public bool IsValidLvb;
        public List<string> LgbPaths = new List<string>();
        public LvbFile(SaintCoinach.IO.File file) {
            var data = file.GetData();
            int offset = 0;

            this.Header = data.ToStructure<HeaderData>(offset);
            IsValidLvb = false;

            // todo: probably parse this properly

            if ((Header.FileID[0] != 'L' && Header.FileID[0] != 'S')
                || (Header.FileID[1] != 'V' && Header.FileID[1] != 'G') || Header.FileID[2] != 'B' /*|| Header.FileID[3] != '\0'*/) {
                IsValidLvb = false;
                //return;
                //throw new InvalidCastException("Not a valid LVB file!");
            }

            //if (Header.FileID[0] != 'L' || Header.FileID[1] != 'V' || Header.FileID[2] != 'B' /*|| Header.FileID[3] != '\0'*/) {
            //    //throw new InvalidCastException("Not a valid LVB file!");
            //    IsValidLvb = false;
            //    return;
            //}


            offset += 12;
            try {
                while (offset + 2 < data.Length) {
                    
                    while (offset + 2 < data.Length && data[offset] == 0 && !char.IsLetterOrDigit((char)data[offset]))
                        offset++;

                    string testStr = data.ReadString(offset);
                    offset += (testStr.Length == 0 ? 1 : testStr.Length);

                    if (testStr.Length > 15 && testStr[0] != '/') {

                        /*
                        if (testStr.Contains("bg.lgb") == false) {
                            offset += testStr.Length;
                            continue;
                        }
                        */

                        if (testStr[0] == '?')
                            testStr = testStr.Substring(1);
                        if (testStr[0] == 'b' && testStr[1] == 'g' && testStr.Contains('/')) {
                            LgbPaths.Add(testStr);
                            IsValidLvb = true;
                        }
                    }
                }
            }
            catch (Exception e) {

            }
        }
    }
}
