using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using NAudio.Wave;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.WebSockets;

namespace WebRTCPOC.Api
{
    public class WebRTCAudioRecordController : ApiController
    {
        private string _id;
        private string _audioFilename;

        WaveFileWriter _audioWriter;
        FileStream _stream;


        public HttpResponseMessage Get()
        {
            if (HttpContext.Current.IsWebSocketRequest)
            {
                HttpContext.Current.AcceptWebSocketRequest(ProcessRecord, new AspNetWebSocketOptions { SubProtocol = "audio-protocol" });
            }
            return new HttpResponseMessage(HttpStatusCode.SwitchingProtocols);
        }
        public byte[] TrimEnd(byte[] array)
        {
            int lastIndex = Array.FindLastIndex(array, b => b != 0);

            Array.Resize(ref array, lastIndex + 1);

            return array;
        }
        private async Task ProcessRecord(AspNetWebSocketContext context)
        {
            try
            {
                bool idReceived = false;
                WebSocket socket = context.WebSocket;
                while (true)
                {


                    if (socket.State == WebSocketState.Open)
                    {
                        if (!idReceived)
                        {
                            await ReceiveIdAsync(socket);
                            idReceived = true;

                            _stream = new FileStream(_audioFilename, FileMode.CreateNew, FileAccess.ReadWrite);
                            _audioWriter = new WaveFileWriter(_stream, WaveFormat.CreateIeeeFloatWaveFormat(44100, 1));
                        }
                        else
                        {
                            await ReceiveSamplesAsync(socket);
                        }




                    }
                    else
                    {

                        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("AzureBlobStorage.ConnectionString"));

                        // Create the blob client.
                        CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                        // Retrieve a reference to a container.
                        CloudBlobContainer container = blobClient.GetContainerReference("webrtcpoccontainer");
                        container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });


                        CloudBlockBlob blockBlob = container.GetBlockBlobReference(Path.GetFileName(_audioFilename));

                        blockBlob.UploadFromStream(_stream);


                        while (!VideoRecorder.VideoIsReady(_id))
                        {
                            Thread.Sleep(1000);
                        }
                        VideoRecorder.CloseWriter(_id);
                        VideoRecorder.UploadVideo(_id, container);

                        _audioWriter.Close();
                        _audioWriter.Dispose();
                        _audioWriter = null;
                        _stream.Close();
                        _stream.Dispose();
                        _stream = null;

                        File.Delete(_audioFilename);
                        File.Delete(Path.ChangeExtension(_audioFilename, ".mp4"));
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                var x = ex;
            }
        }

        private async Task ReceiveSamplesAsync(WebSocket socket)
        {
            try
            {
                WebSocketReceiveResult result = null;
                do
                {
                    ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[4096]);
                    result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                    var bufferDecoded = TrimEnd(buffer.Array);
                    _audioWriter.Write(bufferDecoded, 0, bufferDecoded.Length);
                } while (!result.EndOfMessage);
            }
            catch (Exception ex)
            {
                var x = ex;
            }
        }

        private async Task ReceiveIdAsync(WebSocket socket)
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);
            WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, CancellationToken.None);
            _id = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
            _audioFilename = Path.Combine(HttpContext.Current.Server.MapPath("~/bin"), $"{_id}.wav");
            buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes("Id-Received"));
            await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}