using System.Globalization;
using Spectre.Console;
using RedsXDG;
using Newtonsoft.Json;

namespace RedPowerOffInformer
{
    public class Initialization
    {
        internal static Settings Start()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB", false);
            ApplicationPaths applicationPaths = new ApplicationPaths("RedPowerOffInformer");

            Settings? settings = null;

            bool configExists = File.Exists(applicationPaths.MainConfigFile);

            if (configExists)
            {
                settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(applicationPaths.MainConfigFile));
            }

            settings ??= CreateNewSettingsAndSave(applicationPaths);

            return settings;
        }

        private static Settings CreateNewSettingsAndSave(ApplicationPaths applicationPaths)
        {
            Directory.CreateDirectory(applicationPaths.ConfigHome);

            Settings settings = new();

            settings.TargetGroup = AnsiConsole.Ask<string>("What's your [green]power-off group[/]?");

            File.WriteAllText(applicationPaths.MainConfigFile, JsonConvert.SerializeObject(settings, Formatting.Indented));

            return settings;
        }
    }

    public class Settings
    {
        public string TargetGroup { get; set; } = string.Empty;
    }

}