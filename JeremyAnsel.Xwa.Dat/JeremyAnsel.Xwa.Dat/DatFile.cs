
namespace JeremyAnsel.Xwa.Dat
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Text;

    public class DatFile
    {
        private const long Signature = 0x5602235657062357;

        public DatFile()
        {
            this.Groups = new List<DatGroup>();
        }

        public string FileName { get; private set; }

        public IList<DatGroup> Groups { get; private set; }

        public IEnumerable<DatImage> Images
        {
            get
            {
                return this.Groups.SelectMany(t => t.Images);
            }
        }

        public static DatFile FromFile(string fileName)
        {
            var dat = new DatFile();

            dat.FileName = fileName;

            FileStream filestream = null;

            try
            {
                filestream = new FileStream(fileName, FileMode.Open, FileAccess.Read);

                using (BinaryReader file = new BinaryReader(filestream, Encoding.ASCII))
                {
                    filestream = null;

                    long signature = file.ReadInt64();

                    if (signature != DatFile.Signature)
                    {
                        throw new ArgumentException("invalid file", "fileName");
                    }

                    short groupFormat = file.ReadInt16();
                    short count = file.ReadInt16();

                    file.ReadInt16(); // images count
                    file.ReadInt32(); // images length

                    if (groupFormat != 0)
                    {
                        file.BaseStream.Position += 12;
                    }

                    file.BaseStream.Position += 4;

                    for (int i = 0; i < count; i++)
                    {
                        var group = new DatGroup();

                        group.GroupId = file.ReadInt16();
                        int imageCount = file.ReadInt16();
                        file.ReadInt32(); // length

                        if (groupFormat != 0)
                        {
                            file.ReadInt32(); // colors count
                            int m0C = file.ReadInt32();
                            int m10 = file.ReadInt32();

                            if (m0C != 0 || m10 != 0)
                            {
                                throw new InvalidDataException("unknown data found");
                            }
                        }

                        file.ReadInt32(); // offset

                        ((List<DatImage>)group.Images).Capacity = imageCount;

                        dat.Groups.Add(group);
                    }

                    foreach (var group in dat.Groups)
                    {
                        int imageCount = ((List<DatImage>)group.Images).Capacity;

                        for (int i = 0; i < imageCount; i++)
                        {
                            var image = new DatImage();

                            image.Format = (DatImageFormats)file.ReadInt16();
                            image.Width = file.ReadInt16();
                            image.Height = file.ReadInt16();
                            file.ReadUInt16(); // color key
                            file.ReadInt16(); // colors count
                            image.GroupId = file.ReadInt16();
                            image.ImageId = file.ReadInt16();

                            int dataLength = file.ReadInt32();

                            if (dataLength < 0x2C)
                            {
                                throw new InvalidDataException("image header not found");
                            }

                            file.BaseStream.Position += 0x18;
                            image.OffsetX = file.ReadInt32();
                            image.OffsetY = file.ReadInt32();
                            file.BaseStream.Position += 0x08;
                            image.ColorsCount = (short)file.ReadInt32();

                            image.rawData = file.ReadBytes(dataLength - 0x2C);

                            group.Images.Add(image);
                        }
                    }
                }
            }
            finally
            {
                if (filestream != null)
                {
                    filestream.Dispose();
                }
            }

            return dat;
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Reviewed")]
        public void Save(string fileName)
        {
            FileStream filestream = null;

            try
            {
                filestream = new FileStream(fileName, FileMode.Create, FileAccess.Write);

                using (BinaryWriter file = new BinaryWriter(filestream, Encoding.ASCII))
                {
                    filestream = null;

                    file.Write(DatFile.Signature);
                    file.Write((short)1);

                    file.Write((short)this.Groups.Count);
                    file.Write((short)this.Groups.Sum(t => t.Images.Count));
                    file.Write(this.Groups.Sum(t => t.Images.Sum(i => 0x12 + 0x2C + (i.rawData == null ? 0 : i.rawData.Length))));
                    file.Write(this.Groups.Sum(t => t.Images.Sum(i => i.ColorsCount)));
                    file.Write(0);
                    file.Write(0);
                    file.Write(this.Groups.Count * 0x18);

                    int groupOffset = 0;

                    foreach (var group in this.Groups)
                    {
                        int groupLength = group.Images.Sum(i => 0x12 + 0x2C + (i.rawData == null ? 0 : i.rawData.Length));

                        file.Write(group.GroupId);
                        file.Write((short)group.Images.Count);
                        file.Write(groupLength);
                        file.Write(group.Images.Sum(i => i.ColorsCount));
                        file.Write(0);
                        file.Write(0);
                        file.Write(groupOffset);

                        groupOffset += groupLength;
                    }

                    foreach (var image in this.Groups.SelectMany(t => t.Images))
                    {
                        int rawDataLength = image.rawData == null ? 0 : image.rawData.Length;

                        file.Write((short)image.Format);
                        file.Write(image.Width);
                        file.Write(image.Height);
                        file.Write((ushort)0);
                        file.Write((short)0);
                        file.Write(image.GroupId);
                        file.Write(image.ImageId);
                        file.Write(0x2C + rawDataLength);

                        file.Write(0x2C + rawDataLength);
                        file.Write(0x2C);
                        file.Write(0x2C + image.ColorsCount * 3);
                        file.Write(0x2C + rawDataLength);
                        file.Write((int)image.Width);
                        file.Write((int)image.Height);
                        file.Write(image.OffsetX);
                        file.Write(image.OffsetY);
                        file.Write((int)image.Format);
                        file.Write(0x18);
                        file.Write((int)image.ColorsCount);

                        if (image.rawData != null)
                        {
                            file.Write(image.rawData);
                        }
                    }

                    this.FileName = fileName;
                }
            }
            finally
            {
                if (filestream != null)
                {
                    filestream.Dispose();
                }
            }
        }

        public void ConvertToFormat(DatImageFormats format)
        {
            switch (format)
            {
                case DatImageFormats.Format25:
                    this.ConvertToFormat25();
                    break;

                case DatImageFormats.Format24:
                    this.ConvertToFormat24();
                    break;

                case DatImageFormats.Format7:
                    this.ConvertToFormat7();
                    break;

                case DatImageFormats.Format23:
                    this.ConvertToFormat23();
                    break;

                default:
                    throw new ArgumentOutOfRangeException("format");
            }
        }

        public void ConvertToFormat25()
        {
            this.Groups
                .AsParallel()
                .SelectMany(t => t.Images)
                .ForAll(t => t.ConvertToFormat25());
        }

        public void ConvertToFormat24()
        {
            this.Groups
                .AsParallel()
                .SelectMany(t => t.Images)
                .ForAll(t => t.ConvertToFormat24());
        }

        public void ConvertToFormat7()
        {
            this.Groups
                .AsParallel()
                .SelectMany(t => t.Images)
                .ForAll(t => t.ConvertToFormat7());
        }

        public void ConvertToFormat23()
        {
            this.Groups
                .AsParallel()
                .SelectMany(t => t.Images)
                .ForAll(t => t.ConvertToFormat23());
        }
    }
}
