using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ascii3dEngine.Tests
{
    [TestClass]
    public class UtilitiesTests
    {
        [TestMethod("Check for bug that broke 'Looking Right'")]
        public void CheckAffineTransformationForRotatingAroundUnitWithSource()
        {
            var from = new Point3D(0, 3, 0);
            var to = new Point3D(0, 3, 1);
            var up = Point3D.YUnit;

            var direction = to - from;

            var d = Utilities.DegreesToRadians(-1);
            var transform = Utilities.AffineTransformationForRotatingAroundUnit(up, d, from);
            var actual = direction.ApplyAffineTransformation(transform);
            var expected = new Point3D(-0.01745240643728351, 3, 0.9998476951563913);
            TestUtilities.AssertPointsAreEqual(expected, actual, round: 15);
        }

        [TestMethod("Page 216 Practice Exercise 5.2.1 Apply The Transform")]
        public void Exercise_5_2_1()
        {
            // The practice exercise is in 2d
            var m = new [,]
            {
                { 3.0, 0.0, 0.0, 5.0},
                {-2.0, 1.0, 0.0, 2.0},
                { 0.0, 0.0, 1.0, 0.0},
                { 0.0, 0.0, 0.0, 1.0},
            };

            var P = new Point3D(1, 2, 0);
            var actual = P.ApplyAffineTransformation(m);

            var Q = new Point3D(8, 2, 0);
            Assert.AreEqual(Q, actual);
        }

        [TestMethod("Page 220 Example 5.2.1")]
        public void Example_5_2_1()
        {
            // The practice exercise is in 2d
            var P = new Point3D(3, 5, 0);

            var radians = Utilities.DegreesToRadians(60);

            var m = Utilities.AffineTransformationForRotatingAroundUnit(Point3D.ZUnit, radians);

            var c = Math.Cos(radians);
            var s = Math.Sin(radians);

            // (matches 5.10) transformed to 3d
            TestUtilities.AssertMatrixAreEqual(new [,]
            {
                { c, -s, 0, 0},
                { s,  c, 0, 0},
                { 0,  0, 1, 0},
                { 0,  0, 0, 1},
            }, m);

            var actual = P.ApplyAffineTransformation(m);

            var Q = new Point3D(
                x: (P.X * c) - (P.Y * s),
                y: (P.X * s) + (P.Y * c),
                z: 0 
            );

            Assert.AreEqual(Q, actual);

            Assert.AreEqual(actual.Length, Q.Length, "The Transformation should should not have changed their Length");
        }

        [DataTestMethod]
        [DataRow( 2,  3,  -45,  3.5355,  0.7071, DisplayName = "Page 220 Practice Exercise  5.2.3 Rotate a point A")]
        [DataRow( 1,  1, -180, -1.0,    -1.0,    DisplayName = "Page 220 Practice Exercise  5.2.3 Rotate a point B")]
        [DataRow(60, 61,    4, 55.5987, 65.0368, DisplayName = "Page 220 Practice Exercise  5.2.3 Rotate a point C")]
        public void Exercise_5_2_3(int pX, int pY, int degrees, double qX, double qY)
        {
            // More 2d exercises
            var P = new Point3D(pX, pY, z: 0);
            var m = Utilities.AffineTransformationForRotatingAroundUnit(Point3D.ZUnit, Utilities.DegreesToRadians(degrees));
            var actual = P.ApplyAffineTransformation(m);
            var Q = new Point3D(qX, qY, z: 0);

            TestUtilities.AssertPointsAreEqual(Q, actual, 4);
            Assert.AreEqual(actual.Length, Q.Length, delta: 0.0001);
        }

        [TestMethod("Page 223 Exercise 5.2.5 What is the inverse of a rotation")]
        public void Exercise_5_2_5()
        {
            var random = TestUtilities.NewTestRandom;

            for (int degrees = 0; degrees <= 360; degrees++)
            {
                var radians = Utilities.DegreesToRadians(degrees);

                var unit = TestUtilities.RandomPoint(random, 1.0).Normalized();

                var m = Utilities.AffineTransformationForRotatingAroundUnit(unit, radians);
                var mInverse = Utilities.AffineTransformationForRotatingAroundUnit(unit, -radians);

                var result = DenseMatrix.OfArray(m) * DenseMatrix.OfArray(mInverse);
                TestUtilities.AssertMatrixAreEqual(result, Matrix<double>.Build.DenseIdentity(4), delta: 0.00000001);

                var p = TestUtilities.RandomPoint(random, 1000.0);

                var q = p.ApplyAffineTransformation(m).ApplyAffineTransformation(mInverse);

                TestUtilities.AssertPointsAreEqual(p, q, round: 6);
            }
        }

        [TestMethod("Page 224 Example 5.2.4")]
        public void Example_5_2_4()
        {
            var radians = Utilities.DegreesToRadians(45);
            var rotating = Utilities.AffineTransformationForRotatingAroundUnit(Point3D.ZUnit, radians);

            var translate = DenseMatrix.OfArray(new [,]
            {
                {1.0, 0.0, 0.0, 3.0},
                {0.0, 1.0, 0.0, 5.0},
                {0.0, 0.0, 1.0, 0.0},
                {0.0, 0.0, 0.0, 1.0},
            });
            var scale = DenseMatrix.OfArray(new [,]
            {
                {1.5,  0.0, 0.0, 0.0},
                {0.0, -2.0, 0.0, 0.0},
                {0.0,  0.0, 1.0, 0.0},
                {0.0,  0.0, 0.0, 1.0},
            });

            var matrix = translate * scale * DenseMatrix.OfArray(rotating);
            var actual = matrix * DenseVector.OfArray(new [] { 1.0, 2.0, 0.0, 1.0 });

            var expected = DenseVector.OfArray(new [] { 1.94, 0.758, 0.0, 1.0 });

            TestUtilities.AssertVectorsAreEqual(expected, actual, 0.001);
        }


        [TestMethod("Page 241 Example 5.3.4 Rotating about an axis")]
        public void Example_5_3_4()
        {
            var u = Point3D.Identity.Normalized();
            TestUtilities.AssertPointsAreEqual(new (0.577, 0.577, 0.577), u, 3);
            u = TestUtilities.Round(u, 3);

            var Ru = Utilities.AffineTransformationForRotatingAroundUnit(u, Utilities.DegreesToRadians(45));

            // The book used 0.8047, -0.31, 0.5058 (but it round sign and cosign of 45° to 0.707)
            TestUtilities.AssertMatrixAreEqual(new [,]
            {
                { 0.8046, -0.3105,  0.5055, 0},
                { 0.5055,  0.8046, -0.3105, 0},
                {-0.3105,  0.5055,  0.8046, 0},
                { 0.0,     0.0,     0.0,    1},
            }, Ru, 4);
        }
    }
}