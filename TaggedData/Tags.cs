using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TaggedData
{

    
    public static class Tags
    {
        //this class holds the 4-char length tag definitions for tagged data.
        public const int TAG_SIZE = 4;

        //there are two types of tags: top-level, which are instantiated into objects,
        // and internal, which represent definitions of components of objects.
        //top-level tags are often frames of data.

        public static char[] mTimestamp = "MTMP".ToCharArray(); //given by the machine.
        public static char[] kTimestamp = "TMSP".ToCharArray(); //given by the Kinect


        public static char[] depthMap = "DPMP".ToCharArray();
        public static char[] depthMapdepthWidth = "DPWD".ToCharArray();
        public static char[] depthMapdepthHeight = "DPHT".ToCharArray();
        public static char[] depthMapcolourWidth = "CLWD".ToCharArray();
        public static char[] depthMapcolourHeight = "CLHT".ToCharArray();
        public static char[] depthMapData = "DPMP".ToCharArray(); //NOTE CONFLICT with DPMP ABOVE.

        public static char[] Width = "WDTH".ToCharArray();
        public static char[] Height = "HGHT".ToCharArray();

        public static char[] depthFrame = "DPTH".ToCharArray();
        public static char[] depthData = "DPDT".ToCharArray();
        public static char[] rangeMode = "RANG".ToCharArray();

        public static char[] colourFrame = "CLUR".ToCharArray();
        public static char[] colourFrameImage = "CLRI".ToCharArray(); //the image of the colour frame, encoded in png or jpeg.
        public static char[] colourFrameIndex = "CIND".ToCharArray();

        public static char[] skeletonFrame = "SKLF".ToCharArray();
        public static char[] skeletonTrackingMode = "SKTM".ToCharArray();
        public static char[] floorClipPlane = "FLOR".ToCharArray();
        public static char[] frameNumber = "FNUM".ToCharArray(); //not sure of relation to colourFrameIndex

        public static char[] skeletonData = "SKLD".ToCharArray(); //represents a single skeleton.
        public static char[] trackingID = "TRID".ToCharArray();
        public static char[] skeletonTrackingState = "STRK".ToCharArray();
        public static char[] position = "POSI".ToCharArray();
        public static char[] clippedEdges = "CLIP".ToCharArray();

        public static char[] joint = "JONT".ToCharArray();
        public static char[] jointType = "JNTP".ToCharArray();
        public static char[] jointTrackingState = "JTRK".ToCharArray();
        //there is also a "position" tag within joint.

        public static char[] nullTag = "\0\0\0\0".ToCharArray();

        //A generic constructor delegate.
        public static T Make<T>() where T : new()
        {
            var t = new T();
            return t;
        }

        public static Dictionary<char[], Func<object> > TagConstructors
            = new Dictionary<char[], Func<object> >() 
            { {colourFrame, Make<FColourFrame>} };

        //FrameTags are timestamp-tagged.
        public static List<char[]> TimestampedTags = new List<char[]>()
        {
            depthFrame, colourFrame, skeletonFrame
        };

        public static bool IsTimestampTagged(char[] tag)
        {
            //returns true if the tag will contain a timestamp
            foreach (char[] frameTag in TimestampedTags)
            {
                if (DataUtils.ArraysEqual<char>(tag, frameTag))
                    return true;
            }
            return false;
        }
    }

}
