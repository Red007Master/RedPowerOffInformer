using System.Globalization;
using CommandLine;
using Newtonsoft.Json;
using RedsXDG;
using Spectre.Console;


namespace RedPowerOffInformer
{
    public class Initialization
    {
        internal static (Settings, Options) Start(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB", false);
            ApplicationPaths applicationPaths = new ApplicationPaths("RedPowerOffInformer");

            try
            {
                Clock.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Kyiv");
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
                Clock.TimeZoneOffset = TimeSpan.FromHours(2);
            }

            Settings? settings = null;
            Options? options = null;

            bool configExists = File.Exists(applicationPaths.MainConfigFile);

            if (configExists)
            {
                settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(applicationPaths.MainConfigFile));

                FixSettingIfInvalid(settings, applicationPaths);
            }

            Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(o =>
            {
                options = o;
            });

            settings ??= CreateNewSettingsAndSave(applicationPaths);

            if (options is null)
                Environment.Exit(0);

            return (settings, options);
        }

        private static void FixSettingIfInvalid(Settings? settings, ApplicationPaths applicationPaths)
        {
            bool configFixed = false;

            ArgumentNullException.ThrowIfNull(settings);

            if (string.IsNullOrWhiteSpace(settings.LOEAPIUrl))
            {
                settings.LOEAPIUrl = "https://api.loe.lviv.ua/api/menus?page=1&type=photo-grafic";

                configFixed = true;
            }

            if (configFixed)
            {
                SaveSettings(settings, applicationPaths);
            }
        }

        private static void SaveSettings(Settings settings, ApplicationPaths applicationPaths)
        {
            File.WriteAllText(applicationPaths.MainConfigFile, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }

        private static Settings CreateNewSettingsAndSave(ApplicationPaths applicationPaths)
        {
            Directory.CreateDirectory(applicationPaths.ConfigHome);

            Settings settings = new();

            settings.TargetGroup = AnsiConsole.Ask<string>("What's your [green]power-off group[/]?");

            SaveSettings(settings, applicationPaths);

            return settings;
        }
    }

    public class Settings
    {
        public string TargetGroup { get; set; } = string.Empty;
        public string LOEAPIUrl { get; set; } = "https://api.loe.lviv.ua/api/menus?page=1&type=photo-grafic";
    }

    public class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option('g', "group", Required = false, HelpText = "Output data for a specific group.")]
        public string? Group { get; set; } = null;
    }
}