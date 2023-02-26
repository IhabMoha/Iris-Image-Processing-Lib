/****************************************************************************************
*																						*
*	~---------------|--------------------------------------------------------------~    *
*	|Author			|	Ihab Ahmed Mohammed			       |    *
*	|---------------|--------------------------------------------------------------|    *
*	|Project Name	|	Pupil/Iris processing Library			       |    *
*	|---------------|--------------------------------------------------------------|    *
*	|Description	|	A pupil/Iris Image Processing class library:	       |    *
*	|		|	number of functions for:                               |    *
*	|               |       Convertions                                            |    *
*	|               |       Histogram                                              |    *
*	|               |       Filters                                                |    *
*	|               |       Binarization                                           |    *
*	|               |       Region Growing Segmentation                            |    *
*	|               |       Pupil Processing                                       |    *
*	|               |       Iris  Processing                                       |    *
*	|               |       Fractal and Quantization                               |    *
*	|               |       Run Lenght Features                                    |    *
*	|---------------|--------------------------------------------------------------|    *
*	|Version		|	1.0					       |    *
*	|---------------|--------------------------------------------------------------|    *
*	|Start Date		|	September - 2009			       |    *
*	|---------------|--------------------------------------------------------------|    *
*	|Finish Date	|	September - 2010				       |    *
*	~---------------|--------------------------------------------------------------~    *
*																						*
****************************************************************************************/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EihabIrisLib
{
    public class IrisLib
    {
        // Source and Result Bitmap images
        public Bitmap srcBitmap, resBitmap;
        // Graphics object for drawing on bitmaps
        Graphics srcGraph;
        Graphics resGraph;
        // Source and Result gray level buffers
        public Byte[,] srcImg, resImg, scaledIrisAry;
        // Image total size
        public int totSize;
        // Point data type structure
        public struct Point
        {
            public Point(int nx, int ny)
            {
                x = nx;
                y = ny;
            }

            public int x, y;
        }

        public Point point;

        // Constructor
        public IrisLib()
        {
            totSize = 0;
            srcBitmap = null;
            resBitmap = null;
            point = new Point(0, 0);

            // Histogram Initialization
            histoAry = null;

            // Segmentation Initialization
            stackPoints = null;
        }

        /************************************************************************************
        *   Function	:	Initialize the library                                          *
        *-----------------------------------------------------------------------------------*
        *    Input		:	Source image file name                                          *
        *-----------------------------------------------------------------------------------*
        *    Return		:	-1: Error - File not found or Empty file name                   *
        *                   -2: Error - Invalid image format -or- not supported by GDI+     *
        *                    0: OK                                                          *
        *-----------------------------------------------------------------------------------*
        *  procedure    :   + open the image file,                                          *
        *                   + compute total size,                                           *
        *                   + convert the indexed image to 24-bit (if any),                 *
        *                   + create srcBitmap and resBitmap,                               *
        *                   + create srcImg and resImg by calling toGray function,          *
        *                   + create srcGraph and resGraph                                  *
        ************************************************************************************/
        public int initGray(string imageFileName)
        {
            Bitmap tmp;

            // Check if the image file name is empty
            if (imageFileName == "")
            {
                return -1;
            }

            try
            {
                // Create temprary image
                tmp = (Bitmap)Bitmap.FromFile(imageFileName);
            }
            catch (FileNotFoundException)
            {
                return -1;
            }
            catch (OutOfMemoryException)
            {
                return -2;
            }
            // Initialize image total size
            totSize = tmp.Width * tmp.Height;

            // Create source and result buffers
            srcImg = new Byte[tmp.Height, tmp.Width];
            resImg = new Byte[tmp.Height, tmp.Width];

            // Convert indexed bitmap to 24-bits
            if (tmp.PixelFormat == PixelFormat.Format8bppIndexed)
            {
                srcBitmap = new Bitmap(tmp.Width, tmp.Height, PixelFormat.Format24bppRgb);
                resBitmap = new Bitmap(tmp.Width, tmp.Height, PixelFormat.Format24bppRgb);

                Color c;
                for (int y = 0; y < tmp.Height; y++)
                {
                    for (int x = 0; x < tmp.Width; x++)
                    {
                        c = tmp.GetPixel(x, y);
                        srcBitmap.SetPixel(x, y, c);
                        resBitmap.SetPixel(x, y, c);
                    }
                }
            }
            else
            {
                srcBitmap = (Bitmap)Bitmap.FromFile(imageFileName);
                resBitmap = (Bitmap)Bitmap.FromFile(imageFileName);
            }

            // Get gray values from source bitmap and save them to source and result image buffers
            toGray();

            // Create result bitmap
            byteToBmp(resImg, resBitmap);

            // Create graphics objects
            srcGraph = Graphics.FromImage(srcBitmap);
            resGraph = Graphics.FromImage(resBitmap);

            return 0;
        }

        /*************************************************************
        *    Function	:	Convert both srcImg and resImg to gray   *
        *************************************************************/
        public void toGray()
        {
            int x, y;
            Color c;

            // copy gray pixels from source bitmap to both source and result buffers
            for (y = 0; y < srcBitmap.Height; y++)
                for (x = 0; x < srcBitmap.Width; x++)
                {
                    c = srcBitmap.GetPixel(x, y);
                    srcImg[y, x] = Convert.ToByte((c.R + c.G + c.B) / 3);
                    resImg[y, x] = srcImg[y, x];
                }
        }


/************************************************************************************************
*                                                                                               *
*                                      Convertions                                              *
*                                                                                               *
************************************************************************************************/


        /********************************************************
        *   Function    :   Copy [Byte] sImg to [Byte] rImg     *
        ********************************************************/
        public void copyImage(Byte[,] sImg, Byte[,] rImg)
        {
            int y, x;

            for (y = 0; y < resBitmap.Height; y++)
                for (x = 0; x < resBitmap.Width; x++)
                    rImg[y, x] = sImg[y, x];
        }

        /****************************************************************
        *   Function    :   Convert [Byte] sImg to Bitmap tBitmap image *
        ****************************************************************/
        public void byteToBmp(Byte[,] sImg, Bitmap tBitmap)
        {
            int x, y;

            // Create the result image
            for (y = 0; y < tBitmap.Height; y++)
                for (x = 0; x < tBitmap.Width; x++)
                {
                    tBitmap.SetPixel(x, y, Color.FromArgb(sImg[y, x], sImg[y, x], sImg[y, x]));
                }
        }

        /************************************************************
        *   Function    :   Convert Bitmap bmpImg to [Byte] bImg    *
        ************************************************************/
        public void bmpToByte(Bitmap bmpImg, Byte[,] bImg)
        {
            int x, y;
            Color c;

            // Create the result image
            for (y = 0; y < bmpImg.Height; y++)
                for (x = 0; x < bmpImg.Width; x++)
                {
                    c = bmpImg.GetPixel(x, y);
                    bImg[y, x] = Convert.ToByte((c.R + c.G + c.B) / 3);
                }
        }

        /********************************************************************************
	    *    Function	:	save the result image                                       *
        *-------------------------------------------------------------------------------*
	    *    Input		:	rsault image file name                                      *
	    *-------------------------------------------------------------------------------*
	    *    Return		:	-1: Error - File not found or Empty file name               *
        * 			        -2: Error - Invalid image format -or- same source file name *
        *                    0: OK                                                      *
        ********************************************************************************/
        public int save(string imageFileName)
        {
            if (imageFileName == "")
            {
                MessageBox.Show("Empty file name");
                return -1;
            }

            try
            {
                resBitmap.Save(imageFileName);
            }
            catch (ExternalException)
            {
                MessageBox.Show("Invalid image format -or- same source file name");
                return -2;
            }

            return 0;
        }


/************************************************************************************************
*                                                                                               *
*                                           Histogram                                           *
*                                                                                               *
************************************************************************************************/


        // Histogram array
        public int[] histoAry;

        public void createHistogram(byte[,] img)
        {
            histoAry = null;
            histoAry = new int[256];
            int x, y;

            for (x = 0; x < 256; x++)
                histoAry[x] = 0;

            for (y = 0; y < resBitmap.Height; y++)
                for (x = 0; x < resBitmap.Width; x++)
                {
                    ++histoAry[img[y, x]];
                }
        }

        public void showHistogram(Bitmap resBmp, byte x)
        {
            int co, xco, sx, ex, sy, ey;

            sx = 10; ex = resBmp.Width - sx;
            sy = 50; ey = resBmp.Height - sy;
            xco = sx;

            Graphics g = Graphics.FromImage(resBmp);
            g.Clear(Color.LightGray);
            g.DrawLine(Pens.Blue, sx, ey, ex, ey);
            g.DrawLine(Pens.Blue, sx, ey, sx, sy);

            for (co = 0; co < 256; co++)
            {
                g.DrawRectangle(Pens.Black, xco++, ey - (histoAry[co] / 10), 1, histoAry[co] / 10);
            }

            g.DrawRectangle(Pens.Red, x + sx, ey - (histoAry[x] / 10), 1, histoAry[x] / 10);
        }

        public void histogramNormalization(byte[,] img, int newMin, int newMax)
        {
            int oldMin, oldMax, co;

            // Compute the min
            co = 0;
            while (co < 256)
                if (histoAry[co] == 0)
                    ++co;
                else
                    break;
            oldMin = co;

            // Compute the max
            co = 255;
            while (co > 0)
                if (histoAry[co] == 0)
                    --co;
                else
                    break;
            oldMax = co;

            double perc = (newMax - newMin) / (oldMax - oldMin);

            for (int y = 0; y < resBitmap.Height; y++)
                for (int x = 0; x < resBitmap.Width; x++)
                {
                    img[y, x] =  (byte)(perc * (img[y, x] - oldMin) + newMin);
                }
        }

        public void histogramEquilization(byte[,] img, int newMin, int newMax)
        {
            int co, sum = 0;
            int range = newMax - newMin;
            int[] histoTabel = new int[256];
            double tmp = (double)range / totSize;

            for (co = 0; co < 256; co++)
            {
                sum += histoAry[co];
                histoTabel[co] = (int)Math.Round(tmp * sum + 0.00001);
            }
            
            for (int y = 0; y < resBitmap.Height; y++)
                for (int x = 0; x < resBitmap.Width; x++)
                {
                    img[y, x] = (byte)histoTabel[img[y, x]];
                }
        }
        

/************************************************************************************************
*                                                                                               *
*                                           Filters                                             *
*                                                                                               *
************************************************************************************************/


        public void expFilter(byte[,] sImg, byte[,] rImg)
        {
            for (int y = 0; y < resBitmap.Height; y++)
            {
                for (int x = 0; x < resBitmap.Width; x++)
                {
                    rImg[y, x] = (byte)Math.Abs(20 * Math.Exp(sImg[y, x] / 100));
                }
            }
        }

        public void logFilter(byte[,] sImg, byte[,] rImg)
        {
            for (int y = 0; y < resBitmap.Height; y++)
            {
                for (int x = 0; x < resBitmap.Width; x++)
                {
                    rImg[y, x] = (byte)Math.Abs(20 * Math.Log10(sImg[y, x] * 100));
                }
            }
        }

        /*****************************************************************************
        *   Function	:	apply median filter on the source image and save         *
        *                   the results on the result image                          *
        *----------------------------------------------------------------------------*
        *    Input		:	source and result image                                  *
        *****************************************************************************/
        public void medianFilter(byte[,] sImg, byte[,] rImg)
        {
            int fs = 3;
            // Apply median filter
            // image x, y
            int x, y;
            // source x, y
            int sx, sy;
            // filter x, y
            int fx, fy;
            // half filter size
            int hfs = fs / 2;
            // sorted value (centered in the array)
            int sv = fs * fs / 2;
            // filter array size
            int fas = fs * fs;
            // filter array
            Byte[] fa = new Byte[fas];
            // filter counter
            int fco;

            for (sy = hfs; sy < srcBitmap.Height - hfs; sy++)
                for (sx = hfs; sx < srcBitmap.Width - hfs; sx++)
                {
                    fco = 0;
                    for (fy = -hfs; fy <= hfs; fy++)
                    {
                        y = sy + fy;
                        for (fx = -hfs; fx <= hfs; fx++)
                        {
                            x = sx + fx;
                            fa[fco] = sImg[y, x];
                            ++fco;
                        }
                    }
                    Array.Sort(fa);
                    rImg[sy, sx] = fa[sv];
                }
        }

        public void bilinearScaling(int[,] sAry, Byte[,] rAry, int w1, int w2, int h1, int h2)
        {
            int x, y, nx, ny, ix, iy;
            int A, B, C, D;
            float x_dif, y_dif;

            float x_ratio = (float)w1 / w2;
            float y_ratio = (float)h1 / h2;

            for (ny = 0; ny < h2; ny++)
                for (nx = 0; nx < w2; nx++)
                {
                    x = (int)(x_ratio * nx);
                    y = (int)(y_ratio * ny);
                    x_dif = (x_ratio * nx) - x;
                    y_dif = (y_ratio * ny) - y;

                    ix = x + 1;
                    iy = y + 1;
                    if (ix >= w1)
                        ix = w1 - 1;
                    if (iy >= h1)
                        iy = h1 - 1;

                    A = sAry[y, x];
                    B = sAry[y, ix];
                    C = sAry[iy, x];
                    D = sAry[iy, ix];

                    rAry[ny, nx] = (byte)(A * (1 - x_dif) * (1 - y_dif) +
                                         B * (x_dif) * (1 - y_dif) +
                                         C * (y_dif) * (1 - x_dif) +
                                         D * (x_dif * y_dif));
                }
        }

/************************************************************************************************
*                                                                                               *
*                                      Edge Detection                                           *
*                                                                                               *
************************************************************************************************/


        public void basicEdgeDetector(byte[,] sImg, byte[,] rImg)
        {
            for (int y = 0; y < resBitmap.Height - 2; y++)
            {
                for (int x = 0; x < resBitmap.Width - 2; x++)
                {
                    rImg[y, x] = (byte)Math.Abs(2 * sImg[y, x] - sImg[y, x + 1] - sImg[y + 1, x]);
                }
            }
        }

        public void basicEdgeFilter(byte[,] sImg, byte[,] rImg)
        {
            // image x, y
            int x, y;
            // filter x, y
            int fx, fy;
            // filter array
            int[,] fAry = new int[2, 2] {
                                            {-1, +0},   //{+2, -1},
                                            {-1, +0}    //{-1, +0}
                                        };
            int tmp;

            for (y = 0; y < srcBitmap.Height - 2; y++)
                for (x = 0; x < srcBitmap.Width - 2; x++)
                {
                    tmp = 0;
                    for(fy = 0; fy < 2; fy++)
                        for(fx = 0; fx < 2; fx++)
                        {
                            tmp += sImg[y + fy, x + fx] * fAry[fy, fx];
                        }

                    rImg[y, x] = (byte)Math.Abs(tmp);
                }
        }

        public void robertsCrossEdgeFilter(byte[,] sImg, byte[,] rImg)
        {
            // image x, y
            int x, y;
            // filter x, y
            int fx, fy;
            // filter array
            int[,] fAry = new int[2, 2] {
                                            {+1, +0},
                                            {+0, -1}
                                        };
            int tmp;

            for (y = 0; y < srcBitmap.Height - 2; y++)
                for (x = 0; x < srcBitmap.Width - 2; x++)
                {
                    tmp = 0;
                    for (fy = 0; fy < 2; fy++)
                        for (fx = 0; fx < 2; fx++)
                        {
                            tmp += sImg[y + fy, x + fx] * fAry[fy, fx];
                        }

                    rImg[y, x] = (byte)Math.Abs(tmp);
                }
        }

        /*public void vEdgeDetector(byte[,] sImg, byte[,] rImg)
        {
            int fs = 3;
            // image height and width
            int height, width;
            // image x, y
            int x, y;
            // source x, y
            int sx, sy;
            // filter x, y
            int fx, fy;
            // half filter size
            int hfs = fs / 2;
            /*byte[] fAry = {
                            {1, 1, 0},
                            {1, 1, 0},
                            {1, 1, 0}
                          };
            // filter counter
            int fco;

            height = srcBitmap.Height - hfs;
            width = srcBitmap.Width - hfs;

            for (sy = hfs; sy < height; sy++)
                for (sx = hfs; sx < width; sx++)
                {
                    fco = 0;
                    for (fy = -hfs; fy <= hfs; fy++)
                    {
                        y = sy + fy;
                        for (fx = -hfs; fx <= hfs; fx++)
                        {
                            x = sx + fx;
                            if(sImg[y, x] != fAry[fco])
                            ++fco;
                        }
                    }
                }
        }*/

        /*****************************************************************************
        *   Function	:	reduce the number of colors in the source image and save *
        *                   the result's image on the result image                   *
        *----------------------------------------------------------------------------*
        *    Input		:	the new number of colors for the result image            *
        *                   source and result image                                  *
        *****************************************************************************/
        public void quantizeGrayImage(byte colors, byte[,] sImg, byte[,] rImg)
        {
            byte levelStep = (byte)(256 / colors);
            byte level, co;

            for (int y = 0; y < resBitmap.Height; y++)
                for (int x = 0; x < resBitmap.Width; x++)
                {
                    level = 0;
                    for (co = 0; co < colors; co++)
                    {
                        if (sImg[y, x] >= level && sImg[y, x] < (level + levelStep))
                        {
                            rImg[y, x] = level;
                            break;
                        }
                        level += levelStep;
                    }
                }
        }


/************************************************************************************************
*                                                                                               *
*                                           Binarization                                        *
*                                                                                               *
************************************************************************************************/


        public void customBinarization(byte thresh)
        {
            int y, x;

            for (y = 0; y < srcBitmap.Height; y++)
                for (x = 0; x < srcBitmap.Width; x++)
                {
                    if (srcImg[y, x] < thresh)
                        resImg[y, x] = 0;
                    else
                        resImg[y, x] = 255;
                }
        }

        public void globalBinarization(byte[,] rImg)
        {
            int x, y;
            int sum = 0, gsum = 0, lsum = 0;
            int gco = 0, lco = 0;
            int avg = 0, gavg = 0, lavg = 0;
            int thresh;

            // compute overall average
            for (y = 0; y < resBitmap.Height; y++)
                for (x = 0; x < resBitmap.Width; x++)
                {
                    sum += rImg[y, x];
                }
            avg = sum / totSize;

            // compute the greater/less than average
            for (y = 0; y < resBitmap.Height; y++)
                for (x = 0; x < resBitmap.Width; x++)
                {
                    if (rImg[y, x] > avg)
                    {
                        gsum += rImg[y, x];
                        ++gco;
                    }
                    else
                    {
                        lsum += rImg[y, x];
                        ++lco;
                    }
                }
            gavg = gsum / gco;
            lavg = lsum / lco;

            // compute the threshold
            thresh = (int)((gavg + lavg) * 0.5);

            // change image values
            for (y = 0; y < resBitmap.Height; y++)
                for (x = 0; x < resBitmap.Width; x++)
                {
                    if (rImg[y, x] < thresh)
                        rImg[y, x] = 0;
                    else
                        rImg[y, x] = 255;
                }
        }


/************************************************************************************************
*                                                                                               *
*                                    Region Growing Segmentation                                *
*                                                                                               *
************************************************************************************************/

        // A class for holding segmented region information and pixels
        public class segRegion
        {
            public int pixNo;

            public Point[] points;

            public segRegion nextPtr;

            public segRegion()
            {
                pixNo = 0;
                nextPtr = null;
            }
        }

        // segment points counter
        public int segPointsCo;
        // points array used to hold segment points
        public Point[] stackPoints;

        // Segment an image using Region Grow Non recursively Enhanced method
        public segRegion segmentRegGrowE(int verbose)
        {
            // Record the start time
            DateTime sto = DateTime.Now;
            /*
             * it supposed to be it like this
             * 
             * DateTime sto
             * if(verbose == true)
             * {
             *      sto = DateTime.Now();
             * }
             * 
             * but the other if will not see that sto is set because it is set inside this block
             * which is unseen by the other if (sto must have an initial value), so we decide
             * to give it the initial value without if (an extra work if verbose is false)
             * 
             */

            int y, x, segCo = 0, co;
            segRegion head, end, tmp;

            if (stackPoints == null)
            {
                stackPoints = new Point[totSize];
                for (co = 0; co < totSize; co++)
                    stackPoints[co] = new Point();
            }

            head = new segRegion();
            end = head;

            for (y = 0; y < srcBitmap.Height; y++)
                for (x = 0; x < srcBitmap.Width; x++)
                    if (resImg[y, x] == 0)
                    {
                        ++segCo;
                        segPointsCo = 1;
                        resImg[y, x] = 255;
                        getRegionE(x, y);

                        tmp = new segRegion();
                        tmp.pixNo = segPointsCo;
                        tmp.points = new Point[segPointsCo];
                        for (co = 0; co < segPointsCo; co++)
                            tmp.points[co] = new Point(stackPoints[co].x, stackPoints[co].y);
                        tmp.nextPtr = null;

                        end.nextPtr = tmp;
                        end = tmp;
                    }

            head.pixNo = segCo;

            if (verbose == 1)
            {
                // Record the stop time
                DateTime eto = DateTime.Now;
                // Calculate difference in time
                TimeSpan ts = eto.Subtract(sto);
                // Display results
                String msg = "-> Time consumed:  " + ts.Minutes + ":" + ts.Seconds + ":" + ts.Milliseconds;
                MessageBox.Show(msg, "Seed-Filling Segmentation Results");
            }

            return (head);
        }

        // get one region using Non recursive flood fill algorithm
        public void getRegionE(int x, int y)
        {
            int curPtr = 0, endPtr = 0;
            int cx, cy, chgX, chgY;

            stackPoints[0].x = x;
            stackPoints[0].y = y;

            while (curPtr <= endPtr)
            {
                cx = stackPoints[curPtr].x;
                cy = stackPoints[curPtr].y;

                chgX = cx + 1;

                if (chgX < resBitmap.Width)
                    if (resImg[cy, chgX] == 0)
                    {
                        ++segPointsCo;
                        resImg[cy, chgX] = 255;
                        ++endPtr;
                        stackPoints[endPtr].x = chgX;
                        stackPoints[endPtr].y = cy;
                    }

                chgX = cx - 1;

                if (chgX >= 0)
                    if (resImg[cy, chgX] == 0)
                    {
                        ++segPointsCo;
                        resImg[cy, chgX] = 255;
                        ++endPtr;
                        stackPoints[endPtr].x = chgX;
                        stackPoints[endPtr].y = cy;
                    }

                chgY = cy + 1;

                if (chgY < srcBitmap.Height)
                    if (resImg[chgY, cx] == 0)
                    {
                        ++segPointsCo;
                        resImg[chgY, cx] = 255;
                        ++endPtr;
                        stackPoints[endPtr].x = cx;
                        stackPoints[endPtr].y = chgY;
                    }

                chgY = cy - 1;

                if (chgY >= 0)
                    if (resImg[chgY, cx] == 0)
                    {
                        ++segPointsCo;
                        resImg[chgY, cx] = 255;
                        ++endPtr;
                        stackPoints[endPtr].x = cx;
                        stackPoints[endPtr].y = chgY;
                    }

                ++curPtr;
            }// End while

        }

        // find the largest segment (used to get the pupil region)
        public segRegion findMaxSegment(segRegion head)
        {
            segRegion tmp = head.nextPtr;
            int max = tmp.pixNo;
            int i = 0, indx = 0;

            while (tmp != null)
            {
                if (tmp.pixNo > max)
                {
                    max = tmp.pixNo;
                    indx = i;
                }
                tmp = tmp.nextPtr;
                ++i;
            }

            tmp = head.nextPtr;
            for (int co = 0; co < indx; co++)
            {
                tmp = tmp.nextPtr;
            }

            return (tmp);
        }


/************************************************************************************************
*                                                                                               *
*                                    Pupil Processing Functions                                 *
*                                                                                               *
************************************************************************************************/


        // Pupil center point
        public Point pupilCenter;
        // Pupil radius
        public int pupilRadius;
        // Pupil rectangular area coordinates
        public int pupilLX, pupilRX, pupilUY, pupilDY;

        // Segmentation for pupil
        segRegion head;
        segRegion maxSeg;

        /************************************************************************************
        *   Function	:	Locate the pupil area in an image                               *
        *-----------------------------------------------------------------------------------*
        *    Input		:	Source image file name                                          *
        *                   Pupil to image ratio                                            *
        *                   Verbose:    1 -> display consumed time                          *
        *                               2 -> draw a red arc around the pupil area           *
        *                               3 -> both of 1 and 2                                *
        *-----------------------------------------------------------------------------------*
        *    Return		:	 1: OK                                                          *
        *                    0: pupil radius <= 0                                           *
        *-----------------------------------------------------------------------------------*
        *  procedure    :   + create a histogram for the image                              *
        *                   + compute the threshold value from the histogram based on       *
        *                     the pupil to image ratio                                      *
        *                   + Binarization based on the threshold value                     *
        *                   + Segmentation                                                  *
        *                   + Find max segment as the pupil                                 *
        *                   + Compute the pupil center and radius                           *
        *                   + Fill the white pupil holes with black                         *
        *                   + Apply pupil cicrular fitting                                  *
        ************************************************************************************/
        public int locatePupil(Bitmap resBmp, int pupilImgRatio, int verbose)
        {
            // Record the start time
            DateTime sto = DateTime.Now;

            // Get histogram
            createHistogram(resImg);

            // Find Threshold
            byte indx = 0;
            int co = 0;
            long size;

            size = totSize * pupilImgRatio / 100;
            while (co < size)
            {
                co += histoAry[indx];
                ++indx;
            }

            // Convert the image to 2 colors Black and White (0 or 255) in which the pupil will be black
            customBinarization(indx);

            // Save the binarized image before the segmentation convert it to a white image
            byte[,] tmpImg = new byte[resBmp.Height, resBmp.Width];
            copyImage(resImg, tmpImg);

            head = segmentRegGrowE(0);

            // Find Pupil Segment (Largest Segment)
            maxSeg = findMaxSegment(head);

            // Find Pupil Radius and Center
            computePupilCenterRadius(maxSeg);

            if (pupilRadius > 0)
            {
                int x, y;
                // Convert the result image to white
                for (y = 0; y < resBmp.Height; y++)
                    for (x = 0; x < resBmp.Width; x++)
                        resImg[y, x] = 255;

                // Draw the max segment on the white result image
                for (co = 0; co < maxSeg.pixNo; co++)
                {
                    resImg[maxSeg.points[co].y, maxSeg.points[co].x] = 0;
                }

                // Fill the white holes in the pupil with black color (include it)
                fillPupil();

                // Apply pupil circular fitting
                pupilCircleFitting();

                // Update source and result images with the new fixed pupil
                // Restore the result image from the source image
                Graphics g = Graphics.FromImage(resBmp);
                g.FillEllipse(Brushes.Black, pupilCenter.x - pupilRadius, pupilCenter.y - pupilRadius, pupilRadius * 2, pupilRadius * 2);
                bmpToByte(resBmp, srcImg);
                copyImage(srcImg, resImg);
                byteToBmp(srcImg, this.srcBitmap);
                byteToBmp(srcImg, this.resBitmap);

                if ((verbose & 1) == 1)
                {
                    g.DrawArc(new Pen(Color.FromArgb(255, 0, 0)), (pupilCenter.x - pupilRadius), (pupilCenter.y - pupilRadius), (pupilRadius * 2), (pupilRadius * 2), 0, 360);
                }

                if ( (verbose & 2) == 2)
                {
                    // Record the stop time
                    DateTime eto = DateTime.Now;
                    // Calculate difference in time
                    TimeSpan ts = eto.Subtract(sto);
                    // Display results
                    String msg = "> Time consumed:  " + ts.Minutes + ":" + ts.Seconds + ":" + ts.Milliseconds;
                    //msg = "> Pupil Center:\n";
                    //msg += "          x: " + pupilCenter.x + "\n";
                    //msg += "          y: " + pupilCenter.y + "\n";
                    //msg += "-> Pupil radius: " + pupilRadius + "\n";
                    g.DrawString(msg, new Font(FontFamily.GenericSerif, 12, FontStyle.Bold), Brushes.Green, 0, 0);
                }

                return (1);
            }
            else
                return (0);
        }

        /************************************************************************************
        *   Function	:	Locate the pupil area in an image with result for each step     *
        *-----------------------------------------------------------------------------------*
        *    Input		:	Bitmap array                                                    *
        *                   Pupil to image ratio                                            *
        *-----------------------------------------------------------------------------------*
        *  procedure    :   + create a histogram for the image                              *
        *                   + compute the threshold value from the histogram based on       *
        *                     the pupil to image ratio                                      *
        *                   + Binarization based on the threshold value                     *
        *                   + Segmentation                                                  *
        *                   + Find max segment as the pupil                                 *
        *                   + Compute the pupil center and radius                           *
        *                   + Fill the white pupil holes with black                         *
        *                   + Apply pupil cicrular fitting                                  *
        ************************************************************************************/
        public void locatePupilVerbose(Bitmap[] imgs, int pupilImgRatio)
        {
            createHistogram(resImg);

            // Find Threshold
            byte indx = 0;
            int co = 0;
            long size;

            size = totSize * pupilImgRatio / 100;
            while (co < size)
            {
                co += histoAry[indx];
                ++indx;
            }

            showHistogram(imgs[0], indx);

            // Convert the image to 2 colors Black and White (0 or 255) in which the pupil will be black
            customBinarization(indx);
            byteToBmp(resImg, imgs[1]);

            head = segmentRegGrowE(0);

            // Find Pupil Segment (Largest Segment)
            maxSeg = findMaxSegment(head);

            // Find Pupil Radius and Center
            computePupilCenterRadius(maxSeg);

            if (pupilRadius > 0)
            {
                int x, y;
                // Convert the result image to white
                for (y = 0; y < resBitmap.Height; y++)
                    for (x = 0; x < resBitmap.Width; x++)
                        resImg[y, x] = 255;

                // Draw the max segment on the white result image
                for (co = 0; co < maxSeg.pixNo; co++)
                    resImg[maxSeg.points[co].y, maxSeg.points[co].x] = 0;

                byteToBmp(resImg, imgs[2]);

                fillPupil();
                byteToBmp(resImg, imgs[3]);

                // Apply pupil circular fitting
                pupilCircleFitting();

                // Warning : No update for source and result images
            }
        }

        // Compute the pupil center point and radius
        public void computePupilCenterRadius(segRegion maxSeg)
        {
            pupilCenter = new Point();
            double xAvg = 0, yAvg = 0;
            int co;

            xAvg = maxSeg.points[0].x;
            yAvg = maxSeg.points[0].y;

            pupilLX = pupilRX = maxSeg.points[0].x;
            pupilUY = pupilDY = maxSeg.points[0].y;

            for (co = 1; co < maxSeg.pixNo; co++)
            {
                xAvg += maxSeg.points[co].x;
                yAvg += maxSeg.points[co].y;

                if (maxSeg.points[co].x < pupilLX)
                    pupilLX = maxSeg.points[co].x;
                if (maxSeg.points[co].x > pupilRX)
                    pupilRX = maxSeg.points[co].x;
                if (maxSeg.points[co].y < pupilUY)
                    pupilUY = maxSeg.points[co].y;
                if (maxSeg.points[co].y > pupilDY)
                    pupilDY = maxSeg.points[co].y;

            }

            xAvg /= maxSeg.pixNo;
            yAvg /= maxSeg.pixNo;

            pupilCenter.x = Convert.ToInt16(xAvg);
            pupilCenter.y = Convert.ToInt16(yAvg);

            pupilRadius = Convert.ToInt16(Math.Sqrt(Math.Pow((maxSeg.points[0].x - xAvg), 2) + Math.Pow((maxSeg.points[0].y - yAvg), 2)));
            //MessageBox.Show("Initial Radius= " + pupilRadius);
        }

        // Old complex method
        /**********************************************************
        // Compute the average of pixels of a specific circle
        public double computeCirclePixelsAvg(int ccx, int ccy, int cr)
        {
            int x, y;
            double newAvg = 0;

            for (int d = 0; d < 360; d++)
            {
                x = Convert.ToInt16(cr * Math.Cos(d * Math.PI / 180) + ccx);
                y = Convert.ToInt16(cr * Math.Sin(d * Math.PI / 180) + ccy);
                newAvg += resImg[y, x];
            }
            newAvg /= 360;

            return (newAvg);
        }

        // correct the pupil area by finding it's exact center and radius
        public void pupilCircularFitting()
        {
            int pcx, pcy, pr;
            double oldAvg = 0, newAvg, diff;
            bool finish;

            pcx = pupilCenter.x;
            pcy = pupilCenter.y;
            pr = 3;
            finish = false;

            newAvg = computeCirclePixelsAvg(pcx, pcy, pr);

            while (finish == false)
            {
                diff = 0;
                while (diff == 0.0)
                {
                    oldAvg = newAvg;

                    ++pr;
                    newAvg = computeCirclePixelsAvg(pcx, pcy, pr);

                    diff = newAvg - oldAvg;
                    //Graphics g = Graphics.FromImage(resBitmap);
                    //g.DrawEllipse(Pens.Red, pcx - pr, pcy - pr, pr * 2, pr * 2);

                } //end sub while

                if (((newAvg = computeCirclePixelsAvg(pcx, pcy + 1, pr)) - oldAvg) == 0)
                {
                    ++pcy;
                    continue;
                }
                if (((newAvg = computeCirclePixelsAvg(pcx, pcy - 1, pr)) - oldAvg) == 0)
                {
                    --pcy;
                    continue;
                }
                if (((newAvg = computeCirclePixelsAvg(pcx + 1, pcy, pr)) - oldAvg) == 0)
                {
                    ++pcx;
                    continue;
                }
                if (((newAvg = computeCirclePixelsAvg(pcx - 1, pcy, pr)) - oldAvg) == 0)
                {
                    --pcx;
                    continue;
                }

                // Diagonal
                if (((newAvg = computeCirclePixelsAvg(pcx - 1, pcy - 1, pr)) - oldAvg) == 0)
                {
                    --pcx;
                    --pcy;
                    continue;
                }
                if (((newAvg = computeCirclePixelsAvg(pcx + 1, pcy - 1, pr)) - oldAvg) == 0)
                {
                    ++pcx;
                    --pcy;
                    continue;
                }
                if (((newAvg = computeCirclePixelsAvg(pcx + 1, pcy + 1, pr)) - oldAvg) == 0)
                {
                    ++pcx;
                    ++pcy;
                    continue;
                }
                if (((newAvg = computeCirclePixelsAvg(pcx - 1, pcy + 1, pr)) - oldAvg) == 0)
                {
                    --pcx;
                    ++pcy;
                    continue;
                }

                finish = true;

            } // end main while

            pupilCenter.x = pcx;
            pupilCenter.y = pcy;
            pupilRadius = pr;
        }
        */
        
        // Check if all pixels are black on this circle
        public bool isCirclePixelsBlack(int ccx, int ccy, int cr)
        {
            int x, y;

            int d = 0;
            bool flag = true;
            while(d <= 360)
            {
                x = Convert.ToInt16(cr * Math.Cos(d * Math.PI / 180) + ccx);
                y = Convert.ToInt16(cr * Math.Sin(d * Math.PI / 180) + ccy);
                if (x >= 0 && x < resBitmap.Width && y >= 0 && y < resBitmap.Height)
                {
                    if (resImg[y, x] != 0)
                    {
                        d = 400;
                        flag = false;
                    }
                    else
                        ++d;
                }
                else
                    return false;
            }
            return flag;
        }

        // Fit a circle in the pupil segment
        public void pupilCircleFitting()
        {
            int pcx, pcy, pr;
            bool finish;

            pcx = pupilCenter.x;
            pcy = pupilCenter.y;
            pr = pupilRadius;
            finish = false;

            // if the initial radius large then reduce the radius until the circle fit
            while (isCirclePixelsBlack(pcx, pcy, pr) == false)
                --pr;

            while (finish == false)
            {
                while (isCirclePixelsBlack(pcx, pcy, pr) == true)
                {
                    ++pr;
                    //Graphics g = Graphics.FromImage(resBitmap);
                    //g.DrawEllipse(Pens.Red, pcx - pr, pcy - pr, pr * 2, pr * 2);

                } //end sub while

                if (isCirclePixelsBlack(pcx, pcy + 1, pr) == true)
                {
                    ++pcy;
                }
                else if (isCirclePixelsBlack(pcx, pcy - 1, pr) == true)
                {
                    --pcy;
                }
                else if (isCirclePixelsBlack(pcx + 1, pcy, pr) == true)
                {
                    ++pcx;
                }
                else if (isCirclePixelsBlack(pcx - 1, pcy, pr) == true)
                {
                    --pcx;
                }

                // Diagonal
                else if (isCirclePixelsBlack(pcx - 1, pcy - 1, pr) == true)
                {
                    --pcx;
                    --pcy;
                }
                else if (isCirclePixelsBlack(pcx + 1, pcy - 1, pr) == true)
                {
                    ++pcx;
                    --pcy;
                }
                else if (isCirclePixelsBlack(pcx + 1, pcy + 1, pr) == true)
                {
                    ++pcx;
                    ++pcy;
                }
                else if (isCirclePixelsBlack(pcx - 1, pcy + 1, pr) == true)
                {
                    --pcx;
                    ++pcy;
                }
                else
                    finish = true;

            } // end main while

            pupilCenter.x = pcx;
            pupilCenter.y = pcy;
            pupilRadius = pr;
            //MessageBox.Show("Radius= " + pr);
        }

        // Fill the pupil white areas with black
        public void fillPupil()
        {
            int x, y;
            bool inFlag;
            int hs;
            int h;

            // Fill horizontally
            hs = (pupilRX - pupilLX + 1)/2;
            for (y = pupilUY; y <= pupilDY; y++)
            {
                // Fill left side
                inFlag = false;
                x = pupilLX;
                h = pupilLX + hs;
                while(x<=h)
                {
                    if (resImg[y, x] == 0)
                    {
                        inFlag = true;
                    }
                    else if(inFlag == true)
                    {
                        resImg[y, x] = 0;
                    }
                    ++x;
                }

                // Fill right side
                inFlag = false;
                x = pupilRX;
                h = pupilRX - hs;
                while (x > h)
                {
                    if (resImg[y, x] == 0)
                    {
                        inFlag = true;
                    }
                    else if (inFlag == true)
                    {
                        resImg[y, x] = 0;
                    }
                    --x;
                }
            }

            // Fill vertically
            hs = (pupilDY - pupilUY + 1) / 2;
            for (x = pupilLX; x <= pupilRX; x++)
            {
                // Fill up
                inFlag = false;
                y = pupilUY;
                h = pupilUY + hs;
                while (y <= h)
                {
                    if (resImg[y, x] == 0)
                    {
                        inFlag = true;
                    }
                    else if (inFlag == true)
                    {
                        resImg[y, x] = 0;
                    }
                    ++y;
                }

                // Fill down
                inFlag = false;
                y = pupilDY;
                h = pupilDY - hs;
                while (y > h)
                {
                    if (resImg[y, x] == 0)
                    {
                        inFlag = true;
                    }
                    else if (inFlag == true)
                    {
                        resImg[y, x] = 0;
                    }
                    --y;
                }
            }
        }

        // locate pupil in files and save result images for a number of pupil to image ratios
        public int pupilFileCo = 1;
        public void locatePupilInFiles(string path)
        {
            try
            {
                DirectoryInfo dInfo = new DirectoryInfo(path);
                DirectoryInfo[] dirs = dInfo.GetDirectories();
                FileInfo[] files = dInfo.GetFiles();

                foreach (FileInfo fi in files)
                {
                    fi.Extension.ToLower();
                    if ( (fi.Extension != ".jpg") && (fi.Extension != ".bmp") )
                        continue;

                    // Open the file
                    string filePath = "E:\\Research\\Results\\Selected Images\\" + pupilFileCo.ToString() + "\\";
                    DirectoryInfo dr = new DirectoryInfo(filePath);
                    dr.Create();
                    for (int imgPerx = 1; imgPerx <= 20; imgPerx++)
                    {
                        initGray(fi.FullName);
                        string fn = fi.Name;
                        savePupilResults(resBitmap, (filePath + fn.Insert(fn.IndexOf("."), "Prc_" + imgPerx.ToString()) ), imgPerx, true);
                    }
                    ++pupilFileCo;
                }

                foreach (DirectoryInfo di in dirs)
                {
                    path = di.FullName;
                    locatePupilInFiles(path);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show("Don't have permission: " + ex.Message);
            }
            catch (ArgumentNullException ex)
            {
                MessageBox.Show("path is null: " + ex.Message);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show("path is a zero-length string, contains only white space, or contains one or more invalid characters: " + ex.Message);
            }
            catch (PathTooLongException ex)
            {
                MessageBox.Show("The specified path, file name, or both exceed the system-defined maximum length\n" +
                "on Windows-based platforms, paths must be less than 248 characters and file names must be less than 260 characters: " + ex.Message);
            }
            catch (DirectoryNotFoundException ex)
            {
                MessageBox.Show("The specified path is invalid: " + ex.Message);
            }
            catch (IOException ex)
            {
                MessageBox.Show("path is a file name: " + ex.Message);
            }
        }

        /************************************************************************************
        *   Function	:	Save the resultant image of pupil localization of some image    *
        *-----------------------------------------------------------------------------------*
        *    Input		:	Bitmap image                                                    *
        *                   File name to save the results in                                *
        *                   Pupil to image ratio                                            *
        *                   True/False for using or not using pupil filling technology      *
        *-----------------------------------------------------------------------------------*
        *  procedure    :   + create a histogram for the image                              *
        *                   + compute the threshold value from the histogram based on       *
        *                     the pupil to image ratio                                      *
        *                   + Binarization based on the threshold value                     *
        *                   + Segmentation                                                  *
        *                   + Find max segment as the pupil                                 *
        *                   + Compute the pupil center and radius                           *
        *                   + Fill the white pupil holes with black (if useFillTech is True)*
        *                   + Apply pupil cicrular fitting                                  *
        *                   + Generate result information on result image                   *
        *                   + Save the result image in a file                               *
        ************************************************************************************/
        public void savePupilResults(Bitmap resBmp, string fileName, int pupilImgRatio, bool useFillTech)
        {
            // Record the start time
            DateTime sto = DateTime.Now;

            // Get histogram
            createHistogram(resImg);

            // Find Threshold
            byte indx = 0;
            int co = 0;
            long size;

            // size = imgPer% of the image size, which represent the area taken by the pupil (approximatly)
            size = totSize * pupilImgRatio / 100;
            while (co < size)
            {
                co += histoAry[indx];
                ++indx;
            }

            // Convert the image to 2 colors Black and White (0 or 255) in which the pupil will be black
            customBinarization(indx);

            head = segmentRegGrowE(0);

            // Find Pupil Segment (Largest Segment)
            maxSeg = findMaxSegment(head);

            // Find Pupil Radius and Center
            computePupilCenterRadius(maxSeg);

            String msg;
            Graphics g = Graphics.FromImage(resBmp);

            if (pupilRadius > 0)
            {
                int x, y;
                // Convert the result image to white in case the segmentation did not
                for (y = 0; y < resBmp.Height; y++)
                    for (x = 0; x < resBmp.Width; x++)
                        resImg[y, x] = 255;

                // Draw the max segment on the white result image
                for (co = 0; co < maxSeg.pixNo; co++)
                    resImg[maxSeg.points[co].y, maxSeg.points[co].x] = 0;

                // Fill the white holes in the pupil with black color (include it)
                if (useFillTech == true)
                    fillPupil();

                // Apply pupil circular fitting
                pupilCircleFitting();

                // Record the stop time
                DateTime eto = DateTime.Now;
                // Calculate difference in time
                TimeSpan ts = eto.Subtract(sto);

                Graphics tg = Graphics.FromImage(srcBitmap);
                tg.FillEllipse(Brushes.Red, (pupilCenter.x - pupilRadius), (pupilCenter.y - pupilRadius), (pupilRadius * 2), (pupilRadius * 2));

                int area = 0;
                for (y = pupilUY; y <= pupilDY; y++)
                    for (x = pupilLX; x <= pupilRX; x++)
                    {
                        if (srcBitmap.GetPixel(x, y).R == 255)
                            ++area;
                    }

                // Display results
                //msg = "> Time consumed:  " + ts.Minutes + ":" + ts.Seconds + ":" + ts.Milliseconds + "\n";
                msg = "> Pupil Center:" + "(" + pupilCenter.x + ", " + pupilCenter.y + ")\n";
                msg += "> Pupil radius: " + pupilRadius;
                g.DrawString(msg, new Font(FontFamily.GenericSerif, 10, FontStyle.Bold), Brushes.DarkRed, 0, 0);

                //msg = "> Pupil area:\n";
                msg = "> Pupil area: " + area;
                //msg += "    [by R*R*PI]: " + (int)(pupilRadius * pupilRadius * Math.PI) + "\n";
                //msg += "    [by pixels count]: " + area;
                g.DrawString(msg, new Font(FontFamily.GenericSerif, 10, FontStyle.Bold), Brushes.DarkRed, 0, resBitmap.Height - 20);

                //g.DrawArc(new Pen(Color.FromArgb(255, 0, 0)), (pupilCenter.x - pupilRadius), (pupilCenter.y - pupilRadius), (pupilRadius * 2), (pupilRadius * 2), 0, 360);
                g.FillEllipse(Brushes.Red, (pupilCenter.x - pupilRadius), (pupilCenter.y - pupilRadius), (pupilRadius * 2), (pupilRadius * 2));
            }
            else
                g.DrawString("Can't Find the Pupil", new Font(FontFamily.GenericSerif, 14, FontStyle.Bold), Brushes.Red, 0, 0);

            save(fileName);
        }


/************************************************************************************************
*                                                                                               *
*                                    Iris Processing Functions                                  *
*                                                                                               *
************************************************************************************************/


        // Iris radius
        public int irisRadius;
        // Iris rectangular area coordinates
        int iray1;
        int iray2;
        int irax1;
        int irax2;
        // Iris rectangular area Height and Width
        int iraH;
        int iraW;

        /************************************************************************************
        *   Function	:	Locate the iris area in an image                                *
        *-----------------------------------------------------------------------------------*
        *  procedure    :   + Locate the pupil area                                         *
        *                   + Compute the iris radius                                       *
        *                   + Locate the iris rectangular pixels                            *
        *                   + Filter the iris rectangular pixels                            *
        *                   + Locate the upper lid                                          *
        *                   + Locate the iris pixels (after removing all noise from above)  *
        *                   + Create the iris array                                         *
        *                   + Apply contrast streach on the iris array to unleash details   *
        *                   + Display the iris array (the final result)                     *
        ************************************************************************************/
        public void locateIris()
        {
            // Locate the pupil
            locatePupil(resBitmap, 10, 0);
            // Compute the iris radius
            computeIrisRadius(resBitmap, 40, 2, 0);
            // Locate iris rectangular area pixels
            locateIrisRectPixels(resBitmap, 1.6, 0);
            // Draw the resultant array
            displayIrisArea(resBitmap);
            // Filter iris rectangular area pixels
            filterIrisRectPixels(resBitmap, 4, 0);
            // Locate the upper lid
            //locateUpperLashesCurve(resBitmap, null, 0);
            // Locate iris pixels
            //locateIrisPixels(resBitmap, null, 0);
            isolateUpperLashes(resBitmap, null, 0);
            // Create the iris array
            createIrisArray();
            // Contrast streach the iris
            contrastStreachIris(0);
            // Restore the result bitmap from the source image
            byteToBmp(srcImg, resBitmap);
            // Display the iris array
            displayIrisArray(resBitmap);
        }

        // locate iris in files and save result images
        public int irisFileCo = 1;
        public void locateIrisInFiles(string path)
        {
            try
            {
                DirectoryInfo dInfo = new DirectoryInfo(path);
                DirectoryInfo[] dirs = dInfo.GetDirectories();
                FileInfo[] files = dInfo.GetFiles();

                foreach (FileInfo fi in files)
                {
                    fi.Extension.ToLower();
                    if ((fi.Extension != ".jpg") && (fi.Extension != ".bmp"))
                        continue;

                    // Open the file
                    string filePath = "f:\\results\\" + irisFileCo.ToString() + "\\";
                    DirectoryInfo dr = new DirectoryInfo(filePath);
                    dr.Create();
                    for (int radStps = 1; radStps <= 20; radStps++)
                    {
                        initGray(fi.FullName);
                        string fn = fi.Name;
                        saveIrisResults(resBitmap, (filePath + fn.Insert(fn.IndexOf("."), "_radStps_" + radStps.ToString())), 40, radStps);
                    }
                    ++irisFileCo;
                }

                foreach (DirectoryInfo di in dirs)
                {
                    path = di.FullName;
                    locateIrisInFiles(path);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show("Don't have permission: " + ex.Message);
            }
            catch (ArgumentNullException ex)
            {
                MessageBox.Show("path is null: " + ex.Message);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show("path is a zero-length string, contains only white space, or contains one or more invalid characters: " + ex.Message);
            }
            catch (PathTooLongException ex)
            {
                MessageBox.Show("The specified path, file name, or both exceed the system-defined maximum length " +
                "on Windows-based platforms, paths must be less than 248 characters and file names must be less than 260 characters: " + ex.Message);
            }
            catch (DirectoryNotFoundException ex)
            {
                MessageBox.Show("The specified path is invalid: " + ex.Message);
            }
            catch (IOException ex)
            {
                MessageBox.Show("path is a file name: " + ex.Message);
            }
        }

        /************************************************************************************
        *   Function	:	Save the resultant image of iris localization of some image     *
        *-----------------------------------------------------------------------------------*
        *    Input		:	Bitmap image                                                    *
        *                   File name to save the results in                                *
        *                   Pupil to image ratio                                            *
        *                   True/False for using or not using pupil filling technology      *
        *-----------------------------------------------------------------------------------*
        *  procedure    :   + create a histogram for the image                              *
        *                   + compute the threshold value from the histogram based on       *
        *                     the pupil to image ratio                                      *
        *                   + Binarization based on the threshold value                     *
        *                   + Segmentation                                                  *
        *                   + Find max segment as the pupil                                 *
        *                   + Compute the pupil center and radius                           *
        *                   + Fill the white pupil holes with black (if useFillTech is True)*
        *                   + Apply pupil cicrular fitting                                  *
        *                   + Generate result information on result image                   *
        *                   + Save the result image in a file                               *
        ************************************************************************************/
        public void saveIrisResults(Bitmap resBmp, string fileName, int degree, int radiusSteps)
        {
            locatePupil(resBmp, 10, 0);
            computeIrisRadius(resBmp, degree, radiusSteps, 3);
            save(fileName);
        }

        /************************************************************************************
        *   Function	:	Compute the iris circular radius                                *
        *-----------------------------------------------------------------------------------*
        *    Input		:	Bitmap image                                                    *
        *                   Degree for each quarter of the circle (less than 90)            *
        *                   Radius increment value for each step                            *
        *                   Verbose:    1 -> Draw each step                                 *
        *                               2 -> Draw the resultant circular radius             *
        *                               4 -> Show information                               *
        *                               Or 3, 5, or 6
        *-----------------------------------------------------------------------------------*
        *  procedure    :   + Initialize the iris radius to be more than the pupil radius   *
        *                   + Compute the old average of pixels for 4 arcs of a circle,     *
        *                     with directions 0+up, 0+down, 180+up, 180+down,               *
        *                     each arc have a radius of initial iris raduis and             *
        *                     angle specified by degree                                     *
        *                   + Compute the new average in the same method above after        *
        *                     increasing the irisRadius by the specified radius steps       *
        *                   + Compare old and new average and record the max difference     *
        ************************************************************************************/
        public void computeIrisRadius(Bitmap resBitmap, int degree, int radiusSteps, int verbose)
        {
            // Record the start time
            DateTime sto = DateTime.Now;

            int x, y;
            double newAvg = 0, oldAvg = 0;
            double diff = 0;
            int co, max, pos = 0;

            // Initialize the iris radius to be more than the pupil radius to be in the safe side
            irisRadius = pupilRadius + 6;

            // Compute the average of pixels for 4 arcs of a circle specified by defree and irisRadius
            for (int d = 0; d < degree; d++)
            {
                x = Convert.ToInt16(irisRadius * Math.Cos(d * Math.PI / 180) + pupilCenter.x);
                y = Convert.ToInt16(irisRadius * Math.Sin(d * Math.PI / 180) + pupilCenter.y);
                newAvg += resImg[y, x];
                if( (verbose & 1) == 1)
                    resBitmap.SetPixel(x, y, Color.FromArgb(255, 20, 50));
            }
            for (int d = (360 - degree); d < 360; d++)
            {
                x = Convert.ToInt16(irisRadius * Math.Cos(d * Math.PI / 180) + pupilCenter.x);
                y = Convert.ToInt16(irisRadius * Math.Sin(d * Math.PI / 180) + pupilCenter.y);
                newAvg += resImg[y, x];
                if( (verbose & 1) == 1)
                    resBitmap.SetPixel(x, y, Color.FromArgb(255, 20, 50));
            }
            for (int d = (180 - degree); d < 180; d++)
            {
                x = Convert.ToInt16(irisRadius * Math.Cos(d * Math.PI / 180) + pupilCenter.x);
                y = Convert.ToInt16(irisRadius * Math.Sin(d * Math.PI / 180) + pupilCenter.y);
                newAvg += resImg[y, x];
                if( (verbose & 1) == 1)
                    resBitmap.SetPixel(x, y, Color.FromArgb(255, 20, 50));
            }
            for (int d = 180; d < (180 + degree); d++)
            {
                x = Convert.ToInt16(irisRadius * Math.Cos(d * Math.PI / 180) + pupilCenter.x);
                y = Convert.ToInt16(irisRadius * Math.Sin(d * Math.PI / 180) + pupilCenter.y);
                newAvg += resImg[y, x];
                if( (verbose & 1) == 1)
                    resBitmap.SetPixel(x, y, Color.FromArgb(255, 20, 50));
            }
            newAvg /= (degree * 4);


            max = 0;
            co = 0;
            while (co < 120)
            {
                irisRadius += radiusSteps;
                if (((pupilCenter.x + irisRadius) >= resBitmap.Width) || ((pupilCenter.x - irisRadius) < 0))
                    break;
                // Warrning: you don't check for y, so make sure that the degree is small
                /*if (((pupilCenter.y + irisRadius) >= resBitmap.Height) || ((pupilCenter.y - irisRadius) < 0))
                    break;*/

                oldAvg = newAvg;
                newAvg = 0;

                for (int d = 0; d < degree; d++)
                {
                    x = Convert.ToInt16(irisRadius * Math.Cos(d * Math.PI / 180) + pupilCenter.x);
                    y = Convert.ToInt16(irisRadius * Math.Sin(d * Math.PI / 180) + pupilCenter.y);
                    newAvg += resImg[y, x];
                    if( (verbose & 1) == 1)
                        resBitmap.SetPixel(x, y, Color.FromArgb(255, 20, 50));
                }
                for (int d = (360 - degree); d < 360; d++)
                {
                    x = Convert.ToInt16(irisRadius * Math.Cos(d * Math.PI / 180) + pupilCenter.x);
                    y = Convert.ToInt16(irisRadius * Math.Sin(d * Math.PI / 180) + pupilCenter.y);
                    newAvg += resImg[y, x];
                    if( (verbose & 1) == 1)
                        resBitmap.SetPixel(x, y, Color.FromArgb(255, 20, 50));
                }
                for (int d = (180 - degree); d < 180; d++)
                {
                    x = Convert.ToInt16(irisRadius * Math.Cos(d * Math.PI / 180) + pupilCenter.x);
                    y = Convert.ToInt16(irisRadius * Math.Sin(d * Math.PI / 180) + pupilCenter.y);
                    newAvg += resImg[y, x];
                    if( (verbose & 1) == 1)
                        resBitmap.SetPixel(x, y, Color.FromArgb(255, 20, 50));
                }
                for (int d = 180; d < (180 + degree); d++)
                {
                    x = Convert.ToInt16(irisRadius * Math.Cos(d * Math.PI / 180) + pupilCenter.x);
                    y = Convert.ToInt16(irisRadius * Math.Sin(d * Math.PI / 180) + pupilCenter.y);
                    newAvg += resImg[y, x];
                    if( (verbose & 1) == 1)
                        resBitmap.SetPixel(x, y, Color.FromArgb(255, 20, 50));
                }
                newAvg /= (degree * 4);

                diff = newAvg - oldAvg;
                if (diff > max)
                {
                    max = Convert.ToInt16(diff);
                    pos = irisRadius;
                }

                ++co;
            }

            irisRadius = pos;

            Graphics g = Graphics.FromImage(resBitmap);

            if ((verbose & 2) == 2)
            {
                g.DrawArc(new Pen(Color.FromArgb(255, 0, 0)), pupilCenter.x - irisRadius, pupilCenter.y - irisRadius, irisRadius * 2, irisRadius * 2, 0, 360);
            }

            if ((verbose & 4) == 4)
            {
                // Record the stop time
                DateTime eto = DateTime.Now;
                // Calculate difference in time
                TimeSpan ts = eto.Subtract(sto);
                // Display results
                String msg = "> Time consumed:  " + ts.Minutes + ":" + ts.Seconds + ":" + ts.Milliseconds;
                g.DrawString(msg, new Font(FontFamily.GenericSerif, 12, FontStyle.Bold), Brushes.Green, 0, 0);
            }
        }

        /*****-----|        The following methods work on Iris Flag Array,        |-----*****
        ******-----|        were irisFlagAry[0, 0] = resImg[iray1, irax1]         |-----*****/

        private byte[,] irisFlagAry;

        /************************************************************************************
        *   Function	:	Locate iris pixels in the iris rectangular area                 *
        *                   defined by:                                                     *
        *                       Left edge = pupil center.X - iris radius                    *
        *                       Right edge = pupil center.X + iris radius                   *
        *                       Upper edge = pupil center.Y - iris radius                   *
        *                       Lower edge = pupil center.Y + iris radius                   *
        *-----------------------------------------------------------------------------------*
        *    Input		:	Bitmap image                                                    *
        *                   Theta value for the standard deviation                          *
        *                   Verbose:    1 -> Mark the selected iris rectangles with res     *
        *                               2 -> Draw the consumed time                         *
        *-----------------------------------------------------------------------------------*
        *  procedure    :   + Select two rectangular area defined by:                       *
        *                     Left rectangle: between left iris edge and left pupil edge    *
        *                     Right rectangle: between right iris edge and right pupil edge *
        *                     with height = safe pupil height                               *
        *                   + Compute one mean and standard deviation for the both sides    *
        *                   + Compute min and max                                           *
        *                   + Define irax1, irax2, iray1, iray2, iraW, and iraH             *
        *                   + Create the iris flag array with values:                       *
        *                           0 if the pixel in the iris < min                        *
        *                           1 if the pixel in the iris is between min and max       *
        *                           2 if the pixel in the iris > max                        *
        ************************************************************************************/
        public void locateIrisRectPixels(Bitmap resBmp, double theta, int verbose)
        {
            // Record the start time
            DateTime sto = DateTime.Now;

            int safePupilRadius, safeIrisRadius;
            int leftX1, leftX2, rightX1, rightX2, startY, endY;
            int halfHeight, fullHeight;
            // Safe value for both pupil and iris radius
            int safeValue = 4;

            safePupilRadius = pupilRadius + safeValue;
            safeIrisRadius = irisRadius - safeValue;

            // Left rectangle
            leftX2 = pupilCenter.x - safePupilRadius;
            leftX1 = leftX2 - (safeIrisRadius-safePupilRadius);

            // Right rectangle
            rightX1 = pupilCenter.x + safePupilRadius;
            rightX2 = rightX1 + (safeIrisRadius - safePupilRadius);

            fullHeight = safePupilRadius;
            halfHeight = fullHeight / 2;

            // start and end of Y for both left and right rectangles
            startY = pupilCenter.y - halfHeight;
            endY = startY + fullHeight;

            int x, y;
            if ( (verbose & 1) == 1 )
            {
                for (y = startY; y < endY; y++)
                {
                    for (x = leftX1; x < leftX2; x++)
                        resBmp.SetPixel(x, y, Color.Red);
                    for (x = rightX1; x < rightX2; x++)
                        resBmp.SetPixel(x, y, Color.Red);
                }
            }
            
            // sample size = number of pixels taken by both left and right rectangles
            int samplesSize = ((leftX2 - leftX1) * fullHeight) + ((rightX2 - rightX1) * fullHeight);
            
            // Find the mean
            double mean = 0;
            for (y = startY; y < endY; y++)
            {
                for (x = leftX1; x < leftX2; x++)
                    mean += resImg[y, x];
                for (x = rightX1; x < rightX2; x++)
                    mean += resImg[y, x];
            }

            mean /= samplesSize;

            // Find the standart deviation
            double std = 0;
            double tmp;
            for (y = startY; y < endY; y++)
            {
                for (x = leftX1; x < leftX2; x++)
                {
                    tmp = resImg[y, x] - mean;
                    std += tmp * tmp;
                }
                for (x = rightX1; x < rightX2; x++)
                {
                    tmp = resImg[y, x] - mean;
                    std += tmp * tmp;
                }
            }

            std /= samplesSize;
            std = Math.Sqrt(std);

            // Find Min and Max
            tmp = theta * std;
            double min = mean - tmp;
            double max = mean + tmp;

            if (min < 0)
                min = 0;
            if (max > 255)
                max = 255;

            // Scan all pixels in the Iris rectangular area and build the flag array
            iray1 = pupilCenter.y - irisRadius;
            if (iray1 < 0)
                iray1 = 0;

            iray2 = pupilCenter.y + irisRadius;
            if (iray2 >= resBmp.Height)
                iray2 = resBmp.Height - 1;

            irax1 = pupilCenter.x - irisRadius;
            if (irax1 < 0)
                irax1 = 0;

            irax2 = pupilCenter.x + irisRadius;
            if (irax2 >= resBmp.Width)
                irax2 = resBmp.Width - 1;

            // Iris rectangular area Height and Width
            iraH = iray2 - iray1 + 1;
            iraW = irax2 - irax1 + 1;
            
            irisFlagAry = new byte[iraH, iraW];

            int inCo = 0;

            // Mark pixels < min  as 0
            // Mark pixels between min and max as 1
            // Mark pixels > max  as 2
            for (y = iray1; y <= iray2; y++)
                for (x = irax1; x <= irax2; x++)
                {
                    if (resImg[y, x] < min)
                    {
                        irisFlagAry[y - iray1, x - irax1] = 0;
                    }
                    else if (resImg[y, x] > max)
                    {
                        irisFlagAry[y - iray1, x - irax1] = 2;
                    }
                    else
                    {
                        irisFlagAry[y - iray1, x - irax1] = 1;
                        ++inCo;
                    }
                }

            if ( (verbose & 2) == 2)
            {
                // Record the stop time
                DateTime eto = DateTime.Now;
                // Calculate difference in time
                TimeSpan ts = eto.Subtract(sto);
                // Display results
                String msg = "> Time consumed:  " + ts.Minutes + ":" + ts.Seconds + ":" + ts.Milliseconds;
                Graphics g = Graphics.FromImage(resBmp);
                g.DrawString(msg, new Font(FontFamily.GenericSerif, 12, FontStyle.Bold), Brushes.Green, 0, 0);
                msg = "> Iris pixels found: " + inCo + " (" + (inCo * 100 / (iraW*iraH)) + "%)";
                g.DrawString(msg, new Font(FontFamily.GenericSerif, 12, FontStyle.Bold), Brushes.Green, 0, 20);

            }
        }

        /************************************************************************************
        *   Function	:	Locate iris pixels in the iris rectangular area                 *
        *                   defined by:                                                     *
        *-----------------------------------------------------------------------------------*
        *    Input		:	Bitmap image                                                    *
        *                   Number of pixels with value 1 in the 3*3 filter required to     *
        *                   change the pixel in the center to value 1                       *
        *                   Verbose:    1 -> Display the iris area after filtering          *
        *                               2 -> Draw the consumed time and                     *
        *                                    number of pixels changed                       *
        *-----------------------------------------------------------------------------------*
        *  procedure    :   + Apply 3*3 filter on each pixel in the iris area and           *
        *                     change the value of that pixel to 1 if there are pixOnes or   *
        *                     more in the filter have the value 1                           *
        ************************************************************************************/
        public void filterIrisRectPixels(Bitmap resBmp, int pixOnes, int verbose)
        {
            // Record the start time
            DateTime sto = DateTime.Now;

            // filter size
            int fs = 3;
            // full array filter size
            int fas = fs * fs;
            // image x, y
            int x, y;
            // source x, y
            int sx, sy;
            // filter x, y
            int fx, fy;
            // half filter size
            int hfs = fs / 2;
            int co;
            int totalChanged = 0;

            int height = iraH - hfs;
            int width = iraW - hfs;

            for (sy = hfs; sy < height; sy++)
                for (sx = hfs; sx < width; sx++)
                {
                    if (irisFlagAry[sy, sx] != 1)
                    {
                        co = 0;
                        for (fy = -hfs; fy <= hfs; fy++)
                        {
                            y = sy + fy;
                            for (fx = -hfs; fx <= hfs; fx++)
                            {
                                x = sx + fx;
                                if(irisFlagAry[y, x] == 1)
                                    ++co;
                            }
                        }

                        if (co > pixOnes)
                        {
                            irisFlagAry[sy, sx] = 1;
                            ++totalChanged;
                        }
                    }
                }

            // Display iris rectangular area
            if ((verbose & 1) == 1)
                displayIrisArea(resBmp);

            if ((verbose & 2) == 2)
            {
                // Record the stop time
                DateTime eto = DateTime.Now;
                // Calculate difference in time
                TimeSpan ts = eto.Subtract(sto);
                // Display results
                String msg = "> Time consumed:  " + ts.Minutes + ":" + ts.Seconds + ":" + ts.Milliseconds;
                Graphics g = Graphics.FromImage(resBmp);
                g.DrawString(msg, new Font(FontFamily.GenericSerif, 12, FontStyle.Bold), Brushes.Green, 0, 0);
                msg = "> Filtered pixels: " + totalChanged + " (" + (totalChanged * 100 / (iraW * iraH)) + "%)";
                g.DrawString(msg, new Font(FontFamily.GenericSerif, 12, FontStyle.Bold), Brushes.Green, 0, 20);
            }
        }

        private Color iFillColor = Color.DodgerBlue;
        int[] yAry;

        // Isolate black pixels immediatly which represents the upper lids
        public void isolateUpperLashes(Bitmap resBmp, PictureBox resPicture, int verbose)
        {
            // Record the start time
            DateTime sto = DateTime.Now;

            Graphics g = Graphics.FromImage(resBmp);
            // Draw a red filled iris circle
            g.FillEllipse(Brushes.Red, (pupilCenter.x - irisRadius), (pupilCenter.y - irisRadius), (irisRadius * 2), (irisRadius * 2));
            // Draw a black filled pupil circle
            g.FillEllipse(Brushes.Black, (pupilCenter.x - pupilRadius), (pupilCenter.y - pupilRadius), (pupilRadius * 2), (pupilRadius * 2));

            if ((verbose & 1) == 1)
            {
                resPicture.Refresh();
                MessageBox.Show("Finish isolating iris parts");
            }

            // Collect iris pixels
            int x, y;
            Color c;
            for (y = iray1; y <= iray2; y++)
                for (x = irax1; x <= irax2; x++)
                {
                    c = resBmp.GetPixel(x, y);

                    if (irisFlagAry[y - iray1, x - irax1] == 1)
                    {
                        if (c.R != 255)
                            irisFlagAry[y - iray1, x - irax1] = 0;
                    }
                    else if (c.R == 255)
                    {
                        resBmp.SetPixel(x, y, Color.Yellow);
                    }
                }

            if ((verbose & 1) == 1)
            {
                resPicture.Refresh();
                MessageBox.Show("Finish collecting irs pixels");
            }

            processIrisGaps(resBmp);
            if ((verbose & 1) == 1)
            {
                resPicture.Refresh();
                MessageBox.Show("Finish filling gaps");
            }

            if ((verbose & 1) == 1)
            {
                g.Clear(Color.LightSkyBlue);

                for (y = iray1; y <= iray2; y++)
                    for (x = irax1; x <= irax2; x++)
                    {
                        if (irisFlagAry[y - iray1, x - irax1] == 1)
                        {
                            int cl = resImg[y, x];
                            resBmp.SetPixel(x, y, Color.FromArgb(cl, cl, cl));
                        }
                    }
                resPicture.Refresh();
            }

            if ((verbose & 2) == 2)
            {
                // Record the stop time
                DateTime eto = DateTime.Now;
                // Calculate difference in time
                TimeSpan ts = eto.Subtract(sto);
                // Display results
                String msg = "> Time consumed:  " + ts.Minutes + ":" + ts.Seconds + ":" + ts.Milliseconds;
                g.DrawString(msg, new Font(FontFamily.GenericSerif, 12, FontStyle.Bold), Brushes.Green, 0, 0);
            }
        }

        // Locate upper lashes curve to be removed on further processing
        public int locateUpperLashesCurve(Bitmap resBmp, PictureBox resPicture, int verbose)
        {
            // Record the start time
            DateTime sto = DateTime.Now;

            int co;
            // yCo = upper edge of pupil - 10 to be safe (- iray1 to map it to irisFlagAry)
            int yCo = (pupilCenter.y - pupilRadius) - iray1 - 10;
            // x = pupil center (- irax1 to map it to irisFlagAry)
            int x = pupilCenter.x - irax1;

            // Reserve memory for stack with size = iris rectangular area 
            int totSize = iraW * iraH;

            stackPoints = null;
            stackPoints = new Point[totSize];
            for (co = 0; co < totSize; co++)
                stackPoints[co] = new Point();

//////////////// Get the lash segment ///////////////////////////////////////////////////////////////////

            int lashPointsCo = 0;
            Point[] lashSeg = new Point[totSize];
            for (co = 0; co < totSize; co++)
                lashSeg[co] = new Point();

            while (yCo > 0)
            {
                if (irisFlagAry[yCo, x] == 0)
                {
                    segPointsCo = 1;
                    getReg(resBmp, x, yCo);
                    if (segPointsCo > lashPointsCo)
                    {
                        for (co = 0; co < segPointsCo; co++)
                            lashSeg[co] = stackPoints[co];
                        lashPointsCo = segPointsCo;
                    }
                }
                --yCo;
            }

            // Exit if lash not found
            if (lashPointsCo == 0)
                return -1;

            // Debug for displaying the lash segment
            if ((verbose & 1) == 1)
            {
                for (co = 0; co < lashPointsCo; co++)
                    resBmp.SetPixel(irax1 + lashSeg[co].x, iray1 + lashSeg[co].y, Color.Wheat);
                resPicture.Refresh();
                MessageBox.Show("Finish finding the lash segment");
            }

//////////// Get the smallest Y's ///////////////////////////////////////////////////////////////////////

            yAry = new int[iraW];
            int[] yTmp = new int[iraW];

            for (co = 0; co < iraW; co++)
            {
                yAry[co] = 0;
                yTmp[co] = 0;
            }

            for (co = 0; co < lashPointsCo; co++)
            {
                if (yAry[lashSeg[co].x] < lashSeg[co].y)
                {
                    yAry[lashSeg[co].x] = lashSeg[co].y;
                    yTmp[lashSeg[co].x] = lashSeg[co].y;
                }
            }

            // Debug for Displaying the Lash smallest Y's
            if ((verbose & 1) == 1)
            {
                for (co = 0; co < iraW; co++)
                    if (yAry[co] != 0)
                        resBmp.SetPixel(irax1 + co, iray1 + yAry[co], Color.Red);
                resPicture.Refresh();
                MessageBox.Show("Finish finding the lash curve");
            }

//////////// Cut the zeros Y ////////////////////////////////////////////////////////////////////////////

            // start and end Y Lash
            int startYL = 0, endYL = 0;
            co = 0;
            // Record the start of the Non zero Y
            while (co < iraW && yAry[co] == 0)
                ++co;
            startYL = co;

            co = iraW - 1;
            while (co > 0 && yAry[co] == 0)
                --co;
            endYL = co;

///////////// Smoth Y's /////////////////////////////////////////////////////////////////////////////////

            int startCo, endCo;
            int inerCo;
            int smothY;
            int divY = 3;
            bool saveFlag = true;

            for (int mco = 1; mco < 20; mco++)
            {
                startCo = startYL + mco;
                endCo = endYL - mco;

                if (saveFlag)
                {
                    for (co = startCo; co <= endCo; co++)
                    {
                        smothY = 0;
                        for (inerCo = -mco; inerCo <= mco; inerCo++)
                        {
                            smothY += yAry[co + inerCo];
                        }
                        smothY /= divY;
                        yTmp[co] = smothY;
                    }
                }
                else
                {
                    for (co = startCo; co <= endCo; co++)
                    {
                        smothY = 0;
                        for (inerCo = -mco; inerCo <= mco; inerCo++)
                        {
                            smothY += yTmp[co + inerCo];
                        }
                        smothY /= divY;
                        yAry[co] = smothY;
                    }
                }

                saveFlag = !saveFlag;

                divY += 2;
            }

            if (!saveFlag)
            {
                for (co = 0; co < iraW; co++)
                {
                    yAry[co] = yTmp[co];
                }
            }

            // Debug for Displaying the Y's afetr smothing
            if ((verbose & 1) == 1)
            {
                for (co = 0; co < iraW; co++)
                    if (yAry[co] != 0)
                        resBmp.SetPixel(irax1 + co, iray1 + yAry[co], Color.DarkGreen);
                resPicture.Refresh();
                MessageBox.Show("Finish smoothing the lash curve");
            }

//////////// Find the equation of the lash curve/////////////////////////////////////////////////////////

            // Get the selected pixel samples
            int startLeftSam = iraW / 4;
            int startRightSam = 3 * iraW / 4;
            int sampleNu = 60;
            int halfSampNu = sampleNu / 2;
            Point[] samples = new Point[sampleNu];
            int sampleJmp = 1;

            // Find a and b for the left side of the lash curve
            long sx = 0, sy = 0, sxy = 0, sx2 = 0;
            for (co = 0; co < halfSampNu; co++)
            {
                samples[co] = new Point(startLeftSam + sampleJmp * co, yAry[startLeftSam + sampleJmp * co]);
                sx += (long)samples[co].x;
                sy += (long)samples[co].y;
                sxy += (long)samples[co].x * samples[co].y;
                sx2 += (long)samples[co].x * samples[co].x;
            }

            double la = (double)(halfSampNu * sxy - sx * sy) / (halfSampNu * sx2 - sx * sx);
            double lb = (double)(sx2 * sy - sx * sxy) / (halfSampNu * sx2 - sx * sx);

            // Find a and b for the right side of the lash curve
            sx = 0; sy = 0; sxy = 0; sx2 = 0;
            for (co = halfSampNu; co < sampleNu; co++)
            {
                samples[co] = new Point(startRightSam + sampleJmp * (co - halfSampNu), yAry[startRightSam + sampleJmp * (co - halfSampNu)]);
                sx += (long)samples[co].x;
                sy += (long)samples[co].y;
                sxy += (long)samples[co].x * samples[co].y;
                sx2 += (long)samples[co].x * samples[co].x;
            }

            double ra = (double)(halfSampNu * sxy - sx * sy) / (halfSampNu * sx2 - sx * sx);
            double rb = (double)(sx2 * sy - sx * sxy) / (halfSampNu * sx2 - sx * sx);

///////////// Find the missing pixels in the lash ///////////////////////////////////////////////////////

            // Find the missing curve of the left side of the lash curve
            int ldb = Math.Abs((int)(la * startYL + lb) - yAry[startYL]);
            for (co = 0; co < startYL; co++)
            {
                yAry[co] = Convert.ToInt16(la * co + lb + ldb);
            }

            // Find the missing curve of the right side of the lash curve
            int rdb = Math.Abs((int)(ra * endYL + rb) - yAry[endYL]);
            for (co = endYL + 1; co < iraW; co++)
            {
                yAry[co] = Convert.ToInt16(ra * co + rb + rdb);
            }

            // Debug for Displaying the fixed Lash Y's
            if ((verbose & 1) == 1)
            {
                for (co = 0; co < iraW; co++)
                    resBmp.SetPixel(irax1 + co, iray1 + yAry[co], Color.Red);
                resPicture.Refresh();
            }

            if ( (verbose & 2) == 2)
            {
                // Record the stop time
                DateTime eto = DateTime.Now;
                // Calculate difference in time
                TimeSpan ts = eto.Subtract(sto);
                // Display results
                String msg = "> Time consumed:  " + ts.Minutes + ":" + ts.Seconds + ":" + ts.Milliseconds;
                Graphics g = Graphics.FromImage(resBmp);
                g.DrawString(msg, new Font(FontFamily.GenericSerif, 12, FontStyle.Bold), Brushes.Green, 0, 0);
            }

            return 0;
        }
        
        // Enhanced one
        public int locateUpperLashesCurveEnh(Bitmap resBmp, PictureBox resPicture, int verbose)
        {
            // Record the start time
            DateTime sto = DateTime.Now;

            int co;
            // yCo = upper edge of pupil - 10 to be safe (- iray1 to map it to irisFlagAry)
            int yCo = (pupilCenter.y - pupilRadius) - iray1 - 10;
            // x = pupil center (- irax1 to map it to irisFlagAry)
            int x = pupilCenter.x - irax1;

            // Reserve memory for lashes buffer with size = iris rectangular area
            int totSize = iraW * iraH;

            //////////////// Get the lashes ///////////////////////////////////////////////////////////////////

            int lashPointsCo = 0;
            Point[] lashesBuf = new Point[totSize];

            int xCo;
            while (yCo > 0)
            {
                for (xCo = 0; xCo < iraW; xCo++ )
                    if (irisFlagAry[yCo, xCo] == 0)
                    {
                        lashesBuf[lashPointsCo] = new Point(xCo, yCo);
                        ++lashPointsCo;
                    }
                --yCo;
            }

            // Exit if lash not found
            if (lashPointsCo == 0)
                return -1;

            // Debug for displaying the lash segment
            if ((verbose & 1) == 1)
            {
                for (co = 0; co < lashPointsCo; co++)
                    resBmp.SetPixel(irax1 + lashesBuf[co].x, iray1 + lashesBuf[co].y, Color.Wheat);
                resPicture.Refresh();
                MessageBox.Show("Finish finding the lash segment");
            }

            //////////// Get the smallest Y's ///////////////////////////////////////////////////////////////////////

            yAry = new int[iraW];
            int[] yTmp = new int[iraW];

            for (co = 0; co < iraW; co++)
            {
                yAry[co] = 0;
                yTmp[co] = 0;
            }

            for (co = 0; co < lashPointsCo; co++)
            {
                if (yAry[lashesBuf[co].x] < lashesBuf[co].y)
                {
                    yAry[lashesBuf[co].x] = lashesBuf[co].y;
                    yTmp[lashesBuf[co].x] = lashesBuf[co].y;
                }
            }

            // Debug for Displaying the Lash smallest Y's
            if ((verbose & 1) == 1)
            {
                for (co = 0; co < iraW; co++)
                    if (yAry[co] != 0)
                        resBmp.SetPixel(irax1 + co, iray1 + yAry[co], Color.Red);
                resPicture.Refresh();
                MessageBox.Show("Finish finding the lash curve");
            }

            //////////// Cut the zeros Y ////////////////////////////////////////////////////////////////////////////

            // start and end Y Lash
            int startYL = 0, endYL = 0;
            co = 0;
            // Record the start of the Non zero Y
            while (co < iraW && yAry[co] == 0)
                ++co;
            startYL = co;

            co = iraW - 1;
            while (co > 0 && yAry[co] == 0)
                --co;
            endYL = co;

            ///////////// Smoth Y's /////////////////////////////////////////////////////////////////////////////////

            int startCo, endCo;
            int inerCo;
            int smothY;
            int divY = 3;
            bool saveFlag = true;

            for (int mco = 1; mco < 10; mco++)
            {
                startCo = startYL + mco;
                endCo = endYL - mco;

                if (saveFlag)
                {
                    for (co = startCo; co <= endCo; co++)
                    {
                        smothY = 0;
                        for (inerCo = -mco; inerCo <= mco; inerCo++)
                        {
                            smothY += yAry[co + inerCo];
                        }
                        smothY /= divY;
                        yTmp[co] = smothY;
                    }
                }
                else
                {
                    for (co = startCo; co <= endCo; co++)
                    {
                        smothY = 0;
                        for (inerCo = -mco; inerCo <= mco; inerCo++)
                        {
                            smothY += yTmp[co + inerCo];
                        }
                        smothY /= divY;
                        yAry[co] = smothY;
                    }
                }

                saveFlag = !saveFlag;

                divY += 2;
            }

            if (!saveFlag)
            {
                for (co = 0; co < iraW; co++)
                {
                    yAry[co] = yTmp[co];
                }
            }

            // Debug for Displaying the Y's afetr smothing
            if ((verbose & 1) == 1)
            {
                for (co = 0; co < iraW; co++)
                    if (yAry[co] != 0)
                        resBmp.SetPixel(irax1 + co, iray1 + yAry[co], Color.DarkGreen);
                resPicture.Refresh();
                MessageBox.Show("Finish smoothing the lash curve");
            }

            //////////// Find the equation of the lash curve/////////////////////////////////////////////////////////

            // Get the selected pixel samples
            int startLeftSam = iraW / 4;
            int startRightSam = 3 * iraW / 4;
            int sampleNu = 60;
            int halfSampNu = sampleNu / 2;
            Point[] samples = new Point[sampleNu];
            int sampleJmp = 1;

            // Find a and b for the left side of the lash curve
            long sx = 0, sy = 0, sxy = 0, sx2 = 0;
            for (co = 0; co < halfSampNu; co++)
            {
                samples[co] = new Point(startLeftSam + sampleJmp * co, yAry[startLeftSam + sampleJmp * co]);
                sx += (long)samples[co].x;
                sy += (long)samples[co].y;
                sxy += (long)samples[co].x * samples[co].y;
                sx2 += (long)samples[co].x * samples[co].x;
            }

            double la = (double)(halfSampNu * sxy - sx * sy) / (halfSampNu * sx2 - sx * sx);
            double lb = (double)(sx2 * sy - sx * sxy) / (halfSampNu * sx2 - sx * sx);

            // Find a and b for the right side of the lash curve
            sx = 0; sy = 0; sxy = 0; sx2 = 0;
            for (co = halfSampNu; co < sampleNu; co++)
            {
                samples[co] = new Point(startRightSam + sampleJmp * (co - halfSampNu), yAry[startRightSam + sampleJmp * (co - halfSampNu)]);
                sx += (long)samples[co].x;
                sy += (long)samples[co].y;
                sxy += (long)samples[co].x * samples[co].y;
                sx2 += (long)samples[co].x * samples[co].x;
            }

            double ra = (double)(halfSampNu * sxy - sx * sy) / (halfSampNu * sx2 - sx * sx);
            double rb = (double)(sx2 * sy - sx * sxy) / (halfSampNu * sx2 - sx * sx);

            ///////////// Find the missing pixels in the lash ///////////////////////////////////////////////////////

            // Find the missing curve of the left side of the lash curve
            int ldb = Math.Abs((int)(la * startYL + lb) - yAry[startYL]);
            for (co = 0; co < startYL; co++)
            {
                yAry[co] = Convert.ToInt16(la * co + lb + ldb);
            }

            // Find the missing curve of the right side of the lash curve
            int rdb = Math.Abs((int)(ra * endYL + rb) - yAry[endYL]);
            for (co = endYL + 1; co < iraW; co++)
            {
                yAry[co] = Convert.ToInt16(ra * co + rb + rdb);
            }

            // Debug for Displaying the fixed Lash Y's
            if ((verbose & 1) == 1)
            {
                for (co = 0; co < iraW; co++)
                    resBmp.SetPixel(irax1 + co, iray1 + yAry[co], Color.Red);
                resPicture.Refresh();
            }

            if ((verbose & 2) == 2)
            {
                // Record the stop time
                DateTime eto = DateTime.Now;
                // Calculate difference in time
                TimeSpan ts = eto.Subtract(sto);
                // Display results
                String msg = "> Time consumed:  " + ts.Minutes + ":" + ts.Seconds + ":" + ts.Milliseconds;
                Graphics g = Graphics.FromImage(resBmp);
                g.DrawString(msg, new Font(FontFamily.GenericSerif, 12, FontStyle.Bold), Brushes.Green, 0, 0);
            }

            return 0;
        }

        // Find iris pixels
        public void locateIrisPixels(Bitmap resBitmap, PictureBox resPicture, int verbose)
        {
            // Record the start time
            DateTime sto = DateTime.Now;

            Graphics g = Graphics.FromImage(resBitmap);
            // Draw a red filled iris circle
            g.FillEllipse(Brushes.Red, (pupilCenter.x - irisRadius), (pupilCenter.y - irisRadius), (irisRadius * 2), (irisRadius * 2));
            // Draw a black filled pupil circle
            g.FillEllipse(Brushes.Black, (pupilCenter.x - pupilRadius), (pupilCenter.y - pupilRadius), (pupilRadius * 2), (pupilRadius * 2));
            // Draw a black filled area above the lashes
            for (int co = irax1; co < irax2; co++)
            {
                g.DrawLine(Pens.Black, co, iray1, co, iray1 + yAry[co - irax1]);
            }

            if ((verbose & 1) == 1)
            {
                resPicture.Refresh();
                MessageBox.Show("Finish isolating iris parts");
            }

            // Collect iris pixels
            int x, y;
            Color c;
            for (y = iray1; y <= iray2; y++)
                for (x = irax1; x <= irax2; x++)
                {
                    c = resBitmap.GetPixel(x, y);

                    if (irisFlagAry[y - iray1, x - irax1] == 1)
                    {
                        if (c.R != 255)
                            irisFlagAry[y - iray1, x - irax1] = 0;
                    }
                    else if (c.R == 255)
                    {
                        resBitmap.SetPixel(x, y, Color.Yellow);
                    }
                }

            if ((verbose & 1) == 1)
            {
                resPicture.Refresh();
                MessageBox.Show("Finish collecting irs pixels");
            }

            processIrisGaps(resBitmap);

            if ((verbose & 1) == 1)
            {
                resPicture.Refresh();
                MessageBox.Show("Finish processing gaps");
            }

            if ((verbose & 1) == 1)
            {
                g.Clear(Color.LightSkyBlue);

                for (y = iray1; y <= iray2; y++)
                    for (x = irax1; x <= irax2; x++)
                    {
                        if (irisFlagAry[y - iray1, x - irax1] == 1)
                        {
                            int cl = resImg[y, x];
                            resBitmap.SetPixel(x, y, Color.FromArgb(cl, cl, cl));
                        }
                    }
                resPicture.Refresh();
            }

            if ((verbose & 2) == 2)
            {
                // Record the stop time
                DateTime eto = DateTime.Now;
                // Calculate difference in time
                TimeSpan ts = eto.Subtract(sto);
                // Display results
                String msg = "> Time consumed:  " + ts.Minutes + ":" + ts.Seconds + ":" + ts.Milliseconds;
                g.DrawString(msg, new Font(FontFamily.GenericSerif, 12, FontStyle.Bold), Brushes.Green, 0, 0);
            }
        }

        // Search for gaps in the iris area that must be part of the iris pixels
        public void processIrisGaps(Bitmap resBitmap)
        {
            int x, y;
            Color c;
            bool inFlag;

            // Search Horizontal for Gaps
            int hw = irax1 + iraW / 2;
            for (y = iray1; y <= iray2; y++)
            {
                // Search Left for gaps
                inFlag = false;
                x = irax1;
                while (x <= hw)
                {
                    c = resBitmap.GetPixel(x, y);
                    if (c.R == 255)
                    {
                        if (c.G != 255)
                        {
                            inFlag = true;
                        }
                        else if (inFlag == true)
                        {
                            resBitmap.SetPixel(x, y, Color.DarkBlue);
                            irisFlagAry[y - iray1, x - irax1] = 1;
                        }
                    }
                    ++x;
                }

                // Search Right for gaps
                inFlag = false;
                x = irax2;
                while (x > hw)
                {
                    c = resBitmap.GetPixel(x, y);
                    if (c.R == 255)
                    {
                        if (c.G != 255)
                        {
                            inFlag = true;
                        }
                        else if (inFlag == true)
                        {
                            resBitmap.SetPixel(x, y, Color.DarkBlue);
                            irisFlagAry[y - iray1, x - irax1] = 1;
                        }
                    }
                    --x;
                }
            }

            // Search Vertical for Gaps
            int hh = iray1 + iraH / 2;
            for (x = irax1; x <= irax2; x++)
            {
                // Search up for gaps
                inFlag = false;
                y = iray1;
                while (y <= hh)
                {
                    c = resBitmap.GetPixel(x, y);
                    if (c.R == 255)
                    {
                        if (c.G != 255)
                        {
                            inFlag = true;
                        }
                        else if (inFlag == true)
                        {
                            resBitmap.SetPixel(x, y, Color.DarkBlue);
                            irisFlagAry[y - iray1, x - irax1] = 1;
                        }
                    }
                    ++y;
                }

                // Search Down for gaps
                inFlag = false;
                y = iray2;
                while (y > hh)
                {
                    c = resBitmap.GetPixel(x, y);
                    if (c.R == 255)
                    {
                        if (c.G != 255)
                        {
                            inFlag = true;
                        }
                        else if (inFlag == true)
                        {
                            resBitmap.SetPixel(x, y, Color.DarkBlue);
                            irisFlagAry[y - iray1, x - irax1] = 1;
                        }
                    }
                    --y;
                }
            }
        }
        
        // Display iris area
        public void displayIrisArea(Bitmap resBitmap)
        {
            for (int y = iray1; y <= iray2; y++)
                for (int x = irax1; x <= irax2; x++)
                {
                    if (irisFlagAry[y - iray1, x - irax1] == 0)
                        resBitmap.SetPixel(x, y, Color.Black);
                    else if (irisFlagAry[y - iray1, x - irax1] == 1)
                        resBitmap.SetPixel(x, y, Color.DodgerBlue);
                    else if (irisFlagAry[y - iray1, x - irax1] == 2)
                        resBitmap.SetPixel(x, y, Color.White);
                }
        }

        // Iris array
        private int[,] irisArray;

        // Create the iris array
        public void createIrisArray()
        {
            int x, y;

            irisArray = new int[iraH, iraW];

            for(y = iray1; y <= iray2; y++)
                for (x = irax1; x <= irax2; x++)
                {
                    if(irisFlagAry[y - iray1, x - irax1] == 1)
                        irisArray[y - iray1, x - irax1] = srcImg[y, x];
                    else
                        irisArray[y - iray1, x - irax1] = -1;
                }
        }

        // Display the iris array
        public void displayIrisArray(Bitmap resBitmap)
        {
            int x, y;
            Color c;
            int pix;

            for(y = 0; y < iraH; y++)
                for (x = 0; x < iraW; x++)
                {
                    pix = irisArray[y, x];
                    if (pix != -1)
                    {
                        c = Color.FromArgb(pix, pix, pix);
                        resBitmap.SetPixel(x + irax1, y + iray1, c);
                    }
                }
        }

        // get a region using Non recursive flood fill algorithm
        private void getReg(Bitmap resBitmap, int x, int y)
        {
            int curPtr = 0, endPtr = 0;
            int cx, cy, chgX, chgY;
            

            stackPoints[0].x = x;
            stackPoints[0].y = y;

            irisFlagAry[y, x] =  111;

            int endX = iraW;
            int endY = iraH;
            //int endY = (pupilCenter.y - pupilRadius) - y1;

            while (curPtr <= endPtr)
            {
                cx = stackPoints[curPtr].x;
                cy = stackPoints[curPtr].y;

                chgX = cx + 1;

                if (chgX < endX)
                    if (irisFlagAry[cy, chgX] == 0)
                    {
                        ++segPointsCo;
                        irisFlagAry[cy, chgX] = 10;
                        ++endPtr;
                        stackPoints[endPtr].x = chgX;
                        stackPoints[endPtr].y = cy;
                    }

                chgX = cx - 1;

                if (chgX >= 0)
                    if (irisFlagAry[cy, chgX] == 0)
                    {
                        ++segPointsCo;
                        irisFlagAry[cy, chgX] = 10;
                        ++endPtr;
                        stackPoints[endPtr].x = chgX;
                        stackPoints[endPtr].y = cy;
                    }

                chgY = cy + 1;

                if (chgY < endY)
                    if (irisFlagAry[chgY, cx] == 0)
                    {
                        ++segPointsCo;
                        irisFlagAry[chgY, cx] = 10;
                        ++endPtr;
                        stackPoints[endPtr].x = cx;
                        stackPoints[endPtr].y = chgY;
                    }

                chgY = cy - 1;

                if (chgY >= 0)
                    if (irisFlagAry[chgY, cx] == 0)
                    {
                        ++segPointsCo;
                        irisFlagAry[chgY, cx] = 10;
                        ++endPtr;
                        stackPoints[endPtr].x = cx;
                        stackPoints[endPtr].y = chgY;
                    }

                ++curPtr;
            }// End while

        }

        // Contrast Streach iris image
        public void contrastStreachIris(int verbose)
        {
            // Record the start time
            DateTime sto = DateTime.Now;

            int x, y;
            double mean = 0, std = 0, tmp, alpha;
            double min, max;
            int imgSize = 0;

            // find the Mean
            for (y = 0; y < iraH; y++)
                for (x = 0; x < iraW; x++)
                {
                    if (irisArray[y, x] != -1)
                    {
                        ++imgSize;
                        mean += irisArray[y, x];
                    }
                }
            mean /= imgSize;

            // find the Standard Deviation
            for (y = 0; y < iraH; y++)
                for (x = 0; x < iraW; x++)
                {
                    if (irisArray[y, x] != -1)
                    {
                        tmp = irisArray[y, x] - mean;
                        std += tmp * tmp;
                    }
                }
            std /= imgSize;
            std = Math.Sqrt(std);

            // find Min and Max
            alpha = 2.0;
            tmp = alpha * std;
            min = mean - tmp;
            max = mean + tmp;

            if (min < 0)
                min = 0;
            if (max > 255)
                max = 255;

            // find new image
            tmp = max - min;
            for (y = 0; y < iraH; y++)
                for (x = 0; x < iraW; x++)
                {
                    if (irisArray[y, x] != -1)
                    {
                        if (irisArray[y, x] < min)
                            irisArray[y, x] = 0;
                        else if (irisArray[y, x] > max)
                            irisArray[y, x] = 255;
                        else
                            irisArray[y, x] = (int)((irisArray[y, x] - min) * 255 / tmp);
                    }

                }

            if ((verbose & 1) == 1)
            {
                // Record the stop time
                DateTime eto = DateTime.Now;
                // Calculate difference in time
                TimeSpan ts = eto.Subtract(sto);
                // Display results
                String msg = "> Time consumed:  " + ts.Minutes + ":" + ts.Seconds + ":" + ts.Milliseconds;
                MessageBox.Show(msg);
            }
        }

        public void scaleIris()
        {
            int x, y;

            for (y = 0; y < iraH; y++)
                for (x = 0; x < iraW; x++)
                {
                    if (irisFlagAry[y - iray1, x - irax1] == 1)
                        irisArray[y - iray1, x - irax1] = srcImg[y, x];
                    else
                        irisArray[y - iray1, x - irax1] = -1;
                }
        }


/************************************************************************************************
*                                                                                               *
*                               Fractal and Quantization Functions                              *
*                                                                                               *
************************************************************************************************/


        // Fractal Dimension Index Array
        private int[,] fDI;

        // Maximum box size
        private const int MAXBS = 8;
        private const int REAL_PIX_NO = (2 * MAXBS + 1) * (2 * MAXBS + 1);
        // Used to convert Fd of a pixel from double to integer
        private const int BINZ = 100;
        private const int FRACTAL_LEVELS = 100;

        // Compute the Fractal Dimention Index for each pixel - v1: without any customization
        public void computeFDI_v1(int verbose)
        {
            // Record the start time
            DateTime sto = DateTime.Now;

            int x, y;
            int boxSize, bxCo, byCo;    // box size and counters
            int xp1, xp2, yp1, yp2;     // x and y position of pixels in the box
            int inPixCo;                // Number of pixels included in a box
            int totPixCo;               // total number of pixels included in the last box

            // Store 2log(2L+1)
            double[] xTable = new double[MAXBS];
            // Store log(N(L))
            double[] yTable = new double[MAXBS];
            // Store up and down term of the equation of the slope (Fd of the pixel)
            double uTerm, dTerm;

            // create fractal dimension array
            fDI = new int[iraH, iraW];

            // First value (log(0)) are always 0
            yTable[0] = 0;
            xTable[0] = 0;

            // Compute the x table and down term which are fixed for all box sizes
            dTerm = 0;
            for (x = 1; x < MAXBS; x++)
            {
                xTable[x] = 2 * Math.Log10(2 * x + 1);
                dTerm += xTable[x] * xTable[x];
            }

            // Find fractal dimension for each pixel
            for (y = 0; y < iraH; y++)
            {
                for (x = 0; x < iraW; x++)
                {
                    // if this pixel is not part of the iris, continue to the next pixel
                    if (irisArray[y, x] == -1)
                    {
                        fDI[y, x] = -1;
                        continue;
                    }
                    // for each pixel calculate MAXBS box values
                    totPixCo = 0;

                    for (boxSize = 1; boxSize < MAXBS; boxSize++)
                    {
                        inPixCo = 0;

                        xp1 = x - boxSize;
                        xp2 = x + boxSize;
                        yp1 = y - boxSize;
                        yp2 = y + boxSize;

                        for (byCo = yp1; byCo <= yp2; byCo++)
                        {
                            for (bxCo = xp1; bxCo <= xp2; bxCo++)
                            {
                                if (byCo >= 0 && byCo < iraH && bxCo >= 0 && bxCo < iraW)
                                {
                                    if (irisArray[byCo, bxCo] != -1)
                                    {
                                        ++totPixCo;
                                        // check if this pixel belong or not
                                        if (Math.Abs(irisArray[y, x] - irisArray[byCo, bxCo]) <= boxSize)
                                            ++inPixCo;
                                    }
                                }
                            }
                        }// end of bCo


                        // add number of included pixels to the y table for this boxsize
                        yTable[boxSize] = Math.Log10(inPixCo);
                    }// end of boxSize

                    // check if the number of included pixels in the box is < 50% of 
                    // the total pixels found in the boxes
                    /*if (inPixCo < (totPixCo / 2))
                    {
                        fDI[y, x] = -1;
                        continue;
                    }*/

                    // compute the fractal dimension index for this iris pixel
                    uTerm = 0;
                    for (boxSize = 1; boxSize < MAXBS; boxSize++)
                    {
                        uTerm += xTable[boxSize] * yTable[boxSize];
                    }
                    fDI[y, x] = (int)Math.Round((3.0 - (uTerm / dTerm)) * BINZ);

                }// end of x
            }// end of y

            if ((verbose & 1) == 1)
            {
                // Record the stop time
                DateTime eto = DateTime.Now;
                // Calculate difference in time
                TimeSpan ts = eto.Subtract(sto);
                // Display results
                String msg = "> Time consumed:  " + ts.Minutes + ":" + ts.Seconds + ":" + ts.Milliseconds;
                MessageBox.Show(msg);
            }
        }

        // Compute the Fractal Dimention Index for each pixel - v2: customization in loops
        public void computeFDI_v2(int verbose)
        {
            // Record the start time
            DateTime sto = DateTime.Now;

            int x, y;
            int boxSize, bxCo, byCo;    // box size and counters
            int xp1, xp2, yp1, yp2;     // x and y position of pixels in the box
            int inPixCo;                // Number of pixels included in a box
            int totPixCo;               // total number of pixels included in the last box

            // Store 2log(2L+1)
            double[] xTable = new double[MAXBS];
            // Store log(N(L))
            double[] yTable = new double[MAXBS];
            // Store up and down term of the equation of the slope (Fd of the pixel)
            double uTerm, dTerm;

            // create fractal dimension array
            fDI = new int[iraH, iraW];

            // First value (log(0)) are always 0
            yTable[0] = 0;
            xTable[0] = 0;

            // Compute the x table and down term which are fixed for all box sizes
            dTerm = 0;
            for (x = 1; x < MAXBS; x++)
            {
                xTable[x] = 2 * Math.Log10(2 * x + 1);
                dTerm += xTable[x] * xTable[x];
            }

            // Find fractal dimension for each pixel
            for (y = 0; y < iraH; y++)
            {
                for (x = 0; x < iraW; x++)
                {
                    // if this pixel is not part of the iris, continue to the next pixel
                    if (irisArray[y, x] == -1)
                    {
                        fDI[y, x] = -1;
                        continue;
                    }
                    // for each pixel calculate MAXBS box values
                    totPixCo = 0;

                    for (boxSize = 1; boxSize < MAXBS; boxSize++)
                    {
                        inPixCo = 0;

                        xp1 = x - boxSize;
                        xp2 = x + boxSize;
                        yp1 = y - boxSize;
                        yp2 = y + boxSize;

                        for (byCo = yp1; byCo <= yp2; byCo++)
                        {
                            if (byCo < 0 || byCo >= iraH)       // Customization-1
                                continue;

                            for (bxCo = xp1; bxCo <= xp2; bxCo++)
                            {
                                if (bxCo >= 0 && bxCo < iraW)   // Customization-2
                                {
                                    if (irisArray[byCo, bxCo] != -1)
                                    {
                                        ++totPixCo;
                                        // check if this pixel belong or not
                                        if (Math.Abs(irisArray[y, x] - irisArray[byCo, bxCo]) <= boxSize)
                                            ++inPixCo;
                                    }
                                }
                            }
                        }// end of bCo


                        // add number of included pixels to the y table for this boxsize
                        yTable[boxSize] = Math.Log10(inPixCo);
                    }// end of boxSize

                    // check if the number of included pixels in the box is < 50% of 
                    // the total pixels found in the boxes
                    /*if (inPixCo < (totPixCo / 2))
                    {
                        fDI[y, x] = -1;
                        continue;
                    }*/

                    // compute the fractal dimension index for this iris pixel
                    uTerm = 0;
                    for (boxSize = 1; boxSize < MAXBS; boxSize++)
                    {
                        uTerm += xTable[boxSize] * yTable[boxSize];
                    }
                    //fDI[y, x] = (int)Math.Round((3.0 - (uTerm / dTerm)) * BINZ);
                    fDI[y, x] = (int)Math.Round((uTerm / dTerm) * BINZ);

                }// end of x
            }// end of y

            if ((verbose & 1) == 1)
            {
                // Record the stop time
                DateTime eto = DateTime.Now;
                // Calculate difference in time
                TimeSpan ts = eto.Subtract(sto);
                // Display results
                String msg = "> Time consumed:  " + ts.Minutes + ":" + ts.Seconds + ":" + ts.Milliseconds;
                MessageBox.Show(msg);
            }
        }


        // Quantized Array
        private int[,] quantGray;

        // Maximum quantization levels
        private const int QUANTIZ_LEVELS = 16;

        // Compute the Quantized Gray for each pixel
        int irisImageSize = 0;
        public void computeQuantizedGray(int verbose)
        {
            // Record the start time
            DateTime sto = DateTime.Now;

            int x, y;
            double mean = 0, std = 0, tmp, alpha;
            double min, max;
            
            // Reserve memory for quantized gray array
            quantGray = new int[iraH, iraW];

            // find the Mean
            for (y = 0; y < iraH; y++)
                for (x = 0; x < iraW; x++)
                {
                    if (irisArray[y, x] != -1)
                    {
                        ++irisImageSize;
                        mean += irisArray[y, x];
                    }
                }
            mean /= irisImageSize;

            // find the Standard Deviation
            for (y = 0; y < iraH; y++)
                for (x = 0; x < iraW; x++)
                {
                    if (irisArray[y, x] != -1)
                    {
                        tmp = irisArray[y, x] - mean;
                        std += tmp * tmp;
                    }
                }
            std /= irisImageSize;
            std = Math.Sqrt(std);

            // find Min and Max
            alpha = 2.0;
            tmp = alpha * std;
            min = mean - tmp;
            max = mean + tmp;

            if (min < 0)
                min = 0;
            if (max > 255)
                max = 255;

            // find new image
            tmp = max - min;
            for (y = 0; y < iraH; y++)
                for (x = 0; x < iraW; x++)
                {
                    if (irisArray[y, x] != -1)
                    {
                        if (irisArray[y, x] < min)
                            quantGray[y, x] = 0;
                        else if (irisArray[y, x] > max)
                            quantGray[y, x] = QUANTIZ_LEVELS;
                        else
                            quantGray[y, x] = (int)((irisArray[y, x] - min) * QUANTIZ_LEVELS / tmp);
                    }
                    else
                    {
                        quantGray[y, x] = -1;
                    }

                }

            if ((verbose & 1) == 1)
            {
                // Record the stop time
                DateTime eto = DateTime.Now;
                // Calculate difference in time
                TimeSpan ts = eto.Subtract(sto);
                // Display results
                String msg = "> Time consumed:  " + ts.Minutes + ":" + ts.Seconds + ":" + ts.Milliseconds;
                MessageBox.Show(msg);
            }
        }

        // Fractal Gray Histogram Array
        private int[,] fqHistoAry;

        // Create Fractal Quantized Histogram
        public void computeFGHistogram(int verbose)
        {
            // Record the start time
            DateTime sto = DateTime.Now;

            int y, x;
            fqHistoAry = new int[FRACTAL_LEVELS + 1, QUANTIZ_LEVELS + 1];


            for (y = 0; y < iraH; y++ )
            {
                for (x = 0; x < iraW; x++)
                {
                    if (irisArray[y, x] != -1)
                    {
                        ++fqHistoAry[fDI[y, x], quantGray[y, x]];
                    }
                }
            }

            if ((verbose & 1) == 1)
            {
                // Record the stop time
                DateTime eto = DateTime.Now;
                // Calculate difference in time
                TimeSpan ts = eto.Subtract(sto);
                // Display results
                String msg = "> Time consumed:  " + ts.Minutes + ":" + ts.Seconds + ":" + ts.Milliseconds;
                MessageBox.Show(msg);
            }
        }


/************************************************************************************************
*                                                                                               *
*                                 Run Length Featres Functions                                  *
*                                                                                               *
************************************************************************************************/


        public void computeRunLengthFeatures(int verbose)
        {
            // Record the start time
            DateTime sto = DateTime.Now;

            // Compute GrayLevel Run-Number Vector
            int[] Pg = new int[FRACTAL_LEVELS];
            int f, q;

            for (f = 0; f < FRACTAL_LEVELS; f++)
            {
                Pg[f] = 0;
                for (q = 0; q < QUANTIZ_LEVELS; q++)
                {
                    Pg[f] += fqHistoAry[f, q];
                }
            }

            // Compute Run-Length Run-Number Vector
            int[] Pr = new int[QUANTIZ_LEVELS];
            for (q = 0; q < QUANTIZ_LEVELS; q++)
            {
                Pr[q] = 0;
                for (f = 0; f < FRACTAL_LEVELS; f++)
                {
                    Pr[q] += fqHistoAry[f, q];
                }
            }

            // Compute total number of runs
            int Nr = 0;
            for (f = 0; f < FRACTAL_LEVELS; f++)
                for (f = 0; f < FRACTAL_LEVELS; f++)
                    if (fqHistoAry[f, q] > 0)
                        ++Nr;


            // Initialize features related variables
            double qs = 0;
            double fs = 0;

            // Compute SRE, LRE, and RLN features
            double sreFeature = 0;
            double lreFeature = 0;
            double rlnFeature = 0;
            
            for (q = 0; q < QUANTIZ_LEVELS; q++)
            {
                qs = q * q;
                sreFeature += Pr[q] / qs;
                lreFeature += Pr[q] * qs;
                rlnFeature += Pr[q] * Pr[q];
            }
            sreFeature /= Nr;
            lreFeature /= Nr;
            rlnFeature /= Nr;


            // Compute GLN,
            // LGRE, HGRE,
            // SRLGE, SRHGE, LRLGE, LRHGE features
            double glnFeature = 0;
            double lgreFeature = 0;
            double hgreFeature = 0;
            double srlgeFeature = 0;
            double srhgeFeature = 0;
            double lrlgeFeature = 0;
            double lrhgeFeature = 0;

            fs = 0;
            for (f = 0; f < FRACTAL_LEVELS; f++)
            {
                fs = f * f;
                glnFeature += Pg[f] * Pg[f];
                lgreFeature += Pg[f] / fs;
                hgreFeature += Pg[f] * fs;

                for (q = 0; q < QUANTIZ_LEVELS; q++)
                {
                    qs = q * q;
                    srlgeFeature += fqHistoAry[f, q] / (fs * qs);
                    srhgeFeature += fqHistoAry[f, q] * fs / qs;
                    lrlgeFeature += fqHistoAry[f, q] * qs / fs;
                    lrhgeFeature += fqHistoAry[f, q] * fs * qs;
                }
            }
            glnFeature /= Nr;
            lgreFeature /= Nr;
            hgreFeature /= Nr;
            srlgeFeature /= Nr;
            srhgeFeature /= Nr;
            lrlgeFeature /= Nr;
            lrhgeFeature /= Nr;

            // Compute RP feature
            double rpFeature = 0;
            double Np = irisImageSize;
            rpFeature = Nr / Np;

            if ((verbose & 1) == 1)
            {
                // Record the stop time
                DateTime eto = DateTime.Now;
                // Calculate difference in time
                TimeSpan ts = eto.Subtract(sto);
                // Display results
                String msg = "> Time consumed:  " + ts.Minutes + ":" + ts.Seconds + ":" + ts.Milliseconds;
                MessageBox.Show(msg);
            }
        }

    }// End of class
}// End of namespace
