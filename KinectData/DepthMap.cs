using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TaggedData;using KinectData;

namespace KinectData
{
    public class DepthMap : TagChunk
    {
        //serves as a fake kinect.MapDepthFrameToColorFrame();

        public const double minDepthRange = 0.5;
        public const double maxDepthRange = 8.0;
        public const double depthIncrement = 0.5;

        public int depthWidth, depthHeight, colourWidth, colourHeight;
        public int[] colourCoords;

        public DepthMap()
        {
            //only for use with reading.
            Tag = Tags.depthMap;
        }

        public DepthMap(int depthWidth, int depthHeight, int colourWidth, int colourHeight, int[] colourCoords)
            :this()
        {
            this.depthWidth = depthWidth;
            this.depthHeight = depthHeight;
            this.colourWidth = colourWidth;
            this.colourHeight = colourHeight;
            this.colourCoords = colourCoords; //maybe should be a copy.

        }

        #region File Writing
        
        protected override void WriteElements()
        {
            base.WriteElements();

            memoryStream.Write(Tags.depthMapdepthWidth);
            memoryStream.Write(sizeof(System.Int32));
            memoryStream.Write(this.depthWidth);

            memoryStream.Write(Tags.depthMapdepthHeight);
            memoryStream.Write(sizeof(System.Int32));
            memoryStream.Write(this.depthHeight);

            memoryStream.Write(Tags.depthMapcolourWidth);
            memoryStream.Write(sizeof(System.Int32));
            memoryStream.Write(this.colourWidth);

            memoryStream.Write(Tags.depthMapcolourHeight);
            memoryStream.Write(sizeof(System.Int32));
            memoryStream.Write(this.colourHeight);

            //actual data
            memoryStream.Write(Tags.depthMapData);
            memoryStream.Write(sizeof(System.Int32) * colourCoords.Length);
            for (int i = 0; i < colourCoords.Length; i++ )
            {
                memoryStream.Write(colourCoords[i]);
            }
        }

        #endregion

        #region File Reading
        protected override bool ReadTag(char[] tag, Stream readStream)
        {
            //check each tag.
            int byteLength;

            if (DataUtils.ArraysEqual<char>(tag, Tags.depthMapdepthWidth))
            {
                //length known - skip over.
                readStream.ReadInt32();

                this.depthWidth = readStream.ReadInt32();
                return true;
            }

            if (DataUtils.ArraysEqual<char>(tag, Tags.depthMapdepthHeight))
            {
                //length known - skip over.
                readStream.ReadInt32();

                this.depthHeight = readStream.ReadInt32();
                return true;
            }

            if (DataUtils.ArraysEqual<char>(tag, Tags.depthMapcolourWidth))
            {
                //length known - skip over.
                readStream.ReadInt32();

                this.colourWidth = readStream.ReadInt32();
                return true;
            }

            if (DataUtils.ArraysEqual<char>(tag, Tags.depthMapcolourHeight))
            {
                //length known - skip over.
                readStream.ReadInt32();

                this.colourHeight = readStream.ReadInt32();
                return true;
            }

            if (DataUtils.ArraysEqual<char>(tag, Tags.depthMapData))
            {
                //length sanity check
                byteLength = readStream.ReadInt32();

                int intLength = byteLength / sizeof(System.Int32);
                colourCoords = new int[intLength];

                for (int i = 0; i < intLength; i++) //performance hog?
                {
                    colourCoords[i] = readStream.ReadInt32();
                }
                return true;
            }

            return base.ReadTag(tag, readStream);
        }

        protected override void SanityCheck()
        {
            base.SanityCheck();

            if (colourCoords.Length != colourWidth * colourHeight * 2)
                throw new Exception("DepthMap: length does not match width and height. " +
                        " length = " + colourCoords.Length +
                        " colourWidth = " + colourWidth +
                        " colourHeight = " + colourHeight);
        }

        #endregion
    }
}
