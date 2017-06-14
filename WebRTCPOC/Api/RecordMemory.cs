using Accord.Video.FFMPEG;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Web;
using System;

namespace WebRTCPOC.Api
{
    public static class RecordMemory
    {
        private static object _syncRoot = new object();
        private static Dictionary<string, VideoFileWriter> writers = new Dictionary<string, VideoFileWriter>();
        private static Dictionary<string, bool> ready = new Dictionary<string, bool>();
        public static void CloseWriter(string id)
        {
            lock (writers[id])
            {
                if (writers[id].IsOpen)
                {
                    writers[id].Flush();
                    writers[id].Close();
                }
            }
        }

        public static void AppendVideoFrame(string id, Bitmap bitmap)
        {
            lock (writers[id])
            {
                writers[id].WriteVideoFrame(bitmap);

            }
        }


        public static void AddAudio(string id, byte[] frame)
        {
            lock (_syncRoot)
            {
                
                writers[id].WriteAudioFrame(frame);
            }
        }
        public static void CreateWriter(string id)
        {
            lock (_syncRoot)
            {
                if (!writers.ContainsKey(id))
                {
                    ready.Add(id, false);
                    writers.Add(id, new VideoFileWriter());
                    writers[id].Open(Path.Combine(HttpContext.Current.Server.MapPath("~/records"), $"{id}.mp4"), 320, 240, 10, VideoCodec.MPEG4, 16000, AudioCodec.MP3, 44100 * 16, 44100, 1);
                }
            }
        }

       

        internal static void VideoReady(string id)
        {
            ready[id] = true;
        }

        internal static bool VideoIsReady(string id)
        {
            return ready[id];
        }
    }
}