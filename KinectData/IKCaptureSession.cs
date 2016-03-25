using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using TaggedData;

namespace KinectData
{
    public interface IKCaptureSession : TaggedData.ICaptureSession
    {
        //interface for CaptureSessions that have a Kinect.

        void SetKinect(KinectSensor kinect);

        string FolderPath { get; set; }
    }
}
