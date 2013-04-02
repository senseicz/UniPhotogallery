using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using ServiceStack.Authentication.OpenId;
using ServiceStack.Common;
using ServiceStack.Configuration;
using ServiceStack.OrmLite;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.Text;
using UniPhotoGallery.DomainModel;
using UniPhotoGallery.DomainModel.Auth;
using UniPhotoGallery.DomainModel.Domain;

namespace UniPhotoGallery.Services
{
    /// <summary>
    /// Create your own strong-typed Custom AuthUserSession where you can add additional AuthUserSession 
    /// fields required for your application. The base class is automatically populated with 
    /// User Data as and when they authenticate with your application. 
    /// </summary>
    public class CustomUserSession : AuthUserSession
    {
        public IGalleryService GalleryService { get; set; }
        public IUserService UserService { get; set; }
        
        public string CustomId { get; set; }
        public int OwnerId { get; set; }
        public Owner Owner { get; set; }

        public override void OnAuthenticated(IServiceBase authService, IAuthSession session, IOAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            base.OnAuthenticated(authService, session, tokens, authInfo);

            var appSettings = new AppSettings();
            var config = new AppConfig(appSettings);

            //Populate all matching fields from this session to your own custom User table
            var user = session.TranslateTo<User>();
            user.Id = int.Parse(session.UserAuthId);
            user.GravatarImageUrl64 = !session.Email.IsNullOrEmpty()
                ? CreateGravatarUrl(session.Email, 64)
                : null;

            if (UserService.IsUserOwner(user.Id))
            {
                OwnerId = UserService.GetOwnerIdByUserId(user.Id);
                Owner = UserService.GetOwnerById(OwnerId);
            }

            foreach (var authToken in session.ProviderOAuthAccess)
            {
                if (authToken.Provider == FacebookAuthProvider.Name)
                {
                    user.FacebookName = authToken.DisplayName;
                    user.FacebookFirstName = authToken.FirstName;
                    user.FacebookLastName = authToken.LastName;
                    user.FacebookEmail = authToken.Email;
                    user.Email = authToken.Email;
                    user.UserName = authToken.DisplayName;
                }
                else if (authToken.Provider == TwitterAuthProvider.Name)
                {
                    user.TwitterName = authToken.DisplayName;
                    user.Email = authToken.Email;
                    user.UserName = authToken.DisplayName;
                }
                else if (authToken.Provider == GoogleOpenIdOAuthProvider.Name)
                {
                    user.GoogleUserId = authToken.UserId;
                    user.GoogleFullName = authToken.FullName;
                    user.GoogleEmail = authToken.Email;
                    user.Email = authToken.Email;
                    user.UserName = authToken.FullName;

                }
                else if (authToken.Provider == YahooOpenIdOAuthProvider.Name)
                {
                    user.YahooUserId = authToken.UserId;
                    user.YahooFullName = authToken.FullName;
                    user.YahooEmail = authToken.Email;
                    user.Email = authToken.Email;
                    user.UserName = authToken.FullName;
                }
            }

            if (config.AdminUserNames.Contains(session.Email) && !session.HasRole(RoleNames.Admin))
            {
                using (var assignRoles = authService.ResolveService<AssignRolesService>())
                {
                    assignRoles.Post(new AssignRoles {
                        UserName = session.UserAuthName,
                        Roles = { RoleNames.Admin }
                    });
                }
            }

            //Resolve the DbFactory from the IOC and persist the user info
            authService.TryResolve<IDbConnectionFactory>().Run(db => db.Save(user));
        }

        private static string CreateGravatarUrl(string email, int size = 64)
        {
            var md5 = MD5.Create();
            var md5HadhBytes = md5.ComputeHash(email.ToUtf8Bytes());

            var sb = new StringBuilder();
            for (var i = 0; i < md5HadhBytes.Length; i++)
                sb.Append(md5HadhBytes[i].ToString("x2"));

            string gravatarUrl = "http://www.gravatar.com/avatar/{0}?d=mm&s={1}".Fmt(sb, size);
            return gravatarUrl;
        }
    }
}