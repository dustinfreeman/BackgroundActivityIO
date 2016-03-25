using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TaggedData
{
    public abstract class TagChunk
    {
        //base class for tagged data chunk that may be written or read from a stream.

        public char[] Tag; //the top-level tag of the data. Needs to be given a value in every leaf class.

        public TagChunk()
        {

        }

        #region FPS
        protected static int FPSframeInterval = 30;
        public virtual void FPS()
        {
            //expected overrride.
        }

        #endregion

        #region File Writing
        protected MemoryStream memoryStream;
        protected FileStream captureStream; //the stream that wants to be written to.

        protected virtual void WriteElements()
        {
            //this should not be called without memoryStream being initialized

            //writes all elements of the Tag Chunk to the memory stream
        }

        public void WriteToStream(FileStream captureStream)
        {
            //writes this to a given stream.
            this.captureStream = captureStream;
            memoryStream = new MemoryStream();

            //write initial space for tag and length
            memoryStream.Write(new byte[Tags.TAG_SIZE]); //will be filled in by inheriting classes.
            memoryStream.Write((System.Int32)0); //placeholder for value representing chunk length.
            
            //heavily overloaded function that writes class-specific stuff.
            WriteElements();

            //expect WriteTag and FinishWrite to be called at the end of any WriteToStream
            WriteTag();
            FinishWrite();

            FPS();
        }

        public virtual void WriteTag()
        {
            //write tag and length at the beginning.
            memoryStream.Seek(0, SeekOrigin.Begin);
            memoryStream.Write(Tag);
            //subtract number of bytes representing tag and chunk length.
            memoryStream.Write((System.Int32)memoryStream.Length - Tags.TAG_SIZE - sizeof(System.Int32));
        }

        public void FinishWrite()
        {
            //write everything in memoryStream into captureStream
            memoryStream.WriteTo(captureStream);
            memoryStream.Dispose(); 
            captureStream = null;
            memoryStream = null;
        }

        #endregion

        #region Static File Reading
        public static Encoding encode = Encoding.UTF8;

        public static char[] ReadTagMetaData(FileStream readStream, out int chunkLength)
        {
            //reads and returns the tag, chunkLength
            //leaves the readStream at the beginning of the next tag.
            byte[] tag = new byte[Tags.TAG_SIZE];
            readStream.Read(tag, 0, Tags.TAG_SIZE);
            char[] charTag = encode.GetChars(tag);
            chunkLength = readStream.ReadInt32();

            //skip over chunk to next top-level tag.
            readStream.Seek(chunkLength, SeekOrigin.Current);

            return charTag;
        }

        public static char[] ReadTagToTimestamp(FileStream readStream, out int chunkLength, out long mTimestamp, out long kTimestamp)
        {
            //reads and returns the tag, chunkLength and timestamps
            //leaves the readStream at the beginning of the next tag.

            byte[] tag = new byte[Tags.TAG_SIZE];
            readStream.Read(tag, 0, Tags.TAG_SIZE);
            char[] charTag = encode.GetChars(tag);
            chunkLength = readStream.ReadInt32();

            int readPastSize = 0;
            kTimestamp = 0;
            mTimestamp = 0;

            if (Tags.IsTimestampTagged(charTag))
            {
                for (int i = 0; i < 2; i++)
                {
                    //read tag
                    byte[] TStag = new byte[Tags.TAG_SIZE];
                    readStream.Read(TStag, 0, Tags.TAG_SIZE);
                    char[] charTStag = encode.GetChars(TStag);

                    if (DataUtils.ArraysEqual<char>(charTStag, Tags.kTimestamp))
                    {
                        //length known - skip over.
                        readStream.ReadInt32();
                        kTimestamp = readStream.ReadInt64();
                        readPastSize += Tags.TAG_SIZE + sizeof(System.Int32) + sizeof(System.Int64); //Timestamp Tag Size

                    }
                    if (DataUtils.ArraysEqual<char>(charTStag, Tags.mTimestamp))
                    {
                        //length known - skip over.
                        readStream.ReadInt32();
                        mTimestamp = readStream.ReadInt64();
                        readPastSize += Tags.TAG_SIZE + sizeof(System.Int32) + sizeof(System.Int64); //Timestamp Tag Size
                    }
                }
            }

            //skip over chunk to next top-level tag.
            // already have advanced over timestamp chunk.
            readStream.Seek(chunkLength - readPastSize, SeekOrigin.Current);

            return charTag;
        }

        public static char[] ReadTagToTimestamp(FileStream readStream, out int chunkLength, out long Timestamp)
        {
            //reads and returns the tag, chunkLength and timestamp (m or k Timestamp depending on which is marked as primary).
            //leaves the readStream at the beginning of the next tag.

            long mTimestamp, kTimestamp;
            
            char[] tag = ReadTagToTimestamp(readStream, out chunkLength, out mTimestamp, out kTimestamp);

            if (FFrame.M_TIMESTAMP_PRIMARY)
                Timestamp = mTimestamp;
            else
                Timestamp = kTimestamp;

            return tag;
        }

        public static TagChunk ReadTagChunk(FileStream readStream, char[][] filter)
        {
            //top-level factory. Returns null if no tag is found. 
            //Guaranteed to skip over chunk if tag not recognized.
            //will only read tags included in filter, all tags if filter is empty.
            TagChunk taggedData = null;

            //read tag
            byte[] tag = new byte[Tags.TAG_SIZE];
            readStream.Read(tag, 0, tag.Length);
            char[] charTag = encode.GetChars(tag);

            //read chunk length
            int chunkLength = readStream.ReadInt32();
            //if (chunkLength < 0)
            //{

            //}

            //Console.WriteLine("Chunk: " + charTag + " " + chunkLength);

            //filter tags
            bool skipTag = false;
            if (filter.Length > 0)
            {
                //tag must be included in filter list.
                skipTag = true;
                foreach (char[] filterTag in filter)
                {
                    if (DataUtils.ArraysEqual<char>(filterTag, charTag))
                    {
                        skipTag = false;
                        break;
                    }
                }
            }
            else
            {
                //continue; accept all tags
            }

            //instantiate top-level tag
            if (!skipTag)
            {
                //look in the tag registry.
                foreach (char[] _charTag in Tags.TagConstructors.Keys)
                {
                    if (DataUtils.ArraysEqual<char>(charTag, _charTag))
                        taggedData = (TagChunk)Tags.TagConstructors[_charTag]();
                }
            }

            if (skipTag || taggedData == null)
            {
                //unknown or filtered chunk.
                //advance to end of chunk:
                readStream.Seek(chunkLength, SeekOrigin.Current);
                if (chunkLength < 0)
                    Console.WriteLine("Got Chunk Length of " + chunkLength);
            }
            else
            {
                //load data into instantiated object.
                taggedData.ReadFromStream(readStream, chunkLength);
            }

            //if (taggedData == null)
            //{
            //    Console.WriteLine("taggedData is null");
            //    //trying to catch errors with VP Frame
            //}

            return taggedData;
        }

        public static TagChunk ReadTagChunk(FileStream readStream)
        {
            //top-level factory. Returns null if no tag is found. 
            //Guaranteed to skip over chunk if tag not recognized.
            
            char[][] emptyFilter = new char[0][];
            return ReadTagChunk(readStream, emptyFilter);
        }

        #endregion

        #region Inherited File Reading

        public virtual void ReadFromStream(FileStream readStream, int chunkLength)
        {
            //tag and chunkLength have already been read. 
            //Tag must inform the correct subclass instance.

            captureStream = readStream;

            //read entire chunk into buffer stream
            MemoryStream bufferStream = new MemoryStream();
            readStream.CopyChunkTo(bufferStream, chunkLength);

            //now all chunk data is in bufferStream. No need for further file access. We expect an end-of-stream exception at the end.
            bufferStream.Seek(0, SeekOrigin.Begin);
            
            //now, read tags one-after-one and apply to instantiated class.
            try
            {
                while (true)
                {
                    //read tag
                    byte[] tag = new byte[Tags.TAG_SIZE];
                    bufferStream.Read(tag, 0, tag.Length);
                    char[] charTag = encode.GetChars(tag);

                    //find a home for it.
                    bool tagFound = false;
                    try
                    {
                        tagFound = ReadTag(charTag, bufferStream);
                        if (!tagFound)
                        {
                            if (DataUtils.ArraysEqual<char>(charTag, Tags.nullTag))
                                break; //end of stream. may cause less performance errors than throwing an exception.

                            //must skip over this subchunk.
                            int subchunkLength = bufferStream.ReadInt32();
                            Console.WriteLine("Unidentified subchunkLength " + subchunkLength);
                            Console.WriteLine(bufferStream.Position);
                            bufferStream.Seek(subchunkLength, SeekOrigin.Current); //not sure if this causes problems?
                        }
                    }
                    catch (EndOfStreamException e)
                    {
                        break;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception Reading Tag: " + charTag.ToStringExt() + " " + e);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Read Exception: " + e);
            }
            bufferStream.Close();

            SanityCheck();
        }

        protected virtual bool ReadTag(char[] tag, Stream readStream)
        {
            //assumes we are AFTER the tag, index pointing at the chunk length indicator.

            //this function returns true if the tag was found, false if it was not.

            //if returning true, expect that this function has advanced it past the end of the chunk.

            return false;
        }

        protected virtual void SanityCheck()
        {
            //for checking integrity of read data.
        }

        #endregion
    }
}
