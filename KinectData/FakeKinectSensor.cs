using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Windows.Threading;
using TaggedData;
using KinectData;

namespace KinectData 
{
    public delegate void FColourFrameReadyEvent(FColourFrame colourFrame);
    public delegate void FDepthFrameReadyEvent(FDepthFrame depthFrame);
    public delegate void FSkeletonFrameReadyEvent(FSkeletonFrame skeletonFrame);
    public delegate void FAllFrameReadyEvent(FColourFrame colourFrame, FDepthFrame depthFrame, FSkeletonFrame skeletonFrame);

    public class FakeKinectSensor : TaggedData.CaptureSession
    {
        //meant to mimic the "Microsoft.Kinect.KinectSensor" class for re-play purposes
        //to be used in KinectCaptureViewer, as well as ImageViewer(s)
        //Look at use of Kinect in KinectDiagnosticViewer

        public bool REALTIME_PLAYBACK = true;
        //if true, plays back according to original timestamps.
        //if false, plays back as fast as possible.

        public event FColourFrameReadyEvent FColorFrameReady;
        public event FDepthFrameReadyEvent FDepthFrameReady;
        public event FSkeletonFrameReadyEvent FSkeletonFrameReady;
        public event FAllFrameReadyEvent FAllFrameReady;
        FColourFrame lastColourFrame = null;
        FDepthFrame lastDepthFrame = null;
        FSkeletonFrame lastSkeletonFrame = null;
        private void FFrameReady(FFrame frame)
        {
            //calls the correct event.
            if (frame is FColourFrame)
            {
                lastColourFrame = (FColourFrame)frame;
                if (FColorFrameReady != null)
                {
                    FColorFrameReady(lastColourFrame);
                }
            }

            if (frame is FDepthFrame)
            {
                //if (lastDepthFrame != null)
                //{
                //    double diff = lastDepthFrame.DiffWithFrame((FDepthFrame)frame);
                //    Console.WriteLine("\n" + diff);
                //}

                lastDepthFrame = (FDepthFrame)frame;
                if (FDepthFrameReady != null)
                {
                    FDepthFrameReady(lastDepthFrame);
                }
            }
            

            if (frame is FSkeletonFrame)
            {
                lastSkeletonFrame = (FSkeletonFrame)frame;
                if (FSkeletonFrameReady != null)
                {
                    FSkeletonFrameReady(lastSkeletonFrame);
                }
                if (lastColourFrame != null && lastDepthFrame != null && FAllFrameReady != null)
                {
                    FAllFrameReady(lastColourFrame, lastDepthFrame, lastSkeletonFrame);
                }
            }
            
        }

        public event Action<double> PlayPositionChanged;

        #region Stream Height / Width accessors
        public int ColourStreamWidth
        {
            get {
                if (lastColourFrame != null)
                {
                    return lastColourFrame.Width;
                }
                return 0;
            }
        }
        public int ColourStreamHeight
        {
            get
            {
                if (lastColourFrame != null)
                {
                    return lastColourFrame.Height;
                }
                return 0;
            }
        }
        public int DepthStreamWidth
        {
            get
            {
                if (lastDepthFrame != null)
                {
                    return lastDepthFrame.Width;
                }
                return 0;
            }
        }
        public int DepthStreamHeight
        {
            get
            {
                if (lastDepthFrame != null)
                {
                    return lastDepthFrame.Height;
                }
                return 0;
            }
        }
        #endregion

        DepthMap depthMap;

        Dictionary<long, string> EventTimes;

        int CurrentFrameIndex = 0; //refers to the current index we're at in FrameTimes.    

        //should only be using one timer at a time.
        Timer playbackTimer;
        DispatcherTimer playbackDispatcherTimer;
        bool Playing = false;

