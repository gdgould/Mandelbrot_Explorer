using System;
using System.Drawing;
using System.Windows.Forms;

namespace Mandelbrot_Explorer
{
    public struct Frame
    {
        public decimal X { get; set; }
        public decimal Y { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }

        public Frame(decimal xCoord, decimal yCoord, decimal width, decimal height)
        {
            this.X = xCoord;
            this.Y = yCoord;
            this.Width = width;
            this.Height = height;
        }
    }

    public partial class Form1 : Form
    {
        public Form1(int width, int height, bool isFullscreen)
        {
            this.Width = width;
            this.ImageWidth = width;
            this.Height = height;
            this.ImageHeight = height;
            if (isFullscreen)
            {
                this.WindowState = FormWindowState.Maximized;
            }

            this.KeyUp += Form1_KeyUp;
            this.Load += Form1_Load;
            this.MouseWheel += Display_MouseWheel;
            this.KeyDown += Form1_KeyDown;
            this.KeyPreview = true;
        }

        int ImageWidth;
        int ImageHeight;

        const decimal zoomConst = 1.1m;
        const int initialPixelGroup = 8;
        const int arrowKeyMoveFraction = 200;

        //Arrow key movement
        bool moveUp;
        bool moveDown;
        bool moveLeft;
        bool moveRight;
        bool controlPressed;

        Frame _viewFrame; // The coordinates of the rectangle being displayed
        int _maxIt; // The maximum iteration to calculate to
        int _noOfColors; // The number of colors in the palette
        int _colorShift; // The offset on the starting color in the palette

        // Top-left information labels
        Label completionLabel;
        Label maxIterationLabel;
        Label bitmapSizeLabel;
        Label colorsLabel;
        Label colorShiftLabel;

        PictureBox display;

        Mandelbrot previewCalc; // For calculating the low-resolution preview which determines thread assignments
        Mandelbrot[] calcThreads; // The main calculating threads

        Timer refresh;
        Timer drag;

        int groupIndexCount; // The index of the last thread group that was assigned work (increments by one every refresh)
        double _pixelGroup; // The width of the squares of pixels which are calculated as a group


        /// <summary>
        /// Initiates the display picturebox and completion label
        /// </summary>
        private void SetupUI()
        {
            //Setup Picturebox for display
            display = new PictureBox();
            display.Size = new Size(this.ImageWidth, this.ImageHeight);
            display.Location = new Point(0, 0);
            display.Visible = true;
            display.Parent = this;
            display.SizeMode = PictureBoxSizeMode.Zoom;
            display.MouseUp += Display_MouseUp;
            display.MouseDown += Display_MouseDown;
            display.MouseMove += Display_MouseMove;
            display.MouseWheel += Display_MouseWheel;

            //Setup label to display completion
            completionLabel = new Label();
            completionLabel.Location = new Point(12, 12);
            completionLabel.AutoSize = true;
            completionLabel.Visible = true;
            completionLabel.Parent = this;
            completionLabel.BackColor = Color.Transparent;
            completionLabel.BringToFront();

            SetupStatusLabels();
        }
        /// <summary>
        /// Initiates the top-left status labels
        /// </summary>
        private void SetupStatusLabels()
        {
            maxIterationLabel = new Label();
            maxIterationLabel.Location = new Point(12, 36);
            maxIterationLabel.AutoSize = true;
            maxIterationLabel.Visible = true;
            maxIterationLabel.Parent = this;
            maxIterationLabel.BackColor = Color.Transparent;
            maxIterationLabel.BringToFront();

            bitmapSizeLabel = new Label();
            bitmapSizeLabel.Location = new Point(12, 60);
            bitmapSizeLabel.AutoSize = true;
            bitmapSizeLabel.Visible = true;
            bitmapSizeLabel.Parent = this;
            bitmapSizeLabel.BackColor = Color.Transparent;
            bitmapSizeLabel.BringToFront();

            colorsLabel = new Label();
            colorsLabel.Location = new Point(12, 84);
            colorsLabel.AutoSize = true;
            colorsLabel.Visible = true;
            colorsLabel.Parent = this;
            colorsLabel.BackColor = Color.Transparent;
            colorsLabel.BringToFront();

            colorShiftLabel = new Label();
            colorShiftLabel.Location = new Point(12, 108);
            colorShiftLabel.AutoSize = true;
            colorShiftLabel.Visible = true;
            colorShiftLabel.Parent = this;
            colorShiftLabel.BackColor = Color.Transparent;
            colorShiftLabel.BringToFront();
        }

