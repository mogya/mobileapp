using System.Globalization;

namespace Toggl.Shared.Extensions
{
    public static class BeginningOfWeekExtensions
    {
        public static string ToLocalizedString(this BeginningOfWeek beginningOfWeek, CultureInfo cultureInfo)
            => cultureInfo.DateTimeFormat.DayNames[(int)beginningOfWeek];
    }
}
