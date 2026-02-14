using System.Globalization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Spectre.Console;


namespace RedPowerOffInformer;

public class LOEPowerInfo
{
    public DateOnly InfoFor { get; set; } = new DateOnly();
    public DateTime LastUpdated { get; set; } = new DateTime();

    public GroupInfo[] GroupInfos { get; set; } = [];

    public bool Finished { get; private set; } = false;

    public LOEPowerInfo(string apiResponse, LOEPowerInfoType targetDate)
    {
        string? rawHtml, rawHtmlMobile;

        JArray? menuItems = ExtractMenuItems(apiResponse);

        if (menuItems is null)
        {
            AnsiConsole.MarkupLine($"[bold]menuItems[/] is [bold]null[/] for [bold]{targetDate}[/]");
            return;
        }

        (rawHtml, rawHtmlMobile) = ExtractRawHtml(menuItems, targetDate);

        if (rawHtml != rawHtmlMobile)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            AnsiConsole.WriteLine($"RawHtml is different from RawMobileHtml.");
            AnsiConsole.WriteLine($"[RawHtml------]==[{rawHtml}].");
            AnsiConsole.WriteLine($"[RawMobileHtml]==[{rawHtmlMobile}].");
            AnsiConsole.WriteLine("Using RawHtml for further processing.");

            Console.ForegroundColor = oldColor;
        }

        bool inputDataIsNullOrEmpty = string.IsNullOrWhiteSpace(rawHtml);

        if (!inputDataIsNullOrEmpty)
        {
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

    private JArray? ExtractMenuItems(string apiResponse)
    {
        try
        {
            JObject apiResponseJObect = JObject.Parse(apiResponse);

            return (JArray?)apiResponseJObect?["hydra:member"]?[0]?["menuItems"];
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine("Something sus");
            AnsiConsole.WriteException(ex);
        }

        try
        {
            JObject apiResponseJObect = (JObject)JArray.Parse(apiResponse)[0];

            return (JArray?)apiResponseJObect?["menuItems"];
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[bold]SMERT[/]");
            AnsiConsole.WriteException(ex);
            throw;
        }
    }

    private (string? rawHtml, string? rawHtmlMobile) ExtractRawHtml(JArray menuItems, LOEPowerInfoType targetDate)
    {
        string? rawHtml = null, rawHtmlMobile = null;

        string searchNameString = targetDate.ToString();

        JObject? targetEntry = null;

        foreach (JObject menuItem in menuItems)
        {
            string? menuItemName = (string?)menuItem["name"];

            if (menuItemName == searchNameString)
            {
                targetEntry = menuItem;
                break;
            }
        }

        ArgumentNullException.ThrowIfNull(targetEntry);

        rawHtml = (string?)targetEntry["rawHtml"];
        rawHtmlMobile = (string?)targetEntry["rawMobileHtml"];

        return (rawHtml, rawHtmlMobile);
    }

    private void ParseAndPopulateData(string[] data)
    {
        string scheduleForString = string.Empty;
        string lastUpdatedString = string.Empty;

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
            throw new FormatException("Failed to parse scheduleForString.");

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