using TradeCopia.Domain.Safety;

namespace TradeCopia.Domain.UnitTests
{
    public class AccountSafetyClassifierTests
    {
        [Theory]
        [InlineData("Simulator", "Simulation", false, AccountSafetyClass.Simulation)]
        [InlineData("Simulator", "Live", false, AccountSafetyClass.Simulation)]
        [InlineData("Provider31", "Simulation", false, AccountSafetyClass.Simulation)]
        [InlineData("Playback", "Live", false, AccountSafetyClass.DemoPaper)]
        [InlineData("Provider31", "Live", true, AccountSafetyClass.DemoPaper)]
        [InlineData("InteractiveBrokers", "Live", false, AccountSafetyClass.Live)]
        [InlineData("Unknown", "", false, AccountSafetyClass.Unknown)]
        [InlineData("", "Live", false, AccountSafetyClass.Unknown)]
        [InlineData("Sim101", "Live", false, AccountSafetyClass.Live)]
        public void Official_metadata_not_display_name(string provider, string mode, bool isDemo, AccountSafetyClass expected)
        {
            Assert.Equal(expected, AccountSafetyClassifier.Classify(provider, mode, isDemo));
        }

        [Fact]
        public void Alpha_may_enable_only_simulation_and_demo()
        {
            Assert.True(AccountSafetyClassifier.AlphaMayEnable(AccountSafetyClass.Simulation));
            Assert.True(AccountSafetyClassifier.AlphaMayEnable(AccountSafetyClass.DemoPaper));
            Assert.False(AccountSafetyClassifier.AlphaMayEnable(AccountSafetyClass.Live));
            Assert.False(AccountSafetyClassifier.AlphaMayEnable(AccountSafetyClass.Unknown));
        }
    }
}
