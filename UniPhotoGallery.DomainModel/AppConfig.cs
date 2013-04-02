using System.Collections.Generic;
using ServiceStack.Configuration;

namespace UniPhotoGallery.DomainModel
{
    //Hold App wide configuration you want to accessible by your services
    public class AppConfig
    {
        public AppConfig(IResourceManager appSettings)
        {
            //this.Env = appSettings.Get("Env", Env.Local);
            //this.EnableCdn = appSettings.Get("EnableCdn", false);
            //this.CdnPrefix = appSettings.Get("CdnPrefix", "");
            AdminUserNames = appSettings.Get("AdminUserNames", new List<string>());
            ConnectionString = ConfigUtils.GetConnectionString("DBConn");
            GalleryImagesRoot = appSettings.Get("GalleryImagesRoot", "/GalleryImages");
            UploadDirName = appSettings.Get("UploadDirName", "upload");
        }

        //public Env Env { get; set; }
        //public bool EnableCdn { get; set; }
        //public string CdnPrefix { get; set; }
        public List<string> AdminUserNames { get; set; }
        public string ConnectionString { get; set; }
        public string GalleryImagesRoot { get; set; }
        public string UploadDirName { get; set; }

        //public BundleOptions BundleOptions
        //{
        //    get { return Env.In(Env.Local, Env.Dev) ? BundleOptions.Normal : BundleOptions.MinifiedAndCombined; }
        //}
    }
}
