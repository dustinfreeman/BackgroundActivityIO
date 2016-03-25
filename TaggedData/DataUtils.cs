using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Media;

namespace TaggedData
{
    public static class DataUtils
    {

        #region File I/O extension methods

        

        //alternative: http://stackoverflow.com/questions/17796451/odd-behaviour-copying-more-buffersize-with-net-stream-copytostream-destinatio/17796700#17796700s

        public static void Write(this Stream destStream, int int32)
        {
            byte[] byteArray = BitConverter.GetBytes(int32);
            foreach (byte b in byteArray)
                destStream.WriteByte(b);
        }

        public static void Write(this Stream destStream, long int64)
        {
            byte[] byteArray = BitConverter.GetBytes(int64);
            foreach (byte b in byteArray)
                destStream.WriteByte(b);
        }

        public static void Write(this Stream destStream, float float32)
        {
            byte[] byteArray = BitConverter.GetBytes(float32);
            foreach (byte b in byteArray)
                destStream.WriteByte(b);
        }

        public static void Write(this Stream destStream, double float64)
        {
            byte[] byteArray = BitConverter.GetBytes(float64);
            foreach (byte b in byteArray)
                destStream.WriteByte(b);
        }

        public static void Write(this Stream destStream, byte[] byteArray)
        {
            foreach (byte b in byteArray)
            {
                destStream.WriteByte(b);
            }
        }

        public static void Write(this Stream destStream, char[] charArray)
        {
            foreach (char c in charArray) {
                byte[] _byte = BitConverter.GetBytes(c);
                foreach (byte b in _byte)
                {
                    destStream.WriteByte(b);
                    break; //only write one byte.
                    //it seems the second byte is maybe always 0, which I don't need
                    //maybe for wide chars?
                }
            }
        }

        public static void Write(this BinaryWriter writer, Tuple<float, float, float, float> tuple)
        {
            writer.Write(tuple.Item1);
            writer.Write(tuple.Item2);
            writer.Write(tuple.Item3);
            writer.Write(tuple.Item4);
        }

        public static void Write(this Stream destStream, Tuple<float, float, float, float> tuple)
        {
            destStream.Write(tuple.Item1);
            destStream.Write(tuple.Item2);
            destStream.Write(tuple.Item3);
            destStream.Write(tuple.Item4);
        }

        public static void CopyChunkTo(this Stream sourceStream, Stream destStream, int bytes)
        {
            //could this be buffered more efficiently?
            byte[] byteArray = new byte[bytes];
            sourceStream.Read(byteArray, 0, bytes);
            destStream.Write(byteArray, 0, bytes);
        }

        public static int ReadInt32(this Stream readStream)
        {
            byte[] byteArray = new byte[4];
            readStream.Read(byteArray, 0, 4);

            return
                (((int)byteArray[3]) << 24) |
                (((int)byteArray[2]) << 16) |
                (((int)byteArray[1]) << 8) |
                (((int)byteArray[0]) << 0)   ;
        }

        public static long ReadInt64(this Stream readStream)
        {
            byte[] byteArray = new byte[8];
            readStream.Read(byteArray, 0, 8);

            return
                (((long)byteArray[7]) << 56) |
                (((long)byteArray[6]) << 48) |
                (((long)byteArray[5]) << 40) |
                (((long)byteArray[4]) << 32) |
                (((long)byteArray[3]) << 24) |
                (((long)byteArray[2]) << 16) |
                (((long)byteArray[1]) << 8) |
                (((long)byteArray[0]) << 0)   ;
        }

        public static float ReadFloat32(this Stream readStream)
        {
            byte[] byteArray = new byte[4];
            readStream.Read(byteArray, 0, 4);

            return BitConverter.ToSingle(byteArray, 0);
        }

        public static double ReadFloat64(this Stream readStream)
        {
            byte[] byteArray = new byte[8];
            readStream.Read(byteArray, 0, 8);

            return BitConverter.ToDouble(byteArray, 0);
        }

        public static bool AdvanceToTag(this FileStream readStream, char[] _tag)
        {
            //check if next characters contain the tag. 
            //If they do, return true. If not, advance by 1.
            //Will encounter EOF exception if cannot find.

            Encoding encode = Encoding.UTF8;

            byte[] tag = encode.GetBytes(_tag);
            byte[] buffer = new byte[tag.Length];
            while (true)
            {
                readStream.Read(buffer, 0, buffer.Length);
                readStream.Seek(-buffer.Length, SeekOrigin.Current); //back to start
                if (ArraysEqual<byte>(buffer, tag))
                    return true;

                readStream.Seek(1, SeekOrigin.Current); //otherwise, advance by 1 to keep looking.
            }
        }

        #endregion

        //from: http://stackoverflow.com/questions/713341/comparing-arrays-in-c-sharp
        public static bool ArraysEqual<T>(T[] a1, T[] a2)
        {
            if (ReferenceEquals(a1, a2))
                return true;

            if (a1 == null || a2 == null)
                return false;

            if (a1.Length != a2.Length)
                return false;

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < a1.Length; i++)
            {
                if (!comparer.Equals(a1[i], a2[i])) return false;
            }
            return true;
        }

        public static string ToStringExt(this char[] c)
        {
            string s = "";
            for (int i = 0; i < c.Length; i++)
            {
                s += c[i] + ",";
            }
            return s;
        }
    }
}
