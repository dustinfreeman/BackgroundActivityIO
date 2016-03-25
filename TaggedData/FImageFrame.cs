using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace TaggedData
{
    public abstract class FImageFrame : FFrame
    {
        public int Width;
        public int Height;

        public byte[] image;

        protected override void WriteElements()
        {
            base.WriteElements();

            memoryStream.Write(Tags.Width);
            memoryStream.Write(sizeof(System.Int32));
            memoryStream.Write(this.Width);

            memoryStream.Write(Tags.Height);
            memoryStream.Write(sizeof(System.Int32));
            memoryStream.Write(this.Height);
        }

        protected override bool ReadTag(char[] tag, Stream readStream)
        {
            if (DataUtils.ArraysEqual<char>(tag, Tags.Width))
            {
                //length known - skip over.
                readStream.ReadInt32();

                this.Width = readStream.ReadInt32();
                return true;
            }

            if (DataUtils.ArraysEqual<char>(tag, Tags.Height))
            {
                //length known - skip over.
                readStream.ReadInt32();

                this.Height = readStream.ReadInt32();
                return true;
            }

            return base.ReadTag(tag, readStream);
        }
    }
}
