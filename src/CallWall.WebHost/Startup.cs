using System;
using System.Collections.Generic;
using System.Threading;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using CallWall.Web;
using CallWall.WebHost.Logging;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Microsoft.Practices.Unity;
using Owin;

[assembly: OwinStartup(typeof(CallWall.WebHost.Startup))]
namespace CallWall.WebHost
{
    public class Startup : IDisposable
    {
        private static readonly ILogger _logger;
        private static readonly IUnityContainer _container;

        static Startup()
        {
            _logger = new Log4NetLogger(typeof(Startup));
            _container = Bootstrapper.Initialise();
        }

        public static IUnityContainer Container
        {
            get { return _container; }
        }

        public void Configuration(IAppBuilder app)
        {
            _logger.Info("Owin Starting...");
            var ct = GetShutdownToken(app.Properties);
            ct.Register(Dispose);

            RegisterHubs(app, Container);
            AreaRegistration.RegisterAllAreas();
            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            _logger.Info("Owin started.");
        }

        private void RegisterHubs(IAppBuilder app, IUnityContainer container)
        {
            _logger.Info("SignalR Hubs registration starting ...");
            var config = new HubConfiguration
                {
                    Resolver = new UnitySignalRDependencyResolver(container)
                };

            app.MapSignalR(config);
            _logger.Info("SignalR Hubs registered.");
        }

        public void Dispose()
        {
            _logger.Info("Owin being disposed.");
            using (_container)
            { }
        }


        public const string OwinConstantsHostOnAppDisposing = "host.OnAppDisposing";
        internal static CancellationToken GetShutdownToken(IDictionary<string, object> env)
        {
            object value;
            return env.TryGetValue(OwinConstantsHostOnAppDisposing, out value)
                && value is CancellationToken
                ? (CancellationToken)value
                : default(CancellationToken);
        }
    }
}