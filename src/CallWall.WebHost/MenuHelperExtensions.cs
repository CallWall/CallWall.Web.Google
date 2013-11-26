using System;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;

namespace CallWall.WebHost
{
    public static class MenuHelperExtensions
    {
        public static MvcHtmlString MenuLink(this HtmlHelper helper,
                                             string text,
                                             string action,
                                             string controller,
                                             object routeValues,
                                             object htmlAttributes,
                                             string currentClass)
        {
            var currentController = helper.ViewContext.RouteData.Values["controller"] as string ?? "home";
            var currentAction = helper.ViewContext.RouteData.Values["action"] as string ?? "index";
            var page = string.Format("{0}:{1}", currentController, currentAction);
            var thisPage = string.Format("{0}:{1}", controller, action);
            var attributes = new RouteValueDictionary(htmlAttributes);
            attributes["class"] = string.Equals(page, thisPage, StringComparison.InvariantCultureIgnoreCase) ? currentClass : "";

            var link = helper.ActionLink(text, action, controller, new RouteValueDictionary(routeValues), null);
            var listClass = string.Equals(page, thisPage, StringComparison.InvariantCultureIgnoreCase)
                                ? string.Format(" class=\"{0}\"", currentClass)
                                : string.Empty;

            return new MvcHtmlString(string.Format("<li{0}>{1}</li>", listClass, link));
        }
    }
}