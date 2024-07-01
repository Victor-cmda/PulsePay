using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Middleware
{
    public class ValidateSellerIdAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue("SellerId", out var sellerIdHeader) || !Guid.TryParse(sellerIdHeader, out Guid sellerId))
            {
                context.Result = new BadRequestObjectResult(new { Error = "Invalid or missing SellerId in header." });
                return;
            }

            context.ActionArguments["sellerId"] = sellerId;

            base.OnActionExecuting(context);
        }
    }
}
