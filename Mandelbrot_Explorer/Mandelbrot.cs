using System;
using System.Threading.Tasks;
using System.Drawing;

namespace Mandelbrot_Explorer
{
    public struct DoubleFrame
    {
        private double x;
        private double y;
        private double w;
        private double h;
        public DoubleFrame(double xCoord, double yCoord, double width, double height)
        {
            this.x = xCoord;
            this.y = yCoord;
            this.w = width;
            this.h = height;
        }

        public double X { get { return x; } }
        public double Y { get { return y; } }
        public double Width { get { return w; } }
        public double Height { get { return h; } }
    }


    class Mandelbrot
    {
        public event Action<Bitmap, Frame, int, Size, int, int, int> CalculationFinished;
        public event Action<decimal[], Bitmap, Frame, int, Size> PreivewReady;
        public event Action<Frame, int, Size> CalculationStarted;
        public event Action<Frame, int, Size> CalculationsAborted;

        private Frame frame;
        private DoubleFrame doubleFrame;
        private int maxIteration;
        public Size BitmapSize { get; private set; }

        private Bitmap image;
        private Color[] palette;
        private int colorshift;

        private double _completion;
        private bool _killComp;

        public int Group { get; private set; }
        public int Index { get; private set; }
        public int GroupTotal { get; private set; }


        private decimal[] ticksToCalculateColumn;
        private System.Diagnostics.Stopwatch st;

        public Mandelbrot(Frame frame, int maxiteration, Size bitmapSize, int colors, int colorShift)
        {
            Setup(frame, maxiteration, bitmapSize, colors, colorShift, -1, -1, -1);
        }
        public Mandelbrot(Frame frame, int maxiteration, Size bitmapSize, int colors, int colorShift, int group, int index, int groupTotal)
        {
            Setup(frame, maxiteration, bitmapSize, colors, colorShift, group, index, groupTotal);
        }

        /// <summary>
        /// Sets up the parameters of the Mandelbrot
        /// </summary>
        /// <param name="frame">The frame of the image.</param>
        /// <param name="maxiteration">The maximum iteration to calculate to.</param>
        /// <param name="bitmapSize">The size of the image to be calculated.</param>
        /// <param name="colors">The number of colors to use</param>
        /// <param name="colorShift">The offset on the start of the palette.</param>
        /// <param name="group">The group number of this thread.</param>
        /// <param name="index">The index number of this thread.</param>
        /// <param name="groupTotal">The number of threads in this group.</param>
        private void Setup(Frame frame, int maxiteration, Size bitmapSize, int colors, int colorShift, int group, int index, int groupTotal)
        {
            CalculationFinished += Mandelbrot_CalculationFinished;
            CalculationStarted += Mandelbrot_CalculationStarted;
            CalculationsAborted += Mandelbrot_CalculationsAborted;
            PreivewReady += Mandelbrot_PreivewReady;

            this.frame = frame;
            this.doubleFrame = new DoubleFrame((double)frame.X, (double)frame.Y, (double)frame.Width, (double)frame.Height);
            this.maxIteration = maxiteration;
            this.BitmapSize = bitmapSize;
            image = new Bitmap(bitmapSize.Width, bitmapSize.Height);


            // Color palette setup
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

            palette = ColorFunc.SetupCubicColorPalette(colors, control, dist);
            _completion = 0d;
            _killComp = false;
            colorshift = colorShift;

            this.Group = group;
            this.Index = index;
            this.GroupTotal = groupTotal;

            this.ticksToCalculateColumn = new decimal[bitmapSize.Width];
            for (int i = 0; i < ticksToCalculateColumn.Length; i++)
            {
                ticksToCalculateColumn[i] = 0;
            }
        }

        private void Mandelbrot_PreivewReady(decimal[] arg1, Bitmap arg2, Frame arg3, int arg4, Size arg5)
        {
        }
        private void Mandelbrot_CalculationsAborted(Frame arg1, int arg2, Size arg3)
        {
            _killComp = false;
        }
        private void Mandelbrot_CalculationStarted(Frame arg1, int arg2, Size arg3)
        {
        }
        private void Mandelbrot_CalculationFinished(Bitmap image, Frame frame, int maxIt, Size bitmapSize, int group, int index, int total)
        {
            _completion = 1;
        }


        public async void StartCalculation()
        {
            CalculationStarted(frame, maxIteration, BitmapSize);
            _completion = 0;

            if (frame.Width < 0.0000000005m) // Magic constant checks whether we are nearing the end of double precision.
            {
                await Task.Run(() => CalculateMandelbrot(frame, maxIteration, BitmapSize));
            }
            else
            {
                await Task.Run(() => CalculateMandelbrot(doubleFrame, maxIteration, BitmapSize));
            }


            // When we pop out of the await block, process accordingly
            if (!_killComp)
            {
                CalculationFinished(image, frame, maxIteration, BitmapSize, Group, Index, GroupTotal);
                _completion = 1;
                if (Group == -1)
                {
                    PreivewReady(ticksToCalculateColumn, image, frame, maxIteration, BitmapSize);
                }
            }
            else
            {
                CalculationsAborted(frame, maxIteration, BitmapSize);
            }
        }



        //Double-based calculation (much faster)
        private void CalculateMandelbrot(DoubleFrame frame, int maxiteration, Size bitmapSize)
        {
            image = new Bitmap(bitmapSize.Width, bitmapSize.Height);
            double pixelWidth = frame.Width / bitmapSize.Width;
            for (int i = 0; i < bitmapSize.Width; i++)
            {
                BitmapFunc.PartialOverwrite(image,
                        MandelbrotThread(new DoubleFrame(frame.X + i * pixelWidth, frame.Y, pixelWidth, frame.Height), maxiteration, bitmapSize.Height, i),
                        i, 0);
                _completion += 1d / bitmapSize.Width;
                if (_killComp)
                {
                    return;
                }
            }
        }

