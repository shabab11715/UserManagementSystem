using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Task4.Data;

namespace Task4.Filters
{
    public class RequireVerifiedUserAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var http = context.HttpContext;
            var userId = http.Session.GetInt32("UserId");

            if (userId == null)
            {
                context.Result = new RedirectToActionResult("Login", "Auth", new { reason = "login" });
                return;
            }

            var db = http.RequestServices.GetService(typeof(ApplicationDbContext)) as ApplicationDbContext;
            if (db == null)
            {
                context.Result = new RedirectToActionResult("Login", "Auth", new { reason = "login" });
                return;
            }

            var user = db.Users.FirstOrDefault(u => u.Id == userId.Value);

            if (user == null)
            {
                http.Session.Remove("UserId");
                context.Result = new RedirectToActionResult("Login", "Auth", new { reason = "login" });
                return;
            }

            if (user.IsBlocked)
            {
                http.Session.Remove("UserId");
                context.Result = new RedirectToActionResult("Login", "Auth", new { reason = "blocked" });
                return;
            }

            if (!user.IsEmailVerified)
            {
                http.Session.Remove("UserId");
                context.Result = new RedirectToActionResult("Login", "Auth", new { reason = "unverified" });
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}