using System.Text.RegularExpressions;


namespace RedPowerOffInformer
{
    public class GroupInfo
    {
        public string GroupName { get; private set; } = String.Empty;
        public Period[] PowerOffs { get; private set; } = [];

        public GroupInfo(string groupLine, DateOnly baseDate)
        {
            string groupNamePattern = @"Група (\d+\.\d+)";

            Match match = Regex.Match(groupLine, groupNamePattern);
            if (match.Success)
            {
                GroupName = match.Groups[1].Value;
            }
            else
            {
                throw new FormatException("Invalid group format");
            }

            string periodPattern = @"з (\d{2}:\d{2}) до (\d{2}:\d{2})";
            MatchCollection periodMatches = Regex.Matches(groupLine, periodPattern);

            List<Period> periods = new List<Period>();

            foreach (Match periodMatch in periodMatches)
            {
                string startTimeStr = periodMatch.Groups[1].Value;
                string endTimeStr = periodMatch.Groups[2].Value;

                startTimeStr = AntiMoron.FixMoronTimeString(startTimeStr);
                endTimeStr = AntiMoron.FixMoronTimeString(endTimeStr);

                TimeSpan start = TimeSpan.Parse(startTimeStr);
                TimeSpan end = TimeSpan.Parse(endTimeStr);

                DateTime startDate = baseDate.ToDateTime(TimeOnly.FromTimeSpan(start));
                DateTime endDate = baseDate.ToDateTime(TimeOnly.FromTimeSpan(end));

                // Handle overnight periods (end time < start time)
                if (end < start)
                {
                    endDate = endDate.AddDays(1);
                }

                Period period = new Period(startDate, endDate);
                periods.Add(period);
            }

            PowerOffs = periods.ToArray();
        }
    }

    public class Period
    {
        public DateTime Start { get; private set; } = new DateTime();
        public DateTime End { get; private set; } = new DateTime();

        public PeriodStatus Status { get { return GetPeriodStatus(); } }

        private PeriodStatus GetPeriodStatus()
        {
            if (Start < Clock.Now && End > Clock.Now)
            {
                return PeriodStatus.Active;
            }

            if (Start > Clock.Now)
            {
                return PeriodStatus.Future;
            }

            if (End < Clock.Now)
            {
                return PeriodStatus.Past;
            }

            return PeriodStatus.Unset;
        }

        public Period(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }

        public override string ToString()
        {
            if (Start.Date == End.Date)
            {
                return $"{Start.TimeOfDay} - {End.TimeOfDay} {DateOnly.FromDateTime(End.Date)}";
            }
            else
            {
                return $"{Start} - {End}";
            }
        }
    }

    public enum PeriodStatus
    {
        Unset,
        Future,
        Active,
        Past,
    }
}