        string folderPath;
        public string FolderPath
        {
            get { return folderPath; }
        }
        public FakeKinectSensor(string path, bool verbose = true)
            : base(null, FileMode.Open)
        {
            KinectData.InitKinectTags(); //if not done elsewhere.

            if (path.Substring(path.Length - 4, 4) == ".knt")
            {
                Console.WriteLine("capturePath given");
                //already inclues file name and extension
                this.captureFilePath = path;
                folderPath = path.Substring(0,path.LastIndexOf("\\") + 1);
            }
            else
            {
                //if given folder, assume we're using capture.knt
                this.folderPath = path;
                this.captureFilePath = path + "capture.knt";
            }
            
            if (verbose)
                Console.WriteLine("Opening capture: " + path);

            this.captureStream = new FileStream(this.captureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            TagChunk taggedData = TagChunk.ReadTagChunk(captureStream);
            if (!(taggedData is DepthMap))
            {
                throw new Exception("Capture did not start with depth Map");
            }
            depthMap = (DepthMap)taggedData;

        }

        public override Dictionary<string, long> IndexFrames()
        {
            FColourFrame.USE_COLOUR_JPEG_COMPRESSION = true; //declared instead of detected.
            Dictionary<string, long> FrameStats = base.IndexFrames();

            //may return errors
            long skFrameCount = FrameStats[Tags.skeletonFrame.ToStringExt()];
            long cFrameCount = FrameStats[Tags.colourFrame.ToStringExt()];
            long dFrameCount = FrameStats[Tags.depthFrame.ToStringExt()];

            Console.WriteLine(FrameTimes.Count + " frames loaded");
            Console.WriteLine("Total Time (ms): " + this.Duration);
            Console.WriteLine(skFrameCount + " skeleton frames -> " + skFrameCount / (float)this.Duration * 1000 + " fps");
            Console.WriteLine(dFrameCount + " depth frames -> " + dFrameCount / (float)this.Duration * 1000 + " fps");
            Console.WriteLine(cFrameCount + " colour frames -> " + cFrameCount / (float)this.Duration * 1000 + " fps");

            return FrameStats;
        }

        public void IndexEvents()
        {
            //open any event files and add them to a index of messages.
            EventTimes = new Dictionary<long, string>();

            string EventsFile = folderPath + "Events.csv";
            if (File.Exists(EventsFile))
            {
                StreamReader EventReader = new StreamReader(EventsFile);
                while (!EventReader.EndOfStream)
                {
                    try
                    {
                        string line = EventReader.ReadLine();
                        string[] lineSplit = line.Split(',');
                        EventTimes.Add(Convert.ToInt64(lineSplit[1]), lineSplit[0] + " start: " + lineSplit[3]);
                        EventTimes.Add(Convert.ToInt64(lineSplit[2]), lineSplit[0] + " end: " + lineSplit[3]);
                    }
                    catch
                    {}
                }
            }

            string GesturesDetectedFile = folderPath + "gestures_detected.csv";
            if (File.Exists(GesturesDetectedFile))
            {
                StreamReader GDReader = new StreamReader(GesturesDetectedFile);
                while (!GDReader.EndOfStream)
                {
                    try
                    {
                        string line = GDReader.ReadLine();
                        string[] lineSplit = line.Split(',');
                        long ts = Convert.ToInt64(lineSplit[0]);
                        EventTimes.Add(ts,lineSplit[1] + " detected: " + lineSplit[2]);
                    }
                    catch
                    {}
                }
            }
        }

        public void Play()
        {
            playbackTimer = new Timer(playTick, null, 0, 1);
            Playing = true;
            lastFrameTime = DateTime.Now;
        }

        public bool PlayInitialized
        {
            get
            {
                return _playInitialized;
            }
        }
        bool _playInitialized = false;
        public void InitPlay(Dispatcher dispatcher)
        {
            playbackDispatcherTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(1), DispatcherPriority.Render, playTick, dispatcher);
            Playing = true;
            lastFrameTime = DateTime.Now;
            _playInitialized = true;
        }

        public void PlayToggle()
        {
            Playing = !Playing;
            lastFrameTime = DateTime.Now;
        }

        DateTime lastFrameTime;

        double FractionFromFrameTime(long timestamp)
        {
            return (timestamp - FrameTimes.First().Timestamp) / (double)(this.Duration);
        }
        long FrameTimeFromFraction(double fraction)
        {
            return (long)(fraction * this.Duration + FrameTimes.First().Timestamp);
        }

        public void PlayToValue(double val)
        {
            //set the play position to the val, representing fraction of total play time.

            //find time value.
            long targetTime = FrameTimeFromFraction(val);

            GoToTime(targetTime);
        }

        public void ForwardStep()
        {
            QuickPlay = 1;
        }
        public void RewindStep()
        {
            QuickPlay = -1;
        }

        int QuickPlay = 0; //for playing a couple frames
        void DoQuickPlay()
        {
            QuickPlay = 3;
        }

