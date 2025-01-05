namespace AdminWrapper;


public static class ApplicationPaths
{
    public static readonly DirectoryInfo AdminWrapper;
    public static readonly DirectoryInfo Configs;
    public static readonly DirectoryInfo Logs;
    //public static readonly DirectoryInfo Plugins;
    //public static readonly DirectoryInfo Dependency;

    static ApplicationPaths()
    {
#if DEBUG
        var root = Environment.CurrentDirectory;
#else
        var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
#endif
        AdminWrapper = Directory.CreateDirectory(Path.Combine(root, nameof(AdminWrapper)));
        Configs = Directory.CreateDirectory(Path.Combine(AdminWrapper.FullName, nameof(Configs)));
        Logs = Directory.CreateDirectory(Path.Combine(AdminWrapper.FullName, nameof(Logs)));
        //Plugins = Directory.CreateDirectory(Path.Combine(AdminWraper.FullName, nameof(Plugins)));
        //Dependency = Directory.CreateDirectory(Path.Combine(AdminWraper.FullName, nameof(Dependency)));
    }
}
