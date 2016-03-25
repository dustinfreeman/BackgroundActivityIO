using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Kinect;
using System.Threading;
using System.Runtime.InteropServices;

namespace KinectData
{
    public delegate void AudioDelegate(KinectSensor sensor, string path);

    public class KSoundData
    {
        public short sample;
        public float angle;
        public float angleConfidence;
        public KSoundData(short sample, float angle, float angleConfidence)
        {
            this.sample = sample;
            this.angle = angle;
            this.angleConfidence = angleConfidence;
        }
    }

    public class KinectSound
    {
        #region Settings
        public static uint SOUND_SAMPLES_PER_SECOND = 16000;

        #endregion

        //for recording of sound coming from the Kinect.

        #region Recording

        //taken from Kinect RecordAudio example
        private const int RiffHeaderSize = 20;
        private const string RiffHeaderTag = "RIFF";
        private const int WaveformatExSize = 18; // native sizeof(WAVEFORMATEX)
        private const int DataHeaderSize = 8;
        private const string DataHeaderTag = "data";
        private const int FullHeaderSize = RiffHeaderSize + WaveformatExSize + DataHeaderSize;

        private bool HasInit = false;
        KinectSensor sensor;
        private void Init(KinectSensor sensor)
        {
            if (HasInit)
                return;
            this.sensor = sensor;

            // Register for beam tracking and sound source change notifications
            sensor.AudioSource.BeamAngleMode = BeamAngleMode.Adaptive;
            sensor.AudioSource.BeamAngleChanged += new EventHandler<BeamAngleChangedEventArgs>(AudioSource_BeamAngleChanged);
            sensor.AudioSource.SoundSourceAngleChanged += new EventHandler<SoundSourceAngleChangedEventArgs>(AudioSource_SoundSourceAngleChanged);

            HasInit = true;
        }

        public KinectSound(KinectSensor sensor)
        {
            Init(sensor);
        }

        //called when there is new input from the audio buffer. Given here in case a child class overloads it
        // HINT: IT TOTALLY GETS OVERLOADED.
        protected virtual void audioInputBuffered(byte[] buffer, int offset, int count) { }

