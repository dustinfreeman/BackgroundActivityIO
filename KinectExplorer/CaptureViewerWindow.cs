using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaggedData;using KinectData;

namespace Microsoft.Samples.Kinect.KinectExplorer
{
    using System.Windows;
    using Microsoft.Kinect;
    using Microsoft.Samples.Kinect.WpfViewers;
    using System.Windows.Threading;

    //almost the same as KinectWindow, but for viewing pre-recorded captures.
    public class CaptureViewerWindow : Window
    {
        private readonly KinectCaptureViewer kinectCaptureViewer;
        private FakeKinectSensor fkinect;
        private string path;


        public CaptureViewerWindow()
        {
            this.kinectCaptureViewer = new KinectCaptureViewer();
            Content = this.kinectCaptureViewer;
            float unclearScaleFactor = 600.0f / 480.0f; //empirical
            Width = 640 * 2 * unclearScaleFactor;
            Height = 480 * unclearScaleFactor;
            Title = "Kinect Capture Viewer";

            this.Closed += new EventHandler(CaptureViewerWindow_Closed);
            this.Activated += new EventHandler(CaptureViewerWindow_Activated);
            this.Deactivated += new EventHandler(CaptureViewerWindow_Deactivated);
        }

        void CaptureViewerWindow_Deactivated(object sender, EventArgs e)
        {
            kinectCaptureViewer.ParentWindowActiveStatus(false);
        }
        void CaptureViewerWindow_Activated(object sender, EventArgs e)
        {
            kinectCaptureViewer.ParentWindowActiveStatus(true);
        }

        public FakeKinectSensor FKinect
        {
            get
            {
                return this.fkinect;
            }

            set
            {
                this.fkinect = value;
                this.kinectCaptureViewer.FKinect = this.fkinect;
            }
        }

        public string Path
        {
            get { return path; }
            set
            {
                path = value;
                Title = "Kinect Capture Viewer - " + path;
            }
        }

        void CaptureViewerWindow_Closed(object sender, EventArgs e)
        {
            this.FKinect = null;
        }
    }
}
