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
     
        public HttpResponseMessage Get()
        {
            if (HttpContext.Current.IsWebSocketRequest)
            {
                HttpContext.Current.AcceptWebSocketRequest(ProcessRecord, new AspNetWebSocketOptions { SubProtocol = "video-protocol" });
            }
            return new HttpResponseMessage(HttpStatusCode.SwitchingProtocols);
        }
        private async Task ProcessRecord(AspNetWebSocketContext context)
        {
            bool idReceived = false;
            WebSocket socket = context.WebSocket;

            while (true)
            {
                try
                {
                    if (socket.State == WebSocketState.Open)
                    {
                        if (!idReceived)
                        {
                            await ReceiveIdAsync(socket);
                            idReceived = true;
                            RecordMemory.CreateWriter(_id);
                        }
                        else
                        {
                            await ReceiveFrameAsync(socket);
                        }
                    }
                    else
                    {
                        RecordMemory.VideoReady(_id);
                     
                       
                        break;

                    }
                }

                catch (Exception ex)
                {
                    var x = ex;
                }
            }
        }

        public byte[] TrimEnd(byte[] array)
        {
            int lastIndex = Array.FindLastIndex(array, b => b != 0);

            Array.Resize(ref array, lastIndex + 1);

            return array;
        }

        private async Task ReceiveFrameAsync(WebSocket socket)
        {
            try
            {

                List<byte[]> frameParts = new List<byte[]>();
                WebSocketReceiveResult result = null;
                do
                {
                    ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);
                    result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                    frameParts.Add(buffer.Array);
                } while (!result.EndOfMessage);

                byte[] frame = frameParts.SelectMany(x => x).ToArray();
                RecordMemory.AppendVideoFrame(_id, new System.Drawing.Bitmap(new MemoryStream(TrimEnd(frame))));
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
            buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes("Id-Received"));
            await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}