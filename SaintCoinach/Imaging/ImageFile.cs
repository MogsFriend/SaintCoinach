﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

using SaintCoinach.IO;

using Directory = SaintCoinach.IO.Directory;
using File = SaintCoinach.IO.File;

namespace SaintCoinach.Imaging {
    /// <summary>
    ///     Image file stored inside SqPack.
    /// </summary>
    public class ImageFile : File {
        #region Fields

        private WeakReference<byte[]> _BufferCache;
        private WeakReference<Image> _ImageCache;

        #endregion

        #region Properties

        public ImageHeader ImageHeader { get; private set; }
        public int Width { get { return ImageHeader.Width; } }
        public int Height { get { return ImageHeader.Height; } }
        public ImageFormat Format { get { return ImageHeader.Format; } }

        public byte[] Buffer { get; private set; }
        #endregion

        #region Constructors

        public ImageFile(Pack pack, FileCommonHeader commonHeader)
            : base(pack, commonHeader) {
            var stream = GetSourceStream();
            stream.Position = CommonHeader.EndOfHeader;
            ImageHeader = new ImageHeader(stream);
            Buffer = CommonHeader._Buffer;
        }
        public ImageFile(Pack pack, FileCommonHeader commonHeader, byte[] data)
            : base(pack, commonHeader) {
            ImageHeader = new ImageHeader(new MemoryStream(data));
            var len = data.Length - ImageHeader.EndOfHeader;
            Buffer = new byte[len];
            System.Buffer.BlockCopy(data, (int)ImageHeader.EndOfHeader, Buffer, 0, (int)len);

            this._BufferCache = new WeakReference<byte[]>(Buffer);
            this._BufferCache.SetTarget(Buffer);
        }

        #endregion

        #region Read

        public Image GetImage() {
            if (_ImageCache != null && _ImageCache.TryGetTarget(out var image)) return image;

            image = ImageConverter.Convert(this);

            if (_ImageCache == null)
                _ImageCache = new WeakReference<Image>(image);
            else
                _ImageCache.SetTarget(image);

            return image;
        }

        public override byte[] GetData() {
            if (_BufferCache != null && _BufferCache.TryGetTarget(out var buffer)) return buffer;

            buffer = Read();

            if (_BufferCache == null)
                _BufferCache = new WeakReference<byte[]>(buffer);
            else
                _BufferCache.SetTarget(buffer);

            return buffer;
        }

        private byte[] Read() {
            var sourceStream = GetSourceStream();
            var offsets = GetBlockOffsets();

            byte[] data;
            using (var dataStream = new MemoryStream((int)CommonHeader.Length)) {
                foreach (var offset in offsets) {
                    sourceStream.Position = ImageHeader.EndOfHeader + offset;
                    ReadBlock(sourceStream, dataStream);
                }
                data = dataStream.ToArray();
            }
            return data;
        }

        private IEnumerable<int> GetBlockOffsets() {
            const int CountOffset = 0x14;
            const int EntryLength = 0x14;
            const int BlockInfoOffset = 0x18;
            var buf = Buffer;

            var count = ImageHeader.MipmapCount;
            var currentOffset = 0;
            var offsets = new List<int>();

            for (var i = BlockInfoOffset + count * EntryLength; i + 2 <= buf.Length; i += 2) {
                var len = BitConverter.ToUInt16(buf, i);
                if (len == 0)
                    break;
                offsets.Add(currentOffset);
                currentOffset += len;
            }
            return offsets.ToArray();
        }

        #endregion
    }
}
