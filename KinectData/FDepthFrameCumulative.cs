using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;

namespace KinectData
{
    public class FDepthFrameCumulative :FDepthFrame
    {
        //allows creative averaging.
        int[] counts;
        int[] ids; //saves topmost id's
        int[,] countCube;
        const double BIN_DEPTH = 10; //mm
        const int NUM_DEPTH_BINS = (int)(tooFarDepth / BIN_DEPTH);

        public FDepthFrameCumulative()
            : base()
        {
            this.Width = 640;
            this.Height = 480;
            this.depthFrameShort = new short[this.Width * this.Height];
            counts = new int[this.Width * this.Height];
            ids = new int[this.Width * this.Height];
            countCube = new int[this.Width * this.Height, NUM_DEPTH_BINS];
        }

        public FDepthFrameCumulative(FDepthFrame background) 
            : this()
        {
            this.AddBackgroundData(background);
        }

        public void AddToAverage(FDepthFrame other)
        {
            for (int i = 0; i < other.depthFrameShort.Length; i++)
            {
                int otherRealDepth = other.depthFrameShort[i] >> 3;//DepthImageFrame.PlayerIndexBitmaskWidth;
                int otherPlayer = other.depthFrameShort[i] & 7;//DepthImageFrame.PlayerIndexBitmask;

                if (otherRealDepth == tooNearDepth ||
                    otherRealDepth == tooFarDepth ||
                    otherRealDepth == unknownDepth)
                    continue; //invalid pixel to merge.

                if (counts[i] == 0)
                    this.depthFrameShort[i] = other.depthFrameShort[i]; //exact

                int thisRealDepth = this.depthFrameShort[i] >> 3;//DepthImageFrame.PlayerIndexBitmaskWidth;
                int thisPlayer = this.depthFrameShort[i] & 7;// DepthImageFrame.PlayerIndexBitmask;

                int newRealDepth = (int)(thisRealDepth * (counts[i]) / (double)(counts[i] + 1) + otherRealDepth / (double)(counts[i] + 1));
                int newPlayer = otherPlayer | thisPlayer; //bitwise OR.

                this.depthFrameShort[i] = (short)(newRealDepth << 3 /*DepthImageFrame.PlayerIndexBitmaskWidth*/ );// + newPlayer);

                counts[i]++;
            }
        }

        public void AddToExtrema(FDepthFrame other)
        {
            for (int i = 0; i < other.depthFrameShort.Length; i++)
            {
                int otherRealDepth = other.depthFrameShort[i] >> 3;//DepthImageFrame.PlayerIndexBitmaskWidth;
                int otherPlayer = other.depthFrameShort[i] & 7;//DepthImageFrame.PlayerIndexBitmask;

                if (otherRealDepth == tooNearDepth ||
                    otherRealDepth == tooFarDepth ||
                    otherRealDepth == unknownDepth)
                    continue; //invalid pixel to merge.

                int thisRealDepth = this.depthFrameShort[i] >> 3;//DepthImageFrame.PlayerIndexBitmaskWidth;
                int thisPlayer = this.depthFrameShort[i] & 7;// DepthImageFrame.PlayerIndexBitmask;

                //ignore player id for now.

                //choose the higher depth. 
                //does this run into trouble with spurious super-close values?
                if (thisRealDepth == 0 || otherRealDepth < thisRealDepth)
                    this.depthFrameShort[i] = (short)(otherRealDepth << 3 /*DepthImageFrame.PlayerIndexBitmaskWidth*/ );// + newPlayer);
            }
        }

        //background over-rides objects that are up to a value in front.
        //40 mm used for Nonmoving images.
        const int BG_FRONT_THRESHOLD = 60; //mm
        public void FillInBackground(FDepthFrame background)
        {
            if (background == null)
                return;

            //fills in background as a specific "player id" (colour), overwriting only things behind it.
            byte BackgroundPlayerId = 1;

            for (int i = 0; i < depthFrameShort.Length; i++)
            {
                int realDepth = this.depthFrameShort[i] >> 3;//DepthImageFrame.PlayerIndexBitmaskWidth;
                int bgRealDepth = background.depthFrameShort[i] >> 3;// DepthImageFrame.PlayerIndexBitmaskWidth;
                //above values in mm.

                if (bgRealDepth == tooNearDepth || bgRealDepth == tooFarDepth || bgRealDepth == unknownDepth)
                    continue; //background invalid.

                //if not filled, fill in.
                bool fillIn = false;

                if (realDepth == tooNearDepth ||
                    realDepth == tooFarDepth ||
                    realDepth == unknownDepth
                    )
                    fillIn = true;
                else if (bgRealDepth - BG_FRONT_THRESHOLD < realDepth)
                {
                    fillIn = true;
                }

                if (fillIn)
                {
                    this.depthFrameShort[i] = (short)(background.depthFrameShort[i] + BackgroundPlayerId);
                }
            }
        }

        #region Frequency Count
        FDepthFrame background = null;
        public void AddBackgroundData(FDepthFrame background)
        {
            this.background = background;
        }

