using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mandelbrot_Explorer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Screen s = Screen.FromPoint(new System.Drawing.Point(0, 0));
            Application.Run(new Form1(s.Bounds.Width, s.Bounds.Height, true));
        }
    }
}
