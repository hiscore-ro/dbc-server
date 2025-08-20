namespace DbcServer.Core.Configuration;

public class AppConfiguration
{
    public string DbfPath { get; set; } = "tmp";
    public string ServerUrl { get; set; } = "http://localhost:3000";
    public string Environment { get; set; } = "Production";
    public int CacheTtlMinutes { get; set; } = 15;
    public int MaxSearchResults { get; set; } = 100;
    public UpdateSettings UpdateSettings { get; set; } = new();
}

public class UpdateSettings
{
    public bool EnableAutoUpdate { get; set; } = true;
    public int CheckIntervalMinutes { get; set; } = 60;
    public string UpdateUrl { get; set; } = "https://github.com/hiscore-ro/dbc-server";
}