        private void playTick(Object o, EventArgs e)
        {
            playTick();
        }
        private void playTick(Object o)
        {
            playTick();
        }
        private void playTick()
        {
            if (!Playing && QuickPlay == 0)
                return;
            if (!IndexingFinished)
                return;

            DateTime Now = DateTime.Now;
            double sinceLastFrameMS = (Now - lastFrameTime).TotalMilliseconds;
            double sinceLastFrameUTCunits = sinceLastFrameMS * 10000;
            try
            {
                if (QuickPlay > 0)
                {
                    CurrentFrameIndex++;
                    QuickPlay--;
                }
                else if (QuickPlay < 0)
                {
                    CurrentFrameIndex--;
                    QuickPlay++;
                }
                else if (Playing)
                {
                    while (
                            !REALTIME_PLAYBACK //if not real time playback, just play one frame
                            ||
                            (
                                CurrentFrameIndex == 0
                                    ||
                                (FrameTimes.ElementAt(CurrentFrameIndex).Timestamp - FrameTimes.ElementAt(CurrentFrameIndex - 1).Timestamp)
                                    <
                                sinceLastFrameUTCunits
                            )
                        )
                    {

                        CurrentFrameIndex++; //set up to play the next frame. may be "after" current play time.
                        lastFrameTime = Now;

                        if (!REALTIME_PLAYBACK)
                            break;

                        break; // always break instead of playing multiple frames.
                        //when we always break, doesn't bunch multiple frames in one read, which leads to exponentially-increasing delays.
                    }
                }
                else
                {
                    return; //don't play anything.
                }

                //"play" this frame
                GetFrameAtIndex(CurrentFrameIndex);

                Console.Write("-");
            }
            catch (Exception e)
            {
                if (CurrentFrameIndex >= FrameTimes.Count)
                {
                    Console.WriteLine("Playback finished.");
                    Playing = false;
                    return;
                }

                Console.WriteLine("Playback ERROR: " + e);
            }
        }

        public override FFrame GetFrameAtIndex(int frameIndex)
        {
            FFrame frame = base.GetFrameAtIndex(frameIndex);

            if (frame is FColourFrame)
            {
                Console.Write("c");
            }
            if (frame is FDepthFrame)
            {
                Console.Write("d");
            }
            if (frame is FSkeletonFrame)
            {
                FSkeletonFrame skFrame = (FSkeletonFrame)frame;
                string skInfo = "";
                for (int s = 0; s < skFrame.SkeletonData.Length; s++)
                {
                    if (skFrame.SkeletonData[s].TrackingState == Microsoft.Kinect.SkeletonTrackingState.NotTracked)
                        continue;

                    skInfo = skInfo + (s+1) + ":" + skFrame.SkeletonData[s].TrackingId +","; 
                }
                Console.Write("s(" + skInfo + ")");
            }
            
            FFrameReady(frame); //the event call!
            if (PlayPositionChanged != null)
                PlayPositionChanged(FractionFromFrameTime(frame.mTimestamp));

            //Event Reporting
            if (EventTimes != null && EventTimes.ContainsKey(frame.mTimestamp))
            {
                Console.WriteLine("\n@" + frame.mTimestamp + ": " + EventTimes[frame.mTimestamp]);
            }

            return frame;
        }

        private void _gotoTime(long targetTime)
        {
            //temporarily pause playing.
            bool _Playing = Playing;
            Playing = false;

            //search for frames right AFTER targetTime.
            //binary-style
            int beforeIndex = 0;
            int afterIndex = FrameTimes.Count - 1;
            while (afterIndex - beforeIndex > 1)
            {
                int middleIndex = (afterIndex - beforeIndex) / 2 + beforeIndex;
                if (FrameTimes.ElementAt(middleIndex).Timestamp < targetTime)
                {
                    beforeIndex = middleIndex;
                }
                else
                {
                    afterIndex = middleIndex;
                }
            }
            //set playing frame to beforeIndex
            CurrentFrameIndex = beforeIndex;

            //if was playing before, restart it
            Playing = _Playing;
        }

        public void GoToTime(long targetTime)
        {
            _gotoTime(targetTime);

            if (!Playing)
                DoQuickPlay();
        }

        public FFrame GetFrame(long targetTime)
        {
            _gotoTime(targetTime);

            FFrame frame = GetFrameAtIndex(CurrentFrameIndex);

            return frame;
        }

        public FFrame GetNextFrame()
        {
            CurrentFrameIndex++;
            return GetFrameAtIndex(CurrentFrameIndex);
        }
    }
}
