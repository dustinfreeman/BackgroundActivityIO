using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Kinect;
using TaggedData;using KinectData;

namespace KinectData
{
    public class FSkeletonFrame : FFrame
    {
        //annoying dependency on the Kinect SDK:
        //easier than duplicating everything.
        public Skeleton[] SkeletonData;
        public int skeletonTrackingMode;
        public Tuple<float, float, float, float> FloorClipPlane;
        public int FrameNumber;

        public int SkeletonArrayLength
        {
            get
            {
                if (SkeletonData == null)
                    return 0;
                return SkeletonData.Length;
            }
        }

        public FSkeletonFrame()
        {
            Tag = Tags.skeletonFrame;

            //initialize Skeleton objects
            LoadingSkeletonList = new List<Skeleton>();
        }

        protected override void WriteElements()
        {
            base.WriteElements();

            memoryStream.Write(Tags.skeletonTrackingMode);
            memoryStream.Write(sizeof(System.Int32));
            memoryStream.Write((System.Int32)skeletonTrackingMode);

            memoryStream.Write(Tags.floorClipPlane);
            memoryStream.Write(4 * sizeof(float));
            memoryStream.Write(this.FloorClipPlane);

            memoryStream.Write(Tags.frameNumber);
            memoryStream.Write(sizeof(System.Int32));
            memoryStream.Write(this.FrameNumber);

            //do individual skeleton data, each wrapped in a tag.
            for (int sk = 0; sk < this.SkeletonData.Length; sk++)
            {
                MemoryStream skeletonDataStream = new MemoryStream();

                //write initial space for tag and length
                skeletonDataStream.Write(Tags.skeletonData);
                skeletonDataStream.Write((System.Int32)0);

                //skeleton data section:
                skeletonDataStream.Write(Tags.trackingID);
                skeletonDataStream.Write(sizeof(System.Int32));
                skeletonDataStream.Write(SkeletonData[sk].TrackingId);

                skeletonDataStream.Write(Tags.skeletonTrackingState);
                skeletonDataStream.Write(sizeof(System.Int32));
                skeletonDataStream.Write((System.Int32)SkeletonData[sk].TrackingState);

                skeletonDataStream.Write(Tags.position);
                skeletonDataStream.Write(3 * sizeof(float));
                skeletonDataStream.Write(SkeletonData[sk].Position);

                skeletonDataStream.Write(Tags.clippedEdges);
                skeletonDataStream.Write(sizeof(System.Int32));
                skeletonDataStream.Write((System.Int32)SkeletonData[sk].ClippedEdges);

                foreach (Joint joint in SkeletonData[sk].Joints)
                {
                    MemoryStream jointStream = new MemoryStream();

                    //write initial space for tag and length
                    jointStream.Write(Tags.joint);
                    jointStream.Write((System.Int32)0);

                    jointStream.Write(Tags.jointType);
                    jointStream.Write(sizeof(System.Int32));
                    jointStream.Write((System.Int32)joint.JointType);

                    jointStream.Write(Tags.jointTrackingState);
                    jointStream.Write(sizeof(System.Int32));
                    jointStream.Write((System.Int32)joint.TrackingState);

                    jointStream.Write(Tags.position);
                    jointStream.Write(3 * sizeof(float));
                    jointStream.Write(joint.Position);

                    //---------------------------------------
                    //write tag and length at the beginning.
                    jointStream.Seek(Tags.TAG_SIZE, SeekOrigin.Begin);
                    //subtract number of bytes representing tag and chunk length.
                    jointStream.Write((System.Int32)jointStream.Length - Tags.TAG_SIZE - sizeof(System.Int32));

                    jointStream.WriteTo(skeletonDataStream);
                }

                //NOTE: BoneOrientation data seems to be redundant with Joint data. Not recorded.

                //---------------------------------------
                //write tag and length at the beginning.
                skeletonDataStream.Seek(Tags.TAG_SIZE, SeekOrigin.Begin);
                //subtract number of bytes representing tag and chunk length.
                skeletonDataStream.Write((System.Int32)skeletonDataStream.Length - Tags.TAG_SIZE - sizeof(System.Int32));

                skeletonDataStream.WriteTo(memoryStream);
            }

            FPS();
        }

        public static DateTime LastFPSTime;
        static int fpsIndex = 0;
        static bool verbose = false;
        public override void FPS()
        {
            fpsIndex++;
            if (fpsIndex % FPSframeInterval == 0)
            {
                TimeSpan elapsed = DateTime.Now - LastFPSTime;
                double fps = FPSframeInterval / elapsed.TotalSeconds;
                if (verbose)
                    Console.WriteLine("Skeleton FPS = " + fps);
                LastFPSTime = DateTime.Now;
            }
        }

