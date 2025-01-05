namespace AdminWrapper.Config;

public static class ConfigHandler
{
    private static readonly Dictionary<ushort, ConfigContainer> _configs = new(); 


    public static ConfigContainer LoadPort(ushort port)
    {
        if (_configs.TryGetValue(port, out var config))
            return config;
        
        config = new ConfigContainer(GetPath($"port-{port}"));
        return config;
    }

    public static string GetPath(string file)
    {
        return Path.Combine(ApplicationPaths.Configs.FullName, file);
    }

}
