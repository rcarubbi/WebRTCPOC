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
        BinaryWriter _writer;
        FileStream _stream;
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
                        _stream = new FileStream(_audioFilename, FileMode.CreateNew, FileAccess.Write);
                        _writer = new BinaryWriter(_stream);
                    }
                    else
                    {
                        await ReceiveSamplesAsync(socket);
                    }
                    


                  
                }
                else
                {
                    _writer.Close();
                    _writer.Dispose();
                    _writer = null;
                    RecordMemory.AddAudio(_id, _audioFilename);
                    RecordMemory.CloseWriter(_id);
                    break;
                }
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
                    _writer.Write(buffer.Array);
                    
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
            _audioFilename = Path.Combine(HttpContext.Current.Server.MapPath("~/records"), $"{_id}.wav");
            buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes("Id-Received"));
            await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}