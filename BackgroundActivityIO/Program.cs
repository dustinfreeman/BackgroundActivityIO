using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using System.Threading;
using TaggedData;
using KinectData;
using Microsoft.Samples.Kinect.KinectExplorer;
using System.IO;

namespace BackgroundActivityIO
{
    class Program
    {
        static Thread wpfThread;
        public static Microsoft.Samples.Kinect.KinectExplorer.App app;

        static void Main(string[] args)
        {
            Thread.CurrentThread.Name = "Main Thread";

            if (args.Length == 0)
            {
                // args = new string[1];
                // args[0] = @"D:\BackgroundActivityData\GestureTemplates\Proposed - 0\capture.knt";
            }

            if (args.Length == 1) //Open with file.
            {
                Console.WriteLine("Opening Capture File: " + args[0]);
                StartWPFStuff();
                OpenCaptureFile(args[0]);
            }

        }

        static void OpenCaptureFile(string filePath)
        {
            try
            {
                string path = (string)filePath.Clone();

                FakeKinectSensor kinect = new FakeKinectSensor(path);

                DispatchToApp(() => app.AddViewerWindow(new CaptureViewerWindow { FKinect = kinect, Path = path }));

                //quickly browse file, create time-index of frame locations.
                Console.Write("Indexing Frames...");
                kinect.IndexFrames();
                kinect.IndexEvents();
                Console.WriteLine("Done.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception " + e);
            }
        }

        static void StartWPFStuff()
        {
            //start wpf stuff in separate thread.
            wpfThread = new Thread(new ThreadStart(() =>
            {
                app = new App();
                app.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
                app.InitializeComponent();
                app.Run();
            }));
            wpfThread.Name = "WPF Thread";
            wpfThread.SetApartmentState(ApartmentState.STA);
            wpfThread.Start();
        }

        public static void DispatchToApp(Action action)
        {
            //fix null app error here. Possibly just wait until it is non-null?
            if (app == null)
                Console.WriteLine("Warning: WPF App is null (trying again)");
            while (app == null)
                Thread.Sleep(50); //does this lock the other thread?
            app.Dispatcher.Invoke(action);
            //Console.WriteLine("dispatched action!");
        }
    }
}
