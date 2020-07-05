using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SaintCoinach.Graphics.Lgb {
    public class LgbGroup {
        #region Struct
        [StructLayout(LayoutKind.Sequential)]
        public struct HeaderData {
            public uint LayerID;
            public int Name;
            public int InstanceObjects;
            public int InstanceObjectCount;
            public byte ToolModeVisible;
            public byte ToolModeReadOnly;
            public byte IsBushLayer;
            public byte PS3Visible;
            public int LayerSetRef;
            public ushort FestivalID;
            public ushort FestivalPhaseID;
            public byte IsTemporary;
            public byte IsHousing;
            public ushort VersionMask;
            public uint Reserved;
            public int OBSetReferencedList;
            public int OBSetReferencedListCount;
            public int OBSetEnableReferencedList;
            public int OBSetEnableReferencedListCount;
        }
        #endregion

        #region Properties
        public LgbFile Parent { get; private set; }
        public HeaderData Header { get; private set; }
        public string Name { get; private set; }
        public ILgbEntry[] Entries { get; private set; }
        public Sgb.SgbGroup.ObjectBehaviour[] ObjectBehaviours { get; private set; }

        #endregion

        #region Constructor
        public LgbGroup(LgbFile parent, byte[] buffer, int offset) {
            this.Parent = parent;
            this.Header = buffer.ToStructure<HeaderData>(offset);
            this.Name = buffer.ReadString(offset + Header.Name);

            //uint[] Unknown = new uint[100];
            //System.Buffer.BlockCopy(buffer, offset + System.Runtime.InteropServices.Marshal.SizeOf<HeaderData>(), Unknown, 0, 400);

            
            var entriesOffset = offset + Header.InstanceObjects;
            Entries = new ILgbEntry[Header.InstanceObjectCount];
            for(var i = 0; i < Header.InstanceObjectCount; ++i) {
                var entryOffset = entriesOffset + BitConverter.ToInt32(buffer, entriesOffset + i * 4);
                var type = (LgbEntryType)BitConverter.ToInt32(buffer, entryOffset);

                try {
                    switch (type) {
                        case LgbEntryType.Model:
                            Entries[i] = new LgbModelEntry(Parent.File.Pack.Collection, buffer, entryOffset);
                            break;
                        case LgbEntryType.Gimmick:
                        case LgbEntryType.SharedGroup15:
                            Entries[i] = new LgbGimmickEntry(Parent.File.Pack.Collection, buffer, entryOffset);
                            break;
                        case LgbEntryType.EventObject:
                            Entries[i] = new LgbEventObjectEntry(Parent.File.Pack.Collection, buffer, entryOffset);
                            break;
                        case LgbEntryType.Light:
                            Entries[i] = new LgbLightEntry(Parent.File.Pack.Collection, buffer, entryOffset);
                            break;
                        case LgbEntryType.EventNpc:
                            Entries[i] = new LgbENpcEntry(Parent.File.Pack.Collection, buffer, entryOffset);
                            break;
                        case LgbEntryType.Vfx:
                            Entries[i] = new LgbVfxEntry(Parent.File.Pack.Collection, buffer, entryOffset);
                            break;
                        case LgbEntryType.Sound:
                            Entries[i] = new LgbSoundEntry(Parent.File.Pack.Collection, buffer, entryOffset);
                            break;
                        case LgbEntryType.EnvLocation:
                            Entries[i] = new LgbEnvLocationEntry(Parent.File.Pack.Collection, buffer, entryOffset);
                            break;
                        default:
                            // TODO: Work out other parts.
                            //Debug.WriteLine($"{Parent.File.Path} {type} at 0x{entryOffset:X} in {Name}: Can't read type.");
                            break;
                    }
                } catch (Exception ex) {
                    Debug.WriteLine($"{Parent.File.Path} {type} at 0x{entryOffset:X} in {Name} failure: {ex.Message}");
                }
            }
            this.ObjectBehaviours = new Sgb.SgbGroup.ObjectBehaviour[Header.OBSetEnableReferencedListCount];
            var structSize = Marshal.SizeOf(new Sgb.SgbGroup.ObjectBehaviour());
            for (var i = 0; i < this.ObjectBehaviours.Length; ++i) {
                // offset + fileHdr + obsetOffset + obsetEntryOffset
                this.ObjectBehaviours[i] = buffer.ToStructure<Sgb.SgbGroup.ObjectBehaviour>(offset + (int)Header.OBSetEnableReferencedList + (i * structSize));
                foreach (var mdl in this.Entries.OfType<LgbModelEntry>()) {
                    if (mdl.Header.Id == ObjectBehaviours[i].InstanceId) {
                        mdl.IsEmissive = ObjectBehaviours[i].Emissive == 1 ? true : false;
                        break;
                    }
                }
            }
        }
        #endregion

        public override string ToString() => Name ?? "(null)";
    }
}
