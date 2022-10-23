using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ascii3dEngine.Tests
{
    [TestClass]
    public class MotionMatrixTests
    {
        [TestMethod("Page 224 Practice Exercise 5.2.4 Build One - Understanding")]
        public void Exercise_5_2_4_Understanding()
        {
            // The order of operations in the exercise are different
            // They use rotate, scale, translate
            // But we need scale, rotate, translate (and it turns out that you can't just "undo" that)

            // So thins is not testing "our code" as much as it is testing "our understanding"

            var radians = Utilities.DegreesToRadians(45);

            var c = Math.Cos(radians);
            var s = Math.Sin(radians);

            var rotation = DenseMatrix.OfArray(new [,]
            {
                {c, -s, 0},
                {s,  c, 0},
                {0,  0, 1},
            });

            var scaleByX = 1.5;
            var scaleByY = -2.0;

            var scale = DenseMatrix.OfArray(new [,]
            {
                {scaleByX,        0, 0},
                {       0, scaleByY, 0},
                {       0,        0, 1},
            });

            var translateByX = 3.0;
            var translateByY = 5.0;

            var translate = DenseMatrix.OfArray(new [,]
            {
                {1, 0, translateByX},
                {0, 1, translateByY},
                {0, 0, 1},
            });

            var matrix = translate * scale * rotation;

            var expectedMatrix = DenseMatrix.OfArray(new [,]
            {
                { 1.06 , -1.06 , 3},
                {-1.414, -1.414, 5},
                { 0    ,  0    , 1},
            });

            TestUtilities.AssertMatrixAreEqual(expectedMatrix, matrix, delta: 0.001);

            var p = DenseVector.OfArray(new [] {1.0, 2.0, 1.0});

            var result = expectedMatrix * p;

            var expectedVector = DenseVector.OfArray(new [] {1.94, 0.758, 1.0});

            TestUtilities.AssertVectorsAreEqual(expectedVector, result, delta: 0.001);
        }

        [TestMethod("Page 224 Practice Exercise 5.2.4 Build One - Code")]
        public void Exercise_5_2_4_Code()
        {
            var radians = Utilities.DegreesToRadians(45);

            var scalePoint = new Point3D(1.5, -2.0, z: 0);
            var translatePoint = new Point3D(3, 5, z: 0);
            var p = new Point3D(1.0, 2.0, z: 0);

            var c = Math.Cos(radians);
            var s = Math.Sin(radians);

            var rotation = DenseMatrix.OfArray(new [,]
            {
                {c, -s, 0},
                {s,  c, 0},
                {0,  0, 1},
            });

            var scale = DenseMatrix.OfArray(new [,]
            {
                {scalePoint.X,            0, 0},
                {           0, scalePoint.Y, 0},
                {           0,            0, 1},
            });

            var translate = DenseMatrix.OfArray(new [,]
            {
                {1, 0, translatePoint.X},
                {0, 1, translatePoint.Y},
                {0, 0, 1},
            });

            var matrix = translate * rotation * scale;

            var expected = matrix * DenseVector.OfArray(new [] {p.X, p.Y, 1.0});
            var expectedPoint = new Point3D(expected[0], expected[1], z: 0);

            var actual = new MotionMatrix()
                .SetScale(scalePoint)
                .RotateByZ(radians)
                .MoveTo(translatePoint)
                .Apply(p);

            Assert.AreEqual(expectedPoint, actual);
        }

        [TestMethod("Page 228 Practice Exercise 5.2.22 Tow successive rotations")]
        public void Exercise_5_2_22()
        {
            var random = TestUtilities.NewTestRandom;

            for (int i = 0; i < 100; i++)
            {
                var r1 = Utilities.DegreesToRadians(random.Next(minValue: 0, maxValue: 360));
                var r2 = Utilities.DegreesToRadians(random.Next(minValue: 0, maxValue: 360));

                Action<MotionMatrix, double> action = random.Next(minValue: 0, maxValue: 3) switch
                {
                    0 => (MotionMatrix m, double a) => m.RotateByX(a),
                    1 => (MotionMatrix m, double a) => m.RotateByY(a),
                    2 => (MotionMatrix m, double a) => m.RotateByZ(a),
                    _ => throw new ApplicationException("Ummm how did this happen?"),
                };

                var m1 = new MotionMatrix();
                var m2 = new MotionMatrix();

                action(m1, r1);
                action(m1, r2);

                action(m2, r1 + r2);

                var p = TestUtilities.RandomPoint(random, 1000.0);

                var expected = TestUtilities.Round(m1.Apply(p), 4);
                var actual = TestUtilities.Round(m2.Apply(p), 4);

                Assert.AreEqual(expected, actual);
            }
        }

        [TestMethod("Page 237 Practice Exercise 5.3.2 Rotate points")]
        public void Exercise_5_3_2()
        {
            var motionMatrix = new MotionMatrix().RotateByY(Utilities.DegreesToRadians(30));
            var actual = motionMatrix.Apply(new (3, 1, 4));

            // The book has 4.6 (but they round cos(30°) to .866)
            TestUtilities.AssertPointsAreEqual(new (4.598, 1, 1.964), actual, 3);
        }

        [TestMethod("Page 239 Example 5.3.3")]
        public void Example_5_3_3()
        {
            var actual = new MotionMatrix()
                .RotateByX(Utilities.DegreesToRadians(45))
                .RotateByY(Utilities.DegreesToRadians(30))
                .RotateByZ(Utilities.DegreesToRadians(60))
                .RawRotationMatrix;

            // book had different values but lots of rounding of cos and sin that pile up
            var expected = new [,] {
                { 0.433, -0.436,  0.789},
                { 0.75 ,  0.66 , -0.047},
                {-0.5  ,  0.612,  0.612},
            };

            TestUtilities.AssertMatrixAreEqual(expected, actual, 3);
        }
    }
}