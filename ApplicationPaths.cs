using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedsXDG
{
    public class ApplicationPaths
    {
        public XDGBaseDirectories XDGBaseDirectories { get; private set; } = new XDGBaseDirectories();

        public string ConfigHome { get; private set; }
        public string CacheHome { get; private set; }
        public string DataHome { get; private set; }
        public string StateHome { get; private set; }

        public string MainConfigFile { get; private set; }

        public ApplicationPaths(string programName)
        {
            if (FilenameIsValid(programName))
                throw new ArgumentException($"[{programName}] is NOT a valid filename.");

            ConfigHome = Path.Join(XDGBaseDirectories.ConfigHome, programName);
            CacheHome = Path.Join(XDGBaseDirectories.CacheHome, programName);
            DataHome = Path.Join(XDGBaseDirectories.DataHome, programName);
            StateHome = Path.Join(XDGBaseDirectories.StateHome, programName);

            MainConfigFile = Path.Combine(ConfigHome, "config.json");
        }

        private bool FilenameIsValid(string filename)
        {
            return !string.IsNullOrWhiteSpace(filename) && filename.IndexOfAny(Path.GetInvalidFileNameChars()) != -1;
        }
    }
}