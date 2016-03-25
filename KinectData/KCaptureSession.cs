using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using System.IO;
using TaggedData; using KinectData;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Threading;
//above for Bitmap PNG compression. This depends on PresentationCore, which I don't like.

namespace KinectData
{
    public class KCaptureSession : TaggedData.CaptureSession, IKCaptureSession
    {
        //gets sent Kinect frames by a running app, and records them.
        //This class, when given a Kinect, records frames from it as fast as possible.

        protected KinectSensor kinect;
        protected KinectSound kSound;
        public void SetKinect(KinectSensor kinect)
        {
            this.kinect = kinect;
            kSound = new KinectSound(kinect);
        }

        private bool CAPTURE_AUDIO = true;

        public KCaptureSession(KinectSensor kinect = null, string FolderPath = "./") 
            : base(null, FileMode.Create)
        {
            KinectData.InitKinectTags(); //if not done elsewhere.

            this.kinect = kinect;
            FColourFrame.LastFPSTime = DateTime.Now;
            FSkeletonFrame.LastFPSTime = DateTime.Now;

            //create .knt file.
            //HOLY SHIT WHY WAS FolderPath NEVER SET? FUCK.
            this.FolderPath = FolderPath;
            this.captureFilePath = this.FolderPath + "capture.knt";
            captureStream = new FileStream(captureFilePath, this.captureFileMode, FileAccess.ReadWrite, FileShare.Read);
        }

        public override void StartCapture()
        {
            base.StartCapture();
            
            //unique for each Kinect
            //probably should just use coloured balls in the scene.
            //kinect.MapDepthFrameToColorFrame();
            
            if (FColourFrame.USE_COLOUR_JPEG_COMPRESSION)
            {
                Console.WriteLine("ColourFrame: using JPEG compression.");
            }
            else
            {
                Console.WriteLine("ColourFrame: using PNG compression.");
            }

            if (FDepthFrame.COMPRESSION)
            {
                Console.WriteLine("DepthFrame: using compression.");
            }
            else
            {
                Console.WriteLine("DepthFrame: not using compression.");
            }

            if (ASYNC_WRITE)
            {
                Console.WriteLine("Async Write: Yes");
            }
            else
            {
                Console.WriteLine("Async Write: No");

            }

            //start sound capture.
            if (!CAPTURE_AUDIO)
            {
                Console.WriteLine("Warning: not capturing audio");
            }
            else
            {
                AudioDelegate caller = new AudioDelegate(kSound.StartRecording);
                caller.BeginInvoke(kinect, FolderPath, null, null);
            }
        }

        public override void EndCapture()
        {
            base.EndCapture();

            //wrap up sound file.
            kSound.StopRecording();
        }

        public FDepthFrame lastDepthFrame = null;
        public FSkeletonFrame lastSkeletonFrame = null;

        public override void AddFrame(TagChunk frame, bool Force = false)
        {
            base.AddFrame(frame, Force);

            if (frame is FDepthFrame)
            {
                lastDepthFrame = ((FDepthFrame)frame);
            }
            if (frame is FSkeletonFrame)
            {
                lastSkeletonFrame = ((FSkeletonFrame)frame);
            }
        }
    }
}
