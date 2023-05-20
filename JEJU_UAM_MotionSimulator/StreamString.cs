using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JEJU_UAM_MotionSimulator
{
    public class StreamString
    {
        private Stream ioStream;
        private UnicodeEncoding streamEncoding;

        public StreamString(Stream ioStream)
        {
            this.ioStream = ioStream;
            streamEncoding= new UnicodeEncoding();
        }

        public string ReadString()
        {
            int len = 0;
            string result = "";

            len = ioStream.ReadByte() * 256;
            len += ioStream.ReadByte();

            if (len > 0)
            {
                byte[] inBuffer = new byte[len];
                ioStream.Read(inBuffer, 0, len);
                result = streamEncoding.GetString(inBuffer);
            }
            else
            {
                result = "";
            }

            return result;
        }

        public int WriteString(string outString)
        {
            try
            {
                byte[] outBuffer = streamEncoding.GetBytes(outString);
                int len = outBuffer.Length;
                if (len > UInt16.MaxValue)
                {
                    len = (int)UInt16.MaxValue;
                }
                ioStream.WriteByte((byte)(len / 256));
                ioStream.WriteByte((byte)(len & 255));
                ioStream.Write(outBuffer, 0, len);
                ioStream.Flush();
                return outBuffer.Length + 2;
            }
            catch (IOException)
            {
                return -1;
            }
        }
    }
}
