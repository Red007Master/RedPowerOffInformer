using Spectre.Console;


namespace RedPowerOffInformer
{
    public static class AntiMoron
    {
        public static string FixMoronTimeString(string posibleMoronTimeString)
        {
            posibleMoronTimeString = posibleMoronTimeString.Replace("24:00", "23:59");

            return posibleMoronTimeString;
        }
    }

    public class Work
    {
        public static void Start(Settings settings, Options options)
        {
            string urlContent = GetUrlContent(settings.LOEAPIUrl).Result;

            // File.WriteAllText("apiexample.json", GetUrlContent(settings.LOEAPIUrl).Result);
            // string urlContent = File.ReadAllText("apiexample.json");
            // LOEPowerInfo lOEPowerInfo = new LOEPowerInfo(urlContent);

            string targetGroup = settings.TargetGroup;

            if (options.Group != null)
                targetGroup = options.Group;

            LOEPowerInfo lOEPowerInfoToday = new(urlContent, LOEPowerInfoType.Today);
            LOEPowerInfo lOEPowerInfoTomorrow = new(urlContent, LOEPowerInfoType.Tomorrow);

            OutputLOEInfoIfFinished(lOEPowerInfoToday, targetGroup, true);
            OutputLOEInfoIfFinished(lOEPowerInfoTomorrow, targetGroup);
        }

        private async static Task<string> GetUrlContent(string websiteUrl)
        {
            using (HttpClient client = new())
            {
                HttpResponseMessage response = await client.GetAsync(websiteUrl);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        private static void OutputLOEInfoIfFinished(LOEPowerInfo lOEPowerInfo, string targetGroup, bool addTimeMap = false)
        {
            var table = new Table();

            table.BorderColor(lOEPowerInfo.Finished ? ConsoleColor.White : ConsoleColor.Red);
            table.Border(TableBorder.Square);

            // Add columns
            table.AddColumn("Property");
            table.AddColumn("Value");

            // Add header information
            table.AddRow(new Markup("[bold]Info for[/]"), new Text(lOEPowerInfo.InfoFor.ToString()));
            table.AddRow(new Markup("[bold]Last Updated[/]"), new Text(lOEPowerInfo.LastUpdated.ToString()));

            for (int i = 0; i < lOEPowerInfo.GroupInfos.Length; i++)
            {
                if (lOEPowerInfo.GroupInfos[i].GroupName == targetGroup)
                {
                    table.AddRow(new Markup("[bold]Group Name[/]"), new Text(lOEPowerInfo.GroupInfos[i].GroupName));

                    // Add power-off periods
                    for (int j = 0; j < lOEPowerInfo.GroupInfos[i].PowerOffs.Length; j++)
                    {
                        string timeColor = "white";

                        switch (lOEPowerInfo.GroupInfos[i].PowerOffs[j].Status)
                        {
                            case PeriodStatus.Past:
                                timeColor = "#ff0000";
                                break;

                            case PeriodStatus.Active:
                                timeColor = "#008cff";
                                break;

                            case PeriodStatus.Future:
                                timeColor = "#f7a809";
                                break;

                            case PeriodStatus.Unset:
                                timeColor = "white";
                                break;
                        }

                        if (lOEPowerInfo.GroupInfos[i].PowerOffs[j].Status == PeriodStatus.Past)
                        {
                            table.AddRow(
                                new Markup($"[bold]Period {j + 1}[/]"),
                                new Markup($"[dim strikethrough {timeColor}]{lOEPowerInfo.GroupInfos[i].PowerOffs[j]}[/]"));
                        }
                        else if (lOEPowerInfo.GroupInfos[i].PowerOffs[j].Status == PeriodStatus.Future)
                        {
                            table.AddRow(
                                new Markup($"[bold]Period {j + 1}[/]"),
                                new Markup($"[{timeColor}]{lOEPowerInfo.GroupInfos[i].PowerOffs[j]}[/] (in {(lOEPowerInfo.GroupInfos[i].PowerOffs[j].Start - DateTime.Now).ToString(@"hh\:mm\:ss")})"));
                        }
                        else
                        {
                            table.AddRow(
                                new Markup($"[bold]Period {j + 1}[/]"),
                                new Markup($"[{timeColor}]{lOEPowerInfo.GroupInfos[i].PowerOffs[j]}[/]"));
                        }
                    }

                    if (addTimeMap)
                    {
                        table.AddRow(
                            new Markup($"[bold]TimeMapPointer[/]"),
                            new Markup(new string(' ', DateTime.Now.Hour - 1) + "↓"));

                        table.AddRow(
                            new Markup($"[bold]TimeMap[/]"),
                            new Markup(GetTimeMapString(lOEPowerInfo.GroupInfos[i].PowerOffs)));
                    }

                    break;
                }
            }

            // Render the table
            AnsiConsole.Write(table);
        }

        private static string GetTimeMapString(Period[] powerOffs)
        {
            //█

            bool[] timeMap = new bool[24];

            foreach (Period powerOff in powerOffs)
            {
                int startHour = powerOff.Start.Hour;
                int endHour = powerOff.End.Hour;

                for (int hour = startHour; hour < endHour; hour++)
                {
                    timeMap[hour] = true;
                }
            }

            string result = string.Empty;

            for (int i = 0; i < timeMap.Length; i++)
            {
                result += timeMap[i] ? "[red]█[/]" : "[green]█[/]";
            }

            return result;
        }
    }
}