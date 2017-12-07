using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Jil;
using System.Web.Http;

namespace WebApplication1.App_Start
{
    public class WebApiConfig
    {
        public static void Register(HttpConfiguration configuration)
        {
            configuration.MapHttpAttributeRoutes();

            configuration.Routes.MapHttpRoute("API Default", "api/{controller}/{id}",
                new { id = RouteParameter.Optional });

            configuration.Formatters.Clear();
            configuration.Formatters.Add(new JilFormatter());
        }
    }
}