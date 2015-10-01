
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
            this.Images = new List<DatImage>();
        }

        public DatGroup(short groupId)
            : this()
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

        public IList<DatImage> Images { get; private set; }

        public void ConvertToType(DatImageFormats format)
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
            this.Images
                .AsParallel()
                .ForAll(t => t.ConvertToFormat25());
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
