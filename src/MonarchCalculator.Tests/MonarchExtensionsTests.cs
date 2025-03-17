using Xunit;
using System;
using MonarchCalculator;

namespace MonarchsCalculator.Tests
{
    public class MonarchExtensionsTests
    {
        [Fact]
        public void ParseYears_SingleYear_SetsSameStartAndEnd()
        {
            var monarch = new Monarch { YrsRaw = "1016" };

            monarch.ParseYears();

            Assert.Equal(1016, monarch.StartYear);
            Assert.Equal(1016, monarch.EndYear);
        }

        [Fact]
        public void ParseYears_WhenSecondPartEmpty_SetsEndYearToCurrent()
        {
            var monarch = new Monarch { YrsRaw = "1952-" };
            int currentYear = DateTime.Now.Year;

            monarch.ParseYears();

            Assert.Equal(1952, monarch.StartYear);
            Assert.Equal(currentYear, monarch.EndYear);
        }

        [Fact]
        public void ParseYears_InvalidYear_SetsZero()
        {
            var monarch = new Monarch { YrsRaw = "NotAYear" };

            monarch.ParseYears();

            Assert.Equal(0, monarch.StartYear);
            Assert.Equal(0, monarch.EndYear);
        }
    }
}
