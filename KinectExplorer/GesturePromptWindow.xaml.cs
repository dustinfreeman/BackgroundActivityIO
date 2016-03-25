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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Microsoft.Samples.Kinect.KinectExplorer
{
    /// <summary>
    /// Interaction logic for GesturePromptWindow.xaml
    /// </summary>
    public partial class GesturePromptWindow : Window
    {
        double BEFORE_TIME;
        double DURATION_TIME;
        double AFTER_TIME;

        DateTime PromptStart = DateTime.Now;
        DispatcherTimer PromptTimer = null;

        public GesturePromptWindow()
        {
            InitializeComponent();

            PromptTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(30), DispatcherPriority.Input, PromptTimer_Tick, Dispatcher);
        }

        private void PromptTimer_Tick(Object o, EventArgs e)
        {
            //recalculate everything.
            double total_time = BEFORE_TIME + DURATION_TIME + AFTER_TIME;
            if (total_time == 0)
                return;

            BeforeRect.Width = BEFORE_TIME / total_time * this.Width;
            DuringRect.SetValue(Canvas.LeftProperty, BeforeRect.Width);
            DuringRect.Width = DURATION_TIME / total_time * this.Width;
            AfterRect.SetValue(Canvas.LeftProperty, BeforeRect.Width + DuringRect.Width);
            AfterRect.Width = AFTER_TIME / total_time * this.Width;

            TimeSpan elapsed = DateTime.Now - PromptStart;

            ProgressRect.Width = elapsed.TotalSeconds / total_time * this.Width;
        }

        public void GesturePrompt(string prompt,
            double BEFORE_TIME = 0,
            double DURATION_TIME = 0,
            double AFTER_TIME = 0)
        {
            this.BEFORE_TIME = BEFORE_TIME;
            this.DURATION_TIME = DURATION_TIME;
            this.AFTER_TIME = AFTER_TIME;

            PromptStart = DateTime.Now; //reset

            GesturePromptTxt.Text = prompt;
        }
    }
}
