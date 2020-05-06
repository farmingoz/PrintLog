using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Exceptionless;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using PrintLog.DAL.Data;
using PrintLog.Web.Jobs;

namespace PrintLog.Web {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            var hcBuilder = services.AddHealthChecks();

            hcBuilder.AddCheck("self", () => HealthCheckResult.Healthy());

            hcBuilder.AddSqlServer(
                this.Configuration.GetConnectionString("PrintLog"),
                name: "PrintLogDb-check",
                tags: new string[] { "PrintLogdbcheck" });

            hcBuilder.AddSqlServer(
                this.Configuration.GetConnectionString("Hangfire"),
                name: "HangfireDb-check",
                tags: new string[] { "hangfiredbcheck" });

            services.AddEntityFrameworkSqlServer()
                   .AddDbContext<PrintlogDbContext>(options => {
                       options.UseSqlServer(Configuration.GetConnectionString("PrintLog"),
                           sqlServerOptionsAction: sqlOptions => {
                               sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                           });
                   },
                       ServiceLifetime.Scoped  //Showing explicitly that the DbContext is shared across the HTTP request scope (graph of objects started in the HTTP request)
                   )
                .AddOptions();

            // Add Hangfire services.
            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(Configuration.GetConnectionString("Hangfire"), new SqlServerStorageOptions {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    UsePageLocksOnDequeue = true,
                    DisableGlobalLocks = true,
                    SchemaName = "PrintLog"
                }));

            // Add the processing server as IHostedService
            services.AddHangfireServer(opt => {
                opt.Queues = new[] { "PrintLog" };
            });

            services.AddTransient<IDashboardAuthorizationFilter, DashboardAuthorizationFilter>();
            services.AddSingleton<MasterSchedule>();
            services.AddSingleton<Prisma>();
            services.AddSingleton<Shiki>();
            services.AddSingleton<AutoDelete>();
            // Add framework services.
            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            var exceptionless = this.Configuration.GetSection("AppSettings:Exceptionless");
            var client = ExceptionlessClient.Default;
            client.Configuration.ApiKey = exceptionless["ApiKey"];
            client.Configuration.ServerUrl = exceptionless["ServerUrl"];
            app.UseExceptionless(client);

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            var dashboardAuthorizationFilter = app.ApplicationServices.GetService<IDashboardAuthorizationFilter>();
            app.UseHangfireDashboard("/dashboard", new DashboardOptions {
                Authorization = new[] { dashboardAuthorizationFilter }
            });
            lifetime.ApplicationStarted.Register(() => {
                var scheduler = app.ApplicationServices.GetRequiredService<MasterSchedule>();
                RecurringJob.AddOrUpdate("UpdateJob", () => scheduler.UpdateJob(), "0 5 * * *", TimeZoneInfo.Local, "printlog");

                var autoDelete = app.ApplicationServices.GetRequiredService<AutoDelete>();
                RecurringJob.AddOrUpdate("AutoDelete", () => autoDelete.DeleteLog(), "0 4 * * *", TimeZoneInfo.Local, "printlog");
            });
            lifetime.ApplicationStopping.Register(() => {
                SqlServerStorage.Current.GetConnection().Dispose();
            });
            app.UseEndpoints(endpoints => {
                endpoints.MapHealthChecks("/hc", new HealthCheckOptions() {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                endpoints.MapHealthChecks("/liveness", new HealthCheckOptions {
                    Predicate = r => r.Name.Contains("self")
                });
            });
        }

        private class DashboardAuthorizationFilter : IDashboardAuthorizationFilter {
            public bool Authorize([NotNull] DashboardContext context) {
                //var connection = context.GetHttpContext().Connection;
                //if (connection.RemoteIpAddress.ToString() == connection.LocalIpAddress.ToString()) return true;
                //var ipAddress = connection.RemoteIpAddress.ToString();
                //return ipAddress.StartsWith("10.6.5.") || ipAddress.StartsWith("10.8.5.") || ipAddress.StartsWith("192.168.114.");
                return true;
            }
        }
    }
}
