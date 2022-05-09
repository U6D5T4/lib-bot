using LibBot.ConfigureServicesExtensions;
using LibBot.JobFactory;
using LibBot.Jobs;
using LibBot.Models;
using LibBot.Models.Configurations;
using LibBot.Schedular;
using LibBot.Services;
using LibBot.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using System;
using System.Collections.Generic;
using Telegram.Bot;

namespace LibBot;

public class Startup
{
    private readonly BotConfiguration _botConfiguration;
    private readonly IConfiguration Configuration;
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
        _botConfiguration = Configuration.GetSection("BotConfiguration").Get<BotConfiguration>();
    }
  
    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHostedService<ConfigureWebhook>();


        services.AddSharePointHttpClient(Configuration);
        services.AddHttpClient("tgwebhook")
          .AddTypedClient<ITelegramBotClient>(httpClient
              => new TelegramBotClient(_botConfiguration.BotToken, httpClient));

        services.AddScoped<IHandleUpdateService, HandleUpdateService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IMailService, MailService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISharePointService, SharePointService>();
        services.AddScoped<IUserDbService, UserDbService>();
        services.AddScoped<ICodeDbService, CodeDbService>();
        services.AddScoped<IConfigureDb, ConfigureDb>();
        services.AddScoped<IChatDbService, ChatDbService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IFileService, FileService>();

        services.Configure<EmailConfiguration>(Configuration.GetSection("EmailConfiguration"));
        services.Configure<DbConfiguration>(Configuration.GetSection("DbConfiguration"));
        services.Configure<BotConfiguration>(Configuration.GetSection("BotConfiguration"));
        services.Configure<BotCredentialsConfiguration>(Configuration.GetSection("BotCredentials"));
        services.Configure<SharePointConfiguration>(Configuration.GetSection("SharePointConfiguration"));


        services.AddSingleton<IJobFactory, MyJobFactory>();
        services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();

        #region Adding JobType
        services.AddSingleton<NotificationJob>();
        #endregion

        #region Adding Jobs 
        List<JobMetadata> jobMetadatas = new List<JobMetadata>();
        jobMetadatas.Add(new JobMetadata(Guid.NewGuid(), typeof(NotificationJob), "Notify Job", "0 0 12 * * ?"));

        services.AddSingleton(jobMetadatas);
        #endregion

        services.AddHostedService<NotificationSchedular>();

        services.AddControllers().AddNewtonsoftJson();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseCors();

        app.UseEndpoints(endpoints =>
        {
            var token = _botConfiguration.BotToken;
            endpoints.MapControllerRoute(name: "tgwebhook",
                                         pattern: $"bot/{token}",
                                         new { controller = "Webhook", action = "Post" });

            endpoints.MapControllers();
        });
    }
}
