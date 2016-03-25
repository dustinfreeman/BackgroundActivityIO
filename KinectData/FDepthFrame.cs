using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using TaggedData;using KinectData;

namespace KinectData
{
    public class FDepthFrame : FImageFrame
    {
        public int rangeMode;
        public short[] depthFrameShort;

        public static bool COMPRESSION = true;

        public FDepthFrame()
        {
            Tag = Tags.depthFrame;
        }

        public static DateTime LastFPSTime;
        static int fpsIndex = 0;
        const bool verbose = false;
        public override void FPS()
        {
            fpsIndex++;
            if (fpsIndex % FPSframeInterval == 0)
            {
                TimeSpan elapsed = DateTime.Now - LastFPSTime;
                double fps = FPSframeInterval / elapsed.TotalSeconds;
                if (verbose)
                    Console.WriteLine("Depth FPS = " + fps);
                LastFPSTime = DateTime.Now;
            }
        }

        protected override void WriteElements()
        {
            base.WriteElements();

            memoryStream.Write(Tags.rangeMode);
            memoryStream.Write(sizeof(System.Int32));
            memoryStream.Write((System.Int32)rangeMode);

            memoryStream.Write(Tags.depthData);

            byte[] depthFrameByte = new byte[sizeof(short) * this.depthFrameShort.Length];
            Buffer.BlockCopy(depthFrameShort, 0, depthFrameByte, 0, 2 * this.depthFrameShort.Length);

            if (COMPRESSION)
            {
                byte[] comp_depthFrameByte = new byte[4 * this.depthFrameShort.Length]; //LZF requests the buffer should be slightly larger.
                int compDepthLength = LZF.Compress(depthFrameByte, 2 * this.depthFrameShort.Length, comp_depthFrameByte, 4 * this.depthFrameShort.Length);

                memoryStream.Write(compDepthLength);
                memoryStream.Write(comp_depthFrameByte, 0, compDepthLength);
            }
            else
            {
                memoryStream.Write(depthFrameByte.Length);
                memoryStream.Write(depthFrameByte);
            }

            FPS();
        }

        public static Int32Rect imageCrop = new Int32Rect(93, 66, 433, 386);
        public static byte DepthThresholdHigh = 120;
        public static byte DepthThresholdLow = 40;
        public void SaveToImageFile(string path)
        {
            SaveToImageFile(path, true);
        }
        public virtual void SaveToImageFile(string path, bool crop)
        {
            byte[] asImage = KinectUtilities.ConvertDepthFrame(depthFrameShort, DepthThresholdLow, DepthThresholdHigh);

            BitmapSource imageBS = BitmapSource.Create(
                this.Width,
                this.Height,
                96,
                96,
                PixelFormats.Bgr32,
                BitmapPalettes.Halftone256,
                asImage,
                this.Width * 4); //4 is hard-coded colour stride.

            if (crop)
                imageBS = new CroppedBitmap(imageBS, imageCrop);

            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(imageBS));

            FileStream imgStream = new FileStream(path, FileMode.Create);
            encoder.Save(imgStream);

            imgStream.Close();
        }

        protected const int tooNearDepth = 0, tooFarDepth = 4095, unknownDepth = -1;
        public double DiffWithFrame(FDepthFrame other)
        {
            //returns a distance metric.

            double diff = 0;
            for (int i = 0; i < depthFrameShort.Length; i++)
            {
                int realDepth = this.depthFrameShort[i] >> 3;// DepthImageFrame.PlayerIndexBitmaskWidth;
                int otherRealDepth = other.depthFrameShort[i] >> 3;//DepthImageFrame.PlayerIndexBitmaskWidth;

                //filter out irrelevant
                if (realDepth == tooNearDepth ||
                    realDepth == tooFarDepth ||
                    realDepth == unknownDepth ||
                    otherRealDepth == tooNearDepth ||
                    otherRealDepth == tooFarDepth ||
                    otherRealDepth == unknownDepth)
                    continue;

                diff += Math.Abs(realDepth - otherRealDepth);
            }
            return diff/this.depthFrameShort.Length;
        }

        protected override bool ReadTag(char[] tag, Stream readStream)
        {
            //check each tag.
            int byteLength;

            if (DataUtils.ArraysEqual<char>(tag, Tags.rangeMode))
            {
                //length known - skip over.
                readStream.ReadInt32();

                this.rangeMode = readStream.ReadInt32();
                return true;
            }

            if (DataUtils.ArraysEqual<char>(tag, Tags.depthData))
            {
                byteLength = readStream.ReadInt32();

                byte[] comp_depthFrameByte = new byte[Width * Height * sizeof(short) * 4]; //leaving some extra space.
                readStream.Read(comp_depthFrameByte,0,byteLength);

                byte[] depthFrameByte = new byte[Width * Height * sizeof(short)];
                LZF.Decompress(comp_depthFrameByte, byteLength, depthFrameByte, depthFrameByte.Length);

                depthFrameShort = new short[Width * Height];
                Buffer.BlockCopy(depthFrameByte, 0, depthFrameShort, 0, Width * Height * sizeof(short));
                return true;
            }

            return base.ReadTag(tag, readStream);
        }

    }
}
