﻿using System;
using System.Collections.Generic;
using System.IO;

namespace TSMapEditor.CCEngine
{
    [Flags]
    public enum ShpCompression
    {
        None = 0,
        HasTransparency = 1,
        UsesRle = 2
    }

    /// <summary>
    /// Represents the header of a SHP file.
    /// </summary>
    struct ShpFileHeader
    {
        public const int SizeOf = 8;

        public ShpFileHeader(byte[] buffer)
        {
            if (buffer.Length < SizeOf)
                throw new ArgumentException(nameof(ShpFileHeader) + ": buffer is not long enough");

            Unknown = BitConverter.ToUInt16(buffer, 0);
            if (Unknown != 0)
                throw new ArgumentException("Unexpected field value in SHP header");

            SpriteWidth = BitConverter.ToUInt16(buffer, 2);
            SpriteHeight = BitConverter.ToUInt16(buffer, 4);
            FrameCount = BitConverter.ToUInt16(buffer, 6);
        }

        public ushort Unknown;
        public ushort SpriteWidth;
        public ushort SpriteHeight;
        public ushort FrameCount;
    }

    /// <summary>
    /// Represents the information of a single frame in a SHP file.
    /// </summary>
    public class ShpFrameInfo
    {
        private const int SizeOf = 24;

        public ShpFrameInfo(Stream stream)
        {
            if (stream.Length < stream.Position + SizeOf)
                throw new ArgumentException(nameof(ShpFrameInfo) + ": buffer is not long enough");

            XOffset = ReadUShortFromStream(stream);
            YOffset = ReadUShortFromStream(stream);
            Width = ReadUShortFromStream(stream);
            Height = ReadUShortFromStream(stream);
            Flags = (ShpCompression)ReadUIntFromStream(stream);
            byte r = (byte)stream.ReadByte();
            byte g = (byte)stream.ReadByte();
            byte b = (byte)stream.ReadByte();
            AverageColor = new RGBColor(r, g, b);
            Unknown1 = (byte)stream.ReadByte();
            Unknown2 = ReadUIntFromStream(stream);
            DataOffset = ReadUIntFromStream(stream);
        }

        private ushort ReadUShortFromStream(Stream stream)
        {
            stream.Read(buffer, 0, 2);
            return BitConverter.ToUInt16(buffer, 0);
        }

        private uint ReadUIntFromStream(Stream stream)
        {
            stream.Read(buffer, 0, 4);
            return BitConverter.ToUInt32(buffer, 0);
        }

        byte[] buffer = new byte[4];

        public ushort XOffset;
        public ushort YOffset;
        public ushort Width;
        public ushort Height;
        public ShpCompression Flags;
        public RGBColor AverageColor;
        public byte Unknown1;
        public uint Unknown2;
        public uint DataOffset;
    }

    /// <summary>
    /// Represents a SHP file. Combines the header and frame information
    /// and makes it possible to parse the actual graphical data.
    /// </summary>
    public class ShpFile
    {
        private ShpFileHeader shpFileHeader;
        private List<ShpFrameInfo> shpFrameInfos;

        public int FrameCount => shpFrameInfos.Count;

        public int Width => shpFileHeader.SpriteWidth;
        public int Height => shpFileHeader.SpriteHeight;

        public void ParseFromFile(string filePath)
        {
            using (FileStream stream = File.OpenRead(filePath))
            {
                Parse(stream);
            }
        }

        public void Parse(Stream stream)
        {
            byte[] buffer = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(buffer, 0, buffer.Length);
            ParseFromBuffer(buffer);
        }

        public void ParseFromBuffer(byte[] buffer)
        {
            shpFileHeader = new ShpFileHeader(buffer);
            shpFrameInfos = new List<ShpFrameInfo>(shpFileHeader.FrameCount);

            using (var memoryStream = new MemoryStream(buffer))
            {
                memoryStream.Position = ShpFileHeader.SizeOf;

                for (int i = 0; i < shpFileHeader.FrameCount; i++)
                {
                    var shpFrameInfo = new ShpFrameInfo(memoryStream);
                    shpFrameInfos.Add(shpFrameInfo);
                }
            }
        }

        public ShpFrameInfo GetShpFrameInfo(int frameIndex) => shpFrameInfos[frameIndex];

        public byte[] GetUncompressedFrameData(int frameIndex, byte[] fileData)
        {
            ShpFrameInfo frameInfo = shpFrameInfos[frameIndex];

            if (frameInfo.DataOffset == 0)
                return null;

            byte[] frameData = new byte[frameInfo.Width * frameInfo.Height];
            if ((frameInfo.Flags & ShpCompression.UsesRle) == ShpCompression.None)
            {
                for (int i = 0; i < frameData.Length; i++)
                {
                    frameData[i] = fileData[frameInfo.DataOffset + i];
                }
            }
            else
            {
                int dataOffset = 0;
                for (int lineIndex = 0; lineIndex < frameInfo.Height; lineIndex++)
                {
                    int baseOffset = (int)frameInfo.DataOffset + dataOffset;
                    int lineDataLength = fileData[baseOffset];
                    int currentByteIndex = 2;
                    int positionOnLine = 0;
                    while (currentByteIndex < lineDataLength && positionOnLine < frameInfo.Width)
                    {
                        byte value = fileData[baseOffset + currentByteIndex];
                        if (value == 0)
                        {
                            byte transparentPixelCount = fileData[baseOffset + currentByteIndex + 1];
                            for (int j = 0; j < transparentPixelCount; j++)
                            {
                                frameData[lineIndex * frameInfo.Width + positionOnLine + j] = 0;
                            }
                            positionOnLine += transparentPixelCount;
                            currentByteIndex += 2;
                        }
                        else
                        {
                            frameData[lineIndex * frameInfo.Width + positionOnLine] = value;
                            positionOnLine++;
                            currentByteIndex++;
                        }
                    }

                    dataOffset += lineDataLength;
                }
            }

            return frameData;
        }
    }


}
