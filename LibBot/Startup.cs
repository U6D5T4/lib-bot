using System;
using LibBot.Models;
using LibBot.Services;
using LibBot.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace LibBot;

public class Startup
{
    private readonly BotConfiguration _botConfiguration;
    public Startup(IOptions<BotConfiguration> botConfiguration)
    {
        _botConfiguration = botConfiguration.Value;
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient("SharePoint", httpClient =>
        {
            httpClient.BaseAddress = new Uri("https://u6.itechart-group.com:8443/");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json;Odata=verbose");
        });

        services.AddHttpClient("tgwebhook")
          .AddTypedClient<ITelegramBotClient>(httpClient
              => new TelegramBotClient(_botConfiguration.BotToken, httpClient));

        services.AddScoped<IHandleUpdateService, HandleUpdateService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IMailService, MailService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISharePointService, SharePointService>();

        services.Configure<EmailConfiguration>(Configuration.GetSection("EmailConfiguration"));
        services.AddScoped<IUserDbService, UserDbService>();
        services.AddScoped<ICodeDbService, CodeDbService>();
        services.AddScoped<IConfigureDb, ConfigureDb>();

        services.AddOptions<DbConfiguration>("DbConfiguration");
        services.AddOptions<BotConfiguration>("BotConfiguration");

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