        /// <summary>
        /// Sets up the timers for refreshing the display when zooming or moving to a new resolution
        /// </summary>
        private void SetupRefreshTimers()
        {
            //Refresh the completion counter && checks whether the next resolution should be calculated
            refresh = new System.Windows.Forms.Timer();
            refresh.Tick += Refresh_Tick;
            refresh.Interval = 50;
            refresh.Start();

            //Refresh the view when dragging and zooming
            drag = new System.Windows.Forms.Timer();
            drag.Tick += Drag_Tick;
            drag.Interval = 200;
            drag.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SetupUI();
            SetupRefreshTimers();

            _viewFrame = new Frame(-2.75m, 1.125m, 4, -2.25m);
            _pixelGroup = initialPixelGroup;
            _maxIt = 1000;
            _noOfColors = 500;
            _colorShift = 0;

            mouseDown = false;

            moveUp = false;
            moveDown = false;
            moveRight = false;
            moveLeft = false;
            controlPressed = false;

            groupIndexCount = 0;
            calcThreads = new Mandelbrot[System.Environment.ProcessorCount];

            previousFrame = _viewFrame;
            previousPixelgroup = _pixelGroup;

            LaunchPreviewThread();
        }


        //Navigation global variables:
        int mouseStartX;
        int mouseStartY;
        bool mouseDown;

        int currentMouseY;
        int currentMouseX;

        Frame frameOnDragStart;

        /// <summary>
        /// Triggered when scrolling.  Adjusts the frame to zoom in or out.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Display_MouseWheel(object sender, MouseEventArgs e)
        {
            AbortThreads();
            int mouseX = e.X;
            int mouseY = e.Y;
            if (e.Delta < 0)
            {
                _viewFrame.X -= _viewFrame.Width * (((decimal)mouseX / this.Width) * (zoomConst - 1));
                _viewFrame.Y -= _viewFrame.Height * (((decimal)mouseY / this.Height) * (zoomConst - 1));
                _viewFrame.Width *= zoomConst;
                _viewFrame.Height *= zoomConst;
            }
            else
            {
                _viewFrame.X += _viewFrame.Width * (((decimal)mouseX / this.Width) * (1 - 1 / zoomConst));
                _viewFrame.Y += _viewFrame.Height * (((decimal)mouseY / this.Height) * (1 - 1 / zoomConst));
                _viewFrame.Width /= zoomConst;
                _viewFrame.Height /= zoomConst;
            }

        }
        /// <summary>
        /// Triggered when dragging.  Adjusts currentMouseX/currentMouseY, which are passed to frame updates on a timer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Display_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDown)
            {
                currentMouseX = e.X;
                currentMouseY = e.Y;
            }
        }

