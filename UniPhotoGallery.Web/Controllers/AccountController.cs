using System.Web.Mvc;
using System.Web.Security;
using ServiceStack.ServiceInterface.Auth;
using UniPhotoGallery.DomainModel.Auth;

namespace UniPhotoGallery.Controllers
{
    public class AccountController : ControllerBase
    {
        public IUserAuthRepository UserAuthRepo { get; set; } //injected
        
        public ActionResult Register()
        {
            var registerModel = new RegisterModel();
            return View(registerModel);
        }

        [HttpPost]
        public ActionResult Register(RegisterModel model)
        {
            string hash;
            string salt;
            new SaltedHash().GetHashAndSaltString(model.Password, out hash, out salt);

            var user = new UserAuth
                {
                    DisplayName = model.UserName,
                    Email = model.Email,
                    UserName = model.UserName,
                    PasswordHash = hash,
                    Salt = salt
                };
            
            var response = UserAuthRepo.CreateUserAuth(user, model.Password);
            var authResponse = AuthService.Authenticate(new Auth {UserName = model.UserName, Password = model.Password, RememberMe = true});

            return View();
        }

        [HttpPost]
        public ActionResult Logon(string email, string password)
        {
            var user = UserAuthRepo.GetUserAuthByUserName(email);

            if (user != null)
            {
                var authResponse = AuthService.Authenticate(new Auth
                        {
                            UserName = user.UserName,
                            Password = password,
                            RememberMe = true,
                        });
            }

            return RedirectToAction("Index", "Home");
        }

        public ActionResult Logout()
        {
            AuthService.Post(new Auth {provider = "logout"});
            return RedirectToAction("Index", "Home");
        }
    }
}
