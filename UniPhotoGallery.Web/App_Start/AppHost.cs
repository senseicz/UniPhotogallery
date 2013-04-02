using System.Web.Mvc;
using Funq;
using ServiceStack.Api.Swagger;
using ServiceStack.Authentication.OpenId;
using ServiceStack.Configuration;
using ServiceStack.FluentValidation;
using ServiceStack.Logging;
using ServiceStack.Logging.Elmah;
using ServiceStack.Logging.NLogger;
using ServiceStack.MiniProfiler;
using ServiceStack.MiniProfiler.Data;
using ServiceStack.Mvc;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.SqlServer;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Admin;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.ServiceInterface.Cors;
using ServiceStack.WebHost.Endpoints;
using UniPhotoGallery.Controllers;
using UniPhotoGallery.Data;
using UniPhotoGallery.DomainModel;
using UniPhotoGallery.Extensions;
using UniPhotoGallery.Globals;
using UniPhotoGallery.Services;

namespace UniPhotoGallery
{
    //Provide extra validation for the registration process
    public class CustomRegistrationValidator : RegistrationValidator
    {
        public CustomRegistrationValidator()
        {
            RuleSet(ApplyTo.Post, () => RuleFor(x => x.DisplayName).NotEmpty());
        }
    }

    public class AppHost : AppHostBase
    {
        public AppHost()
            : base("UniPhotoGallery", typeof(AppHost).Assembly)
        {
        }

        public static AppConfig AppConfig;

        public static void Start()
        {
            new AppHost().Init();
        }

        public override void Configure(Container container)
        {
            //Register Typed Config some services might need to access
            var appSettings = new AppSettings();
            AppConfig = new AppConfig(appSettings);
            container.Register(AppConfig);
            
            //NLog
            container.Register(x => new NLogFactory());
            //Elmah 
            container.Register(x => new ElmahLogFactory(container.Resolve<NLogFactory>()));
            LogManager.LogFactory = container.Resolve<ElmahLogFactory>();
            container.Register<ILog>(x => LogManager.GetLogger(GetType()));

            //Cache
            container.Register<ICacheClientExtended>(new MemoryCacheClientExtended());
            
            //Plugins.Add(new RazorFormat());

            container.Register<IDbConnectionFactory>(new OrmLiteConnectionFactory(AppConfig.ConnectionString, false, SqlServerDialect.Provider));
            ConfigureAuth(container);

            //Register all repositories
            container.Register<IDBSetupRepository>(c => new DBSetupRepository { _db = c.Resolve<IDbConnectionFactory>() });
            container.Register<IGalleryRepository>(c => new GalleryRepository { _db = c.Resolve<IDbConnectionFactory>() });
            container.Register<IUserRepository>(c => new UserRepository { _db = c.Resolve<IDbConnectionFactory>() });
            container.Register<IPhotoRepository>(c => new PhotoRepository { _db = c.Resolve<IDbConnectionFactory>() });

            //Setup database
            SetupDatabase(container);

            //Register services
            container.Register<IBaseService>(c => new BaseService
                {
                    UserRepo = c.Resolve<IUserRepository>(),
                    Cacher = c.Resolve<ICacheClientExtended>(),
                    Logger = c.Resolve<ILog>(),
                    AppConfig = c.Resolve<AppConfig>()
                });

            container.Register<IImageProcessingService>(c => new ImageProcessingService());

            container.Register<IUserService>(c => new UserService() {_baseService = c.Resolve<IBaseService>()});
            
            container.Register<IPhotoService>(c => new PhotoService
                {
                    _baseService = c.Resolve<IBaseService>(),
                    _photoRepo = c.Resolve<IPhotoRepository>(),
                    ImageProcessingService = c.Resolve<IImageProcessingService>(),
                    AppConfig = c.Resolve<AppConfig>(),
                    UserService = c.Resolve<IUserService>()
                });

            container.Register<IGalleryService>(c => new GalleryService
            {
                _baseService = c.Resolve<IBaseService>(),
                _galleryRepo = c.Resolve<IGalleryRepository>(),
                PhotoService = c.Resolve<IPhotoService>(),
                UserService = c.Resolve<IUserService>()
            });

            ConfigureServiceRoutes();

            Plugins.Add(new SwaggerFeature());
            Plugins.Add(new CorsFeature());
            
            /*
            SetConfig(new EndpointHostConfig {
                CustomHttpHandlers = {
                    { HttpStatusCode.NotFound, new RazorHandler("/notfound") },
                    { HttpStatusCode.Unauthorized, new RazorHandler("/login") },
                }
            });
            */

            PhotoExtension.PhotoService = container.Resolve<IPhotoService>();

            //Set MVC to use the same Funq IOC as ServiceStack
            ControllerBuilder.Current.SetControllerFactory(new FunqControllerFactory(container));
            ServiceStackController.CatchAllController = reqCtx => container.TryResolve<HomeController>();
        }

