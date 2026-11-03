using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;


namespace CopyTrader.Web.Attributes
{
    public class CustomAuthorizeAttribute : TypeFilterAttribute
    {
        public CustomAuthorizeAttribute(string role) : base(typeof(AuthorizeRoleFilter))
        {
            Arguments = new object[] { role };
        }
    }

    public class AuthorizeRoleFilter : IAuthorizationFilter
    {
        private readonly string _role;

        public AuthorizeRoleFilter(string role)
        {
            _role = role;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (!context.HttpContext.User.Identity.IsAuthenticated)
            {
                // Redirect to login page or handle unauthorized access
                context.Result = new RedirectResult("/Identity/Account/Login");
                return;
            }

            if (!context.HttpContext.User.IsInRole(_role))
            {
                // Handle unauthorized access
                context.Result = new ForbidResult();
            }
        }
    }
}