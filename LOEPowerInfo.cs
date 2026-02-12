using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Globalization;
using Newtonsoft.Json.Linq;
using System.Text;
using Spectre.Console;

namespace RedPowerOffInformer;

public class LOEPowerInfo
{
    public DateOnly InfoFor { get; set; } = new DateOnly();
    public DateTime LastUpdated { get; set; } = new DateTime();

    public GroupInfo[] GroupInfos { get; set; } = [];

    public bool Finished { get; private set; } = false;

    public LOEPowerInfo(string urlContent, LOEPowerInfoType targetDate)
    {
        JObject? lOEPowerInfoJObj = null;

        bool primaryParse = true;

        try
        {
            lOEPowerInfoJObj = JObject.Parse(urlContent);
        }
        catch (System.Exception ex)
        {
            Console.WriteLine("Something sus");
            AnsiConsole.WriteException(ex);
            primaryParse = false;
        }

        lOEPowerInfoJObj ??= (JObject)JArray.Parse(urlContent)[0];

        string? rawHtml, rawHtmlMobile;

        if (primaryParse)
        {
            if (targetDate == LOEPowerInfoType.Today)
            {
                rawHtml = (string?)lOEPowerInfoJObj["hydra:member"]?[0]?["menuItems"]?[0]?["rawHtml"];
                rawHtmlMobile = (string?)lOEPowerInfoJObj["hydra:member"]?[0]?["menuItems"]?[0]?["rawMobileHtml"];
            }
            else if (targetDate == LOEPowerInfoType.Tomorrow)
            {
                rawHtml = (string?)lOEPowerInfoJObj["hydra:member"]?[0]?["menuItems"]?[2]?["rawHtml"];
                rawHtmlMobile = (string?)lOEPowerInfoJObj["hydra:member"]?[0]?["menuItems"]?[2]?["rawMobileHtml"];
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        else
        {
            if (targetDate == LOEPowerInfoType.Today)
            {
                rawHtml = (string?)lOEPowerInfoJObj?["menuItems"]?[0]?["rawHtml"];
                rawHtmlMobile = (string?)lOEPowerInfoJObj?["menuItems"]?[0]?["rawMobileHtml"];
            }
            else if (targetDate == LOEPowerInfoType.Tomorrow)
            {
                rawHtml = (string?)lOEPowerInfoJObj["hydra:member"]?[0]?["menuItems"]?[2]?["rawHtml"];
                rawHtmlMobile = (string?)lOEPowerInfoJObj["hydra:member"]?[0]?["menuItems"]?[2]?["rawMobileHtml"];
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        bool inputDataIsNullOrEmpty = string.IsNullOrWhiteSpace(rawHtml) || string.IsNullOrWhiteSpace(rawHtmlMobile);

        if (!inputDataIsNullOrEmpty)
        {
            if (rawHtml != rawHtmlMobile)
            {
                ConsoleColor oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine($"RawHtml is different from RawMobileHtml.");
                Console.WriteLine($"[RawHtml------]==[{rawHtml}].");
                Console.WriteLine($"[RawMobileHtml]==[{rawHtmlMobile}].");

                Console.ForegroundColor = oldColor;
            }

            HtmlDocument document = new();

            ArgumentException.ThrowIfNullOrWhiteSpace(rawHtml);
            document.LoadHtml(rawHtml);

            HtmlNodeCollection paragraphs = document.DocumentNode.SelectNodes("//div/p");

            List<string> textToParse = new List<string>();

            foreach (var p in paragraphs)
            {
                textToParse.Add(p.InnerText);
            }

            ParseAndPopulateData(textToParse.ToArray());

            Finished = true;
        }
    }

    public LOEPowerInfo(string[] data)
    {
        ParseAndPopulateData(data);
    }

    private void ParseAndPopulateData(string[] data)
    {
        string scheduleForString = String.Empty;
        string lastUpdatedString = String.Empty;

        List<string> groupLines = new List<string>();

        for (int i = 0; i < data.Length; i++)
        {
            if (data[i].Contains("Графік погодинних відключень на"))
            {
                scheduleForString = data[i];
            }
            else if (data[i].Contains("Інформація станом на"))
            {
                lastUpdatedString = data[i];
            }
            else if (data[i].StartsWith("Група"))
            {
                groupLines.Add(data[i]);
            }
            else
            {
                throw new FormatException("Unhandled format.");
            }
        }

        if (scheduleForString == String.Empty)
            throw new FormatException("Failed to parse scheduleForString.");

        if (lastUpdatedString == String.Empty)
            throw new FormatException("Failezxd to parse scheduleForString.");

        CultureInfo ci = new CultureInfo("ua-UK");

        string scheduleForPatern = @"Графік погодинних відключень на (\d{2}.\d{2}.\d{4})";
        string lastUpdatedPatern = @"Інформація станом на (\d{2}.\d{2} \d{2}.\d{2}.\d{4})";

        Match scheduleForMatch = Regex.Match(scheduleForString, scheduleForPatern);
        InfoFor = DateOnly.ParseExact(AntiMoron.FixMoronTimeString(scheduleForMatch.Groups[1].Value), "dd.MM.yyyy", ci);
        Match lastUpdatedMatch = Regex.Match(lastUpdatedString, lastUpdatedPatern);
        LastUpdated = DateTime.ParseExact(AntiMoron.FixMoronTimeString(lastUpdatedMatch.Groups[1].Value), "HH:mm dd.MM.yyyy", ci);

        GroupInfos = ParseGroupLines(groupLines.ToArray(), InfoFor);
    }

    private GroupInfo[] ParseGroupLines(string[] groupLines, DateOnly baseDate)
    {
        List<GroupInfo> groupInfos = new List<GroupInfo>();

        for (int i = 0; i < groupLines.Length; i++)
        {
            groupInfos.Add(new GroupInfo(groupLines[i], baseDate));
        }

        return groupInfos.ToArray();
    }
}

public enum LOEPowerInfoType
{
    Today,
    Tomorrow
}