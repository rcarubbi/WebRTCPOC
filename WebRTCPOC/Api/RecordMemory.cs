using Accord.Video.FFMPEG;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;

namespace WebRTCPOC.Api
{
    public static class RecordMemory
    {
        private static object _syncRoot = new object();
        private static Dictionary<string, VideoFileWriter> writers = new Dictionary<string, VideoFileWriter>();

        public static void CloseWriter(string id)
        {
            lock (writers[id])
            {
                if (writers[id].IsOpen)
                    writers[id].Close();
            }
        }

        public static void AppendVideoFrame(string id, Bitmap bitmap)
        {
            lock (writers[id])
            {
                writers[id].WriteVideoFrame(bitmap);
            }
        }


        public static void AppendAudioFrame(string id, byte[] sample)
        {
            lock (writers[id])
            {
                writers[id].WriteAudioFrame(sample);
            }
        }
        public static void CreateWriter(string id)
        {
            lock (_syncRoot)
            {
                if (!writers.ContainsKey(id))
                {
                    writers.Add(id, new VideoFileWriter());
                    writers[id].Open(Path.Combine(HttpContext.Current.Server.MapPath("~/records"), $"{id}.mp4"), 320, 240, 10, VideoCodec.MPEG4, 16000, AudioCodec.None, 160000, 44100, 1);
                }
            }
        }

    }
}