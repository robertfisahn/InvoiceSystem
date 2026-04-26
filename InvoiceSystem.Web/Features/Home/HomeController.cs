using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Features.Home;

public class HomeController : Controller
{
    [Route("")]
    public IActionResult Index()
    {
        return View();
    }
}