        public void AddToFrequencyCount(FDepthFrame other)
        {
            AddToFrequencyCount(other, 0);
        }
        public void AddToFrequencyCount(FDepthFrame other, int otherId)
        {
            //For each pixel, if there is something present and above the background, add it to counts

            for (int i = 0; i < other.depthFrameShort.Length; i++)
            {
                int otherRealDepth = other.depthFrameShort[i] >> 3;//DepthImageFrame.PlayerIndexBitmaskWidth;
                int otherPlayer = other.depthFrameShort[i] & 7;//DepthImageFrame.PlayerIndexBitmask;

                if (otherRealDepth == tooNearDepth ||
                    otherRealDepth == tooFarDepth ||
                    otherRealDepth == unknownDepth)
                    continue; //invalid pixel to merge.

                int backgroundRealDepth = 0;
                int backgroundPlayer = 0;
                if (background != null)
                {
                    backgroundRealDepth = background.depthFrameShort[i] >> 3;//DepthImageFrame.PlayerIndexBitmaskWidth;
                    backgroundPlayer = background.depthFrameShort[i] & 7;// DepthImageFrame.PlayerIndexBitmask;
                }

                if (background == null ||
                    backgroundRealDepth == tooNearDepth ||
                    backgroundRealDepth == tooFarDepth ||
                    backgroundRealDepth == unknownDepth ||
                    backgroundRealDepth - otherRealDepth > BG_FRONT_THRESHOLD)
                {
                    counts[i]++;

                    countCube[i,(int)(otherRealDepth / BIN_DEPTH)]++;

                    //gesture id addition
                    int thisRealDepth = this.depthFrameShort[i] >> 3;//DepthImageFrame.PlayerIndexBitmaskWidth;
                    //int thisPlayer = this.depthFrameShort[i] & 7;// DepthImageFrame.PlayerIndexBitmask;
                    int thisPlayer = ids[i];// DepthImageFrame.PlayerIndexBitmask;

                    if (otherId != 0 && (thisPlayer == 0 || thisRealDepth > otherRealDepth))
                    {
                        //ids mask each other out going towards the Kinect.
                        this.depthFrameShort[i] = (short)(otherRealDepth << 3 + otherId);
                        ids[i] = otherId;
                    }

                }
            }
        }

        private double ImageColourScale(int pixelInput)
        {
            return Math.Sqrt(pixelInput);
        }

        private void countsToImageFile(int[] countArray, string path, bool crop )
        {
            //saves the given count array to path.

            double maxCount = ImageColourScale(countArray.Max());
            byte[] image = new byte[this.Width * this.Height * 4];

            for (int i = 0; i < countArray.Length; i++)
            {
                //int thisPlayer = this.depthFrameShort[i] & 7;// DepthImageFrame.PlayerIndexBitmask;
                int thisPlayer = this.ids[i];// DepthImageFrame.PlayerIndexBitmask;

                if (countArray[i] >= 0)
                {
                    byte val = (byte)(ImageColourScale(countArray[i]) / maxCount * 255);
                    if ((thisPlayer & 1) != 0 || thisPlayer == 0)
                        image[4 * i + 0] = val;
                    if ((thisPlayer & 2) != 0 || thisPlayer == 0)
                        image[4 * i + 1] = val;
                    if ((thisPlayer & 4) != 0 || thisPlayer == 0)
                        image[4 * i + 2] = val;
                }
                else
                {
                    //negative
                    image[4 * i + 0] = 40;
                }

                image[4 * i + 3] = 255;
            }

            BitmapSource imageBS = BitmapSource.Create(
                this.Width,
                this.Height,
                96,
                96,
                PixelFormats.Bgr32,
                BitmapPalettes.Halftone256,
                image,
                this.Width * 4); //4 is hard-coded colour stride.

            if (crop)
                imageBS = new CroppedBitmap(imageBS, imageCrop);

            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(imageBS));

            FileStream imgStream = new FileStream(path, FileMode.Create);
            encoder.Save(imgStream);

            imgStream.Close();
        }

        public void SaveCountsToImageFile(string path, bool crop)
        {
            countsToImageFile(counts, path, crop);
        }

        public void SaveCubeCountsToImageFile(string path, bool crop)
        {
            //integrate the count cube!
            int[] integratedCounts = new int[counts.Length];
            for (int i = 0; i < integratedCounts.Length; i++)
            {
                for (int depth = 0; depth < NUM_DEPTH_BINS; depth++)
                {
                    integratedCounts[i] += countCube[i,depth];
                }
            }
            countsToImageFile(integratedCounts, path, crop);
        }

        public void SaveCubeCountsToFile(string path)
        {
            //outputs the cube counts to file in CSV format.
            StreamWriter fileStream = new StreamWriter(path);
            for (int i = 0; i < 640*480 - 1; i++)
            {
                for (int depth = 0; depth < NUM_DEPTH_BINS; depth++)
                {
                    fileStream.Write(countCube[i, depth] + ",");
                }
                fileStream.WriteLine();
            }
            fileStream.Close();
        }

        #endregion

        public static FDepthFrameCumulative operator -(FDepthFrameCumulative d1, FDepthFrameCumulative d2)
        {
            //only works on counts currently
            //normalizes by the size of largest bin.
            // subtracts to zero.
            FDepthFrameCumulative d = new FDepthFrameCumulative(d1.background);
            double d1Max = d1.counts.Max();
            double d2Max = d2.counts.Max();

            double d1CubeMax = d1.countCube.Cast<int>().Max();
            double d2CubeMax = d2.countCube.Cast<int>().Max();

            for (int i = 0; i < d.counts.Length; i++)
            {
                d.counts[i] = (int)(d1.counts[i] - d2.counts[i]*(d1Max/d2Max));
                d.ids[i] = d1.ids[i]; //take only d1 ids.

                for (int depth = 0; depth < NUM_DEPTH_BINS; depth++)
                {
                    d.countCube[i,depth] = (int)( d1.countCube[i,depth] - d2.countCube[i,depth]*(d1CubeMax/d2CubeMax) );

                    if (d.countCube[i, depth] < 0)
                        d.countCube[i, depth] = 0;
                }
            }

            return d;
        }
    }
}
