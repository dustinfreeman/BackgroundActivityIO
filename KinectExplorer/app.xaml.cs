//------------------------------------------------------------------------------
// <copyright file="app.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.KinectExplorer
{
    using System.Windows;
    using TaggedData;using KinectData;
    using System.Collections.Generic;
    using System;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //new KinectSensorWindow().Show();
        }

        public void LiveKinectPreview()
        {
            Console.WriteLine("About to Show LiveKinectPreview - can take ~30 seconds to open (new on Windows 8?)");
            //new KinectSensorWindow().Show();
            KinectSensorWindow ksw = new KinectSensorWindow();
            ksw.Show();
            Console.WriteLine("Showing LiveKinectPreview");
        }

        GesturePromptWindow gpw = null;
        public void OpenGesturePromptWindow()
        {
            gpw = new GesturePromptWindow();
            gpw.Show();
        }

        public void GesturePrompt(string prompt, 
            double BEFORE_TIME = 0, 
            double DURATION_TIME = 0,
            double AFTER_TIME = 0)
        {
            Console.WriteLine("Gesture Prompt: " + prompt);
            if (gpw != null)
                gpw.GesturePrompt(prompt, BEFORE_TIME, DURATION_TIME, AFTER_TIME);
            else
                Console.WriteLine("gpw does not exist.");
        }

        List<CaptureViewerWindow> viewerWindowList = new List<CaptureViewerWindow>();
        public void AddViewerWindow(CaptureViewerWindow cvw)
        {
            viewerWindowList.Add(cvw);
            cvw.Show();
        }
    }
}