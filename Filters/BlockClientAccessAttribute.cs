using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Survery_Certification_App_Consume.Filters
{
    public sealed class BlockClientAccessAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var role = context.HttpContext.Session.GetString("UserRole");
            var roleName = context.HttpContext.Session.GetString("RoleName");

            if (!IsClient(role) && !IsClient(roleName))
            {
                base.OnActionExecuting(context);
                return;
            }

            var request = context.HttpContext.Request;
            var acceptsHtml = request.Headers.Accept.Any(value =>
                value?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true);
            var isAjax = string.Equals(
                request.Headers["X-Requested-With"],
                "XMLHttpRequest",
                StringComparison.OrdinalIgnoreCase);

            if (HttpMethods.IsGet(request.Method) && acceptsHtml && !isAjax)
            {
                context.Result = new RedirectToActionResult("ClientDashboard", "Home", null);
                return;
            }

            context.Result = new ObjectResult(new
            {
                message = "Client accounts can access assigned reports only."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        private static bool IsClient(string? role) =>
            string.Equals(role?.Trim(), "Client", StringComparison.OrdinalIgnoreCase);
    }
}
