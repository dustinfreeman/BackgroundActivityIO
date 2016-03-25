using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using System.IO;
using TaggedData;

namespace KinectData
{
    public class KinectProcessor
    {
        //a class that takes datas from an actual Kinect sensor, and puts it into a KCaptureSession

        private KinectSensor kinect;

        private IKCaptureSession captureSession;
        public IKCaptureSession CaptureSession
        {
            get { return captureSession; }
        }

        static void InitializeKinectServices(KinectSensor sensor)
        {
            // Centralized control of the formats for Color/Depth and enabling skeletalViewer
            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            sensor.SkeletonStream.Enable();

            // Start streaming
            try
            {
                sensor.Start();
            }
            catch (IOException)
            {
                Console.WriteLine("Sensor Conflict.");
            }

            sensor.AudioSource.Start();

            Console.WriteLine("Sensor Started!");
        }

        protected virtual void kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            if (!CaptureSession.Capturing)
                return;

            try
            {
                FSkeletonFrame newSkeletonFrame = null;
                using (SkeletonFrame frame = e.OpenSkeletonFrame())
                {
                    if (frame != null)
                    {
                        newSkeletonFrame = FrameConverter.CreateFrame(frame, kinect);
                        frame.Dispose(); //recommended by SDK documentation
                    }
                    else
                    {
                        Console.WriteLine("got null skeleton frame");
                    }
                }

                if (newSkeletonFrame != null)
                {
                    captureSession.AddFrame(newSkeletonFrame);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex);
            }

        }

        protected virtual void kinect_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            if (!CaptureSession.Capturing)
                return;

            try
            {
                FDepthFrame newDepthFrame = null;
                using (DepthImageFrame imageFrame = e.OpenDepthImageFrame())
                {
                    if (imageFrame != null)
                    {
                        newDepthFrame = FrameConverter.CreateFrame(imageFrame, kinect);
                        imageFrame.Dispose(); //recommended by SDK documentation
                    }
                    else
                    {
                        Console.WriteLine("got null depth frame");
                    }
                }

                if (newDepthFrame != null)
                {
                    captureSession.AddFrame(newDepthFrame);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex);
            }
        }

        protected virtual void kinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            if (!CaptureSession.Capturing)
                return; 
            
            try
            {
                FColourFrame newColourFrame = null;
                using (ColorImageFrame imageFrame = e.OpenColorImageFrame())
                {
                    if (imageFrame != null)
                    {
                        newColourFrame = FrameConverter.CreateFrame(imageFrame);
                        imageFrame.Dispose(); //recommended by SDK documentation
                    }
                    else
                    {
                        Console.WriteLine("got null colourframe");
                    }
                }

                if (newColourFrame != null)
                {
                    captureSession.AddFrame(newColourFrame);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex);
            }
        }

        public KinectProcessor(IKCaptureSession given_CaptureSession = null)
        {
            InitializeKinectServices(KinectSensor.KinectSensors[0]);

            foreach (KinectSensor kinect in KinectSensor.KinectSensors)
            {
                this.kinect = kinect;
                if (given_CaptureSession == null)
                {
                    captureSession = new KCaptureSession();
                }
                else
                {
                    captureSession = given_CaptureSession;
                }
                captureSession.SetKinect(kinect);

                //initial stuff.
                DepthMap depthMap = FrameConverter.CreateDepthMap(kinect);
                captureSession.AddFrame(depthMap, true);

                //hook up events
                kinect.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(kinect_ColorFrameReady);
                kinect.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(kinect_DepthFrameReady);
                kinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinect_SkeletonFrameReady);

                break; //HACK only one kinect.
            }
        }
        
    }
}
