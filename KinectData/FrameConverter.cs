using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaggedData;
using KinectData;
using Microsoft.Kinect;

namespace KinectData
{
    static class FrameConverter
    {
        //converts raw data from a live Kinect (Kinect SDK format) to our KinectData.Frame inheritor classes.

        public static FColourFrame CreateFrame(ColorImageFrame imageFrame)
        {
            FColourFrame newColourFrame = new FColourFrame();
            try
            {
                newColourFrame.kTimestamp = imageFrame.Timestamp;
                DateTime Now = DateTime.Now;
                newColourFrame.mTimestamp = Now.ToFileTimeUtc();
                newColourFrame.Width = imageFrame.Width;
                newColourFrame.Height = imageFrame.Height;
                newColourFrame.image = new byte[imageFrame.PixelDataLength];
                imageFrame.CopyPixelDataTo(newColourFrame.image);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
            return newColourFrame;
        }

        public static FDepthFrame CreateFrame(DepthImageFrame imageFrame, KinectSensor kinect)
        {
            FDepthFrame newDepthFrame = new FDepthFrame();
            try
            {
                newDepthFrame.kTimestamp = imageFrame.Timestamp;
                DateTime Now = DateTime.Now;
                newDepthFrame.mTimestamp = Now.ToFileTimeUtc();
                newDepthFrame.Width = imageFrame.Width;
                newDepthFrame.Height = imageFrame.Height;
                newDepthFrame.rangeMode = (System.Int32)kinect.DepthStream.Range;
                newDepthFrame.depthFrameShort = new short[imageFrame.PixelDataLength];
                imageFrame.CopyPixelDataTo(newDepthFrame.depthFrameShort);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
            return newDepthFrame;
        }

        public static FSkeletonFrame CreateFrame(SkeletonFrame frame, KinectSensor kinect)
        {
            FSkeletonFrame newSkeletonFrame = new FSkeletonFrame();
            try
            {
                newSkeletonFrame.kTimestamp = frame.Timestamp;
                DateTime Now = DateTime.Now;
                newSkeletonFrame.mTimestamp = Now.ToFileTimeUtc();
                newSkeletonFrame.skeletonTrackingMode = (System.Int32)kinect.SkeletonStream.TrackingMode;
                newSkeletonFrame.FloorClipPlane = frame.FloorClipPlane;
                newSkeletonFrame.FrameNumber = frame.FrameNumber;

                newSkeletonFrame.SkeletonData = new Skeleton[frame.SkeletonArrayLength];
                frame.CopySkeletonDataTo(newSkeletonFrame.SkeletonData);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
            return newSkeletonFrame;
        }

        public static DepthMap CreateDepthMap(KinectSensor kinect)
        {
            short[] FarDepthMap = new short[kinect.DepthStream.FramePixelDataLength];
            //convert from m to mm, then bitshift to make Kinect Depth Image format.
            short kinectDepthValue = (short)((short)(kinect.DepthStream.MaxDepth * 1000) << DepthImageFrame.PlayerIndexBitmaskWidth);
            for (int i = 0; i < FarDepthMap.Length; i++)
            {
                FarDepthMap[i] = kinectDepthValue;
            }
            ColorImagePoint[] ColourCoords = new ColorImagePoint[kinect.ColorStream.FrameWidth * kinect.ColorStream.FrameHeight];
            //only looks at max depth
            kinect.MapDepthFrameToColorFrame(kinect.DepthStream.Format, FarDepthMap, kinect.ColorStream.Format, ColourCoords);

            int[] _ColourCoords = new int[ColourCoords.Length * 2];
            for (int i = 0; i < ColourCoords.Length; i++)
            {
                _ColourCoords[2 * i + 0] = ColourCoords[i].X;
                _ColourCoords[2 * i + 1] = ColourCoords[i].Y;
            }

            DepthMap depthMap = new DepthMap(
                kinect.DepthStream.FrameWidth,
                kinect.DepthStream.FrameHeight,
                kinect.ColorStream.FrameWidth,
                kinect.ColorStream.FrameHeight,
                _ColourCoords);

            return depthMap;
        }
    }
}
