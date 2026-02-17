using System.Numerics;
using System.Text;
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

            string targetGroup = options.Group ??= settings.TargetGroup;

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
                        (string timeMap, string timeMapPointer, string timeMapLegendLine1, string timeMapLegendLine2) = GetTimeMap(lOEPowerInfo.GroupInfos[i].PowerOffs);

                        table.AddRow(
                            new Markup($"[bold]TimeMapPointer[/]"),
                            new Markup(timeMapPointer));

                        table.AddRow(
                            new Markup($"[bold]TimeMap[/]"),
                            new Markup(timeMap));

                        table.AddRow(
                            new Markup($""),
                            new Markup(timeMapLegendLine1));

                        table.AddRow(
                            new Markup($""),
                            new Markup(timeMapLegendLine2));
                    }

                    break;
                }
            }

            // Render the table
            AnsiConsole.Write(table);
        }

        private static (string timeMap, string timeMapPointer, string timeMapLegendLine1, string timeMapLegendLine2) GetTimeMap(Period[] powerOffs)
        {
            string timeMap = string.Empty;
            string timeMapPointer = string.Empty;

            int timeBlockLength = 30;
            int coefficient = 60 / timeBlockLength;
            int timeBlockArraySize = 24 * 60 / timeBlockLength;

            bool[] timeMapArray = new bool[timeBlockArraySize];
            char[] timeMapLegendArrayLine1 = new char[timeBlockArraySize];
            char[] timeMapLegendArrayLine2 = new char[timeBlockArraySize];

            foreach (Period powerOff in powerOffs)
            {
                DateTime startTime = powerOff.Start;
                DateTime endTime = powerOff.End;

                double startTimeInMinutes = startTime.Hour * 60 + startTime.Minute;
                int startBlock = Convert.ToInt32(Math.Round(startTimeInMinutes / timeBlockLength));

                double durationInMinutes = (endTime - startTime).TotalMinutes;
                int blockDuration = Convert.ToInt32(Math.Round(durationInMinutes / timeBlockLength));

                startBlock = Math.Max(0, startBlock);
                blockDuration = Math.Max(0, blockDuration);

                for (int blockI = startBlock; blockI < startBlock + blockDuration; blockI++)
                {
                    timeMapArray[blockI] = true;
                }
            }


            string colorOnEven = "#1fce00";
            string colorOnOdd = "#18a100";
            string colorOffEven = "#c90000";
            string colorOffOdd = "#a30000";

            for (int i = 0; i < timeMapArray.Length; i++)
            {
                bool even = i % 2 == 0;
                string currentColor = timeMapArray[i] ? (even ? colorOffEven : colorOffOdd) : (even ? colorOnEven : colorOnOdd);

                timeMap += $"[{currentColor}]█[/]";
            }


            DateTime currentTime = DateTime.Now;

            double currentTimeInMinutes = currentTime.Hour * 60 + currentTime.Minute;
            int currentTimeBlock = Convert.ToInt32(Math.Round(currentTimeInMinutes / timeBlockLength));

            currentTimeBlock = Math.Clamp(currentTimeBlock, 0, timeBlockArraySize - 1);

            timeMapPointer = new string(' ', currentTimeBlock) + "[slowblink bold]↓[/]";


            Array.Fill(timeMapLegendArrayLine1, ' ');
            Array.Fill(timeMapLegendArrayLine2, ' ');

            for (int i = 0; i < timeMapLegendArrayLine1.Length; i++)
            {
                if (i % 2 == 0)
                {
                    string targetHour = (i / coefficient).ToString();

                    timeMapLegendArrayLine1[i] = targetHour[0];

                    if (targetHour.Length == 2)
                        timeMapLegendArrayLine2[i] = targetHour[1];
                }
            }


            return (timeMap, timeMapPointer, new string(timeMapLegendArrayLine1), new string(timeMapLegendArrayLine2));
        }
    }
}