        private void ConfigureAuth(Container container)
        {
            //Enable and register existing services you want this host to make use of.
            //Look in Web.config for examples on how to configure your oauth providers, e.g. oauth.facebook.AppId, etc.
            var appSettings = new AppSettings();

            //Register all Authentication methods you want to enable for this web app.            
            Plugins.Add(new AuthFeature(
                () => new CustomUserSession() {GalleryService = container.Resolve<IGalleryService>(), UserService = container.Resolve<IUserService>()}, 
                new IAuthProvider[] {
                    new CredentialsAuthProvider(),              //HTML Form post of UserName/Password credentials
                    new TwitterAuthProvider(appSettings),       //Sign-in with Twitter
                    new FacebookAuthProvider(appSettings),      //Sign-in with Facebook
                    new DigestAuthProvider(appSettings),        //Sign-in with Digest Auth
                    new BasicAuthProvider(),                    //Sign-in with Basic Auth
                    new GoogleOpenIdOAuthProvider(appSettings), //Sign-in with Google OpenId
                    new YahooOpenIdOAuthProvider(appSettings),  //Sign-in with Yahoo OpenId
                    new OpenIdOAuthProvider(appSettings),       //Sign-in with Custom OpenId
                }));

            //Provide service for new users to register so they can login with supplied credentials.
            Plugins.Add(new RegistrationFeature());

            //override the default registration validation with your own custom implementation
            container.RegisterAs<CustomRegistrationValidator, IValidator<Registration>>();

            //Create a DB Factory configured to access the UserAuth SQL Server DB
            var connStr = AppConfig.ConnectionString;

            container.Register<IDbConnectionFactory>(
                new OrmLiteConnectionFactory(connStr, //ConnectionString in Web.Config
                    SqlServerOrmLiteDialectProvider.Instance)
                    {
                        ConnectionFilter = x => new ProfiledDbConnection(x, Profiler.Current)
                    });

            //Store User Data into the referenced SqlServer database
            container.Register<IUserAuthRepository>(c => new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>())); //Use OrmLite DB Connection to persist the UserAuth and AuthProvider info

            var authRepo = (OrmLiteAuthRepository)container.Resolve<IUserAuthRepository>(); //If using and RDBMS to persist UserAuth, we must create required tables
            if (appSettings.Get("RecreateAuthTables", false))
            {
                authRepo.DropAndReCreateTables(); //Drop and re-create all Auth and registration tables
            }
            else
            {                
                authRepo.CreateMissingTables(); //Create only the missing tables
            }

            Plugins.Add(new RequestLogsFeature());
        }

        private void ConfigureServiceRoutes()
        {
            Routes
                //Hello World RPC example
                .Add<Hello>("/hello")
                .Add<Hello>("/hello/{Name*}")

                //Simple REST TODO example
                .Add<Todo>("/todos")
                .Add<Todo>("/todos/{Id}")
            ;
        }

        private void SetupDatabase(Container container)
        {
            var dbFactory = container.Resolve<IDBSetupRepository>();
            dbFactory.SetupDatabase();
        }
    }
}