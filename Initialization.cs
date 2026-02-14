using System.Globalization;
using Spectre.Console;
using RedsXDG;
using Newtonsoft.Json;
using CommandLine;

namespace RedPowerOffInformer
{
    public class Initialization
    {
        internal static (Settings, Options) Start(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB", false);
            ApplicationPaths applicationPaths = new ApplicationPaths("RedPowerOffInformer");

            Settings? settings = null;
            Options? options = null;

            bool configExists = File.Exists(applicationPaths.MainConfigFile);

            if (configExists)
            {
                settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(applicationPaths.MainConfigFile));
            }

            Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(o =>
            {
               options = o;
            });

            settings ??= CreateNewSettingsAndSave(applicationPaths);
            options ??= new Options();

            return (settings, options);
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

    public class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option('g', "group", Required = false, HelpText = "Output data for a specific group.")]
        public string? Group { get; set; } = null;
    }
}