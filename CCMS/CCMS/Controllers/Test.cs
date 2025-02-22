using Microsoft.AspNetCore.Mvc;

namespace CCMS.Controllers
{
    public class Test : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
