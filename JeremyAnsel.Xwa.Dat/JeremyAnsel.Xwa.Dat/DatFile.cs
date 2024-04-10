
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

        public string FileName { get; private set; }

        public bool HasImagesData { get; private set; }

        public IList<DatGroup> Groups { get; } = new List<DatGroup>();

        public IEnumerable<DatImage> Images
        {
            get
            {
                return this.Groups.SelectMany(t => t.Images);
            }
        }

        public DatImageFormat Format
        {
            get
            {
                if (this.Groups.Count == 0)
                {
                    return (DatImageFormat)(-1);
                }

                DatImageFormat format = this.Groups[0].Format;

                if (this.Groups.Any(t => t.Format != format))
                {
                    return (DatImageFormat)(-1);
                }

                return format;
            }
        }

        public static DatFile FromFile(string fileName)
        {
            return FromFile(fileName, true);
        }

        public static DatFile FromFile(string fileName, bool includeImagesData)
        {
            using var filestream = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            DatFile dat = FromStream(filestream, includeImagesData);
            dat.FileName = fileName;

            return dat;
        }

        public static DatFile FromStream(Stream stream)
        {
            return FromStream(stream, true);
        }

        [SuppressMessage("Style", "IDE0017:Simplifier l'initialisation des objets", Justification = "Reviewed.")]
        public static DatFile FromStream(Stream stream, bool includeImagesData)
        {
            var dat = new DatFile();

            dat.HasImagesData = includeImagesData;

            using var file = new BinaryReader(stream, Encoding.ASCII, true);

            long signature = file.ReadInt64();

            if (signature != DatFile.Signature)
            {
                throw new InvalidDataException();
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
                        throw new InvalidDataException();
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

                    image.Format = (DatImageFormat)file.ReadInt16();
                    image.Width = file.ReadInt16();
                    image.Height = file.ReadInt16();
                    file.ReadUInt16(); // color key
                    file.ReadInt16(); // colors count
                    image.GroupId = file.ReadInt16();
                    image.ImageId = file.ReadInt16();

                    int dataLength = file.ReadInt32();

                    if (dataLength < 0x2C)
                    {
                        throw new InvalidDataException();
                    }

                    file.BaseStream.Position += 0x18;
                    image.OffsetX = file.ReadInt32();
                    image.OffsetY = file.ReadInt32();
                    file.BaseStream.Position += 0x08;
                    image.ColorsCount = (short)file.ReadInt32();

                    if (image.Format == DatImageFormat.Format25)
                    {
                        switch (image.ColorsCount)
                        {
                            case 0:
                                if (dataLength - 0x2C < image.Width * image.Height * 4)
                                {
                                    image.Format = DatImageFormat.FormatBc7;
                                }

                                break;

                            case 1:
                                image.Format = DatImageFormat.Format25C;
                                image.ColorsCount = 0;
                                break;

                            case 2:
                                image.Format = DatImageFormat.FormatBc3;
                                image.ColorsCount = 0;
                                break;

                            case 3:
                                image.Format = DatImageFormat.FormatBc5;
                                image.ColorsCount = 0;
                                break;
                        }
                    }

                    if (includeImagesData)
                    {
                        image.rawData = file.ReadBytes(dataLength - 0x2C);
                    }
                    else
                    {
                        file.BaseStream.Position += dataLength - 0x2C;
                    }

                    group.Images.Add(image);
                }
            }

            return dat;
        }

        public static DatImage GetImageDataById(string fileName, short groupId, short imageId)
        {
            using var filestream = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            return GetImageDataById(filestream, groupId, imageId);
        }

        public static DatImage GetImageDataById(Stream stream, short groupId, short imageId)
        {
            using var file = new BinaryReader(stream, Encoding.ASCII, true);

            long signature = file.ReadInt64();

            if (signature != DatFile.Signature)
            {
                throw new InvalidDataException();
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

            int imagesCount = 0;

            for (int i = 0; i < count; i++)
            {
                file.ReadInt16(); // groupId
                int imageCount = file.ReadInt16();
                file.ReadInt32(); // length

                if (groupFormat != 0)
                {
                    file.ReadInt32(); // colors count
                    int m0C = file.ReadInt32();
                    int m10 = file.ReadInt32();

                    if (m0C != 0 || m10 != 0)
                    {
                        throw new InvalidDataException();
                    }
                }

                file.ReadInt32(); // offset

                imagesCount += imageCount;
            }

            for (int i = 0; i < imagesCount; i++)
            {
                var image = new DatImage();

                image.Format = (DatImageFormat)file.ReadInt16();
                image.Width = file.ReadInt16();
                image.Height = file.ReadInt16();
                file.ReadUInt16(); // color key
                file.ReadInt16(); // colors count
                image.GroupId = file.ReadInt16();
                image.ImageId = file.ReadInt16();

                int dataLength = file.ReadInt32();

                if (dataLength < 0x2C)
                {
                    throw new InvalidDataException();
                }

                file.BaseStream.Position += 0x18;
                image.OffsetX = file.ReadInt32();
                image.OffsetY = file.ReadInt32();
                file.BaseStream.Position += 0x08;
                image.ColorsCount = (short)file.ReadInt32();

                if (image.Format == DatImageFormat.Format25)
                {
                    switch (image.ColorsCount)
                    {
                        case 0:
                            if (dataLength - 0x2C < image.Width * image.Height * 4)
                            {
                                image.Format = DatImageFormat.FormatBc7;
                            }

                            break;

                        case 1:
                            image.Format = DatImageFormat.Format25C;
                            image.ColorsCount = 0;
                            break;

                        case 2:
                            image.Format = DatImageFormat.FormatBc3;
                            image.ColorsCount = 0;
                            break;

                        case 3:
                            image.Format = DatImageFormat.FormatBc5;
                            image.ColorsCount = 0;
                            break;
                    }
                }

                if (image.GroupId != groupId || image.ImageId != imageId)
                {
                    file.BaseStream.Position += dataLength - 0x2C;
                    continue;
                }

                image.rawData = file.ReadBytes(dataLength - 0x2C);
                return image;
            }

            return null;
        }

        public void Save(string fileName)
        {
            using var filestream = new FileStream(fileName, FileMode.Create, FileAccess.Write);

            this.Save(filestream);
            this.FileName = fileName;
        }

        public void Save(Stream stream)
        {
            using var file = new BinaryWriter(stream, Encoding.ASCII, true);

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

                DatImageFormat format;
                short colorsCount;

                switch (image.Format)
                {
                    case DatImageFormat.Format25C:
                        format = DatImageFormat.Format25;
                        colorsCount = 1;
                        break;

                    case DatImageFormat.FormatBc7:
                        format = DatImageFormat.Format25;
                        colorsCount = 0;
                        break;

                    case DatImageFormat.FormatBc3:
                        format = DatImageFormat.Format25;
                        colorsCount = 2;
                        break;

                    case DatImageFormat.FormatBc5:
                        format = DatImageFormat.Format25;
                        colorsCount = 3;
                        break;

                    default:
                        format = image.Format;
                        colorsCount = image.ColorsCount;
                        break;
                }

                file.Write((short)format);
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
                file.Write((int)format);
                file.Write(0x18);
                file.Write((int)colorsCount);

                if (image.rawData != null)
                {
                    file.Write(image.rawData);
                }
            }
        }

        public DatGroup GetGroupById(short groupId)
        {
            foreach (var group in this.Groups)
            {
                if (group.GroupId == groupId)
                {
                    return group;
                }
            }

            return null;
        }

        public DatImage GetImageById(short groupId, short imageId)
        {
            DatGroup group = this.GetGroupById(groupId);

            if (group == null)
            {
                return null;
            }

            foreach (var image in group.Images)
            {
                if (image.ImageId == imageId)
                {
                    return image;
                }
            }

            return null;
        }

        public void ConvertToFormat(DatImageFormat format)
        {
            switch (format)
            {
                case DatImageFormat.Format25:
                    this.ConvertToFormat25();
                    break;

                case DatImageFormat.Format25C:
                    this.ConvertToFormat25Compressed();
                    break;

                case DatImageFormat.FormatBc7:
                    this.ConvertToFormatBc7();
                    break;

                case DatImageFormat.FormatBc3:
                    this.ConvertToFormatBc3();
                    break;

                case DatImageFormat.FormatBc5:
                    this.ConvertToFormatBc5();
                    break;

                case DatImageFormat.Format24:
                    this.ConvertToFormat24();
                    break;

                case DatImageFormat.Format7:
                    this.ConvertToFormat7();
                    break;

                case DatImageFormat.Format23:
                    this.ConvertToFormat23();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(format));
            }
        }

        public void ConvertToFormat25()
        {
            this.Groups
                .AsParallel()
                .SelectMany(t => t.Images)
                .ForAll(t => t.ConvertToFormat25());
        }

        public void ConvertToFormat25Compressed()
        {
            this.Groups
                .AsParallel()
                .SelectMany(t => t.Images)
                .ForAll(t => t.ConvertToFormat25Compressed());
        }

        public void ConvertToFormatBc7()
        {
            this.Groups
                .AsParallel()
                .SelectMany(t => t.Images)
                .ForAll(t => t.ConvertToFormatBc7());
        }

        public void ConvertToFormatBc3()
        {
            this.Groups
                .AsParallel()
                .SelectMany(t => t.Images)
                .ForAll(t => t.ConvertToFormatBc3());
        }

        public void ConvertToFormatBc5()
        {
            this.Groups
                .AsParallel()
                .SelectMany(t => t.Images)
                .ForAll(t => t.ConvertToFormatBc5());
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

        public void FlipUpsideDown()
        {
            this.Groups
                .AsParallel()
                .SelectMany(t => t.Images)
                .ForAll(t => t.FlipUpsideDown());
        }
    }
}
