using System.Web.Mvc;
using ServiceStack.Text;
using UniPhotoGallery.Services;

namespace UniPhotoGallery.Extensions
{
    public static class PhotoExtension
    {
        public static IPhotoService PhotoService { get; set; }
        
        public static MvcHtmlString Photo(this HtmlHelper helper, int photoId, string typeName)
        {
            var photo = PhotoService.GetPhoto(photoId);

            if (photo != null)
            {
                var photoUrl = PhotoService.GetPhotoURL(photoId, typeName);
                if (!string.IsNullOrEmpty(photoUrl))
                {
                    var retString = "<img src=\"{0}\" title=\"{1}\" data-description=\"{1}\" />".Fmt(photoUrl, photo.Description);
                    return new MvcHtmlString(retString);
                }
            }

            return new MvcHtmlString(string.Empty);
        }
    }
}