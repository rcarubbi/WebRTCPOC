using AVRecordManager;
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
        private Stream _stream;
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
                        
                        _manager = new BlobStorageManager($"{_id}.adat");
                        _stream = _manager.OpenWrite();
                    }
                    else
                    {
                        var samples = await ReceiveSamplesAsync(socket);
                        _stream.Write(samples, 0, samples.Length);
                    }
                }
                else
                {
                   

                    _manager.Commit(_stream);
                    _stream.Close();
                    _stream.Dispose();
                    _stream = null;

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