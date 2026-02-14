using CommandLine;

namespace RedPowerOffInformer;

class Program
{
    static void Main(string[] args)
    {
        (Settings settings, Options options) = Initialization.Start(args);

        Work.Start(settings, options);
    }
}