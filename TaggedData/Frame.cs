/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
//above for Bitmap PNG compression. Depends on PresentationCore, which I don't like.

namespace MSKinectData
{ 
    //Once used by Improv Remix.
    //    MAY IT RISE AGAIN ONE DAY.
    
    
    /*
    public class Frame : ICloneable
    {
        //only Frame used by Improv Remix. Will likely be phased out.
        public delegate void WriteToFileDelegate(string RecordingPath);

        #region Static Settings
        public static bool FILE_RGB_COMPRESSION = true;
        public static bool FILE_DEPTH_COMPRESSION = true;
        public static bool COMPUTE_COLOUR_DEPTH = false;

        #endregion

        #region Members
        protected long timeStamp;
        public long TimeStamp
        { get { return timeStamp; } }

        protected byte[] colourFrame;
        public byte[] ColourFrame
        {
            get { return colourFrame; }
        }
        protected short[] depthFrame;
        public short[] DepthFrame
        {
            get { return depthFrame; }
        }

        //depthFrame32 holds the depth frame processed for colour display, 
        // with grayscale depth and highlighted people.
        // in practice, not often processed. May be null.
        protected byte[] depthFrame32;
        public byte[] DepthFrame32
        {
            get { return depthFrame32; }
        }

        protected Skeleton[] skeletonData = new Skeleton[6];
        public Skeleton[] SkeletonData
        {
            get { return skeletonData; }
        }
        #endregion

        public Frame()
        {

        }

        public Frame(long timeStamp, byte[] colourFrame, short[] depthFrame, Skeleton[] skeletonData, byte[] depthFrame32 = null)
        {
            this.timeStamp = timeStamp;
            this.colourFrame = new byte[colourFrame.Length];
            this.depthFrame = new short[depthFrame.Length];

            colourFrame.CopyTo(this.colourFrame, 0);
            depthFrame.CopyTo(this.depthFrame, 0);
            skeletonData.CopyTo(this.skeletonData, 0);

            if (depthFrame32 != null)
            {
                this.depthFrame32 = new byte[depthFrame32.Length];
                depthFrame32.CopyTo(this.depthFrame32, 0);
            }

        }
        public object Clone() //does the required typecast cause a performance hit?
        {
            return this.MemberwiseClone();
        }

        #region Utilities

        private Nullable<bool> hasPeoplePixels = null;
        public bool HasPeoplePixels
        {
            get
            {
                if (hasPeoplePixels == null)
                    CheckDepthForPeople();
                return (bool)hasPeoplePixels;
            }
        }

        void CheckDepthForPeople()
        {
            for (int i = 0; i < depthFrame.Length; i++)
            {
                int player = depthFrame[i] & 0x07;
                if (player != 0)
                {
                    hasPeoplePixels = true;
                    return;
                }
            }

            hasPeoplePixels = false;
        }

        #endregion

        #region File I/O
        public void WriteToFileAsync(string path)
        {
            //async file write call

            WriteToFileDelegate caller = new WriteToFileDelegate(this.WriteToFile);

            caller.BeginInvoke(path, null, null);
        }

        public void WriteToFile(string path)
        {
            if (FILE_RGB_COMPRESSION)
                _WriteToFileCompressed(path);
            else
                _WriteToFile(path);
        }

        private void _WriteToFile(string path)
        {
            string filename = path + ".rmx";

            //prepare colour buffer with alpha removed.
            byte[] smallcolourFrame = new byte[3 * colourFrame.Length / 4];
            for (int i = 0; i < colourFrame.Length / 4; i++)
            {
                smallcolourFrame[3 * i + 0] = colourFrame[4 * i + 0];
                smallcolourFrame[3 * i + 1] = colourFrame[4 * i + 1];
                smallcolourFrame[3 * i + 2] = colourFrame[4 * i + 2];
            }

            FileStream stream = new FileStream(filename, FileMode.Create);
            using (BinaryWriter writer = new BinaryWriter(stream))
            {

                writer.Write(timeStamp);

                //Depth Size: 300 KB (recorded at 320 x 240)
                writer.Write(depthFrame.Length);
                for (int i = 0; i < depthFrame.Length; i++)
                {
                    writer.Write(depthFrame[i]);
                }
                //writer.Write(depthFrame[i], 0, depthFrame.Length);

                //Colour Size: 901 KB
                writer.Write(colourFrame.Length);
                writer.Write(smallcolourFrame);

                //Skeleton Size: 2KB
                if (skeletonData == null)
                {
                    writer.Write(0);
                    return;
                }
                else
                {
                    writer.Write(1);
                }
                writer.Write(skeletonData.Length);
                for (int s = 0; s < skeletonData.Length; s++)
                {
                    writer.Write(skeletonData[s].TrackingId);
                    writer.Write((int)skeletonData[s].TrackingState);
                    writer.Write(skeletonData[s].Position);

                    writer.Write(skeletonData[s].Joints.Count);
                    foreach (Joint joint in skeletonData[s].Joints)
                    {
                        writer.Write((int)joint.JointType);
                        writer.Write((int)joint.TrackingState);
                        writer.Write(joint.Position);
                    }
                }
            }
            stream.Close();
        }

        private void _WriteToFileCompressed(string path)
        {
            string filename = path + ".rmx";

            BitmapSource image = BitmapSource.Create(
                640,
                480,
                96,
                96,
                PixelFormats.Bgr32,
                BitmapPalettes.Halftone256,
                colourFrame,
                640 * 4); //???

            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = 80;
            encoder.Frames.Add(BitmapFrame.Create(image));

            FileStream stream = new FileStream(filename, FileMode.Create);
            FileStream img_stream = new FileStream(path + ".jpg", FileMode.Create);
            using (BinaryWriter writer = new BinaryWriter(stream))
            {

                writer.Write(timeStamp);

                if (FILE_DEPTH_COMPRESSION)
                {
                    byte[] depthFrameByte = new byte[2 * depthFrame.Length];
                    //for (int i = 0; i < depthFrame.Length; i++)
                    //{
                    //    //LSB first.
                    //    depthFrameByte[2 * i + 0] = (byte)(depthFrame[i] & 255);
                    //    depthFrameByte[2 * i + 1] = (byte)(depthFrame[i] >> 8);
                    //}

                    Buffer.BlockCopy(depthFrame, 0, depthFrameByte, 0, 2 * depthFrame.Length);

                    byte[] comp_depthFrameByte = new byte[4 * depthFrame.Length]; //LZF requests the buffer should be slightly larger.
                    int compDepthLength = LZF.Compress(depthFrameByte, 2 * depthFrame.Length, comp_depthFrameByte, 4 * depthFrame.Length);

                    writer.Write(compDepthLength);
                    writer.Write(comp_depthFrameByte, 0, compDepthLength);

                }
                else
                {
                    writer.Write(depthFrame.Length);
                    for (int i = 0; i < depthFrame.Length; i++)
                    {
                        writer.Write(depthFrame[i]);
                    }
                    //writer.Write(depthFrame[i], 0, depthFrame.Length);
                }

                //Colour Size: 901 KB
                writer.Write(colourFrame.Length);
                //encoder.Save(stream); //sketchy reference to stream?
                encoder.Save(img_stream);

                //Skeleton Size: 2KB
                if (skeletonData == null)
                {
                    writer.Write(0);
                    return;
                }
                else
                {
                    writer.Write(1);
                }
                if (skeletonData[0] == null)
                {
                    writer.Write(0);
                }
                else
                {
                    writer.Write(skeletonData.Length);
                    for (int s = 0; s < skeletonData.Length; s++)
                    {
                        writer.Write(skeletonData[s].TrackingId);
                        writer.Write((int)skeletonData[s].TrackingState);
                        writer.Write(skeletonData[s].Position);

                        writer.Write(skeletonData[s].Joints.Count);
                        foreach (Joint joint in skeletonData[s].Joints)
                        {
                            writer.Write((int)joint.JointType);
                            writer.Write((int)joint.TrackingState);
                            writer.Write(joint.Position);
                        }
                    }
                }

                //Audio
                //audio length
                writer.Write(0);
            }
            stream.Close();
        }

        public void LoadOnlyTimestamp(string path)
        {
            Load(path, true);
        }

        public bool Load(string path, bool OnlyTimestamp = false, bool GetColour = true)
        {
            bool retValue;
            try
            {
                retValue = LoadFromFile(path, OnlyTimestamp, GetColour);

                if (!OnlyTimestamp)
                    Uncompress();
            }
            catch (Exception e)
            {
                //do nothing, will be marked as not complete.
                //depthFrame = null;
                //colourFrame = null;
                //skeletonFrame = null;
                Console.WriteLine("Could not load from " + path + " Exception: " + e);
                return false;
            }

            if (depthFrame != null && COMPUTE_COLOUR_DEPTH)
                depthFrame32 = KinectUtilities.ConvertDepthFrame(depthFrame);
            return retValue;
        }

        byte[] comp_depthFrameByte = null;
        byte[] comp_colourFrame = null;
        private int _LZF_space_scaling = 2; //make the arrays slightly larger for space.

        public bool LoadFromFile(string path, bool OnlyTimestamp, bool GetColour)
        {
            //loads data into frame object, but does not yet decompress.

            string filename = path + ".rmx";


            FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);

            using (BinaryReader reader = new BinaryReader(stream))
            {
                timeStamp = reader.ReadInt64();

                if (OnlyTimestamp)
                    return true;

                //read depth
                if (FILE_DEPTH_COMPRESSION)
                {
                    //read compressed data off disk.
                    int compDepthLength = reader.ReadInt32();
                    comp_depthFrameByte = new byte[_LZF_space_scaling * compDepthLength]; //made bigger because maybe LZF needs it?
                    reader.Read(comp_depthFrameByte, 0, compDepthLength);
                }
                else
                {
                    int depthLength = reader.ReadInt32();
                    depthFrame = new short[depthLength];
                    for (int i = 0; i < depthLength; i++)
                    {
                        depthFrame[i] = reader.ReadInt16();
                    }
                }

                //read colour
                int colourLength = reader.ReadInt32();
                if (GetColour)
                {

                    if (FILE_RGB_COMPRESSION)
                    {
                        FileStream imageStreamSource = new FileStream(path + ".jpg", FileMode.Open, FileAccess.Read, FileShare.Read);

                        //comp_colourFrame = new byte[imageStreamSource.Length];
                        //imageStreamSource.Read(comp_colourFrame, 0, (int)imageStreamSource.Length);

                        JpegBitmapDecoder decoder = new JpegBitmapDecoder(imageStreamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                        BitmapSource bitmapSource = decoder.Frames[0];

                        colourFrame = new byte[640 * 480 * 4];
                        //I don't know why the following is required. something stupid is happening.
                        byte[] squishedColourFrame = new byte[640 * 480 * 4];
                        bitmapSource.CopyPixels(squishedColourFrame, 640 * 4, 0);

                        for (int y = 0; y < 480; y++)
                        {
                            for (int x = 0; x < 640; x++)
                            {
                                for (int c = 0; c < 3; c++)
                                {
                                    colourFrame[(4 * x + 4 * 640 * y) + c] = squishedColourFrame[(3 * x + 4 * 640 * y) + c];
                                }
                            }
                        }

                    }
                    else
                    {
                        //batch read
                        //translate read bits into colourFrame array.
                        colourFrame = new byte[colourLength];

                        byte[] smallColourFrame = new byte[3 * colourLength / 4];
                        reader.Read(smallColourFrame, 0, 3 * colourLength / 4);
                        for (int i = 0; i < colourLength / 4; i++)
                        {
                            colourFrame[4 * i + 0] = smallColourFrame[3 * i + 0]; //blue
                            colourFrame[4 * i + 1] = smallColourFrame[3 * i + 1]; //green
                            colourFrame[4 * i + 2] = smallColourFrame[3 * i + 2]; //red
                        }
                    }
                }

                //read skeleton
                int SkeletonPresence = reader.ReadInt32();

                if (SkeletonPresence != 0)
                {
                    int numSkeletons = reader.ReadInt32();
                    skeletonData = new Skeleton[numSkeletons];

                    for (int s = 0; s < numSkeletons; s++)
                    {
                        skeletonData[s] = new Skeleton();
                        skeletonData[s].TrackingId = reader.ReadInt32();
                        skeletonData[s].TrackingState = (SkeletonTrackingState)reader.ReadInt32();
                        skeletonData[s].Position = reader.ReadSkeletonPoint();

                        int jointCount = reader.ReadInt32();
                        for (int j = 0; j < jointCount; j++)
                        {
                            JointType jointType = (JointType)reader.ReadInt32();
                            Joint newJoint = skeletonData[s].Joints[jointType];
                            newJoint.TrackingState = (JointTrackingState)reader.ReadInt32();
                            newJoint.Position = reader.ReadSkeletonPoint();
                            skeletonData[s].Joints[jointType] = newJoint;
                        }
                    }
                }
            }

            stream.Close();

            return true;
        }

        public void Uncompress()
        {
            if (FILE_DEPTH_COMPRESSION)
            {
                //set up decompress byte buffer, then decompress.
                int depthFrameByteLength = 2 * 2 * 640 * 480;
                byte[] depthFrameByte = new byte[depthFrameByteLength]; //twice as many as shorts, times 2 for space.
                int decompLength = LZF.Decompress(comp_depthFrameByte, comp_depthFrameByte.Length / _LZF_space_scaling, depthFrameByte, depthFrameByteLength);
                //expect decompLength to 2*640*480

                //copy bytes into short array.
                depthFrame = new short[640 * 480];
                Buffer.BlockCopy(depthFrameByte, 0, depthFrame, 0, 2 * depthFrame.Length);
            }

            if (FILE_RGB_COMPRESSION)
            {
                //MemoryStream imageStreamSource = new MemoryStream();
                //imageStreamSource.Write(comp_colourFrame, 0, comp_colourFrame.Length);

                //JpegBitmapDecoder decoder = new JpegBitmapDecoder(imageStreamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                //BitmapSource bitmapSource = decoder.Frames[0];

                //colourFrame = new byte[640*480*4];
                ////I don't know why the following is required. something stupid is happening.
                //byte[] squishedColourFrame = new byte[640 * 480 * 4];
                //bitmapSource.CopyPixels(squishedColourFrame, 640 * 4, 0);

                //for (int y = 0; y < 480; y++)
                //{
                //    for (int x = 0; x < 640; x++)
                //    {
                //        for (int c = 0; c < 3; c++)
                //        {
                //            colourFrame[(4 * x + 4 * 640 * y) + c] = squishedColourFrame[(3 * x + 4 * 640 * y) + c];
                //        }
                //    }
                //}
            }

        }

        public void Unload()
        {
            //unloads all except timestamp

            //does not unload compressed stuff.

            //to save memory
            depthFrame = null;
            colourFrame = null;
            depthFrame32 = null;
            skeletonData = null;
        }
        #endregion
 
        public static SkeletonPoint ReadSkeletonPoint(this BinaryReader reader)
        {
            SkeletonPoint sp = new SkeletonPoint();
            sp.X = reader.ReadSingle();
            sp.Y = reader.ReadSingle();
            sp.Z = reader.ReadSingle();
            return sp;
        }
    }
   * 
   * 
   
}*/
