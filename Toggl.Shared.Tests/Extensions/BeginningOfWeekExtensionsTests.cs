using FluentAssertions;
using System.Globalization;
using Toggl.Shared.Extensions;
using Xunit;

namespace Toggl.Shared.Tests
{
    public sealed class BeginningOfWeekExtensionsTests
    {
        [Theory, LogIfTooSlow]
        [InlineData(BeginningOfWeek.Monday, "月曜日")]
        [InlineData(BeginningOfWeek.Tuesday, "火曜日")]
        [InlineData(BeginningOfWeek.Wednesday, "水曜日")]
        [InlineData(BeginningOfWeek.Thursday, "木曜日")]
        [InlineData(BeginningOfWeek.Friday, "金曜日")]
        [InlineData(BeginningOfWeek.Saturday, "土曜日")]
        [InlineData(BeginningOfWeek.Sunday, "日曜日")]
        public void LocalizesToJapaneseProperly(BeginningOfWeek beginningOfWeek, string translation)
        {
            CultureInfo.CurrentCulture = new CultureInfo("ja");

            beginningOfWeek.ToLocalizedString(CultureInfo.CurrentCulture).Should().Be(translation);
        }

        [Theory, LogIfTooSlow]
        [InlineData(BeginningOfWeek.Monday, "Monday")]
        [InlineData(BeginningOfWeek.Tuesday, "Tuesday")]
        [InlineData(BeginningOfWeek.Wednesday, "Wednesday")]
        [InlineData(BeginningOfWeek.Thursday, "Thursday")]
        [InlineData(BeginningOfWeek.Friday, "Friday")]
        [InlineData(BeginningOfWeek.Saturday, "Saturday")]
        [InlineData(BeginningOfWeek.Sunday, "Sunday")]
        public void LocalizesToEnglishProperly(BeginningOfWeek beginningOfWeek, string translation)
        {
            CultureInfo.CurrentCulture = new CultureInfo("en");

            beginningOfWeek.ToLocalizedString(CultureInfo.CurrentCulture).Should().Be(translation);
        }
    }
}
