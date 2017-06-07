using Accord.Video.FFMPEG;
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
    public class WebRTCAudioRecordController : ApiController
    {
        private string _id;

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
                        RecordMemory.CreateWriter(_id);
                    }
                    else
                    {
                        await ReceiveSamplesAsync(socket);
                    }
                    


                  
                }
                else
                {
                    RecordMemory.CloseWriter(_id);
                    break;
                }
            }
        }

        private async Task ReceiveSamplesAsync(WebSocket socket)
        {
            try
            {

                List<byte[]> sample = new List<byte[]>();
                WebSocketReceiveResult result = null;
                do
                {
                    ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[4096]);
                    result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                    sample.Add(buffer.Array);
                } while (!result.EndOfMessage);

                byte[] samples = sample.SelectMany(x => x).ToArray();
                RecordMemory.AppendAudioFrame(_id, samples);
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