        private Bitmap MandelbrotThread(DoubleFrame frame, int maxiteration, int columnHeight, int columnIndex)
        {
            Bitmap returnImage = new Bitmap(1, columnHeight);
            double itSurvived;
            double pixelHeight = frame.Height / (double)columnHeight;

            st = new System.Diagnostics.Stopwatch();

            st.Start();
            for (int i = 0; i < columnHeight; i++)
            {
                itSurvived = PixelEscape(frame.X, frame.Y + i * pixelHeight, maxiteration);
                if (itSurvived < 0)
                {
                    returnImage.SetPixel(0, i, Color.Black);
                }
                else
                {
                    returnImage.SetPixel(0, i, ColorFunc.LinearInterpolate(palette[((int)Math.Floor(itSurvived) + colorshift) % palette.Length],
                        palette[((int)Math.Floor(itSurvived) + colorshift + 1) % palette.Length], (itSurvived % 1)));
                }
            }
            st.Stop();

            if (Group == -1)
            {
                ticksToCalculateColumn[columnIndex] = st.ElapsedTicks;
            }
            return returnImage;
        }
        private double PixelEscape(double x, double y, int maxiteration)
        {
            double rsquare, isquare, zsquare, x1, y1;
            rsquare = isquare = zsquare = 0;
            double iteration = 0;

            while (rsquare + isquare <= 16 && iteration < maxiteration)
            {
                x1 = rsquare - isquare + x;
                y1 = zsquare - rsquare - isquare + y;
                rsquare = x1 * x1;
                isquare = y1 * y1;
                zsquare = (x1 + y1) * (x1 + y1);
                iteration++;
            }
            if (iteration < maxiteration)
            {
                //double nu = Math.Log10(Math.Log10(rsquare + isquare) / 0.60205999132) / 0.30102999566;    //0.30102999566 = Math.Log10(2);   | Are equivalent
                //double nu = Math.Log10(Math.Log10(rsquare + isquare)) / 0.30102999566 + 0.732020845645;                                    //|
                iteration += Math.Log10(Math.Log10(rsquare + isquare)) / -0.30102999566 + 0.267979154355;
                return iteration;
            }
            else { return -1; }
        }

            
        //Decimal-based calculation (goes to a higher zoom depth)
        private void CalculateMandelbrot(Frame frame, int maxiteration, Size bitmapSize)
        {
            image = new Bitmap(bitmapSize.Width, bitmapSize.Height);
            decimal pixelWidth = frame.Width / bitmapSize.Width;
            for (int i = 0; i < bitmapSize.Width; i++)
            {
                BitmapFunc.PartialOverwrite(image,
                        MandelbrotThread(new Frame(frame.X + i * pixelWidth, frame.Y, pixelWidth, frame.Height), maxiteration, bitmapSize.Height, i),
                        i, 0);
                _completion += 1d / bitmapSize.Width;
                if (_killComp)
                {
                    return;
                }
            }
        }
        private Bitmap MandelbrotThread(Frame frame, int maxiteration, int columnHeight, int columnIndex)
        {
            Bitmap returnImage = new Bitmap(1, columnHeight);
            double itSurvived;
            decimal pixelHeight = frame.Height / (decimal)columnHeight;

            st = new System.Diagnostics.Stopwatch();

            st.Start();
            for (int i = 0; i < columnHeight; i++)
            {
                itSurvived = PixelEscape(frame.X, frame.Y + i * pixelHeight, maxiteration);
                if (itSurvived < 0)
                {
                    returnImage.SetPixel(0, i, Color.Black);
                }
                else
                {
                    returnImage.SetPixel(0, i, ColorFunc.LinearInterpolate(palette[((int)Math.Floor(itSurvived) + colorshift) % palette.Length],
                        palette[((int)Math.Floor(itSurvived) + colorshift + 1) % palette.Length], (itSurvived % 1)));
                }

            }
            st.Stop();

            if (Group == -1)
            {
                ticksToCalculateColumn[columnIndex] = st.ElapsedTicks;
            }
            return returnImage;
        }
        private double PixelEscape(decimal x, decimal y, int maxiteration)
        {
            decimal rsquare, isquare, zsquare, x1, y1;
            rsquare = isquare = zsquare = 0;
            double iteration = 0;

            while (rsquare + isquare <= 16 && iteration < maxiteration)
            {
                x1 = rsquare - isquare + x;
                y1 = zsquare - rsquare - isquare + y;
                rsquare = x1 * x1;
                isquare = y1 * y1;
                zsquare = (x1 + y1) * (x1 + y1);
                iteration++;
            }
            if (iteration < maxiteration)
            {
                //double nu = Math.Log10(Math.Log10(rsquare + isquare) / 0.60205999132) / 0.30102999566;    //0.30102999566 = Math.Log10(2);   | Are equivalent
                //double nu = Math.Log10(Math.Log10(rsquare + isquare)) / 0.30102999566 + 0.732020845645;                                    //|
                iteration += Math.Log10(Math.Log10((double)(rsquare + isquare))) / -0.30102999566d + 0.267979154355d;
                return iteration;
            }
            else { return -1; }
        }



        public void Dispose()
        {
            //_image.Dispose();
            image = null;
        }

        public Bitmap GetImage()
        {
            if (_completion == 1)
            {
                return image;
            }
            return null;
        }
        public double CheckCompletion()
        {
            return _completion;
        }
        public void KillCalculations()
        {
            _killComp = true;
        }
    }
}
