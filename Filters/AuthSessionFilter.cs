using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Survery_Certification_App_Consume.Filters
{
    public sealed class AuthSessionFilter : IActionFilter
    {
        private static readonly HashSet<string> PublicActions = new(StringComparer.OrdinalIgnoreCase)
        {
            "Auth.Login",
            "Auth.ForgotPassword",
            "Auth.SendOtp",
            "Auth.VerifyOtp",
            "Auth.ResendOtp",
            "Auth.SendRegistrationOtp",
            "Auth.ResetPassword",
            "Home.LandingPage",
            "Home.Login",
            "Home.ExternalLogin",
            "Home.SendOtp",
            "Home.VerifyOtp",
            "Home.Register",
            "Home.RegisterProjects",
            "Home.RegisterRoles",
            "Home.Logout",
            "Home.Error",
            "Home.TowageDemo",
            "Home.SeaFasteningDemo",
            "Home.LoadoutDemo"
        };

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (IsPublicAction(context))
                return;

            ApplyNoCacheHeaders(context.HttpContext.Response);

            var jwt = context.HttpContext.Session.GetString("JWT");
            var userId = context.HttpContext.Session.GetInt32("UserId") ?? 0;
            if (!string.IsNullOrWhiteSpace(jwt) && userId > 0)
                return;

            context.HttpContext.Session.Clear();

            if (WantsJson(context.HttpContext.Request))
            {
                context.Result = new JsonResult(new { message = "Session expired. Please sign in again." })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
                return;
            }

            context.Result = new RedirectToActionResult("Login", "Auth", new
            {
                returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString
            });
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (!IsPublicAction(context))
                ApplyNoCacheHeaders(context.HttpContext.Response);
        }

        private static bool IsPublicAction(FilterContext context)
        {
            if (context.ActionDescriptor is not ControllerActionDescriptor descriptor)
                return false;

            return PublicActions.Contains($"{descriptor.ControllerName}.{descriptor.ActionName}");
        }

        private static bool WantsJson(HttpRequest request)
        {
            if (string.Equals(
                    request.Headers["X-Requested-With"],
                    "XMLHttpRequest",
                    StringComparison.OrdinalIgnoreCase))
                return true;

            return request.Headers.Accept.Any(value =>
                value?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
        }

        private static void ApplyNoCacheHeaders(HttpResponse response)
        {
            response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
            response.Headers["Pragma"] = "no-cache";
            response.Headers["Expires"] = "0";
            response.Headers["Surrogate-Control"] = "no-store";
        }
    }
}
