using System.Web.Mvc;

namespace WebVideoPortal.Controllers
{
    public class HomeController : Controller
    {
        #region // Index Action
        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }
        #endregion

        #region // Shoutbox
        [HttpGet]
        public ActionResult Shoutbox()
        {
            return View();
        }
        #endregion

        #region // Resources
        public ActionResult Resources()
        {
            return View();
        }
        #endregion
    }
}