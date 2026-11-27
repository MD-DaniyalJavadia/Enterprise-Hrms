using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers;

[AllowAnonymous]
public sealed class ErrorController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        ViewData["StatusCode"] = 500;
        ViewData["Title"] = "Unexpected Error";
        ViewData["Message"] = "An unexpected error occurred while processing your request. Our team has been notified.";
        return View("Index");
    }

    [HttpGet]
    public IActionResult StatusCode(int code)
    {
        ViewData["StatusCode"] = code;
        ViewData["Title"] = code switch
        {
            404 => "Page Not Found",
            403 => "Access Denied",
            429 => "Too Many Requests",
            _ => "Request Error"
        };
        ViewData["Message"] = code switch
        {
            404 => "The requested page or resource could not be found.",
            403 => "You do not have permission to perform this action.",
            429 => "Too many requests received. Please wait a moment and try again.",
            _ => "We were unable to complete your request."
        };
        return View("Index");
    }
}
