
namespace JeremyAnsel.Xwa.Dat
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class DatGroup
    {
        private short groupId;

        public DatGroup()
        {
        }

        public DatGroup(short groupId)
        {
            this.GroupId = groupId;
        }

        public short GroupId
        {
            get { return this.groupId; }
            set
            {
                this.groupId = value;

                foreach (var image in this.Images)
                {
                    image.GroupId = this.groupId;
                }
            }
        }

        public IList<DatImage> Images { get; } = new List<DatImage>();

        public DatImageFormat Format
        {
            get
            {
                if (this.Images.Count == 0)
                {
                    return (DatImageFormat)(-1);
                }

                DatImageFormat format = this.Images[0].Format;

                if (this.Images.Any(t => t.Format != format))
                {
                    return (DatImageFormat)(-1);
                }

                return format;
            }
        }

        public void ConvertToType(DatImageFormat format)
        {
            switch (format)
            {
                case DatImageFormat.Format25:
                    this.ConvertToFormat25();
                    break;

                case DatImageFormat.Format25C:
                    this.ConvertToFormat25Compressed();
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
            this.Images
                .AsParallel()
                .ForAll(t => t.ConvertToFormat25());
        }

        public void ConvertToFormat25Compressed()
        {
            this.Images
                .AsParallel()
                .ForAll(t => t.ConvertToFormat25Compressed());
        }

        public void ConvertToFormat24()
        {
            this.Images
                .AsParallel()
                .ForAll(t => t.ConvertToFormat24());
        }

        public void ConvertToFormat7()
        {
            this.Images
                .AsParallel()
                .ForAll(t => t.ConvertToFormat7());
        }

        public void ConvertToFormat23()
        {
            this.Images
                .AsParallel()
                .ForAll(t => t.ConvertToFormat23());
        }
    }
}
