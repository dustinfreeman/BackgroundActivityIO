using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using System.IO;
using System.Windows.Media;

namespace KinectData
{
    public static class KinectUtilities
    {
        public static readonly int Bgr32BytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;
        
        // Converts a 16-bit grayscale depth frame which includes player indexes into a 32-bit frame
        // that displays different players in different colors
        static public byte[] ConvertDepthFrame(short[] depthFrame)
        {
            return ConvertDepthFrame(depthFrame, 0, 255);
        }

        static public byte[] ConvertDepthFrame(short[] depthFrame, byte intensityMapLow, byte intensityMapHigh)
        {
            //for converting from depth to colour
            // color divisors for tinting depth pixels
            int[] IntensityShiftByPlayerR = { 0, 1, 0, 2, 0, 0, 2, 0 };
            int[] IntensityShiftByPlayerG = { 0, 1, 2, 0, 2, 0, 0, 1 };
            int[] IntensityShiftByPlayerB = { 0, 0, 2, 2, 0, 2, 0, 2 };
            //Changed these. Used to be:
            //int[] IntensityShiftByPlayerR = { 1, 2, 0, 2, 0, 0, 2, 0 };
            //int[] IntensityShiftByPlayerG = { 1, 2, 2, 0, 2, 0, 0, 1 };
            //int[] IntensityShiftByPlayerB = { 1, 0, 2, 2, 0, 2, 0, 2 };

            const int RedIndex = 2;
            const int GreenIndex = 1;
            const int BlueIndex = 0;

            int tooNearDepth = 0;
            int tooFarDepth = 4095;
            int unknownDepth = -1;

            byte[] lastDepthFrame32 = new byte[640 * 480 * Bgr32BytesPerPixel];

            for (int i16 = 0, i32 = 0; i16 < depthFrame.Length && i32 < lastDepthFrame32.Length; i16++, i32 += 4)
            {
                int player = depthFrame[i16] & DepthImageFrame.PlayerIndexBitmask;
                int realDepth = depthFrame[i16] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                // transform 13-bit depth information into an 8-bit intensity appropriate
                // for display (we disregard information in most significant bit)
                byte intensity = (byte)(~(realDepth >> 4));

                //we re-map intensity here. i.e. fudging.
                if (intensity >= intensityMapHigh)
                    intensity = 255; //white in front
                else if (intensity <= intensityMapLow)
                    intensity = 0; //black behind.
                else 
                    intensity = (byte)((intensity - intensityMapLow) / (double)(intensityMapHigh - intensityMapLow) * 255);

                if (player == 0 && realDepth == 0)
                {
                    // white 
                    lastDepthFrame32[i32 + RedIndex] = 0;// 255;
                    lastDepthFrame32[i32 + GreenIndex] = 0;//255;
                    lastDepthFrame32[i32 + BlueIndex] = 0;//255;
                }
                else if (player == 0 && realDepth == tooFarDepth)
                {
                    // dark purple
                    lastDepthFrame32[i32 + RedIndex] = 66;
                    lastDepthFrame32[i32 + GreenIndex] = 0;
                    lastDepthFrame32[i32 + BlueIndex] = 66;
                }
                else if (player == 0 && realDepth == unknownDepth)
                {
                    // dark brown
                    lastDepthFrame32[i32 + RedIndex] = 66;
                    lastDepthFrame32[i32 + GreenIndex] = 66;
                    lastDepthFrame32[i32 + BlueIndex] = 33;
                }
                else
                {
                    if (intensity == 255)
                        player = 0;
                    // tint the intensity by dividing by per-player values
                    lastDepthFrame32[i32 + RedIndex] = (byte)(intensity >> IntensityShiftByPlayerR[player]);
                    lastDepthFrame32[i32 + GreenIndex] = (byte)(intensity >> IntensityShiftByPlayerG[player]);
                    lastDepthFrame32[i32 + BlueIndex] = (byte)(intensity >> IntensityShiftByPlayerB[player]);

                } 
            }

            return lastDepthFrame32;
        }


        #region Transformations
        //convert FOV from degrees to radians.
        //depth numbers taken directly from here:
        //http://social.msdn.microsoft.com/Forums/ar/kinectsdknuiapi/thread/413ba48d-e88c-4988-8a34-3b22077da14d
        //could change with different depth resolutions.
        public static double DepthHorizontalFOV
        {
            get
            {
                return 58.5 * Math.PI / 180.0;
                //return Sensor.DepthStream.NominalHorizontalFieldOfView *Math.PI/180.0;
            }
        }
        public static double DepthVerticalFOV
        {
            get
            {
                return 45.6 * Math.PI / 180.0;
                //return Sensor.DepthStream.NominalVerticalFieldOfView * Math.PI / 180.0; 
            }
        }
        public static double ColourHorizontalFOV
        {
            get
            {
                return 62 * Math.PI / 180.0;
                //return Sensor.ColorStream.NominalHorizontalFieldOfView * Math.PI / 180.0; 
            }
        }
        public static double ColourVerticalFOV
        {
            get
            {
                return 48.6 * Math.PI / 180.0;
                //return Sensor.ColorStream.NominalVerticalFieldOfView * Math.PI / 180.0; 
            }
        }

