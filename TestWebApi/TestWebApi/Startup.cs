namespace TestWebApi
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.FileProviders;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;
    using System;
    using TestWebApi.Filters;
    using TestWebApi.Interfaces;
    using TestWebApi.Middleware;
    using TestWebApi.Services;

    public class Startup
    {
        public Startup(IHostingEnvironment env, IConfiguration config)
        {
            HostingEnvironment = env;
            Configuration = config;
        }

        public IHostingEnvironment HostingEnvironment { get; }
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.

        // !! ASP.NET's container refers to the types it manages as services.
        // !! Controller Constructors can accept arguments that are not provided by dependency injection, but these must support default values.
        // !! IStartupFilter !!
        // Anywhere you need HttpContext, use HttpContextAccessor.
        public void ConfigureServices(IServiceCollection services)
        {
            // Create an interface and concrete
            services.AddSingleton<IToDoItemsList, ToDoItemsList>();
            // AddTransient: lifetime: each use.  lightweight, stateless services.
            // AddScoped: life time: each request.
            // AddSingleton: life time: lifetime of Application.

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            // Lifetime Samples
            services.AddTransient<IOperationTransient, Operation>();
            services.AddScoped<IOperationScoped, Operation>();
            services.AddSingleton<IOperationSingleton, Operation>();
            services.AddSingleton<IOperationSingletonInstance>(new Operation(Guid.Empty)); //using a specific instance, instead of letting IOC control lifetime.
            services.AddTransient<OperationService, OperationService>();

            //The container will call Dispose for IDisposable types it creates.

            // Avoid statics.

            services.AddTransient<StaticFileNotFoundRewriter>();

            services.AddOptions();
            services.AddMvc(
                options => 
                {
                    options.Filters.Add(new AsyncActionFilter());
                })
                 .AddJsonOptions(options =>
                 {                     
                     // options.SerializerSettings.Formatting = Formatting.Indented;
                     options.SerializerSettings.Converters = new JsonConverter[] { new StringEnumConverter() };
                     options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                     options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                 });

            // Optional.
            services.AddAuthorization(options =>
            {
                options.AddPolicy("BadgeEntry", policy =>
                    policy.RequireAssertion((Func<AuthorizationHandlerContext, bool>)CheckPolicy));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseExceptionHandler("/Home/Error"); // Call first to catch exceptions thrown in the following middleware.
                                                    // This is the path things get routed to when there is an exception.

            app.UseMiddleware<RequestLogger>();
            app.UseMiddleware<StaticFileNotFoundRewriter>(); // Won't accept parameter.

            // Serve my app-specific default file, if present.
            app.UseFileServer(GetFileServerOptions());

            app.UseAuthentication();               // Authenticate before you access secure resources.

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseExceptionHandler();
            app.UseStatusCodePages();
            // app.UseMvcWithDefaultRoute();

            // *** The first app.Run delegate terminates the pipeline.
            // *** app.use chains the delegates
            // *** Map* extensions are used as a convention for branching the pipeline
            app.Use(async (context, next) =>
            {
                // Do work that doesn't write to the Response.
                await next.Invoke();
                // Do logging or other work that doesn't write to the Response.

                //*** Don't call next.Invoke after the response has been sent to the client.
                //** HttpResponse.HasStarted is a useful hint to indicate if headers have been sent and/or the body has been written to.

                // ** When Map is used, the matched path segment(s) are removed from HttpRequest.Path and appended to HttpRequest.PathBase for each request

            });
            
            // app.UseMiddleware<FailureEmulator>();
            
            //app.Run(async context =>
            //{
            //    await context.Response.WriteAsync("Hello, World!");
            //});

            // app.UseMvcWithDefaultRoute();

            app.UseMvc();
        }

        private bool CheckPolicy(AuthorizationHandlerContext context)
        {
            if (context.Resource != null)
            {
                return false;
            }

            return context.User.HasClaim(c =>
                            (c.Issuer == "https://microsoftsecurity"));
        }

        private static string GetUiFolder()
        {
            return @"C:\ProgramData\Server Management Experience\Ux";
        }

        /// <summary>
        /// Generate and return file server options.
        /// </summary>
        /// <returns>returns the file server options for serving static files.</returns>
        private static FileServerOptions GetFileServerOptions()
        {
            var fileServerOptions = new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(GetUiFolder()),
                RequestPath = string.Empty,
                EnableDefaultFiles = true
            };

            fileServerOptions.StaticFileOptions.ServeUnknownFileTypes = true;
            fileServerOptions.StaticFileOptions.OnPrepareResponse =
                (responseContext) =>
                {
                    if (responseContext.File.Name == "index.html")
                    {
                        responseContext.Context.Response.Headers.Add("Cache-Control", new[] { "no-cache", "no-store", "must-revalidate" });
                        responseContext.Context.Response.Headers.Add("Pragma", new[] { "no-cache" });
                        responseContext.Context.Response.Headers.Add("Expires", new[] { "0" });
                    }
                };

            return fileServerOptions;
        }
    }
}
