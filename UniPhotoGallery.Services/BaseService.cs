using ServiceStack.Logging;
using UniPhotoGallery.Data;
using UniPhotoGallery.DomainModel;
using UniPhotoGallery.Globals;

namespace UniPhotoGallery.Services
{
    public interface IBaseService
    {
        IUserRepository UserRepo { get; set; }
        ILog Logger { get; set; }
        ICacheClientExtended Cacher { get; set; }
        AppConfig AppConfig { get; set; }
    }

    public class BaseService : IBaseService
    {
        public IUserRepository UserRepo { get; set; }
        public ILog Logger { get; set; }
        public ICacheClientExtended Cacher { get; set; }
        public AppConfig AppConfig { get; set; }
    }
}
