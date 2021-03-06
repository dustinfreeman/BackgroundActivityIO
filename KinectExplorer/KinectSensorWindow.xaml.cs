//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.KinectExplorer
{
    using System;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Documents;
    using System.Windows.Input;
    using Microsoft.Kinect;

    /// <summary>
    /// Interaction logic for ExplorerWindow.xaml
    /// </summary>
    public partial class KinectSensorWindow : Window
    {
        //window for showing enumeration of attached Kinects.

        #region Private state
        private readonly KinectSensorItems sensorItems;
        #endregion Private state

        #region Window events
        public KinectSensorWindow()
        {
            InitializeComponent();
            this.sensorItems = new KinectSensorItems();
            this.kinectSensors.ItemsSource = this.sensorItems;
        }

        private void WindowLoaded(object sender, EventArgs e)
        {
            this.KinectStart();
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            this.KinectStop();
        }
        #endregion ctor & Window events

        #region Multi-Kinect discovery + setup
        private void KinectStart()
        {
            // listen to any status change for Kinects.
            KinectSensor.KinectSensors.StatusChanged += this.KinectsStatusChanged;

            // show status for each sensor that is found now.
            foreach (KinectSensor kinect in KinectSensor.KinectSensors)
            {
                this.ShowStatus(kinect, kinect.Status);
            }
        }

        private void KinectStop()
        {
            foreach (KinectSensorItem sensorItem in this.sensorItems)
            {
                var kinectWindow = sensorItem.Window;
                if (kinectWindow != null)
                {
                    kinectWindow.Kinect = null;
                    kinectWindow.Close();
                }
            }

            this.sensorItems.Clear();
        }

        private void ShowStatus(KinectSensor kinectSensor, KinectStatus kinectStatus)
        {
            sensorStatusChanges.Text += kinectSensor.DeviceConnectionId + " " + kinectStatus + "\n";

            KinectSensorItem sensorItem;
            this.sensorItems.SensorLookup.TryGetValue(kinectSensor, out sensorItem);
            switch (kinectStatus)
            {
                case KinectStatus.Disconnected:
                case KinectStatus.NotPowered:
                    if (sensorItem != null)
                    {
                        this.sensorItems.Remove(sensorItem);
                        if (sensorItem.Window != null)
                        {
                            sensorItem.Window.Close();
                            sensorItem.Window = null;
                        }
                    }

                    break;
                default:
                    if (sensorItem == null)
                    {
                        sensorItem = new KinectSensorItem
                        {
                            Window = null,
                            Sensor = kinectSensor,
                            Status = kinectSensor.Status
                        };
                        this.sensorItems.Add(sensorItem);
                    }

                    // show a window, if one isn't already shown, unless we are initializing
                    if (sensorItem.Window == null && kinectStatus != KinectStatus.Initializing)
                    {
                        var kinectWindow = new KinectWindow { Kinect = kinectSensor };
                        kinectWindow.Show();

                        sensorItem.Window = kinectWindow;
                    }

                    if (sensorItem.Window != null)
                    {
                        sensorItem.Window.StatusChanged();
                    }

                    sensorItem.Status = kinectStatus;
                    break;
            }
        }

        private void KinectsStatusChanged(object sender, StatusChangedEventArgs e)
        {
            this.ShowStatus(e.Sensor, e.Status);
        }
        #endregion Multi-Kinect discovery + setup
    }
}
