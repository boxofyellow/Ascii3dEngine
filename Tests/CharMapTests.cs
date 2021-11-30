using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ascii3dEngine.Tests
{
    [TestClass]
    public class CharMapTests
    {
        // See https://github.com/boxofyellow/Ascii3dEngine/commit/62253d1967cf57b427c49c0d009cc66beba32520#diff-0edd15c8285e8b0627a8526f65ee718b332f881b36c7a002fda15bc5405f3c9e
        // There were some issue with this method returning close, but not correct values
        [TestMethod]
        public void CharMapTests_PickFromCountWithCount()
        {
            var map = StaticColorValidationData.Map;
            int max = map.MaxX * map.MaxY
                    + 1; // We do this normally but it should still "work"

            // again the -1 here is not something that we do but it should work.
            for (int target = -1; target <= max; target++)
            {
                int minDifference = int.MaxValue;
                char bestChar = default;
                int bestCount = default;

                foreach (var count in map.Counts)
                {
                    int difference = Math.Abs(target - count.Count);
                    if (difference < minDifference)
                    {
                        bestChar = (char)count.Char;
                        bestCount = count.Count;
                        minDifference = difference;
                    }
                }

                var result = map.PickFromCountWithCount(target);
                int resultDifference = Math.Abs(target - result.Count);

                Assert.AreEqual(minDifference, resultDifference, $@"Did not get the expected result form PickFromCountWithCount
{nameof(target)}:{target},
{nameof(minDifference)}:{minDifference}
{nameof(bestChar)}:{bestChar}
{nameof(bestCount)}:{bestCount}
{nameof(result)}:{result}");
            }
        }

        [TestMethod]
        public void CharMapTests_PickFromRatio()
        {
            var map = StaticColorValidationData.Map;
            double max = map.MaxX * map.MaxY;

            int numberToCheck = 1000000;
            double improvement = 0;
            int count = 0;

            Random r = new(5);
            for (int i = 0; i < numberToCheck; i++)
            {
                double val = r.NextDouble() / 2.0;  // We want [0-0.5)  "t<0.5"
                int target = (int)(val * max);

                var ratioMatch = map.PickFromRatio(val);
                double ratioDif = Math.Abs(ratioMatch.PixelRatio - val);

                for (int j = -2; j < 3; j++)
                {
                    var match = map.PickFromCountWithCount(target + j);
                    double dif = Math.Abs(((double)match.Count / max) - val);
                    Assert.IsFalse(dif < ratioDif, $@"We did not pick the best one!
{nameof(i)}:{i}
{nameof(val)}:{val}
{nameof(j)}:{j}
{nameof(target)}:{target}
{nameof(ratioMatch)}:{ratioMatch}
{nameof(ratioDif)}:{ratioDif}
{nameof(match)}:{match}
{nameof(dif)}:{dif}");

                    if (j == 0 && ratioMatch.Character != match.Character)
                    {
                        count++;
                        improvement += dif - ratioDif;
                    }
                }
            }

            Console.WriteLine($"{nameof(count)}:{count}");
            Console.WriteLine($"{nameof(improvement)}:{improvement}");

            Assert.AreNotEqual(0, count, "Failed to find any that were improved");
        }
    }
}