        bool sampling = false;
        FileStream fileStream;
        FileStream sampleStream = null;
        BinaryWriter sampleWriter = null;
        int recordingLength = 0;
        public void StartRecording(KinectSensor sensor, string path)
        {
            Init(sensor); //if not done already.

            var buffer = new byte[4096];
            recordingLength = 0;
            const string OutputFileName = "sound.wav";

            Thread.CurrentThread.Priority = ThreadPriority.Highest;


            fileStream = new FileStream(path + OutputFileName, FileMode.Create);
            // FXCop note: This try/finally block may look strange, but it is
            // the recommended way to correctly dispose a stream that is used
            // by a writer to avoid the stream from being double disposed.
            // For more information see FXCop rule: CA2202

            sampling = true;
            sampleStream = new FileStream(path + "samples", FileMode.Create);
            sampleWriter = new BinaryWriter(sampleStream);

            try
            {
                WriteWavHeader(fileStream);

                //WriteStatus(OutputFileName, source, numBeamChanged, numSoundSourceChanged);

                // Start capturing audio                               
                using (var audioStream = sensor.AudioSource.Start())
                {
                    // Simply copy the data from the stream down to the file
                    int count;
                    while ((count = audioStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        //this writes to the WAV file
                        fileStream.Write(buffer, 0, count);

                        //this writes to the Kinect sound-format stream
                        for (int i = 0; i < buffer.Length; i += 2)
                        {
                            short sample = (short)(buffer[i] | (buffer[i + 1] << 8));
                            //clapCaller.BeginInvoke(sample, null, null);
                            sampleWriter.Write(sample);
                            sampleWriter.Write((float)CurrentSoundAngle);
                            sampleWriter.Write((float)CurrentSoundAngleConfidence);
                        }

                        audioInputBuffered(buffer, 0, count);
                        
                        recordingLength += count;

                        if (!sampling)  //has been set to false by StopRecording()
                            break;

                        //WriteStatus(OutputFileName, source, numBeamChanged, numSoundSourceChanged);
                    }

                    if (sampleStream != null)
                    {
                        UpdateDataLength(fileStream, recordingLength);
                        sampleWriter.Dispose();
                        sampleWriter.Close();
                        sampleStream.Dispose();
                        sampleStream.Close();
                        fileStream.Close();
                        sampleStream = null;
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Caught exception in sound recording: " + e);
            }
            finally
            {
                StopRecording();
            }

            sensor.AudioSource.Stop();
        }

        public void StopRecording()
        {
            sampling = false;
        }

        public double CurrentSoundAngle = 0;
        public double CurrentSoundAngleConfidence = 0;
        public double CurrentBeamAngle = 0;

        private void AudioSource_SoundSourceAngleChanged(object sender, SoundSourceAngleChangedEventArgs e)
        {
            CurrentSoundAngle = e.Angle;
            CurrentSoundAngleConfidence = e.ConfidenceLevel;
        }

        private void AudioSource_BeamAngleChanged(object sender, BeamAngleChangedEventArgs e)
        {
            CurrentBeamAngle = e.Angle;
        }

        /// <summary>
        /// A bare bones WAV file header writer
        /// </summary>        
        public void WriteWavHeader(Stream stream)
        {
            // Data length to be fixed up later
            int dataLength = 0;

            // We need to use a memory stream because the BinaryWriter will close the underlying stream when it is closed
            MemoryStream memStream = null;
            BinaryWriter bw = null;

            // FXCop note: This try/finally block may look strange, but it is
            // the recommended way to correctly dispose a stream that is used
            // by a writer to avoid the stream from being double disposed.
            // For more information see FXCop rule: CA2202
            try
            {
                memStream = new MemoryStream(64);

                WAVEFORMATEX format = new WAVEFORMATEX
                {
                    FormatTag = 1,
                    Channels = 1,
                    SamplesPerSec = SOUND_SAMPLES_PER_SECOND,
                    AvgBytesPerSec = 32000,
                    BlockAlign = 2,
                    BitsPerSample = 16,
                    Size = 0
                };

                bw = new BinaryWriter(memStream);

                // RIFF header
                WriteHeaderString(memStream, RiffHeaderTag);
                bw.Write(dataLength + FullHeaderSize - 8); // File size - 8
                WriteHeaderString(memStream, "WAVE");
                WriteHeaderString(memStream, "fmt ");
                bw.Write(WaveformatExSize);

                // WAVEFORMATEX
                bw.Write(format.FormatTag);
                bw.Write(format.Channels);
                bw.Write(format.SamplesPerSec);
                bw.Write(format.AvgBytesPerSec);
                bw.Write(format.BlockAlign);
                bw.Write(format.BitsPerSample);
                bw.Write(format.Size);

                // data header
                WriteHeaderString(memStream, DataHeaderTag);
                bw.Write(dataLength);
                memStream.WriteTo(stream);
            }
            finally
            {
                if (bw != null)
                {
                    memStream = null;
                    bw.Dispose();
                }

                if (memStream != null)
                {
                    memStream.Dispose();
                }
            }
        }

        public void UpdateDataLength(Stream stream, int dataLength)
        {
            using (var bw = new BinaryWriter(stream))
            {
                // Write file size - 8 to riff header
                bw.Seek(RiffHeaderTag.Length, SeekOrigin.Begin);
                bw.Write(dataLength + FullHeaderSize - 8);

                // Write data size to data header
                bw.Seek(FullHeaderSize - 4, SeekOrigin.Begin);
                bw.Write(dataLength);
            }
        }

        public void WriteHeaderString(Stream stream, string s)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(s);
            //Debug.Assert(bytes.Length == s.Length, "The bytes and the string should be the same length.");
            stream.Write(bytes, 0, bytes.Length);
        }

        public struct WAVEFORMATEX
        {
            public ushort FormatTag;
            public ushort Channels;
            public uint SamplesPerSec;
            public uint AvgBytesPerSec;
            public ushort BlockAlign;
            public ushort BitsPerSample;
            public ushort Size;
        }

        #endregion

        #region Playback
        //sound api functions 
        [DllImport("winmm.dll")]
        static extern Int32 mciSendString(string command, StringBuilder buffer, int bufferSize, IntPtr hwndCallback);

        //http://www.codeproject.com/Questions/183548/How-to-use-mciSendString-in-a-threaded-application

        public void Play(string path, int id)
        {
            StringBuilder sb = new StringBuilder();
            string strId = "audiofile" + id;
            mciSendString("open \"" + path + "\" type waveaudio alias " + strId, null, 0, IntPtr.Zero);
            mciSendString("play " + strId + " from 0", null, 0, IntPtr.Zero);
        }

        public void StopPlayback(int id)
        {
            string strId = "audiofile" + id;

            //mciSendString("pause " + strId, null, 0, IntPtr.Zero);
            mciSendString("close " + strId, null, 0, IntPtr.Zero);

        }

        //old version, no multiple play
        //MediaPlayer MPlayer;
        //MediaTimeline MTimeline;
        //public void Play(string path)
        //{
        //    //System.Media.SoundPlayer player = new System.Media.SoundPlayer(Path + "sound.wav");
        //    //player.Play();
        //    //maybe a video misalignment due to loading time?
        //    //http://msdn.microsoft.com/en-us/library/system.media.soundplayer.aspx

        //    MPlayer = new MediaPlayer();
        //    MTimeline = new MediaTimeline(new Uri(Path + "sound.wav"));
        //    MTimeline.CurrentTimeInvalidated += new EventHandler(MTimeline_CurrentTimeInvalidated);
        //    MPlayer.Clock = MTimeline.CreateClock(true) as MediaClock;
        //    Console.WriteLine(MPlayer.Clock.CurrentState);
        //    MPlayer.Clock.Controller.Begin();
        //    Console.WriteLine(MPlayer.Clock.CurrentState);

        //    //http://stackoverflow.com/questions/869761/wpf-implementing-a-mediaplayer-audio-video-seeker
        //}

        #endregion

    }
}
