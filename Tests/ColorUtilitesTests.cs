using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;

namespace Ascii3dEngine.Tests
{
    [TestClass]
    public class ColorUtilitiesTests
    {
        [TestMethod]
        public void ColorUtilitiesTests_NamedColorsMapToThenSelves()
        {
            foreach(ConsoleColor consoleColor in ColorUtilities.ConsoleColors)
            {
                Color color = ColorUtilities.NamedColor(consoleColor);
                var result = ColorUtilities.BestMatch(TestUtilities.Map, color);

                //  We don't need to check the foreground color b/c it is not used when char is ' ', and it is totally fine two methods computed different values 
                //  This 0 here may be a little sketchy these are floats after all, but it does happen to hold true, and if it stops we should look to see why 
                Assert.IsTrue(result.Background == consoleColor && result.Character == ' ' && result.Difference == 0, $@"Failed to get expected color back
{nameof(consoleColor)}:{consoleColor}
{nameof(color)}:{color}
{nameof(result)}:{result}");
            }
        }

        [TestMethod]
        public void ColorUtilitiesTests_FancyMatchesBruteForce()
        {
            var result = ColorUtilities.BruteForce.TimeTest(TestUtilities.TestSettings);
            Assert.IsTrue(result.Max < 2.5 && result.Avg < 0.2, $@"Out Side of range {result}");
        }
    }
}