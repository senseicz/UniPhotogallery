using ServiceStack.Logging;
using ServiceStack.Mvc;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.WebHost.Endpoints.Extensions;
using UniPhotoGallery.DomainModel.Domain;
using UniPhotoGallery.Globals;
using UniPhotoGallery.Services;

namespace UniPhotoGallery.Controllers
{
    public class ControllerBase : ServiceStackController<CustomUserSession>
    {
        public ILog Logger { get; set; }
        public ICacheClientExtended Cacher { get; set; }
        public IGalleryService GalleryService { get; set; }
        public IPhotoService PhotoService { get; set; }
        public IUserService UserService { get; set; }

        private Owner _selectedUser;

        public ControllerBase()
        {
            
        }

        public AuthService AuthService
        {
            get
            {
                var authService = ServiceStack.WebHost.Endpoints.AppHostBase.Instance.Container.Resolve<AuthService>();
                authService.RequestContext = new HttpRequestContext(
                    System.Web.HttpContext.Current.Request.ToRequest(),
                    System.Web.HttpContext.Current.Response.ToResponse(),
                    null);

                return authService;
            }
        }

        public Owner SelectedUser
        {
            get
            {
                var selectedUserName = RouteData.Values["username"].ToString();

                if (!string.IsNullOrEmpty(selectedUserName))
                {
                    return _selectedUser ?? (_selectedUser = UserService.GetOwnerByDirectory(selectedUserName));
                }
                return null;
            }
        }
    }
}
