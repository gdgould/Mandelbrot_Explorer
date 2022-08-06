# Version History

## 0.8.1.3 (August 6, 2022)

* First version uploaded to GitHub.
* Code commented and streamlined.
* Removed any references to 1920x1080 screens in the code.
* Changed version numbering, which was very unofficial prior to this release.  Versions previously named "x.y.*.*" will now be name "(x-1).(y+1).*.*".
* Frames are now mutable, which simplifies parts of the code considerably.

# Previous Versions

Version history was only loosely kept track of prior to 0.7.1.3.  For an idea of the order which features were added, read on.

## 0.8.1.2

* Added the ability to define custom color palettes using control points and widths.
* Streamlined color palette calculating code.

## 0.8.1.1

* Added decimal-based calculation, which triggers after zooming in beyond the double-precision limit.
* Added arrow key controls.
* Added cubic (Hermite spline) interpolation for colors instead of linear interpolation.

## 0.8.1.0

* Added color palette controls.
* Changed the maximum iteration controls from multiplying/dividing by sqrt(2) to adding/subtracting 200, for finer control.

## 0.8.0.0

* Updated the core engine, and streamlined the inner loop code by removing multiplications.
* Added the Mandelbrot class for calculations, reserving Form1 for display control.

## 0.7.2.2

* Fully fixed color gradient code.  There was a problem with Linear_interpolation, causing value 1 to always be considered smaller, even if this was not the case.

## 0.7.2.1

* Fixed gradient code.  Most gradients should be moving in the right direction now.

## 0.7.2.0

* Added a new color scheme: a looping palette of 500 colors.
* Removed histogram coloring, which is not necessary for the new palette.

## 0.7.1.0

* Fixed linear interpolation coloring by storing pixelValues as decimals, and adding rounded values to the histogram.

## 0.7.0.0

* Added histogram coloring.  Now contrast is maintained when zooming in.
* Linear interpolation is not working.

## 0.6.0.1

* Fixed resolution issue.

## 0.6.0.0

* Added smoother coloring by using linear interpolation.  However, screen resolution is unexpectedly low, and does not increase normally.

## 0.5.1.1

* percentDone label re-instated, since UI updates are now on a timer.
* Removed full screen option, since it currently causes an error when switching while calculating.

## 0.5.1.0

* UI now updates only when all threads are finished calculating.

## 0.5.0.1

* percentDone removed, speed reference no longer necessary.

## 0.5.0.0

* Calculations are now multithreaded, which improves performance 5-10x.
* Currently UI updates on the same timer as percentDone.

## 0.4.1.0

* Added a timer to update the percentDone label without throwing an exception.

## 0.4.0.0

* Calculations are now done on a thread, so the UI does not freeze when calculating.
* Threads can be aborted when a control is used (zooming / changing maxIteration).
* Adding this thread breaks the percentDone label.

## 0.3.2.1

* Now using label.Refresh to update progress during calculation.

## 0.3.2.0

* Added progress label in the top left corner.  However, it only updates after calculations are finished.

## 0.3.1.0

* Press 'M' to switch between regular and fullscreen views.  Fullscreen hides the taskbar as well.

## 0.3.0.0

* Now adapts to monitor sizes, no longer stuck with 1440x900.

## 0.2.1.0

* Press 'C' to save a picture in png format.

## 0.2.0.0

* Added zoom.  Click to increase zoom 2-fold, centering on the mouse.  Click near the edges to zoom out.

## 0.1.1.0

* 'Q' and 'W' keys now increase and decrease quality.
* 'A' and 'S' keys now increase and decrease maximum iteration.

## 0.1.0.1

* Added green on black color scheme.

## 0.1.0.0

* Basic calculating algorithm built.