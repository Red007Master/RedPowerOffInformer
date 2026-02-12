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
        public static void Start(string targetGroup)
        {
            string loeAPIUrl = "https://api.loe.lviv.ua/api/menus?page=1&type=photo-grafic";
            string urlContent = GetUrlContent(loeAPIUrl).Result;

            // File.WriteAllText("apiexample.json", GetUrlContent(loeAPIUrl).Result);
            // string urlContent = File.ReadAllText("apiexample.json");
            // LOEPowerInfo lOEPowerInfo = new LOEPowerInfo(urlContent);

            LOEPowerInfo lOEPowerInfoToday = new(urlContent, LOEPowerInfoType.Today);
            LOEPowerInfo lOEPowerInfoTomorrow = new(urlContent, LOEPowerInfoType.Tomorrow);

            OutputLOEInfoIfFinished(lOEPowerInfoToday, targetGroup);
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

        private static void OutputLOEInfoIfFinished(LOEPowerInfo lOEPowerInfo, string targetGroup)
        {
            var table = new Table();

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
                                timeColor = "red";
                                break;

                            case PeriodStatus.Active:
                                timeColor = "green";
                                break;

                            case PeriodStatus.Future:
                                timeColor = "blue";
                                break;

                            case PeriodStatus.Unset:
                                timeColor = "white";
                                break;
                        }

                        if (lOEPowerInfo.GroupInfos[i].PowerOffs[j].Status == PeriodStatus.Future)
                        {
                            table.AddRow(new Markup($"[bold]Period {j + 1}[/]"), new Markup($"[{timeColor}]{lOEPowerInfo.GroupInfos[i].PowerOffs[j]}[/] (in {(lOEPowerInfo.GroupInfos[i].PowerOffs[j].Start - DateTime.Now).ToString(@"hh\:mm\:ss")})"));
                        }
                        else
                        {
                            table.AddRow(new Markup($"[bold]Period {j + 1}[/]"), new Markup($"[{timeColor}]{lOEPowerInfo.GroupInfos[i].PowerOffs[j]}[/]"));
                        }
                    }

                    break;
                }
            }

            // Render the table
            AnsiConsole.Write(table);
        }
    }
}