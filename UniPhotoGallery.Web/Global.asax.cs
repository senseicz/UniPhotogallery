using System;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using ServiceStack.MiniProfiler;

namespace UniPhotoGallery
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            //WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            (new AppHost()).Init();
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            Console.WriteLine("Application_BeginRequest");

            if (Request.IsLocal)
            {
                Profiler.Start();
            }
        }

        protected void Application_EndRequest(object sender, EventArgs e)
        {
            Console.WriteLine("Application_EndRequest");
            Profiler.Stop();
        }
    }
}