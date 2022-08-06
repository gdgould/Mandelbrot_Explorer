using System;
using System.Drawing;

namespace Mandelbrot_Explorer
{
    public static class ColorFunc
    {

        public static Color LinearInterpolate(Color value0, Color value1, double pointBetween)
        {
            if (pointBetween < 0 || pointBetween > 1) { return Color.Black; }
            double redRate = value0.R - ((value0.R - value1.R) * pointBetween);
            double greenRate = value0.G - ((value0.G - value1.G) * pointBetween);
            double blueRate = value0.B - ((value0.B - value1.B) * pointBetween);

            return Color.FromArgb((int)Math.Round(redRate), (int)Math.Round(greenRate), (int)Math.Round(blueRate));
        }

        private static double FindTangent(double value0, double value1, double value2, double distance0, double distance1)
        {
            if ((value0 < value1 && value2 < value1) || (value0 > value1 && value2 > value1))
            {
                return 0;
            }
            else
            {
                return ((value1 - value0) / distance0 + (value2 - value1) / distance1) / 2;
            }
        }

        private static double EvaluateHermiteSpline(double value0, double value1, double tan0, double tan1, double t)
        {
            return (2 * t * t * t - 3 * t * t + 1) * value0 + (t * t * t - 2 * t * t + t) * tan0 + (-2 * t * t * t + 3 * t * t) * value1 + (t * t * t - t * t) * tan1;
        }

        public static Color CubicInterpolate(Color ref0, Color value0, Color value1, Color ref1, double d0, double d1, double d2, double pointBetween)
        {
            double distanceStretch = 1 / d1;
            d1 = 1;
            d0 *= distanceStretch;
            d2 *= distanceStretch;

            double tan0R = FindTangent(ref0.R, value0.R, value1.R, d0, d1);
            double tan0G = FindTangent(ref0.G, value0.G, value1.G, d0, d1);
            double tan0B = FindTangent(ref0.B, value0.B, value1.B, d0, d1);
            double tan0A = FindTangent(ref0.A, value0.A, value1.A, d0, d1);

            double tan1R = FindTangent(value0.R, value1.R, ref1.R, d1, d2);
            double tan1G = FindTangent(value0.G, value1.G, ref1.G, d1, d2);
            double tan1B = FindTangent(value0.B, value1.B, ref1.B, d1, d2);
            double tan1A = FindTangent(value0.A, value1.A, ref1.A, d1, d2);

            int returnR = (int)Math.Round(EvaluateHermiteSpline(value0.R, value1.R, tan0R, tan1R, pointBetween));
            int returnG = (int)Math.Round(EvaluateHermiteSpline(value0.G, value1.G, tan0G, tan1G, pointBetween));
            int returnB = (int)Math.Round(EvaluateHermiteSpline(value0.B, value1.B, tan0B, tan1B, pointBetween));
            int returnA = (int)Math.Round(EvaluateHermiteSpline(value0.A, value1.A, tan0A, tan1A, pointBetween));

            if (value0.R == value1.R)
            {
                returnR = value0.R;
            }
            if (value0.G == value1.G)
            {
                returnG = value0.G;
            }
            if (value0.B == value1.B)
            {
                returnB = value0.B;
            }
            if (value0.A == value1.A)
            {
                returnA = value0.A;
            }

            if (returnR > 255) { returnR = 255; }
            if (returnG > 255) { returnG = 255; }
            if (returnB > 255) { returnB = 255; }
            if (returnA > 255) { returnA = 255; }

            if (returnR < 0) { returnR = 0; }
            if (returnG < 0) { returnG = 0; }
            if (returnB < 0) { returnB = 0; }
            if (returnA < 0) { returnA = 0; }

            return Color.FromArgb(returnA, returnR, returnG, returnB);
        }

