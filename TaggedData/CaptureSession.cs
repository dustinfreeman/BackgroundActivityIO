using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace TaggedData
{
    public struct FrameTimeData
    {
        public long Timestamp; //units of 100 ns
        public char[] CharTag; //RIFF Tag of length 4.
        public long FilePosition; //in capturefile
        public FrameTimeData(long timestamp, char[] charTag, long filePosition)
        {
            this.Timestamp = timestamp;
            this.CharTag = new char[charTag.Length];
            charTag.CopyTo(this.CharTag, 0);
            this.FilePosition = filePosition;
        }
    }

    public class CaptureSession: ICaptureSession
    {
        //listing of all frames indexed by time. 
        protected List<FrameTimeData> FrameTimes = new List<FrameTimeData>();
        protected virtual void AddFrameTime(long timestamp, char[] charTag, long filePosition)
        {
            //expect filePosition to be accurate.
            FrameTimes.Add(new FrameTimeData(timestamp, charTag, filePosition));
        }

        public virtual int NumFrames
        { get { return FrameTimes.Count; } }

        #region File Access
        //Paths

        //Will have the core file containing all the RIFF data, then possibly
        // some other meta-data files
        public const string DEFAULT_CAPTURE_PATH = "capture.dat"; //has different usage in KinectCapture classes.
        public const FileMode DEFAULT_FILE_MODE = FileMode.Create;
        protected string captureFilePath = null;
        protected FileMode captureFileMode;
        protected FileStream captureStream;

        public const string DEFAULT_CAPTURE_FOLDER = "./";
        
        String folderPath = DEFAULT_CAPTURE_FOLDER;
        public string FolderPath
        {
            get { return folderPath; }
            set { folderPath = value; }
            //the above SHOULD check if the capture file is already open, but it does not.
        }

        #endregion

        #region Constructors
        protected virtual void InitTags()
        {
            //used for any type of capture that inherits from CaptureSession
            //will load any extra tags not included by default.
        }

        public CaptureSession(string captureFilePath, FileMode fileMode)
        {
            //meaningful fileModes are:
            // Create - overwrites
            // Open - as if to access existing captures (NOTE: FrameTime will need to be indexed)
            // Append - to add new capture data (existing data not immediately important)
            InitTags();
            this.captureFilePath = captureFilePath;
            this.captureFileMode = fileMode;
            if (captureFilePath != null)
            {
                captureStream = new FileStream(captureFilePath, this.captureFileMode, FileAccess.ReadWrite, FileShare.Read);
                if (fileMode == FileMode.Open)
                {
                    IndexFrames();
                }
            }

            if (ASYNC_WRITE)
            {
                writeThread = new Thread(new ThreadStart(() =>
                {
                    WriteQueueLoop();
                }));
                writeThread.Name = "Write Queue Thread";
                writeThread.Start();
            }
        }

        public CaptureSession()
            : this(DEFAULT_CAPTURE_PATH, DEFAULT_FILE_MODE)
        { }

        public CaptureSession(string captureFilePath)
            : this(captureFilePath, DEFAULT_FILE_MODE)
        { }

        public CaptureSession(FileMode fileMode)
            : this(DEFAULT_CAPTURE_PATH, fileMode)
        { }

        #endregion

        public double IndexingProgress
        {
            get
            {
                return indexingProgress;
            }
        }
        double indexingProgress = -1;
        protected bool IndexingFinished = true;
        public virtual Dictionary<string, long> IndexFrames()
        {
            IndexingFinished = false;
            //loads pre-existing capture data into FrameTimes
            // when done, captureStream is at the end.
            // returns statistics of framecounts of each type
            FrameTimes.Clear();
            captureStream.Seek(0, SeekOrigin.Begin);

            Dictionary<string, long> FrameTypeCounts = new Dictionary<string, long>();

            try
            {
                int chunkLength; long timestamp; char[] charTag;

                while (true && captureStream.Position < captureStream.Length)
                {
                    //read tag
                    long chunkStart = captureStream.Position;

                    charTag = TagChunk.ReadTagToTimestamp(captureStream, out chunkLength, out timestamp);

                    //only add to frame index if has a timestamp tag.
                    if (Tags.IsTimestampTagged(charTag))
                    {
                        AddFrameTime(timestamp, charTag, chunkStart);
                        
                        indexingProgress = captureStream.Position / (double)captureStream.Length;

                        //Console.WriteLine(indexingProgress + " Frame Time Add: " + timestamp + " "
                        //    + charTag[0] + charTag[1] + charTag[2] + charTag[3] + " "
                        //    + chunkStart);

                        string charTagString = charTag.ToStringExt();
                        if (!FrameTypeCounts.ContainsKey(charTagString))
                            FrameTypeCounts.Add(charTagString, 0);
                        FrameTypeCounts[charTagString]++;
                    }
                }
            }
            catch (EndOfStreamException e)
            {
                //do nothing
            }

            indexingProgress = 1.0;
            IndexingFinished = true;
            captureStream.Seek(0, SeekOrigin.End);

            return FrameTypeCounts;
        }

        //an increasing index of every frame of FColourframe.
        int colourFrameIndex = 0;

        public bool Capturing { get; set; }
        public virtual void StartCapture()
        {
            Capturing = true;
        }

        public virtual void EndCapture()
        {
            //for cleanup
            Capturing = false;
            //WARNING: currently, no graceful resume.
        }

        public FColourFrame lastColourFrame = null;

        private void _AddFrame(TagChunk frame, bool Force = false)
        {
            captureStream.Seek(0, SeekOrigin.End); //bring it to the end, in case it was brought elsewhere.

            long ts = 0; //not timestamp-tagged.
            if (frame is FFrame)
            {
                ts = ((FFrame)frame).mTimestamp;
            }
            if (Tags.IsTimestampTagged(frame.Tag))
                AddFrameTime(ts, frame.Tag, captureStream.Position);

            frame.WriteToStream(captureStream);
            captureStream.Flush(); //needed if writing and reading to the file at the same time.

            if (!Capturing && !Force && FrameWriteQueue.Count == 0)
            {
                //EndCapture() has been called.
                if (captureStream == null)
                    return;
                captureStream.Dispose();
                captureStream.Close();
                captureStream = null;
            }
        }

        private long _LastTimestampFrameAdded = 0;
        public long LastTimestampFrameAdded()
        {
            return _LastTimestampFrameAdded;
        }
        public virtual void AddFrame(TagChunk frame, bool Force = false)
        {
            if (!Capturing && !Force)
            {
                //Console.WriteLine("Not Capturing currently - frame rejected.");
                return;
            }

            //do timestamps and index before it gets added to the write queue.
            long ts = DateTime.Now.ToFileTimeUtc();
            if (frame is FFrame) //meaning it has a timestamp
            {
                ts = ((FFrame)frame).mTimestamp;
            }
            _LastTimestampFrameAdded = ts;

            if (frame is FColourFrame)
            {
                FColourFrame colourFrame = ((FColourFrame)frame);
                colourFrame.colourFrameIndex = colourFrameIndex;
                colourFrameIndex++;
                lastColourFrame = colourFrame;
            }

            //file writing control
            if (ASYNC_WRITE)
            {
                if (FrameWriteQueue.Contains(frame)) {
                    Console.WriteLine("Frame already enqueued");
                }
                lock (FrameWriteQueue)
                {
                    FrameWriteQueue.Enqueue(frame);
                }
            }
            else
            {
                _AddFrame(frame, Force);
            }
        }

        public long Duration
        {
            get
            {
                if (FrameTimes.Count < 2)
                    return 0;
                return FrameTimes.Last().Timestamp - FrameTimes.First().Timestamp;
            }
        }

        protected int _ClosestFrameIndexToTime(long timestamp, List<FrameTimeData> FrameTimeList) {
            //binary searches through ColourFrameTimes to find the closest frame to the timestamp.
            //returns the index in ColourFrameTimes
            int imax = FrameTimeList.Count;
            int imin = 0;
            int imid = -1;
            while (imax - imin > 1)
            {
                imid = (imax - imin) / 2 + imin;

                //search
                if (FrameTimeList[imid].Timestamp < timestamp)
                    imin = imid;
                else //if (ColourFrameTimes[imid].Item1 > timestamp)
                    imax = imid;
            }

            //expect imin == imax
            return imid;
        }

        protected virtual int ClosestFrameIndexToTime(long timestamp)
        {
            return _ClosestFrameIndexToTime(timestamp, this.FrameTimes);
        }

        public virtual FFrame GetFrameAtIndex(int frameIndex)
        {
            if (FrameTimes.Count == 0)
                return null;

            if (frameIndex >= FrameTimes.Count)
                //this happens if we haven't indexed enough.
                captureStream.Seek(FrameTimes.Last().FilePosition, SeekOrigin.Begin);
            else
                captureStream.Seek(FrameTimes.ElementAt(frameIndex).FilePosition, SeekOrigin.Begin);

            FFrame frame = (FFrame)TagChunk.ReadTagChunk(captureStream);

            if (frame == null)
            {
                throw new Exception("Unexpected Frame Type: " + FrameTimes.ElementAt(frameIndex).CharTag.ToStringExt());
            }

            return frame;
        }

        #region Asynchoronous Frame Adding
        //WARNING: Code not guaranteed to work.
        // I don't think Async writing properly locks the file.
        public static bool ASYNC_WRITE = false;
        Queue<TagChunk> FrameWriteQueue = new Queue<TagChunk>();
        static Thread writeThread;

        private void WriteQueueLoop()
        {
            while (true)
            {
                Thread.Sleep(10);

                TagChunk writeData = null;
                lock (FrameWriteQueue)
                {
                    while (FrameWriteQueue.Count > 0)
                        writeData = FrameWriteQueue.Dequeue();
                }
                if (writeData == null)
                    continue;

                _AddFrame(writeData);

            }
        }

        #endregion

    }
}
