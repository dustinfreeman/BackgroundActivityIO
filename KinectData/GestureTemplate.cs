using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectData
{
    public class GestureTemplate
    {
        public List<TrackedPoint> Points;
        public string Name;
        public long timeWindow; //match gestures of similar time-length. Currently not time-invariant, sadly.

        public GestureTemplate(string Name, List<TrackedPoint> Points, long timeWindow)
        {
            this.Name = Name;
            this.Points = Points;
            this.timeWindow = timeWindow;
        }
    }
}
