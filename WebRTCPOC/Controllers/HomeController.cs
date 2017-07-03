using System.Web.Mvc;
using WebRTCPOC.Models;

namespace WebRTCPOC.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index(string id)
        {
            var user = new User { Name = id };
            return View(user);
        }

      
        
    }
}