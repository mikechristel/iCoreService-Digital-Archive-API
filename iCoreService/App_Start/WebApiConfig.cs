using System.Web.Http;
using Newtonsoft.Json.Serialization;

namespace iCoreService
{
    /// <summary>
    /// API global configuration
    /// </summary>
    public static class WebApiConfig
    {
        /// <summary>
        /// Register the configuration with the running assembly.
        /// </summary>
        /// <param name="config">A reference to the active configuration.</param>
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Add in support for cross-domain access.  Following advice of Mihai-Andrei Dinculescu in:
            // http://stackoverflow.com/questions/27504256/mvc-web-api-no-access-control-allow-origin-header-is-present-on-the-requested
            config.EnableCors();

            // How to globally enable NewtonSoft JSON.Net serializer with camelCased property names.
            // http://stackoverflow.com/questions/28552567/web-api-2-how-to-return-json-with-camelcased-property-names-on-objects-and-the
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Formatters.JsonFormatter.UseDataContractJsonSerializer = false;

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
