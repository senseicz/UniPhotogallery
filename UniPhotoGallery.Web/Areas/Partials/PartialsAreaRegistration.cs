using System.Web.Mvc;

namespace UniPhotoGallery.Areas.Partials
{
    public class PartialsAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "Partials";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "Partials_default",
                "Partials/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
