using Accord.Video.FFMPEG;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Web;

namespace WebRTCPOC.Api
{
    public static class VideoRecorder
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
            writers[id].WriteVideoFrame(bitmap);
        }

        public static void AddAudio(string id, byte[] frame)
        {

            writers[id].WriteAudioFrame(frame);

        }
        public static void CreateWriter(string id)
        {

            if (!writers.ContainsKey(id))
            {
                ready.Add(id, false);
                writers.Add(id, new VideoFileWriter());
                writers[id].Open(Path.Combine(HttpContext.Current.Server.MapPath("~/bin"), $"{id}.mp4"), 320, 240, 10, VideoCodec.MPEG4, 16000, AudioCodec.MP3, 44100 * 16, 44100, 1);
            }

        }
        public static void VideoReady(string id)
        {
            ready[id] = true;
        }

        public static bool VideoIsReady(string id)
        {
            return ready[id];
        }

        public static void UploadVideo(string id, CloudBlobContainer container)
        {
            CloudBlockBlob blockBlobVideo = container.GetBlockBlobReference($"{id}.mp4");
            blockBlobVideo.UploadFromFile(Path.Combine(HttpContext.Current.Server.MapPath("~/bin"), $"{id}.mp4"));
        }
    }
}