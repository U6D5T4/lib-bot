using FireSharp.Interfaces;

namespace LibBot.Services.Interfaces;

public interface IConfigureDb
{
    public IFirebaseClient GetFirebaseClient();
}
