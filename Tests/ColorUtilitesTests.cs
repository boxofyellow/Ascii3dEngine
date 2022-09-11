using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ascii3dEngine.Engine;

namespace Ascii3dEngine.Tests
{
    [TestClass]
    public class ColorUtilitiesTests
    {
        [TestMethod]
        public void ColorUtilitiesTests_NamedColorsMapToThenSelves()
        {   
            var octree = StaticColorValidationData.CreateOctree(8);

            foreach(var consoleColor in ColorUtilities.ConsoleColors)
            {
                var color = ColorUtilities.NamedColor(consoleColor);
                var match = ColorUtilities.BestMatch(StaticColorValidationData.Map, color);

                //  We don't need to check the foreground color b/c it is not used when char is ' ', and it is totally fine two methods computed different values 
                Assert.IsTrue(match.Background == consoleColor && match.Character == ' ' && match.Result == color, $@"Failed to get expected color back from ColorUtilities
{nameof(consoleColor)}:{consoleColor}
{nameof(color)}:{color}
{nameof(match)}:{match}");

                // this will pick some different items, but they are "correct"
                var octMatch = octree.BestMatch(color);
                Assert.AreEqual(color, octMatch.Result, $@"Failed to get expected color back from ColorOctree
{nameof(consoleColor)}:{consoleColor}
{nameof(color)}:{color}
{nameof(octMatch)}:{octMatch}
{nameof(match)}:{match}");
            }
        }
    }
}