        public static Color[] SetupColorPalette(int noOfColors)
        {
            Color[] controlPoints =
            {
                Color.FromArgb(0, 7, 100),
                Color.FromArgb(32, 107, 203),
                Color.FromArgb(237, 255, 255),
                Color.FromArgb(255, 170, 0),
                Color.FromArgb(0, 2, 0)
            };

            Color[] colorPalette = new Color[noOfColors];
            int i = 0;
            for (; i < colorPalette.Length * 0.16; i++)
            {
                double j = i / (colorPalette.Length * 0.16d);
                colorPalette[i] = LinearInterpolate(controlPoints[0], controlPoints[1], j);
            }
            for (; i < colorPalette.Length * 0.42; i++)
            {
                double j = (i - (colorPalette.Length * 0.16d)) / (colorPalette.Length * 0.26d);
                colorPalette[i] = LinearInterpolate(controlPoints[1], controlPoints[2], j);
            }
            for (; i < colorPalette.Length * 0.64; i++)
            {
                double j = (i - (colorPalette.Length * 0.42d)) / (colorPalette.Length * 0.22d);
                colorPalette[i] = LinearInterpolate(controlPoints[2], controlPoints[3], j);
            }
            for (; i < colorPalette.Length * 0.86; i++)
            {
                double j = (i - (colorPalette.Length * 0.64d)) / (colorPalette.Length * 0.22d);
                colorPalette[i] = LinearInterpolate(controlPoints[3], controlPoints[4], j);
            }
            for (; i < colorPalette.Length * 1; i++)
            {
                double j = (i - (colorPalette.Length * 0.86d)) / (colorPalette.Length * 0.14d);
                colorPalette[i] = LinearInterpolate(controlPoints[4], controlPoints[0], j);
            }

            return colorPalette;
        }

        public static Color[] SetupCubicColorPalette2(int noOfColors)
        {
            Color[] control =
            {
                Color.FromArgb(0, 7, 100),
                Color.FromArgb(32, 107, 203),
                Color.FromArgb(237, 255, 255),
                Color.FromArgb(255, 170, 0),
                Color.FromArgb(0, 2, 0)
            };

            double[] dist =
            {
                0.16,
                0.26,
                0.22,
                0.22,
                0.14
            };

            Color[] colorPalette = new Color[noOfColors];
            int i = 0;
            for (; i < colorPalette.Length * dist[0]; i++)
            {
                double j = i / (colorPalette.Length * dist[0]);
                colorPalette[i] = CubicInterpolate(control[4], control[0], control[1], control[2], dist[4], dist[0], dist[1], j);
            }
            for (; i < colorPalette.Length * (dist[0] + dist[1]); i++)
            {
                double j = (i - (colorPalette.Length * dist[0])) / (colorPalette.Length * dist[1]);
                colorPalette[i] = CubicInterpolate(control[0], control[1], control[2], control[3], dist[0], dist[1], dist[2], j);
            }
            for (; i < colorPalette.Length * (dist[0] + dist[1] + dist[2]); i++)
            {
                double j = (i - (colorPalette.Length * (dist[0] + dist[1]))) / (colorPalette.Length * dist[2]);
                colorPalette[i] = CubicInterpolate(control[1], control[2], control[3], control[4], dist[1], dist[2], dist[3], j);
            }
            for (; i < colorPalette.Length * (dist[0] + dist[1] + dist[2] + dist[3]); i++)
            {
                double j = (i - (colorPalette.Length * (dist[0] + dist[1] + dist[2]))) / (colorPalette.Length * dist[3]);
                colorPalette[i] = CubicInterpolate(control[2], control[3], control[4], control[0], dist[2], dist[3], dist[4], j);
            }
            for (; i < colorPalette.Length * 1; i++)
            {
                double j = (i - (colorPalette.Length * (dist[0] + dist[1] + dist[2] + dist[3]))) / (colorPalette.Length * dist[4]);
                colorPalette[i] = CubicInterpolate(control[3], control[4], control[0], control[1], dist[3], dist[4], dist[0], j);
            }

            return colorPalette;
        }

        public static Color[] SetupCubicColorPalette(int noOfColors, Color[] controlPoints, double[] widths)
        {
            Color[] colorPalette = new Color[noOfColors];

            int length = controlPoints.Length;

            int arrayCount = 0;
            double between;
            for (int i = 0; i < length; i++)
            {
                for (; arrayCount < noOfColors * SumTerms(widths, i + 1); arrayCount++)
                {
                    between = (arrayCount - noOfColors * SumTerms(widths, i)) / (noOfColors * widths[i]);
                    colorPalette[arrayCount] = CubicInterpolate(controlPoints[(i - 1 + length) % length], controlPoints[i], controlPoints[(i + 1) % length],
                        controlPoints[(i + 2) % length], widths[(i - 1 + length) % length], widths[i], widths[(i + 1) % length], between);
                }
            }

            return colorPalette;
        }

        public static double SumTerms(double[] arr, int terms)
        {
            double output = 0;
            for (int i = 0; i < Math.Min(terms, arr.Length); i++)
            {
                output += arr[i];
            }

            return output;
        }
    }
}