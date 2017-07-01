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
            BlobStorageManager indexStorageManager = new BlobStorageManager(Path.ChangeExtension(filename, ".vidx"));

          
            var videoStream = videoStorageManager.OpenRead();
            var indexStream = indexStorageManager.OpenRead();

            string outputFullPath = Path.Combine(_workspace, Path.ChangeExtension(filename, ".mp4"));

            using (VideoFileWriter videoWriter = new VideoFileWriter())
            using (StreamReader indexReader = new StreamReader(indexStream))
            {
                videoWriter.Open(outputFullPath, 320, 240, 10, VideoCodec.MPEG4, 16000, AudioCodec.MP3, 44100 * 16, 44100, 1);
                string length = string.Empty;
                do
                {
                    length = indexReader.ReadLine();
                    if (!string.IsNullOrEmpty(length) && int.Parse(length) > 100)
                    {
                        using (var frame = await ReadFrame(videoStream, int.Parse(length)))
                        {
                            videoWriter.WriteVideoFrame(frame);
                        }
                    }
                } while (!string.IsNullOrWhiteSpace(length));
            }

           
            
            var outputFullPathAudio = ReadAudio(Path.ChangeExtension(filename, ".adat"));

           
            
            MergeAV(outputFullPath, outputFullPathAudio);

            FileStream fs = new FileStream(outputFullPath, FileMode.Open);
            return fs;
        }



        private void MergeAV(string outputFullPath, string outputFullPathAudio)
        {
            var output = string.Concat(DateTime.Now.ToString("yyyyMMddHHmmss"), Path.GetFileName(outputFullPath));
            output = Path.Combine(Path.GetDirectoryName(outputFullPath), output);

            var p = Process.Start("ffmpeg.exe", $"-i {outputFullPath} -i {outputFullPathAudio} -c:v copy -c:a aac -strict experimental -map 0:v:0 -map 1:a:0 {output}");
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

        private async Task<Bitmap> ReadFrame(Stream videoStream, int length)
        {
            byte[] buffer = new byte[length];
            await videoStream.ReadAsync(buffer, 0, length);
            return new Bitmap(new MemoryStream(buffer));
        }
    }
}
