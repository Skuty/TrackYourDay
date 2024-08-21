using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackYourDay.Tests.Analytics
{
    public class GropuedActivityTests
    {
        [Fact]
        public void GivenDurationWasNotExtendedByActivity_WhenDurationIsExtendedByActivityDuration_ThenDurrationIsExtendedByTimeOfActivity()
        {
            // Given

            // When

            // Then
            Assert.Fail();
        }

        [Fact]
        public void GivenDurationWasNotExtendedByActivity_WhenDurationIsExtendedByActivityDuration_ThenDurrationIsNotChanged()
        {
            // Given

            // When

            // Then
            Assert.Fail();
        }

        [Fact]
        public void GivenDurationWasNotReducedByBreak_WhenDurationIsReducedByNotOverlappingBreak_ThenDurrationIsReducedByFullTimeOfBreak)
        {
            // Given

            // When

            // Then
            Assert.Fail();
        }

        [Fact]
        public void GivenDurationWasNotReducedByBreak_WhenDurationIsReducedByPartiallyOverlappingBreak_ThenDurrationIsReducedByTimeOfPartiallyOverlappingBreak()
        {
            // Given

            // When

            // Then
            Assert.Fail();
        }

        [Fact]
        public void GivenDurationWasNotReducedByBreak_WhenDurationIsReducedByBreak_ThenDurrationIsReducedByTimeOfPartiallyOverlappingBreak()
        {
            // Given

            // When

            // Then
            Assert.Fail();
        }


        [Fact]
        public void GivenDurationWasReducedByBreak_WhenDurationIsReducedByBreak_ThenDurrationIsNotChanged()
        {
            // Given

            // When

            // Then
            Assert.Fail();
        }
    }
}
