using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace WebRTCPOC.Hubs
{
    public class WebRTCHub : Hub
    {
        public void Hello()
        {
            Clients.All.hello();
        }
    }
}