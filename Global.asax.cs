namespace WebApplication1
{
    using System.Web.Helpers;
    using System.Web.Mvc;
    using System.Web.Optimization;
    using System.Web.Routing;
    using Boilerplate.Web.Mvc;
    using WebApplication1.Services;
    using NWebsec.Csp;
    using System.Web;
    using System.Collections.Generic;
    using System;
    using WebApplication1.Controllers;
    using System.Net;

    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // Ensure that the X-AspNetMvc-Version HTTP header is not 
            MvcHandler.DisableMvcResponseHeader = true;

            ConfigureViewEngines();
            ConfigureAntiForgeryTokens();
            
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        /// <summary>
        /// Handles the Content Security Policy (CSP) violation errors. For more information see FilterConfig.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CspViolationReportEventArgs"/> instance containing the event data.</param>
        protected void NWebsecHttpHeaderSecurityModule_CspViolationReported(object sender, CspViolationReportEventArgs e)
        {
            // Log the Content Security Policy (CSP) violation.
            CspViolationReport violationReport = e.ViolationReport;
            CspReportDetails reportDetails = violationReport.Details;
            string violationReportString = string.Format(
                "UserAgent:<{0}>\r\nBlockedUri:<{1}>\r\nColumnNumber:<{2}>\r\nDocumentUri:<{3}>\r\nEffectiveDirective:<{4}>\r\nLineNumber:<{5}>\r\nOriginalPolicy:<{6}>\r\nReferrer:<{7}>\r\nScriptSample:<{8}>\r\nSourceFile:<{9}>\r\nStatusCode:<{10}>\r\nViolatedDirective:<{11}>",
                violationReport.UserAgent,
                reportDetails.BlockedUri,
                reportDetails.ColumnNumber,
                reportDetails.DocumentUri,
                reportDetails.EffectiveDirective,
                reportDetails.LineNumber,
                reportDetails.OriginalPolicy,
                reportDetails.Referrer,
                reportDetails.ScriptSample,
                reportDetails.SourceFile,
                reportDetails.StatusCode,
                reportDetails.ViolatedDirective);
            CspViolationException exception = new CspViolationException(violationReportString);
            DependencyResolver.Current.GetService<ILoggingService>().Log(exception);
        }
        
        /// <summary>
        /// Configures the view engines. By default, Asp.Net MVC includes the Web Forms (WebFormsViewEngine) and 
        /// Razor (RazorViewEngine) view engines that supports both C# (.cshtml) and VB (.vbhtml). You can remove view 
        /// engines you are not using here for better performance and include a custom Razor view engine that only 
        /// supports C#.
        /// </summary>
        private static void ConfigureViewEngines()
        {
            //ViewEngines.Engines.Clear();
            //ViewEngines.Engines.Add(new CSharpRazorViewEngine());

            //https://blogs.msdn.microsoft.com/marcinon/2011/08/16/optimizing-asp-net-mvc-view-lookup-performance/
            ViewEngines.Engines.Clear();
            var ve = new RazorViewEngine()
            {
                FileExtensions = new string[] { "cshtml" },
                AreaMasterLocationFormats = new string[] { "~/Areas/{2}/Views/{1}/{0}.cshtml", "~/Areas/{2}/Views/Shared/{0}.cshtml" },
                AreaPartialViewLocationFormats = new string[] { "~/Areas/{2}/Views/{1}/{0}.cshtml", "~/Areas/{2}/Views/Shared/{0}.cshtml" },
                AreaViewLocationFormats = new string[] { "~/Areas/{2}/Views/{1}/{0}.cshtml", "~/Areas/{2}/Views/Shared/{0}.cshtml" },
                MasterLocationFormats = new string[] { "~/Views/{1}/{0}.cshtml", "~/Views/Shared/{0}.cshtml" },
                PartialViewLocationFormats = new string[] { "~/Views/{1}/{0}.cshtml", "~/Views/Shared/{0}.cshtml" },
                ViewLocationFormats = new string[] { "~/Views/{1}/{0}.cshtml", "~/Views/Shared/{0}.cshtml" }
            };
            ve.ViewLocationCache = new TwoLevelViewCache(ve.ViewLocationCache);
            ViewEngines.Engines.Add(ve);
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            var app = sender as HttpApplication;
            if (app != null && app.Context != null)
            {
                app.Context.Response.Headers.Remove("Server");
            }
        }

        public class TwoLevelViewCache : IViewLocationCache
        {
            private readonly static object s_key = new object();
            private readonly IViewLocationCache _cache;

            public TwoLevelViewCache(IViewLocationCache cache)
            {
                _cache = cache;
            }

            private static IDictionary<string, string> GetRequestCache(HttpContextBase httpContext)
            {
                var d = httpContext.Items[s_key] as IDictionary<string, string>;
                if (d == null)
                {
                    d = new Dictionary<string, string>();
                    httpContext.Items[s_key] = d;
                }
                return d;
            }

            public string GetViewLocation(HttpContextBase httpContext, string key)
            {
                var d = GetRequestCache(httpContext);
                string location;
                if (!d.TryGetValue(key, out location))
                {
                    location = _cache.GetViewLocation(httpContext, key);
                    d[key] = location;
                }
                return location;
            }

            public void InsertViewLocation(HttpContextBase httpContext, string key, string virtualPath)
            {
                _cache.InsertViewLocation(httpContext, key, virtualPath);
            }
        }
        protected void Application_Error()
        {
            Exception exception = Server.GetLastError();
            Server.ClearError();

            int code = 0;
            if (exception.GetType() == typeof(HttpException))
            {
                code = ((HttpException)exception).GetHttpCode();
            }
            else
            {
                code = 500;
            }
            Server.TransferRequest("~/error/index?statusCode=" + code);
            //IController controller = new ErrorController();
            //controller.Execute(new RequestContext(new HttpContextWrapper(Context), routeData));
            //Response.End();
        }

        /// <summary>
        /// Configures the anti-forgery tokens. See 
        /// http://www.asp.net/mvc/overview/security/xsrfcsrf-prevention-in-aspnet-mvc-and-web-pages
        /// </summary>
        private static void ConfigureAntiForgeryTokens()
        {
            // Rename the Anti-Forgery cookie from "__RequestVerificationToken" to "f". This adds a little security 
            // through obscurity and also saves sending a few characters over the wire. Sadly there is no way to change 
            // the form input name which is hard coded in the @Html.AntiForgeryToken helper and the 
            // ValidationAntiforgeryTokenAttribute to  __RequestVerificationToken.
            // <input name="__RequestVerificationToken" type="hidden" value="..." />
            AntiForgeryConfig.CookieName = "f";

            // If you have enabled SSL. Uncomment this line to ensure that the Anti-Forgery 
            // cookie requires SSL to be sent across the wire. 
            // AntiForgeryConfig.RequireSsl = true;
        }
    }
}
