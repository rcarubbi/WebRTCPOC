using AVRecordManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public class WebRTCVideoRecordController : ApiController
    {
        private string _id;
        private Stream _stream;
        private BlobStorageManager _manager;
        private StreamWriter _sw;
        public HttpResponseMessage Get()
        {
            if (HttpContext.Current.IsWebSocketRequest)
            {
                HttpContext.Current.AcceptWebSocketRequest(ProcessRecord, new AspNetWebSocketOptions { SubProtocol = "video-protocol" });
            }
            return new HttpResponseMessage(HttpStatusCode.SwitchingProtocols);
        }


        private bool IsIdEmpty()
        {
            return string.IsNullOrWhiteSpace(_id);
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
                        _manager = new BlobStorageManager($"{_id}.vdat");
                        _stream = _manager.OpenWrite();
                        _sw = new StreamWriter(_stream);
                    }
                    else
                    {
                        var frame = await ReceiveFrameAsync(socket);
                        await _sw.WriteLineAsync(frame);
                    }
                }
                else
                {
                    _manager.Commit(_stream);
                    _sw.Close();
                    _sw.Dispose();
                    _sw = null;
                    _stream.Close();
                    _stream.Dispose();
                    _stream = null;
                    break;
                }
            }
        }

        private async Task<string> ReceiveFrameAsync(WebSocket socket)
        {
            List<byte[]> base64BufferParts = new List<byte[]>();
            WebSocketReceiveResult result = null;
            do
            {
                ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[8000]);
                result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                base64BufferParts.Add(buffer.Array);
            } while (!result.EndOfMessage);

            byte[] base64Buffer = base64BufferParts.SelectMany(x => x).ToArray().TrimEnd();
           
            using (var ms = new MemoryStream(base64Buffer))
            using (var sr = new StreamReader(ms))
            {
                return sr.ReadToEnd();    
            }
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