using Accord.Video.FFMPEG;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System;
using NAudio.Wave;
using System.Diagnostics;

namespace AVRecordManager
{
    public class AVFileReader
    {
        private string _workspace;
        public AVFileReader(string workspace = null)
        {
            if (workspace == null)
                _workspace = HttpContext.Current.Server.MapPath("~/bin");
            else
                _workspace = workspace;
        }

        

        public async Task<FileStream> ReadVideo(string filename)
        {
            BlobStorageManager videoStorageManager = new BlobStorageManager(filename);
            var videoStream = videoStorageManager.OpenRead();
            var sr = new StreamReader(videoStream);
            string outputFullPath = Path.Combine(_workspace, Path.ChangeExtension(filename, ".mp4"));

            using (VideoFileWriter videoWriter = new VideoFileWriter())
            { 
                videoWriter.Open(outputFullPath, 320, 240, 10, VideoCodec.MPEG4, 16000, AudioCodec.MP3, 44100 * 16, 44100, 1);
                string length = string.Empty;
                var i = 0;
                do
                {
                    try
                    {
                        using (var frame = await ReadFrameAsync(sr))
                        {
                            //  frame.Save($"{i}.jpg");
                            i++;
                            videoWriter.WriteVideoFrame(frame);
                        }
                    }
                    catch { }
                } while (!sr.EndOfStream);
            }

           
            
            var outputFullPathAudio = ReadAudio(Path.ChangeExtension(filename, ".adat"));

            
            MergeAV(outputFullPath, outputFullPathAudio);

            sr.Close();
            sr.Dispose();
            sr = null;
            videoStream.Close();
            videoStream.Dispose();
            videoStream = null;

            FileStream fs = new FileStream(outputFullPath, FileMode.Open);
            return fs;
        }



        private void MergeAV(string outputFullPath, string outputFullPathAudio)
        {
            var output = string.Concat(DateTime.Now.ToString("yyyyMMddHHmmss"), Path.GetFileName(outputFullPath));
            output = Path.Combine(Path.GetDirectoryName(outputFullPath), output);

            var p = Process.Start("ffmpeg.exe", $"-i {outputFullPath} -i {outputFullPathAudio} -c:v copy -c:a mp3 -map 0:v:0 -map 1:a:0 {output}");
            p.WaitForExit();
        }

        private string ReadAudio(string filename)
        {
            string outputFullPathAudio = Path.Combine(_workspace, Path.ChangeExtension(filename, ".wav"));
          

            BlobStorageManager audioStorageManager = new BlobStorageManager(filename);
            var audioStream = audioStorageManager.OpenRead();

 
          
            using (FileStream audioFs = new FileStream(outputFullPathAudio, FileMode.Create))
            using (WaveFileWriter audioWriter = new WaveFileWriter(audioFs, WaveFormat.CreateIeeeFloatWaveFormat(44100, 1)))
            {
                var buffer = new byte[4096];
                int bytesRead = 0;
                do
                {
                    bytesRead = audioStream.Read(buffer, 0, 4096);
                    audioWriter.Write(buffer, 0, buffer.Length);
                } while (bytesRead > 0);
            }
            return outputFullPathAudio;
        }

        private async Task<Bitmap> ReadFrameAsync(StreamReader sr)
        {
            var base64 = await sr.ReadLineAsync();
            return new Bitmap(new MemoryStream(Convert.FromBase64String(base64.Split(',')[1])));
        }
    }
}
