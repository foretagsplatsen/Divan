using System;
namespace Divan
{
    public interface ICouchServer
    {
        void CreateDatabase(string name);
        void Debug(string message);
        void DeleteAllDatabases();
        void DeleteDatabase(string name);
        void DeleteDatabases(string regExp);
        T GetDatabase<T>(string name) where T : ICouchDatabase, new();
        ICouchDatabase GetDatabase(string name);
        T GetDatabase<T>() where T : ICouchDatabase, new();
        System.Collections.Generic.IList<string> GetDatabaseNames();
        T GetExistingDatabase<T>() where T : ICouchDatabase, new();
        T GetExistingDatabase<T>(string name) where T : ICouchDatabase, new();
        ICouchDatabase GetNewDatabase(string name);
        T GetNewDatabase<T>(string name) where T : ICouchDatabase, new();
        bool HasDatabase(string name);
        ICouchRequest Request();
        string ServerName { get; }
        string DatabasePrefix { get; set; }
        bool RunningOnMono { get; }
        string Host { get; }
        int Port { get; }
        string UserName { get; }
        string Password { get; }
        string EncodedCredentials { get; }
    }
}
