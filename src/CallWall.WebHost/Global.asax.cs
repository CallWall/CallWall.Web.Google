using System.Web.Security;
using CallWall.Web;
using CallWall.WebHost.Logging;
using CallWall.WebHost.Providers;
using Microsoft.Practices.Unity;

namespace CallWall.WebHost
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        private readonly ILogger _logger;

        public MvcApplication()
        {
            _logger =new Log4NetLogger(GetType());
        }

        protected void Application_Start()
        {
            _logger.Info("Starting Application...");
            // The Startup class now does most of the work to play nicely with OWIN.
            _logger.Info("Application started.");
        }

        protected void FormsAuthentication_OnAuthentication(object sender, FormsAuthenticationEventArgs args)
        {
            _logger.Info("Authentication user...");
            //HACK: How do I inject the security provider into the global asax?
            //Perhaps this : http://www.hanselman.com/blog/IPrincipalUserModelBinderInASPNETMVCForEasierTesting.aspx 
            var securityProvider = Startup.Container.Resolve<ISecurityProvider>();

            var principal = securityProvider.GetPrincipal(args.Context.Request);
            if (principal != null)
            {
                args.Context.User = principal;
                _logger.Info("User authenticated.");
            }
            else
            {
                _logger.Info("User is anonymous.");
            }
        }

        public override void Dispose()
        {
            _logger.Info("Application being disposed.");
            base.Dispose();
        }
    }
}