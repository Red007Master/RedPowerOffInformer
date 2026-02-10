namespace RedPowerOffInformer;

class Program
{
    static void Main(string[] args)
    {
        Settings settings = Initialization.Start();
        Work.Start(settings.TargetGroup);
    }
}