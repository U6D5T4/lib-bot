using FireSharp;
using FireSharp.Config;
using FireSharp.Interfaces;
using LibBot.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LibBot.Services;

public class ConfigureDb: IConfigureDb
{
    private static DbConfiguration _dbConfig;

    public ConfigureDb(IConfiguration configuration)
    {
        _dbConfig = configuration.GetSection("DbConfiguration").Get<DbConfiguration>();
    }

    public IFirebaseClient GetFirebaseClient()
    {
        IFirebaseConfig config = new FirebaseConfig
        {
            AuthSecret = _dbConfig.AuthSecret,
            BasePath = _dbConfig.BasePath
        };

        IFirebaseClient client = new FirebaseClient(config);
        return client;
    }
}


