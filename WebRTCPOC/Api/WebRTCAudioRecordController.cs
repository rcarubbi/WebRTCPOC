using AVRecordManager;
using System;
using System.Configuration;
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
        private MemoryStream _buffer;
        private BlobStorageManager _manager;
        public HttpResponseMessage Get()
        {
            if (HttpContext.Current.IsWebSocketRequest)
            {
                HttpContext.Current.AcceptWebSocketRequest(ProcessRecord, new AspNetWebSocketOptions { SubProtocol = "audio-protocol" });
            }
            return new HttpResponseMessage(HttpStatusCode.SwitchingProtocols);
        }

        private async Task ProcessRecord(AspNetWebSocketContext context)
        {

            WebSocket socket = context.WebSocket;
            while (true)
            {
                if (socket.State == WebSocketState.Open)
                {
                    if (IsIdEmpty())
                    {
                        _id = await ReceiveIdAsync(socket);
                        _buffer = new MemoryStream(int.Parse(ConfigurationManager.AppSettings["Audio.BufferSize"]));
                        _manager = new BlobStorageManager($"{_id}.adat");
                        _manager.OpenWrite();
                    }
                    else
                    {
                        var samples = await ReceiveSamplesAsync(socket);
                        if (_buffer.Position + samples.Length > _buffer.Capacity)
                        {
                               await _manager.UploadAsync(_buffer);
                        }

                        _buffer.Write(samples, 0, samples.Length);
                    }
                }
                else
                {
                    if (_buffer.Position > 0)
                    {
                         await  _manager.UploadAsync(_buffer);
                    }

                    _manager.Commit();
                    _manager.Dispose();
                    break;
                }
            }

        }

       

        private bool IsIdEmpty()
        {
            return string.IsNullOrWhiteSpace(_id);
        }

        private async Task<byte[]> ReceiveSamplesAsync(WebSocket socket)
        {
            WebSocketReceiveResult result = null;
            ArraySegment<byte> arraySegment = new ArraySegment<byte>(new byte[4096]);
            result = await socket.ReceiveAsync(arraySegment, CancellationToken.None);
            var buffer = arraySegment.Array.TrimEnd();
            return buffer;
        }

        private async Task<string> ReceiveIdAsync(WebSocket socket)
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);
            WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, CancellationToken.None);
            string id = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
            buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes("Id-Received"));
            await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            return id;
        }
    }
}