        List<Skeleton> LoadingSkeletonList;
        Joint currentJoint;
        int LoadingJointIndex = -1;
        public override void ReadFromStream(FileStream readStream, int chunkLength)
        {
            base.ReadFromStream(readStream, chunkLength);

            //load into actual variables.
            this.SkeletonData = new Skeleton[LoadingSkeletonList.Count];
            for (int sk = 0; sk < this.SkeletonArrayLength; sk++)
            {
                SkeletonData[sk] = LoadingSkeletonList.ElementAt(sk);
            }
            //HACK should we update joints -> bones. ?
        }

        protected override bool ReadTag(char[] tag, Stream readStream)
        {
            //check each tag.
            int byteLength;

            if (DataUtils.ArraysEqual<char>(tag, Tags.skeletonTrackingMode))
            {
                //length known - skip over.
                readStream.ReadInt32();

                this.skeletonTrackingMode = readStream.ReadInt32();
                return true;
            }

            if (DataUtils.ArraysEqual<char>(tag, Tags.floorClipPlane))
            {
                byteLength = readStream.ReadInt32(); //expected to be 4*sizeof(float)

                float f1 = readStream.ReadFloat32();
                float f2 = readStream.ReadFloat32();
                float f3 = readStream.ReadFloat32();
                float f4 = readStream.ReadFloat32();
                this.FloorClipPlane = new Tuple<float, float, float, float>(f1, f2, f3, f4);
                return true;
            }

            if (DataUtils.ArraysEqual<char>(tag, Tags.frameNumber))
            {
                //length known - skip over.
                readStream.ReadInt32();

                this.FrameNumber = readStream.ReadInt32();
                return true;
            }

            if (DataUtils.ArraysEqual<char>(tag, Tags.skeletonData))
            {
                //length irrelevant, there will be sub-tags.
                byteLength = readStream.ReadInt32();

                LoadingSkeletonList.Add(new Skeleton()); //push a new Skeleton onto the list.
                LoadingJointIndex = -1; //re-start joint counter
                return true;
            }

            if (DataUtils.ArraysEqual<char>(tag, Tags.trackingID))
            {
                //length known - skip over.
                readStream.ReadInt32();

                LoadingSkeletonList.Last().TrackingId = readStream.ReadInt32();
                return true;
            }

            if (DataUtils.ArraysEqual<char>(tag, Tags.skeletonTrackingState))
            {
                //length known - skip over.
                readStream.ReadInt32();

                LoadingSkeletonList.Last().TrackingState = (SkeletonTrackingState)readStream.ReadInt32();
                return true;
            }

            if (DataUtils.ArraysEqual<char>(tag, Tags.position))
            {
                //length known - skip over.
                readStream.ReadInt32();

                //HACK position tag - both skeletonData and joint contain this.
                //use LoadingJointIndex to tell if we have moved on to individual joints
                //this is a terrible hack.

                SkeletonPoint sPoint = new SkeletonPoint();
                sPoint.X = readStream.ReadFloat32();
                sPoint.Y = readStream.ReadFloat32();
                sPoint.Z = readStream.ReadFloat32();

                if (LoadingJointIndex < 0)
                {
                    LoadingSkeletonList.Last().Position = sPoint;
                }
                else
                {
                    currentJoint.Position = sPoint;
                }

                return true;
            }

            if (DataUtils.ArraysEqual<char>(tag, Tags.clippedEdges))
            {
                //length known - skip over.
                readStream.ReadInt32();

                LoadingSkeletonList.Last().ClippedEdges = (FrameEdges)readStream.ReadInt32();
                return true;
            }

            if (DataUtils.ArraysEqual<char>(tag, Tags.joint))
            {
                //length irrelevant, there will be sub-tags.
                byteLength = readStream.ReadInt32();

                if (LoadingJointIndex >= 0)
                {
                    //last joint is finished, load it into skeleton
                    LoadingSkeletonList.Last().Joints[(JointType)LoadingJointIndex] = currentJoint;
                }

                LoadingJointIndex++; //should have started at -1 with a new Skeleton
                currentJoint = LoadingSkeletonList.Last().Joints[(JointType)LoadingJointIndex];
                return true;
            }

            if (DataUtils.ArraysEqual<char>(tag, Tags.jointType))
            {
                //length known - skip over.
                readStream.ReadInt32();

                JointType jointType = (JointType)readStream.ReadInt32();
                //currentJoint.JointType = jointType; //read-only, interestingly.

                return true;
            }

            if (DataUtils.ArraysEqual<char>(tag, Tags.jointTrackingState))
            {
                //length known - skip over.
                readStream.ReadInt32();

                currentJoint.TrackingState = (JointTrackingState)readStream.ReadInt32();
                return true;
            }

            return base.ReadTag(tag, readStream);
        }

        
    }
}
