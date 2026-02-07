using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Task4.Data;

namespace Task4.Filters
{
    public class RequireActiveUserAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var http = context.HttpContext;
            var userId = http.Session.GetInt32("UserId");

            if (userId == null)
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            var db = http.RequestServices.GetService(typeof(ApplicationDbContext)) as ApplicationDbContext;
            if (db == null)
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            var user = db.Users.FirstOrDefault(u => u.Id == userId.Value);

            if (user == null)
            {
                http.Session.Remove("UserId");
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            if (user.IsBlocked || !user.IsEmailVerified)
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
