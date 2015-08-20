using System.Net.Http.Formatting;
using System.Reflection;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;
using Owin;
using Panteon.Host.Infrastructure;

namespace Panteon.Host.API
{
    public class Startup
    {
        private ILifetimeScope _container;

        public void Configuration(IAppBuilder appBuilder)
        {
            var config = new HttpConfiguration();

            appBuilder.UseCors(Microsoft.Owin.Cors.CorsOptions.AllowAll);

            ContainerBuilder containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterModule<HostingModule>();
            // Register your Web API controllers.
            containerBuilder.RegisterApiControllers(Assembly.GetExecutingAssembly());

            _container = containerBuilder.Build();


            var resolver = new AutofacWebApiDependencyResolver(_container);
            config.DependencyResolver = resolver;

            config.Formatters.Clear();
            config.Formatters.Add(new JsonMediaTypeFormatter());
            config.Routes.MapHttpRoute("DefaultApi", "api/{controller}/{id}", new { id = RouteParameter.Optional });
            config.MapHttpAttributeRoutes();

            // OWIN WEB API SETUP:
            // Register the Autofac middleware FIRST, then the Autofac Web API middleware,
            // and finally the standard Web API middleware.
            appBuilder.UseAutofacMiddleware(_container);
            appBuilder.UseAutofacWebApi(config);
            appBuilder.UseWebApi(config);
        }
    }
}