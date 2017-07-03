using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WebRTCPOC.Models;

namespace WebRTCPOC.Api
{
    public class UserController : ApiController
    {
        public static Dictionary<string, string> users = new Dictionary<string, string>();

        public IHttpActionResult Get([FromUri] User user)
        {
            if (!string.IsNullOrEmpty(user.Id))
            {
                return Ok(users.Where(x => x.Value == user.Id).First().Key);
            }
            else if (!string.IsNullOrWhiteSpace(user.Name))
            {
                return Ok(users[user.Name]);
            }
            else
            {
                return BadRequest();
            }
        }

        public IHttpActionResult Post([FromBody] User user)
        {
            if (users.ContainsKey(user.Name))
            {
                users[user.Name] = user.Id;
            }
            else
            {
                users.Add(user.Name, user.Id);
            }

            return Ok();
        }
    }
}
