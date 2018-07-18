﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SaintCoinach.Graphics.Lgb {
    public class LgbLightEntry : ILgbEntry {
        private static Ex.ExCollection Collection;
        private static Ex.ISheet EventObjectSheet;
        private static Ex.ISheet ExportedSgSheet;

        #region Struct
        [StructLayout(LayoutKind.Sequential)]
        public struct HeaderData {
            public LgbEntryType Type;
            public uint EventObjectId;
            public int NameOffset;
            public Vector3 Translation;
            public Vector3 Rotation;
            public Vector3 Scale;
            public uint EntryCount;
            public Vector2 Entry1;
            public int UnknownFlag1;
            public Vector2 Entry2;
            public int Entry2NameOffset;
            public ushort Entry3NameOffset;
            public short UnknownFlag2;
            public Vector2 Entry3;
            public short UnknownFlag3;
            public short UnknownFlag4;
            public Vector2 Entry4;
            public Vector2 Entry5;
            // + unknowns
        }
        #endregion

        #region Properties
        LgbEntryType ILgbEntry.Type { get { return Header.Type; } }
        public HeaderData Header { get; private set; }
        public string Name { get; private set; }
        public string Entry2Name, Entry3Name;
        public Sgb.SgbFile Gimmick { get; private set; }
        #endregion

        #region Constructor
        public LgbLightEntry(IO.PackCollection packs, byte[] buffer, int offset) {
            this.Header = buffer.ToStructure<HeaderData>(offset);
        }
        #endregion
    }
}
