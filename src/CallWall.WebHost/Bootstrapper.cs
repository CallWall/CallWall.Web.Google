using System;
using System.Linq;
using System.Web.Mvc;
using CallWall.Web;
using CallWall.WebHost.Hubs;
using CallWall.WebHost.Logging;
using CallWall.WebHost.Providers;
using CallWall.WebHost.Unity;
using Microsoft.Practices.Unity;
using Unity.Mvc4;

namespace CallWall.WebHost
{
    public static class Bootstrapper
    {
        public static IUnityContainer Initialise()
        {
            var container = Container.Create();

            DependencyResolver.SetResolver(new UnityDependencyResolver(container));

            return container;
        }
    }

    public sealed class TypeRegistry : ITypeRegistry
    {
        private readonly IUnityContainer _container;

        public TypeRegistry(IUnityContainer container)
        {
            _container = container;
        }

        public void RegisterType<TFrom, TTo>() where TTo : TFrom
        {
            _container.RegisterType<TFrom, TTo>();
        }

        public void RegisterType<TFrom, TTo>(string name) where TTo : TFrom
        {
            _container.RegisterType<TFrom, TTo>(name);
        }
    }

    public static class Container
    {
        public static IUnityContainer Create()
        {
            var container = new UnityContainer();

            container.AddNewExtension<GenericSupportExtension>();

            // register all your components with the container here
            // it is NOT necessary to register your controllers

            // e.g. container.RegisterType<ITestService, TestService>();    
            RegisterTypes(container);

            return container;
        }

        public static void RegisterTypes(IUnityContainer container)
        {
            new LoggerFactory().CreateLogger(typeof(Bootstrapper)).Trace("Registering types");
            container.RegisterType<ILoggerFactory, LoggerFactory>();
            container.RegisterType<ISecurityProvider, SecurityProvider>();
            container.RegisterType<ContactsHub>();

            InitialiseModules(container);
        }

        private static void InitialiseModules(IUnityContainer container)
        {
            var typeRegistry = new TypeRegistry(container);
            
            var moduleConfig = CallWallModuleSection.GetConfig();
            var modules = from moduleType in moduleConfig.Modules.Cast<ModuleElement>().Select(m => m.Type)
                          select (IModule)Activator.CreateInstance(moduleType);

            foreach (var module in modules)
            {
                module.Initialise(typeRegistry);
            }
        }

        public static bool IsModule(Type type)
        {
            var moduleType = typeof(IModule);
            return type.IsPublic
                   && !type.IsAbstract
                   && moduleType.IsAssignableFrom(type);
        }
    }
}