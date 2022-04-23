using FireSharp;
using FireSharp.Config;
using FireSharp.Interfaces;
using LibBot.Models.Configurations;
using LibBot.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace LibBot.Services;

public class ConfigureDb: IConfigureDb
{
    private static DbConfiguration _dbConfiguration;

    public ConfigureDb(IOptions<DbConfiguration> dbConfiguration)
    {
        _dbConfiguration = dbConfiguration.Value;
    }

    public IFirebaseClient GetFirebaseClient()
    {
        IFirebaseConfig config = new FirebaseConfig
        {
            AuthSecret = _dbConfiguration.AuthSecret,
            BasePath = _dbConfiguration.BasePath
        };

        IFirebaseClient client = new FirebaseClient(config);
        return client;
    }
}


