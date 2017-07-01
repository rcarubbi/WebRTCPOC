using AVRecordManager;
using System;
using System.Collections.Generic;
using System.Configuration;
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
        private MemoryStream _buffer;
        private MemoryStream _indexBuffer;
        private BlobStorageManager _manager;
        private BlobStorageManager _indexManager;
        private StreamWriter _indexStreamWriter;
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
                        _buffer = new MemoryStream(int.Parse(ConfigurationManager.AppSettings["Video.BufferSize"]));
                        _indexBuffer = new MemoryStream();
                        _indexStreamWriter = new StreamWriter(_indexBuffer);

                        _manager = new BlobStorageManager($"{_id}.vdat");
                        _manager.OpenWrite();

                        _indexManager = new BlobStorageManager($"{_id}.vidx");
                        _indexManager.OpenWrite();
                    }
                    else
                    {
                        var frame = await ReceiveFrameAsync(socket);

                        if (_buffer.Position + frame.Length > _buffer.Capacity)
                        {
                            await _manager.UploadAsync(_buffer);
                            await _indexManager.UploadAsync(_indexBuffer);
                        }

                        await _buffer.WriteAsync(frame, 0, frame.Length);
                        await _indexStreamWriter.WriteLineAsync(frame.Length.ToString());

                    }
                }
                else
                {
                    if (_buffer.Position > 0)
                    {
                        await _manager.UploadAsync(_buffer);
                        await _indexManager.UploadAsync(_indexBuffer);
                    }

                    _manager.Commit();
                    _indexManager.Commit();
                    _manager.Dispose();
                    _indexManager.Dispose();

                    break;
                }
            }
        }

        private async Task<byte[]> ReceiveFrameAsync(WebSocket socket)
        {
            List<byte[]> frameParts = new List<byte[]>();
            WebSocketReceiveResult result = null;
            do
            {
                ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[6144]);
                result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                frameParts.Add(buffer.Array);
            } while (!result.EndOfMessage);

            byte[] frame = frameParts.SelectMany(x => x).ToArray().TrimEnd();
            return frame;
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