        //HACK preview temporarily assumes 640 x 480 for both colour and depth.
        public static DepthImagePoint MapFromSkeletonToDepth(SkeletonPoint skeletonPoint)
        {
            double Xangle = Math.Tan(skeletonPoint.X / skeletonPoint.Z);
            double Yangle = Math.Tan(skeletonPoint.Y / skeletonPoint.Z);
            int x_d = (int)((Xangle / DepthHorizontalFOV + 0.5) * 640);
            int y_d = (int)((-Yangle / DepthVerticalFOV + 0.5) * 480);
            DepthImagePoint depthPoint = new DepthImagePoint();
            depthPoint.X = x_d;
            depthPoint.Y = y_d;
            //Fuck you and your sealed classes Microsoft.
            //depthPoint.PlayerIndex = 0;
            //depthPoint.Depth = (short)(skeletonPoint.Z) << DepthImageFrame.PlayerIndexBitmaskWidth;
            return depthPoint;
        }

        public static ColorImagePoint MapFromSkeletonToColour(SkeletonPoint skeletonPoint)
        {
            double Xangle = Math.Tan(skeletonPoint.X / skeletonPoint.Z);
            double Yangle = Math.Tan(skeletonPoint.Y / skeletonPoint.Z);
            int x_c = (int)((Xangle / ColourHorizontalFOV + 0.5) * 640);
            int y_c = (int)((-Yangle / ColourVerticalFOV + 0.5) * 480);
            ColorImagePoint colourPoint = new ColorImagePoint();
            colourPoint.X = x_c;
            colourPoint.Y = y_c;
            return colourPoint;
        }

        #endregion
        
    }

    public class KVector
    {
        public float X=0;
        public float Y=0;
        public float Z=0;
        public float W=0;

        public KVector(float X, float Y, float Z, float W)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
            this.W = W;
        }

        public float Magnitude()
        {
            return (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        }

        public static float Distance(Joint j1, Joint j2)
        {
            return Distance(j1.Position, j2.Position);
        }

        public static float Distance(SkeletonPoint j1, SkeletonPoint j2)
        {
            KVector v = new KVector(j1.X - j2.X, j1.Y - j2.Y, j1.Z - j2.Z, 1);
            return v.Magnitude();
        }
    }

    public class TrackedPoint
    {
        public double X, Y, Z;
        public JointTrackingState State;
        public TrackedPoint() { }
        public TrackedPoint(double X, double Y, double Z)
        {
            this.X = X; this.Y = Y; this.Z = Z;
        }
        public TrackedPoint(double X, double Y, double Z, JointTrackingState TrackingState)
            : this(X, Y, Z)
        {
            this.State = TrackingState;
        }

        public double Magnitude()
        {
            return Math.Sqrt(X * X + Y * Y + Z * Z);
        }

        public void DivideBy(double d)
        {
            X /= d; Y /= d; Z /= d;
        }

        public void Normalize()
        {
            double M = this.Magnitude();
            X /= M;
            Y /= M;
            Z /= M;
        }

        public TrackedPoint RotateByAxisAngle(TrackedPoint axis, double angle)
        {
            //http://en.wikipedia.org/wiki/Rodrigues%27_rotation_formula
            //expect axis to be unit vector!!!!!

            TrackedPoint AxisXThis = axis * this;
            double AxisDotThis = TrackedPoint.Dot(axis, this);
            double CosAngle = Math.Cos(angle);
            double SinAngle = Math.Sin(angle);

            TrackedPoint newThis;
            newThis = this * CosAngle + AxisXThis * SinAngle + axis * AxisDotThis*(1-CosAngle);

            return newThis;
        }

        public TrackedPoint RotateAboutAxes(float alpha, float beta, float gamma)
        {
            TrackedPoint xAxis = new TrackedPoint(1, 0, 0);
            TrackedPoint yAxis = new TrackedPoint(1, 0, 0);
            TrackedPoint zAxis = new TrackedPoint(1, 0, 0);

            TrackedPoint newPoint;
            //choice of axis rotation order is arbitrary here. Hoping for small angle.
            newPoint = this.RotateByAxisAngle(xAxis, alpha);
            newPoint = this.RotateByAxisAngle(yAxis, beta);
            newPoint = this.RotateByAxisAngle(zAxis, gamma);

            return newPoint;
        }

        public static double Dot(TrackedPoint p1, TrackedPoint p2)
        {
            return p1.X * p2.X + p1.Y * p2.Y + p1.Z * p2.Z;
        }

        public static TrackedPoint operator +(TrackedPoint p1, TrackedPoint p2)
        {
            return new TrackedPoint(p1.X + p2.X, p1.Y + p2.Y, p1.Z + p2.Z);
        }

        public static TrackedPoint operator -(TrackedPoint p2, TrackedPoint p1)
        {
            //this unusual number-ordering seems somewhat non-intuitive, but is still correct.
            return new TrackedPoint(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
        }

        public static TrackedPoint operator /(TrackedPoint t, double d)
        {
            return new TrackedPoint(t.X / d, t.Y / d, t.Z / d);
        }

        public static TrackedPoint operator *(TrackedPoint t, double d)
        {
            return new TrackedPoint(t.X * d, t.Y * d, t.Z * d);
        }

        public static TrackedPoint operator *(TrackedPoint t1, TrackedPoint t2)
        {
            //does cross product
            return new TrackedPoint(
                t1.Y * t2.Z - t1.Z * t2.Y,
                t1.Z * t2.X - t1.X * t2.Z,
                t1.X * t2.Y - t1.Y * t2.X);
        }
    }

    public class TimeTrackedPoint : TrackedPoint
    {
        public long timestamp;

        public TimeTrackedPoint() { }

        public TimeTrackedPoint(long timestamp, TrackedPoint point)
        {
            this.timestamp = timestamp;
            this.X = point.X;
            this.Y = point.Y;
            this.Z = point.Z;
            this.State = point.State;
        }
    }
}
