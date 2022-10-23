using MathNet.Numerics.LinearAlgebra;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ascii3dEngine.Tests
{
    public static class TestUtilities
    {
        public static Point3D Round(Point3D p, int round) => p.Transform(x => Math.Round(x, round));

        public static Point3D RandomPoint(Random r, double scale) => new Point3D().Transform(_ => (r.NextDouble() * 2.0 * scale) - scale);

        public static void AssertPointsAreEqual(Point3D expected, Point3D actual, int round)
        {
            var rounded = Round(actual, round);
            Assert.AreEqual(expected, rounded, $"Expected {expected} got {actual} that was rounded to {rounded}");
        }

        public static void AssertMatrixAreEqual(double[,] expected, double[,] actual, int? round = null)
        {
            for (int i = 0; i < 2; i++)
            {
                Assert.AreEqual(expected.GetLength(i), actual.GetLength(i), $"Expected {expected.GetLength(i)} got {actual.GetLength(i)} Array's GetLength({i}) did not match");
            }

            for(int i = 0; i < expected.GetLength(0); i++)
            for(int j = 0; j < expected.GetLength(1); j++)
            {
                if (round.HasValue)
                {
                    var rounded = Math.Round(actual[i, j], round.Value);
                    Assert.AreEqual(expected[i, j], rounded, $"at [{i},{j}] Expected {expected[i, j]} got {actual[i, j]} that was rounded to {rounded}");
                }
                else
                {
                    Assert.AreEqual(expected[i, j], actual[i, j], $"at [{i},{j}] Expected {expected[i, j]} got {actual[i, j]}");
                }
            }
        }

        public static void AssertMatrixAreEqual(Matrix<double> expected, Matrix<double> actual, double delta)
        {
            Assert.AreEqual($"{expected.RowCount}x{expected.ColumnCount}", $"{actual.RowCount}x{actual.ColumnCount}");

            for (int row = 0; row < expected.RowCount; row++)
            for (int column = 0; column < expected.RowCount; column++)
            {
                Assert.AreEqual(expected[row, column], actual[row, column], delta, $"at [{row},{column}] Expected {expected[row, column]} got {actual[row, column]}");
            }
        }

        public static void AssertVectorsAreEqual(Vector<double> expected, Vector<double> actual, double delta)
        {
            Assert.AreEqual(expected.Count, actual.Count);

            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i], actual[i], delta, $"at [{i}] Expected {expected[i]} got {actual[i]}");
            }
        }

        // https://xkcd.com/221/ - Predictable test make debugging easier
        public static Random NewTestRandom => new (4);
    }
}