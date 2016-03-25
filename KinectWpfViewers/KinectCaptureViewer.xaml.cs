using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TaggedData;
using KinectData;
using System.Windows.Threading;

namespace Microsoft.Samples.Kinect.WpfViewers
{
    //modified from KinectDiagnosticViewer by Dustin for playback purposes.
    public partial class KinectCaptureViewer : UserControl
    {
        private static Action EmptyDelegate = delegate() { };

        private FakeKinectSensor fkinect;

        DispatcherTimer IndexingDisplay;

        DispatcherTimer PlaybackControl;

        public KinectCaptureViewer()
        {
            InitializeComponent();

            IndexingDisplay = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Input, IndexingTick, this.Dispatcher);
            PlaybackControl = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Input, PlaybackControlTick, Dispatcher);

            this.Loaded += new RoutedEventHandler(KinectCaptureViewer_Loaded);
            this.KeyUp += new KeyEventHandler(KinectCaptureViewer_KeyUp);
        }

        void KinectCaptureViewer_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.G)
            {
                GotoTimeDialog();
            }
            if (e.Key == Key.P)
            {
                FKinect.PlayToggle();
            }
        }

        GetTextDialog getTextDialog;
        void GotoTimeDialog()
        {
            if (getTextDialog != null)
            {
                getTextDialog.Hide();
                getTextDialog.OK -= new Action(dialog_OK);
            }

            //Get goal time in Kinect time
            getTextDialog = new GetTextDialog();
            getTextDialog.Title = "Goto Time";
            getTextDialog.Description = "Goto Time: ";
            getTextDialog.OK += new Action(dialog_OK);
            getTextDialog.ShowDialog();
        }

        void dialog_OK()
        {
            long GoToTime = getTextDialog.Long;
            if (GoToTime == 0)
            {
                Console.WriteLine("Not a valid number");
            }
            else
            {
                Console.WriteLine("Going To Frame at: " + GoToTime);
                FKinect.GoToTime(GoToTime);
            }
        }

        AdornerLayer myAdornerLayer;
        RectangleAdorner sliderProgressRect;
        void KinectCaptureViewer_Loaded(object sender, RoutedEventArgs e)
        {
            myAdornerLayer = AdornerLayer.GetAdornerLayer(TimeSlider);
            sliderProgressRect = new RectangleAdorner(TimeSlider);
            myAdornerLayer.Add(sliderProgressRect);
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
                this.kinectDiagnosticViewer.FKinect = this.fkinect;

                FKinect.FAllFrameReady += new FAllFrameReadyEvent(FKinect_FAllFrameReady);
                FKinect.FColorFrameReady += new FColourFrameReadyEvent(FKinect_FColorFrameReady);
                FKinect.FDepthFrameReady += new FDepthFrameReadyEvent(FKinect_FDepthFrameReady);
                FKinect.FSkeletonFrameReady += new FSkeletonFrameReadyEvent(FKinect_FSkeletonFrameReady);
                FKinect.PlayPositionChanged += new Action<double>(FKinect_PlayPositionChanged);
            }
        }

        #region Kinect Frame Ready Events (used for timing)
        void FKinect_FSkeletonFrameReady(FSkeletonFrame skeletonFrame)
        {
            FrameReadyTimestamp(skeletonFrame.mTimestamp);
        }

        void FKinect_FDepthFrameReady(FDepthFrame depthFrame)
        {
            FrameReadyTimestamp(depthFrame.mTimestamp);
        }

        void FKinect_FColorFrameReady(FColourFrame colourFrame)
        {
            FrameReadyTimestamp(colourFrame.mTimestamp);
        }

        void FKinect_FAllFrameReady(FColourFrame colourFrame, FDepthFrame depthFrame, FSkeletonFrame skeletonFrame)
        {
            FrameReadyTimestamp(skeletonFrame.mTimestamp);
        }

        void FrameReadyTimestamp(long mTimestamp)
        {
            DateTime time = DateTime.FromFileTimeUtc(mTimestamp);
            //time.ToLocalTime();

            TimeLbl.Text = mTimestamp + " | " + time.ToLongTimeString();
        }

        #endregion

        bool IndexFinished = false;
        private void IndexingTick(Object o, EventArgs e)
        {
            if (FKinect == null)
                return;

            if (FKinect.IndexingProgress > 0)
                sliderProgressRect.Value = FKinect.IndexingProgress;

            sliderProgressRect.InvalidateVisual(); //forces a re-render

            if (FKinect.IndexingProgress == 1.0 && !IndexFinished)
            {
                IndexFinished = true;
                if (!FKinect.PlayInitialized)
                    FKinect.InitPlay(Dispatcher);
            }
        }

        bool ParentActiveStatus = false;
        public void ParentWindowActiveStatus(bool ActiveStatus)
        {
            ParentActiveStatus = ActiveStatus;
        }

        private void PlaybackControlTick(Object o, EventArgs e)
        {
            if (ParentActiveStatus)
            {
                if (Keyboard.IsKeyDown(Key.Right))
                {
                    FKinect.ForwardStep();
                }
                if (Keyboard.IsKeyDown(Key.Left))
                {
                    FKinect.RewindStep();
                }
            }
        }

        void FKinect_PlayPositionChanged(double obj)
        {
            TimeSlider.Value = obj*(TimeSlider.Maximum - TimeSlider.Minimum);
        }
        private void TimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!TimeSlider.IsMouseCaptureWithin)
                return; //don't want no circular events, now do we?

            //convert to fraction out of 1.
            double val = (e.NewValue - ((Slider)e.Source).Minimum)/(((Slider)e.Source).Maximum - ((Slider)e.Source).Minimum);

            FKinect.PlayToValue(val);
        }

        private void PlayPauseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!FKinect.PlayInitialized)
                FKinect.InitPlay(Dispatcher);
            else
                FKinect.PlayToggle();
        }

        private void GotoFrameBtn_Click(object sender, RoutedEventArgs e)
        {
            GotoTimeDialog();
        }
    }

    public class RectangleAdorner : Adorner
    {
        // Be sure to call the base class constructor.
        public RectangleAdorner(UIElement adornedElement)
        : base(adornedElement) 
        {
            this.IsHitTestVisible = false;
        }

        public double Value = 0.0;

        // A common way to implement an adorner's rendering behavior is to override the OnRender
        // method, which is called by the layout system as part of a rendering pass.
        protected override void OnRender(DrawingContext drawingContext)
        {
            Rect adornedElementRect = new Rect(this.AdornedElement.RenderSize);
            SolidColorBrush renderBrush = new SolidColorBrush(Colors.Blue);
            renderBrush.Opacity = 0.2;
            Pen renderPen = new Pen(new SolidColorBrush(Colors.Navy), 1.5);

            drawingContext.DrawRectangle(renderBrush, renderPen, 
                new Rect(adornedElementRect.X, 
                adornedElementRect.Y, 
                adornedElementRect.Width * Value, 
                adornedElementRect.Height));
        }
    }
}
