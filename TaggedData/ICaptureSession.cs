using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TaggedData
{
    public interface ICaptureSession
    {
        bool Capturing { get; set; }

        void StartCapture();
        void EndCapture();

        void AddFrame(TagChunk frame, bool Force = false);

        long LastTimestampFrameAdded();
    }
}
