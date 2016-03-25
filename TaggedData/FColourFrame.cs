using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace TaggedData
{
    public class FColourFrame : FImageFrame
    {
        public static bool USE_COLOUR_JPEG_COMPRESSION = true; //if false, uses PNG compression.
        //this should probably be encoded or detected when reading, currently you just get an error.
        public static bool SEPARATE_IMAGE_FILES = false;
        public static int JPEG_QUALITY = 90;

        public static int BPP = 4; //4 is hard-coded colour stride.

        public int colourFrameIndex;

        public FColourFrame()
        {
            Tag = Tags.colourFrame;
        }

        public virtual BitmapSource BitmapSource
        {
            get
            {
                BitmapSource imageBS = BitmapSource.Create(
                this.Width,
                this.Height,
                96,
                96,
                PixelFormats.Bgr32,
                BitmapPalettes.Halftone256,
                this.image,
                this.Width * BPP); 
                
                return imageBS;
            }

        }

        string PathFromStream(FileStream stream)
        {
            //extracts the folder path from the stream.

            String captureFileName = stream.Name;
            int lastSlash = captureFileName.LastIndexOf("\\");
            String capturePath = captureFileName.Substring(0, lastSlash + 1);
            return capturePath;
        }

        string GetImageFilename(string path)
        {
            //returns file name without extension
            return path + "IMG_" + String.Format("{0:00000}", colourFrameIndex);// +".png";
        }

        protected override void WriteElements()
        {
            base.WriteElements();

            memoryStream.Write(Tags.colourFrameIndex);
            memoryStream.Write(sizeof(int));
            memoryStream.Write(colourFrameIndex);

            //write out to a PNG or JPEG, save the index.
            BitmapSource imageBS = this.BitmapSource;

            BitmapEncoder encoder;
            if (USE_COLOUR_JPEG_COMPRESSION)
            {
                encoder = new JpegBitmapEncoder();
                ((JpegBitmapEncoder)encoder).QualityLevel = JPEG_QUALITY;
            }
            else
            {
                encoder = new PngBitmapEncoder();
            }
            encoder.Frames.Add(BitmapFrame.Create(imageBS));

            MemoryStream imgStream = new MemoryStream();
            encoder.Save(imgStream);

            if (SEPARATE_IMAGE_FILES)
            {
                //write out to separate image.

                //get path from captureStream.Name
                String capturePath = PathFromStream(this.captureStream);
                String ImageFilename = GetImageFilename(capturePath);
                if (USE_COLOUR_JPEG_COMPRESSION)
                { ImageFilename += ".jpg"; }
                else { ImageFilename += ".png"; }

                FileStream imgFileStream = new FileStream(ImageFilename, FileMode.Create);
                imgStream.WriteTo(imgFileStream);
            }
            else
            {
                memoryStream.Write(Tags.colourFrameImage);
                memoryStream.Write((System.Int32)imgStream.Length);
                imgStream.WriteTo(memoryStream);
            }
        }

        public void WriteImageToFile(string ImageFilename)
        {
            //path should be sent without extension

            //write out to a PNG or JPEG, save the index.
            BitmapSource imageBS = this.BitmapSource;

            BitmapEncoder encoder;
            if (USE_COLOUR_JPEG_COMPRESSION)
            {
                encoder = new JpegBitmapEncoder();
                ((JpegBitmapEncoder)encoder).QualityLevel = JPEG_QUALITY;
            }
            else
            {
                encoder = new PngBitmapEncoder();
            }
            encoder.Frames.Add(BitmapFrame.Create(imageBS));

            MemoryStream imgStream = new MemoryStream();
            encoder.Save(imgStream);

            //get path from captureStream.Name
            if (USE_COLOUR_JPEG_COMPRESSION)
            { ImageFilename += ".jpg"; }
            else { ImageFilename += ".png"; }

            FileStream imgFileStream = new FileStream(ImageFilename, FileMode.Create);
            imgStream.WriteTo(imgFileStream);
            imgFileStream.Close();

            imgStream.Close();
        }

        public static DateTime LastFPSTime;
        const bool verbose = false;
        public override void FPS()
        {
            if (verbose && colourFrameIndex % FPSframeInterval == 0)
            {
                TimeSpan elapsed = DateTime.Now - LastFPSTime;
                double fps = FPSframeInterval / elapsed.TotalSeconds;
                Console.WriteLine("Colour FPS = " + fps + " index: " + colourFrameIndex);
                LastFPSTime = DateTime.Now;
            }
        }

        public static int FPScounter = 0;
        public static void staticFPS()
        {
            int frameInterval = 30;
            if (FPScounter % frameInterval == 0)
            {
                TimeSpan elapsed = DateTime.Now - LastFPSTime;
                double fps = frameInterval / elapsed.TotalSeconds;
                Console.WriteLine("Colour FPS = " + fps);
                LastFPSTime = DateTime.Now;
            }
            FPScounter++;
        }

        protected override bool ReadTag(char[] tag, Stream readStream)
        {
            if (DataUtils.ArraysEqual<char>(tag, Tags.colourFrameIndex))
            {
                //length known - skip over.
                int length = readStream.ReadInt32();

                this.colourFrameIndex = readStream.ReadInt32();
                //Console.WriteLine("int colourFrameIndex " + colourFrameIndex);

                return true;
            }

            if (DataUtils.ArraysEqual<char>(tag, Tags.colourFrameImage))
            {
                //jpeg or png encoding.
                int length = readStream.ReadInt32();

                //temporary memory stream.
                MemoryStream imgMemStream = new MemoryStream();
                readStream.CopyChunkTo(imgMemStream, length);
                imgMemStream.Seek(0, SeekOrigin.Begin);

                BitmapDecoder decoder; 
                if (USE_COLOUR_JPEG_COMPRESSION)
                    decoder = new JpegBitmapDecoder(imgMemStream,
                    BitmapCreateOptions.DelayCreation, BitmapCacheOption.Default);
                else //png
                    decoder = new PngBitmapDecoder(imgMemStream,
                    BitmapCreateOptions.DelayCreation, BitmapCacheOption.Default);

                image = LoadImage(decoder,false);

                imgMemStream.Close();
                return true;
            }

            return base.ReadTag(tag, readStream);
        }

        public override void ReadFromStream(FileStream readStream, int chunkLength)
        {
            base.ReadFromStream(readStream, chunkLength);

            if (SEPARATE_IMAGE_FILES)
            {
                //after done reading all tags, load external image

                //find file
                String capturePath = PathFromStream(readStream);
                string ImageFileName = GetImageFilename(capturePath); //without extension

                BitmapDecoder decoder;

                FileStream imageFileStream;

                if (File.Exists(ImageFileName + ".png"))
                {
                    ImageFileName += ".png";
                    imageFileStream = new FileStream(ImageFileName, FileMode.Open, FileAccess.Read);
                    decoder = new PngBitmapDecoder(imageFileStream,
                        BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                }
                else if (File.Exists(ImageFileName + ".jpg"))
                {
                    ImageFileName += ".jpg";
                    imageFileStream = new FileStream(ImageFileName, FileMode.Open, FileAccess.Read);
                    decoder = new JpegBitmapDecoder(imageFileStream,
                        BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                }
                else
                    throw new FileNotFoundException("Image not found: " + ImageFileName);

                image = LoadImage(decoder,true);

                imageFileStream.Dispose(); //prevents memory leaks?
            }
        }

        byte[] LoadImage(BitmapDecoder decoder, bool SourceIsSeparateImageFile)
        {
            BitmapSource bitmapSource = decoder.Frames[0];
            decoder.Frames[0].Freeze();
            bitmapSource.Freeze(); //added to try to fix memory leak.
            
            byte[] loadedImage = new byte[bitmapSource.PixelWidth * bitmapSource.PixelHeight * 4];

            //I don't know why the following distinction matters and it makes me uncomfortable.
            if (SourceIsSeparateImageFile)
            {
                //I don't know why the following is required. something stupid is happening.
                //has something to do with matching up 24-bit rgb vs. 32-bit rgb.

                //this spaces the image out, from 3 bpp to 4 bpp.
                byte[] squishedColourFrame = new byte[bitmapSource.PixelWidth * bitmapSource.PixelHeight * 4];
                bitmapSource.CopyPixels(squishedColourFrame, bitmapSource.PixelWidth * 4, 0);

                for (int y = 0; y < bitmapSource.PixelHeight; y++)
                {
                    for (int x = 0; x < bitmapSource.PixelWidth; x++)
                    {
                        for (int c = 0; c < 3; c++)
                        {
                            loadedImage[(4 * x + 4 * bitmapSource.PixelWidth * y) + c] = squishedColourFrame[(3 * x + 4 * bitmapSource.PixelWidth * y) + c];
                        }
                    }
                }

                //here is the in-place version of the above algorithm.
                //UNTESTED
                //for (
                //    int p1 = bitmapSource.PixelWidth * bitmapSource.PixelHeight * 3 - 1,
                //    p2 = bitmapSource.PixelWidth * bitmapSource.PixelHeight * 4 - 2; 
                //    p1 > 0; 
                //    p1--)
                //{
                //    loadedImage[p2] = loadedImage[p1];
                //    if (p1 % 4 == 0)
                //        p2--; //skip over the alpha.
                //}
            }
            else
            {
                bitmapSource.CopyPixels(loadedImage, bitmapSource.PixelWidth * 4, 0);
            }
            
            return loadedImage;
        }
    }
}
