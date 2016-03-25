using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TaggedData
{
    public abstract class FFrame : TagChunk
    {
        //time-tagged data inherited by ColourFrame, DepthFrame, etc.

        public static bool M_TIMESTAMP_PRIMARY = true;
        //if true, mTimestamp is primary. Data in Sept-Aug 2012 was recorded with kTimestamp primary, 
        //  with conversion of all files to mTimestamp primary in early Jan 2013.
        //  This variable should always be true.

        public long kTimestamp; //Kinect-given timestamp. Units of 0.1 ms (100 microseconds)
        public long mTimestamp; //Machine timestamp (UTC). Units of 10 ns 

        protected override void WriteElements()
        {
            base.WriteElements();

            //every frame has a timestamp.
            //timestamp is expected to be the first tag - leads to bulk reads.

            if (M_TIMESTAMP_PRIMARY)
            {
                Write_mTimestamp(memoryStream);
                Write_kTimestamp(memoryStream);
            }
            else
            {
                Write_kTimestamp(memoryStream);
                Write_mTimestamp(memoryStream);
            }
        }

        #region Timestamp write helpers
        private void Write_kTimestamp(Stream captureStream)
        {
            captureStream.Write(Tags.kTimestamp);
            captureStream.Write(sizeof(long));
            captureStream.Write(this.kTimestamp);
        }

        private void Write_mTimestamp(Stream captureStream)
        {
            captureStream.Write(Tags.mTimestamp);
            captureStream.Write(sizeof(long));
            captureStream.Write(this.mTimestamp);
        }

        #endregion

        protected override bool ReadTag(char[] tag, Stream readStream)
        {
            if (DataUtils.ArraysEqual<char>(tag, Tags.mTimestamp))
            {
                //chunk length known - skip over.
                readStream.ReadInt32();

                this.mTimestamp = readStream.ReadInt64();
                return true;
            }

            if (DataUtils.ArraysEqual<char>(tag, Tags.kTimestamp))
            {
                //chunk length known - skip over.
                readStream.ReadInt32();

                this.kTimestamp = readStream.ReadInt64();
                return true;
            }

            return base.ReadTag(tag, readStream);
        }
    }
}
