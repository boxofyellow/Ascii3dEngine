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
            CharMap map = StaticColorValidationData.Map;
            int max = map.MaxX * map.MaxY
                    + 1; // We do do this normally but it should still "work"

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
    }
}
