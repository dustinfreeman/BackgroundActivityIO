using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectData
{
    public class GestureRecognizer
    {
        //built off the $1 and Protractor Recognizers

        public long MAX_TIME_WINDOW = 300;
        Dictionary<string, GestureTemplate> Templates;

        public const double SCORE_THRESHOLD = 100; //must be above this for notability.

        public GestureRecognizer()
        {
            Templates = new Dictionary<string, GestureTemplate>();
        }

        public void AddRawTemplate(string gestureType, List<TrackedPoint> rawTemplate, long timeWindow)
        {
            //assumes not processed
            List<TrackedPoint> template;

            //pre-processing.
            template = PreProcess(rawTemplate);

            //add to templates dictionary
            Templates.Add(gestureType, new GestureTemplate(gestureType,template, timeWindow));

            if (timeWindow > MAX_TIME_WINDOW)
                MAX_TIME_WINDOW = timeWindow;
        }

        public Dictionary<string, double> DetectGesture(List<TimeTrackedPoint> rawGesture)
        {
            //returns ordered list of gesture matched against templates.

            Dictionary<string, double> nBest = new Dictionary<string, double>();
            foreach (string gestureType in Templates.Keys)
            {
                List<TrackedPoint> clippedGesture = ClipGesture(rawGesture, Templates[gestureType].timeWindow);

                List<TrackedPoint> gesture = PreProcess(clippedGesture);

                if (gesture.Count < 3)
                    continue;

                double score = Score(Templates[gestureType].Points , gesture);
                nBest.Add(gestureType, score);
            }

            return nBest;
        }

        List<TrackedPoint> ClipGesture(List<TimeTrackedPoint> rawGesture, long timeWindow)
        {
            //returns a clipped gesture, with any points BEFORE timeWindow removed from beginning.
            List<TrackedPoint> clippedGesture = new List<TrackedPoint>();
            foreach (TimeTrackedPoint ttp in rawGesture)
            {
                if (rawGesture.Last().timestamp - ttp.timestamp <= timeWindow)
                    clippedGesture.Add(ttp);
            }

            return clippedGesture;
        }

        double Score(List<TrackedPoint> template, List<TrackedPoint> gesture)
        {
            List<TrackedPoint> searchingTemplate = template;

            const int MAX_ITERATIONS = 5;
            int it = 0;
            float lastAlpha = (float)(Math.PI/4.0), lastBeta = (float)(Math.PI/4.0), lastGamma = (float)(Math.PI/4.0);

            while (it < MAX_ITERATIONS)
            {
                float alpha, beta, gamma;

                RotationSearchIteration(searchingTemplate, gesture, out alpha, out beta, out gamma);

                if (alpha > lastAlpha || beta > lastBeta || gamma > lastGamma)
                {
                    break; //rotated farther than we have before.
                }
                lastAlpha = alpha; lastBeta = beta; lastGamma = gamma;

                searchingTemplate = RotateAboutAxes(searchingTemplate,alpha,beta,gamma);

                it++;
            }

            return 1/Distance(template, gesture); 
        }

        void RotationSearchIteration(List<TrackedPoint> template, List<TrackedPoint> gesture, out float alpha, out float beta, out float gamma)
        {
            //performs a single-iteration search of rotation, and returns the rotation points.

            //form A.x = B, solve for x, where x is the vector of angles alpha, beta, gamma.
            //matrix derivation done by Dustin, images in Dropbox.

            //prepare matrices
            double[,] A = new double[3,3];
            double[,] B = new double[3,1];

            for (int i = 0; i < NUM_POINTS; i++)
            {
                A[0,0] += template[i].Z*template[i].Z + template[i].Y*template[i].Y;
                A[1,0] += -template[i].X*template[i].Y;
                A[2,0] += - template[i].X*template[i].Z;
                A[0,1] += -template[i].X*template[i].Y;
                A[1,1] += template[i].X*template[i].X + template[i].Z*template[i].Z;
                A[2,1] += -template[i].Y*template[i].Z;
                A[0,2] += -template[i].X*template[i].Z;
                A[1,2] += -template[i].Y*template[i].Z;
                A[2,2] += template[i].Y*template[i].Y + template[i].Z*template[i].Z;

                B[0,0] += template[i].Y * (template[i].Z - gesture[i].Z) + template[i].Z * (gesture[i].Y - template[i].Y);
                B[1,0] += template[i].X * (gesture[i].Z - template[i].Z) + template[i].Z * (template[i].X - gesture[i].X);
                B[2,0] += template[i].X * (template[i].Y - gesture[i].Y) + template[i].Y * (gesture[i].Y - template[i].Y);
            }

            double[,] solution = MatrixLibrary.Matrix.SolveLinear(A, B);

            alpha = (float)solution[0,0];
            beta = (float)solution[1,0];
            gamma = (float)solution[2,0];
        }

        List<TrackedPoint> RotateAboutAxes(List<TrackedPoint> points, float alpha, float beta, float gamma)
        {
            List<TrackedPoint> newPoints = new List<TrackedPoint>();

            for (int i = 0; i < NUM_POINTS; i++)
            {
                newPoints.Add(points[i].RotateAboutAxes(alpha, beta, gamma));
            }

            return newPoints;
        }

        double Distance(List<TrackedPoint> template, List<TrackedPoint> gesture)
        {
            double distance = 0;
            for (int i = 0; i < Math.Min(template.Count,gesture.Count); i++)
            {
                distance += (template.ElementAt(i) - gesture.ElementAt(i)).Magnitude();
            }
            return distance/NUM_POINTS;
        }

        List<TrackedPoint> PreProcess(List<TrackedPoint> rawGesture)
        {
            List<TrackedPoint> processedGesture;

            //process the gesture to fixed-length
            processedGesture = ProcessToFixedLength(rawGesture);

            //translate to centroid
            processedGesture = TranslateToOrigin(processedGesture);

            //rotate to indicative angle to nearest major axis.
            processedGesture = RotateToIndicativeOrientation(processedGesture);

            return processedGesture;
        }

        const int NUM_POINTS = 64; //from $1
        List<TrackedPoint> ResampleToFixedNumber(List<TrackedPoint> _gesture)
        {
            //copy over, as it will be modified.
            List<TrackedPoint> gesture = new List<TrackedPoint>();
            foreach (TrackedPoint tp in _gesture)
                gesture.Add(tp);

            //resamples the gesture to unit length.
            double lengthPer = PathLength(gesture) / (NUM_POINTS);

            double D = 0;

            List<TrackedPoint> resampledGesture = new List<TrackedPoint>();
            resampledGesture.Add(gesture.ElementAt(0));

            for (int i = 1; i < gesture.Count; i++)
            {
                TrackedPoint currentVector = gesture.ElementAt(i) - gesture.ElementAt(i - 1);
                double d = (currentVector).Magnitude();

                if (D + d >= lengthPer)
                {
                    //add intermediate point
                    double interpolation = (lengthPer - D) / d;

                    TrackedPoint q = new TrackedPoint(gesture.ElementAt(i - 1).X + interpolation * currentVector.X,
                        gesture.ElementAt(i - 1).Y + interpolation * currentVector.Y,
                        gesture.ElementAt(i - 1).Z + interpolation * currentVector.Z);

                    resampledGesture.Add(q);
                    gesture.Insert(i, q);
                    D = 0;
                }
                else
                {
                    D += d;
                }
            }

            return resampledGesture;
        }

        List<TrackedPoint> ScaleToUnitLength(List<TrackedPoint> gesture)
        {
            //have resampled, equidistant points. Now make the entire path equal unit distance.

            List<TrackedPoint> newGesture = new List<TrackedPoint>();

            double length = PathLength(gesture);
            for (int i = 0; i < gesture.Count; i++)
            {
                newGesture.Add(gesture.ElementAt(i) / length);
            }

            return newGesture;
        }

        List<TrackedPoint> ProcessToFixedLength(List<TrackedPoint> gesture)
        {
            List<TrackedPoint> resampledGesture = ResampleToFixedNumber(gesture);
            resampledGesture = ScaleToUnitLength(resampledGesture);

            //double unitCheck = PathLength(resampledGesture);
            //Console.WriteLine(unitCheck);

            return resampledGesture;        
        }

        double PathLength(List<TrackedPoint> gesture)
        {
            double length = 0;
            for (int i = 1; i < gesture.Count; i++)
            {
                length += (gesture.ElementAt(i) - gesture.ElementAt(i - 1)).Magnitude();
            }
            return length;
        }

        TrackedPoint FindCentroid(List<TrackedPoint> gesture)
        {
            TrackedPoint centroid = new TrackedPoint();
            for (int i = 0; i < gesture.Count; i++)
            {
                centroid += gesture.ElementAt(i);
            }
            centroid /= gesture.Count;
            return centroid;
        }

        List<TrackedPoint> TranslateToOrigin(List<TrackedPoint> gesture)
        {
            //translates the gesture so it is centered on the origin.
            TrackedPoint centroid = FindCentroid(gesture);

            List<TrackedPoint> newGesture = new List<TrackedPoint>();
            for (int i = 0; i < gesture.Count; i++)
            {
                newGesture.Add(gesture.ElementAt(i) - centroid);
            }
            return newGesture;
        }

        List<TrackedPoint> RotateToIndicativeOrientation(List<TrackedPoint> gesture)
        {
            List<TrackedPoint> newGesture = new List<TrackedPoint>();

            TrackedPoint indicative = gesture.ElementAt(0);
            indicative = indicative/indicative.Magnitude(); //normalizes and creates a copy

            //find which of the +ve or -ve x,y,z axes this is closest to.
            TrackedPoint majorAxis;
            if (Math.Abs(indicative.X) > Math.Abs(indicative.Y) && Math.Abs(indicative.X) > Math.Abs(indicative.Z))
            {
                //x axis
                majorAxis = new TrackedPoint(1, 0, 0);
                if (indicative.X < 0)
                    majorAxis *= -1;
            }
            else if (Math.Abs(indicative.Y) > Math.Abs(indicative.X))
            {
                //y axis
                majorAxis = new TrackedPoint(0, 1, 0);
                if (indicative.Y < 0)
                    majorAxis *= -1;
            }
            else
            {
                //z axis
                majorAxis = new TrackedPoint(0, 0, 1);
                if (indicative.Z < 0)
                    majorAxis *= -1;
            }

            //find axis and angle of rotation
            TrackedPoint rotationAxis = indicative * majorAxis; //indicative and majorAxis are known to be unit vectors.
            double angle = Math.Asin(rotationAxis.Magnitude()); //this feels odd. Could do the dot product if I wanted.
            rotationAxis.Normalize();

            //rotate each point.
            for (int i = 0; i < gesture.Count; i++)
            {
                newGesture.Add(gesture.ElementAt(i).RotateByAxisAngle(rotationAxis,angle));
            }

            return newGesture;
        }
    }
}
