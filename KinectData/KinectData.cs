using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using System.IO;
using TaggedData;

namespace KinectData
{
    public static class KinectData
    {
        static bool _Has_Init = false;
        public static void InitKinectTags()
        {
            if (_Has_Init)
                return;

            //prevents double event hooking. I think:
            // http://stackoverflow.com/questions/937181/c-sharp-pattern-to-prevent-an-event-handler-hooked-twice
            Tags.TagConstructors.Add(Tags.skeletonFrame, Tags.Make<FSkeletonFrame>);
            Tags.TagConstructors.Add(Tags.depthFrame, Tags.Make<FDepthFrame>);
            Tags.TagConstructors.Add(Tags.depthMap, Tags.Make<DepthMap>);

            _Has_Init = true;
        }

        public static void Write(this BinaryWriter writer, SkeletonPoint sp)
        {
            writer.Write(sp.X);
            writer.Write(sp.Y);
            writer.Write(sp.Z);
        }

        public static void Write(this Stream stream, SkeletonPoint sp)
        {
            stream.Write(sp.X);
            stream.Write(sp.Y);
            stream.Write(sp.Z);
        }
    }
}
