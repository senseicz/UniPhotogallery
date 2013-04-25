using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace UniPhotoGallery
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("api/{*pathInfo}");

            //routes.MapRoute(
            //    name: "Default",
            //    url: "{controller}/{action}/{id}",
            //    defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            //);

            routes.MapRoute(
                name: "AdminRoute",
                url: "admin/{action}/{id}",
                defaults: new { controller = "Admin", action = "Index", id = UrlParameter.Optional });

            routes.MapRoute(
                name: "AccountRoute",
                url: "account/{action}/{id}",
                defaults: new { controller = "Account", action = "Index", id = UrlParameter.Optional });

            routes.MapRoute(
                name: "UserRoute",
                url: "{username}/{controller}/{action}/{id}",
                defaults: new { username = "", controller = "Home", action = "Index", id = UrlParameter.Optional });

        }
    }
}