        /// <summary>
        /// Triggered at the beginning of dragging.  Stores the current frame for later.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Display_MouseDown(object sender, MouseEventArgs e)
        {
            frameOnDragStart = _viewFrame;
            mouseDown = true;
            mouseStartX = e.X;
            mouseStartY = e.Y;
        }
        /// <summary>
        /// Triggered at the end of dragging.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Display_MouseUp(object sender, MouseEventArgs e)
        {
            mouseDown = false;
        }


        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // Arrow-key motion of the frame
            if (e.KeyCode == Keys.Up)
            {
                moveUp = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                moveDown = true;
            }
            else if (e.KeyCode == Keys.Left)
            {
                moveLeft = true;
            }
            else if (e.KeyCode == Keys.Right)
            {
                moveRight = true;
            }
        }
        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            {
                moveUp = false;
            }
            else if (e.KeyCode == Keys.Down)
            {
                moveDown = false;
            }
            else if (e.KeyCode == Keys.Left)
            {
                moveLeft = false;
            }
            else if (e.KeyCode == Keys.Right)
            {
                moveRight = false;
            }

            // Display adjustments
            else if (e.KeyCode == Keys.PageUp || e.KeyCode == Keys.W)
            {
                _maxIt += 200;
            }
            else if (e.KeyCode == Keys.PageDown || e.KeyCode == Keys.Q)
            {
                if (_maxIt > 200)
                {
                    _maxIt -= 200;
                }
            }
            else if (e.KeyCode == Keys.S)
            {
                _noOfColors += 5;
            }
            else if (e.KeyCode == Keys.A)
            {
                if (_noOfColors > 5)
                {
                    _noOfColors -= 5;
                }
            }
            else if (e.KeyCode == Keys.X)
            {
                _colorShift += 5;
                if (_colorShift > _noOfColors)
                {
                    _colorShift -= _noOfColors;
                }
            }
            else if (e.KeyCode == Keys.Z)
            {
                _colorShift -= 5;
                if (_colorShift < 0)
                {
                    _colorShift += _noOfColors;
                }
            }

            else if (e.KeyCode == Keys.Home)
            {
                AbortThreads();
                _viewFrame = new Frame(-2.75m, 1.125m, 4, -2.25m);
                _maxIt = 600;
                _colorShift = 0;
                _noOfColors = 500;
                previousFrame = new Frame();        //Will trigger the refresh timer without causing a null exception
            }
            else if (e.KeyCode == Keys.C)
            {
                AbortThreads();
                refresh.Stop();
                drag.Stop();
                SaveBitmapToFile();
                refresh.Start();
                drag.Start();
            }
            else
            {
                AbortThreads();
                previousFrame = new Frame();        // " "
            }

        }


        /// <summary>
        /// Saves the currently displayed image to a file, with a separate file recording its coordinates.
        /// </summary>
        private void SaveBitmapToFile()
        {
            SaveFileDialog saveImageDialog = new SaveFileDialog();
            saveImageDialog.Filter = "Png Image | *.png";
            saveImageDialog.Title = "Save Image";
            saveImageDialog.ShowDialog();


            if (saveImageDialog.FileName != "")
            {
                string saveImagePath = saveImageDialog.FileName;
                display.Image.Save(saveImagePath);

                System.IO.File.WriteAllText(saveImagePath.Replace(".png", ".mlf"), $"{_viewFrame.X}\n{_viewFrame.Y}\n{_viewFrame.Width}\n{_viewFrame.Height}\n{_maxIt}\n{_noOfColors}\n{_colorShift}");
            }
        }

        /// <summary>
        /// Stops and resets all of the calculation threads
        /// </summary>
        private void AbortThreads()
        {
            try
            {
                previewCalc.KillCalculations();
                previewCalc.Dispose();
            }
            catch { }

            for (int i = 0; i < calcThreads.Length; i++)
            {
                try
                {
                    calcThreads[i].KillCalculations();
                    calcThreads[i].Dispose();
                }
                catch { }
            }
            calcThreads = new Mandelbrot[System.Environment.ProcessorCount];
        }

        /// <summary>
        /// Launches the preview thread, which calculates the whole image at very low resolution
        /// </summary>
        private void LaunchPreviewThread()
        {
            _pixelGroup = initialPixelGroup;
            previewCalc = new Mandelbrot(_viewFrame, _maxIt, new Size((int)Math.Round(this.ImageWidth / _pixelGroup), (int)Math.Round(this.ImageHeight / _pixelGroup)), _noOfColors, _colorShift);
            previewCalc.PreivewReady += PreviewCalc_PreivewReady;
            previewCalc.StartCalculation();
        }

        decimal[] _threadWidths;
        /// <summary>
        /// Uses the calculation data from the preview thread to spread the calulation work evenly among all the worker threads
        /// </summary>
        /// <param name="arg1">The time it took to calculate each column of the image.</param>
        /// <param name="arg2">The image generated.</param>
        /// <param name="arg3">The frame of the image generated.</param>
        /// <param name="arg4">The maximum iteration used in the calculation.</param>
        /// <param name="arg5">The size of the canvas to be drawn to.</param>
        private void PreviewCalc_PreivewReady(decimal[] arg1, Bitmap arg2, Frame arg3, int arg4, Size arg5)
        {
            _threadWidths = new decimal[calcThreads.Length];

            decimal sum = 0;
            for (int i = 0; i < arg1.Length; i++)
            {
                sum += arg1[i];
            }

            decimal threadTotal = sum / calcThreads.Length;

            decimal counter = 0;
            int j = 0;
            int k = 0;
            int maxLines;
            for (int i = 0; i < arg1.Length; i++)
            {
                maxLines = (int)Math.Ceiling((arg1.Length - (decimal)i) / (_threadWidths.Length - (decimal)j));
                counter += arg1[i];
                k++;

                if (counter >= threadTotal)
                {
                    _threadWidths[j] = k / (decimal)arg5.Width;

                    counter = 0;
                    k = 0;
                    j++;
                }
                if (maxLines == 1 && j + 1 != _threadWidths.Length)
                {
                    for (; j < _threadWidths.Length; j++)
                    {
                        _threadWidths[j] = 1;
                    }
                    goto end;
                }
            }
            _threadWidths[j] = k / (decimal)arg5.Width;
        end:

            display.Image = arg2;
            display.Refresh();
            bitmapSizeLabel.Text = $"Current image size: {(this.ImageWidth / _pixelGroup).ToString()} x {(this.ImageHeight / _pixelGroup).ToString()}";
            if (_pixelGroup > 0.25)
            {
                _pixelGroup /= 2;
            }

        }


        /// <summary>
        /// Launches the main calulation threads.  Requires that _threadWidths has been set.
        /// </summary>
        /// <param name="pixelgroup"></param>
        private void LaunchPixelgroupThread(double pixelgroup)
        {
            if (pixelgroup > initialPixelGroup) { pixelgroup = initialPixelGroup; }
            if (pixelgroup < 0.25) { pixelgroup = 0.25; }

            groupFinished = 0;
            groupIndexCount++;
            decimal offset = 0;
            for (int i = 0; i < calcThreads.Length; i++)
            {
                calcThreads[i] = new Mandelbrot(new Frame(_viewFrame.X + _viewFrame.Width * offset, _viewFrame.Y, _viewFrame.Width * _threadWidths[i], _viewFrame.Height), _maxIt, new Size((int)Math.Round((this.ImageWidth / (decimal)pixelgroup) * _threadWidths[i]), (int)Math.Round(this.ImageHeight / (decimal)pixelgroup)), _noOfColors, _colorShift, groupIndexCount, i, calcThreads.Length);
                calcThreads[i].CalculationFinished += Mandelbrot_CalculationFinished;
                calcThreads[i].StartCalculation();

                offset += _threadWidths[i];
            }
        }


        int groupFinished;
        Bitmap finalImage;
        /// <summary>
        /// Triggered when a main calculation thread finishes.
        /// </summary>
        /// <param name="image">The image the thread has calculated.</param>
        /// <param name="frame">The frame of the image.</param>
        /// <param name="maxIteration">The max iteration used to calculate the image.</param>
        /// <param name="bitmapSize">The size of the image.</param>
        /// <param name="group">The group number of the thread.</param>
        /// <param name="index">The index number of the thread within the group.</param>
        /// <param name="groupTotal">The number of threads launched in this group.</param>
        private void Mandelbrot_CalculationFinished(Bitmap image, Frame frame, int maxIteration, Size bitmapSize, int group, int index, int groupTotal)
        {
            // Preview thread finishing
            if (group == -1)
            {
                display.Image = image;
                display.Refresh();
                previewCalc.Dispose();
            }
            else
            {
                groupFinished++;
                if (groupFinished == groupTotal)
                {
                    int totalWidth = 0;
                    for (int i = 0; i < groupTotal; i++)
                    {
                        totalWidth += calcThreads[i].BitmapSize.Width;
                    }

                    finalImage = new Bitmap(totalWidth, bitmapSize.Height);

                    int widthOffset = 0;
                    for (int i = 0; i < groupTotal; i++)
                    {
                        BitmapFunc.PartialOverwrite(finalImage, calcThreads[i].GetImage(), widthOffset, 0);
                        widthOffset += calcThreads[i].BitmapSize.Width;

                        calcThreads[i].Dispose();
                    }
                    display.Image = finalImage;
                    display.Refresh();
                    bitmapSizeLabel.Text = $"Current image size: {(this.ImageWidth / _pixelGroup).ToString()} x {(this.ImageHeight / _pixelGroup).ToString()}";
                    if (_pixelGroup > 0.25)
                    {
                        _pixelGroup /= 2;
                    }
                }
            }
        }


        Frame previousFrame;
        double previousPixelgroup;
        /// <summary>
        /// Triggered on a timer to refresh the display and update the labels.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Refresh_Tick(object sender, EventArgs e)
        {
            double totalCompletion = 0;
            for (int i = 0; i < calcThreads.Length; i++)
            {
                totalCompletion += calcThreads[i] == null ? 0 : calcThreads[i].CheckCompletion();
            }
            completionLabel.Text = $"Completion of current stage: {(Math.Round(totalCompletion * (10000d / calcThreads.Length)) / 100d).ToString()}%";
            completionLabel.Refresh();

            if (!_viewFrame.Equals(previousFrame))
            {
                previousFrame = _viewFrame;
                previousPixelgroup = _pixelGroup = 8;

                LaunchPreviewThread();
            }
            else if (_pixelGroup != previousPixelgroup)
            {
                previousPixelgroup = _pixelGroup;
                LaunchPixelgroupThread(_pixelGroup);
            }

            maxIterationLabel.Text = $"Maximum iteration (Q/W): {_maxIt.ToString()}";
            colorsLabel.Text = $"Number of colors (A/S): {_noOfColors.ToString()}";
            colorShiftLabel.Text = $"Color shift (Z/X): {_colorShift.ToString()}";
        }

        /// <summary>
        /// Triggered on a timer to update the display when dragging.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Drag_Tick(object sender, EventArgs e)
        {
            if (_pixelGroup != initialPixelGroup)
            {
                if (mouseDown)
                {
                    AbortThreads();
                    _viewFrame = new Frame(frameOnDragStart.X + frameOnDragStart.Width * ((mouseStartX - currentMouseX) / (decimal)this.Width),
                                           frameOnDragStart.Y + frameOnDragStart.Height * ((mouseStartY - currentMouseY) / (decimal)this.Height),
                                           frameOnDragStart.Width, frameOnDragStart.Height);
                }

                else if (moveUp && !moveDown)
                {
                    AbortThreads();

                    if (controlPressed) { _viewFrame = new Frame(_viewFrame.X, _viewFrame.Y + _viewFrame.Width / (100 * arrowKeyMoveFraction), _viewFrame.Width, _viewFrame.Height); }
                    else { _viewFrame = new Frame(_viewFrame.X, _viewFrame.Y + _viewFrame.Width / arrowKeyMoveFraction, _viewFrame.Width, _viewFrame.Height); }
                }
                else if (!moveUp && moveDown)
                {
                    AbortThreads();

                    if (controlPressed) { _viewFrame = new Frame(_viewFrame.X, _viewFrame.Y - _viewFrame.Width / (100 * arrowKeyMoveFraction), _viewFrame.Width, _viewFrame.Height); }
                    else { _viewFrame = new Frame(_viewFrame.X, _viewFrame.Y - _viewFrame.Width / arrowKeyMoveFraction, _viewFrame.Width, _viewFrame.Height); }
                }
                else if (moveRight && !moveLeft)
                {
                    AbortThreads();

                    if (controlPressed) { _viewFrame = new Frame(_viewFrame.X + _viewFrame.Width / (100 * arrowKeyMoveFraction), _viewFrame.Y, _viewFrame.Width, _viewFrame.Height); }
                    else { _viewFrame = new Frame(_viewFrame.X + _viewFrame.Width / arrowKeyMoveFraction, _viewFrame.Y, _viewFrame.Width, _viewFrame.Height); }
                }
                else if (!moveRight && moveLeft)
                {
                    AbortThreads();

                    if (controlPressed) { _viewFrame = new Frame(_viewFrame.X - _viewFrame.Width / (100 * arrowKeyMoveFraction), _viewFrame.Y, _viewFrame.Width, _viewFrame.Height); }
                    else { _viewFrame = new Frame(_viewFrame.X - _viewFrame.Width / arrowKeyMoveFraction, _viewFrame.Y, _viewFrame.Width, _viewFrame.Height); }
                }
            }
        }
    }
}
