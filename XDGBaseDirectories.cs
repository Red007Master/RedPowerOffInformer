namespace RedsXDG;

public class XDGBaseDirectories
{
    private static string GetHome()
        => Environment.GetEnvironmentVariable("HOME")
           ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    // Single directories (user-specific)
    public string ConfigHome
        => Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
           ?? Path.Combine(GetHome(), ".config");

    public string CacheHome
        => Environment.GetEnvironmentVariable("XDG_CACHE_HOME")
           ?? Path.Combine(GetHome(), ".cache");

    public string DataHome
        => Environment.GetEnvironmentVariable("XDG_DATA_HOME")
           ?? Path.Combine(GetHome(), ".local/share");

    public string StateHome
        => Environment.GetEnvironmentVariable("XDG_STATE_HOME")
           ?? Path.Combine(GetHome(), ".local/state");

    // This one is special - must exist and be valid
    public string RuntimeDir
        => Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR")
           ?? throw new InvalidOperationException("XDG_RUNTIME_DIR not set");

    // Search paths (system-wide, colon-separated)
    public string[] ConfigDirs
        => (Environment.GetEnvironmentVariable("XDG_CONFIG_DIRS") ?? "/etc/xdg")
           .Split(':', StringSplitOptions.RemoveEmptyEntries);

    public string[] DataDirs
        => (Environment.GetEnvironmentVariable("XDG_DATA_DIRS") ?? "/usr/local/share:/usr/share")
           .Split(':', StringSplitOptions.RemoveEmptyEntries);
}
