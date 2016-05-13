﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;


namespace TugasAkhir1
{
    public class DWT
    {
        //Haar Discrete Wavelet Transform
        //Matrix transform = 
        /// [ 1/sqrt(2)  1/sqrt(2) ] 
        /// [ 1/sqrt(2) -1/sqrt(2) ]
        //1.4142135623730950488016887242097 = Math.Sqrt(2)
        //0.70710678118654752440084436210485 = 1/Math.Sqrt(2)
        //For High Pass Filter
        private const double w0 =  0.70710678118654752440084436210485;//1/1.4142135623730950488016887242097;//  0.5; 
        private const double w1 = -0.70710678118654752440084436210485;//-1/1.4142135623730950488016887242097;// -0.5;

        //For Low Pass Filter
        private const double s0 =  0.70710678118654752440084436210485;//1/1.4142135623730950488016887242097;//  0.5;
        private const double s1 =  0.70710678118654752440084436210485;//1/1.4142135623730950488016887242097;//  0.5;

        public static void Forward1D(double[] data)
        {
            double[] temp = new double[data.Length];
            int h = data.Length >> 1;
            for (int i = 0; i < h; i++)
            {
                int k = (i << 1);
                temp[i] = data[k] * s0 + data[k + 1] * s1;
                temp[i + h] = data[k] * w0 + data[k + 1] * w1;
            }

            for (int i = 0; i < data.Length; i++)
                data[i] = temp[i];
        }

        public static void Forward2D(double[,] data, int level)
        {
            int rows = data.GetLength(0);
            int cols = data.GetLength(1);

            double[] row;
            double[] col;

            for (int k = 0; k < level; k++)
            {
                int lev = 1 << k;
                int levCols = cols / lev;
                int levRows = rows / lev;
                row = new double[levCols];

                for (int i = 0; i < levRows; i++)
                {
                    for (int j = 0; j < row.Length; j++)
                        row[j] = data[i, j];

                    Forward1D(row);

                    for (int j = 0; j < row.Length; j++)
                        data[i, j] = row[j];
                }

                col = new double[levRows];

                for (int j = 0; j < levCols; j++)
                {
                    for (int i = 0; i < col.Length; i++)
                        col[i] = data[i, j];

                    Forward1D(col);

                    for (int i = 0; i < col.Length; i++)
                        data[i, j] = col[i];
                }
            }
        }

        public static void Inverse1D(double[] data)
        {
            double[] temp = new double[data.Length];

            int h = data.Length >> 1;
            for (int i = 0; i < h; i++)
            {
                int k = (i << 1);
                temp[k] = (data[i] * s0 + data[i + h] * w0);// w0; //Changed After Matrix Transformation Changed to 1/sqrt(2)
                temp[k + 1] = (data[i] * s1 + data[i + h] * w1);// s0; //Changed After Matrix Transformation Changed to 1/sqrt(2)
            }

            for (int i = 0; i < data.Length; i++)
                data[i] = temp[i];
        }

        #region buggy Inverse2d
        //public void Inverse2D(double[,] data, int level)
        //{
        //    int rows = data.GetLength(0);
        //    int cols = data.GetLength(1);

        //    double[] col = new double[rows];
        //    double[] row = new double[cols];

        //    for (int l = 0; l < level; l++)
        //    {
        //        for (int j = 0; j < cols; j++)
        //        {
        //            for (int i = 0; i < row.Length; i++)
        //                col[i] = data[i, j];

        //            Inverse1D(col);

        //            for (int i = 0; i < col.Length; i++)
        //                data[i, j] = col[i];
        //        }

        //        for (int i = 0; i < rows; i++)
        //        {
        //            for (int j = 0; j < row.Length; j++)
        //                row[j] = data[i, j];

        //            Inverse1D(row);

        //            for (int j = 0; j < row.Length; j++)
        //                data[i, j] = row[j];
        //        }
        //    }
        //}
        #endregion

        public static void Inverse2D(double[,] data, int iterations)
        {
            int rows = data.GetLength(0);
            int cols = data.GetLength(1);

            double[] col;
            double[] row;

            for (int k = iterations - 1; k >= 0; k--)
            {
                int lev = 1 << k;

                int levCols = cols / lev;
                int levRows = rows / lev;

                col = new double[levRows];
                for (int j = 0; j < levCols; j++)
                {
                    for (int i = 0; i < col.Length; i++)
                        col[i] = data[i, j];

                    Inverse1D(col);

                    for (int i = 0; i < col.Length; i++)
                        data[i, j] = col[i];
                }

                row = new double[levCols];
                for (int i = 0; i < levRows; i++)
                {
                    for (int j = 0; j < row.Length; j++)
                        row[j] = data[i, j];

                    Inverse1D(row);

                    for (int j = 0; j < row.Length; j++)
                        data[i, j] = row[j];
                }
            }
        }

        public static double Scale(double fromMin, double fromMax, double toMin, double toMax, double x)
        {
            if (fromMax - fromMin == 0) return 0;
            double value = (toMax - toMin) * (x - fromMin) / (fromMax - fromMin) + toMin;
            if (value > toMax)
            {
                value = toMax;
            }
            if (value < toMin)
            {
                value = toMin;
            }
            return value;
        }

        public static Bitmap TransformDWT(bool Forward, bool Safe, int Levels, Bitmap OriginalImage/*, Bitmap TransformedImage*/)
        {

            //Bitmap bmp = Forward ? OriginalImage : TransformedImage;
            Bitmap bmp = OriginalImage;
            Bitmap bmp2;
            //int Iterations = 0;
            //int.TryParse(Levels, out Iterations);

            int maxScale = (int)(Math.Log(bmp.Width < bmp.Height ? bmp.Width : bmp.Height) / Math.Log(2));
            if (Levels < 1 || Levels > maxScale)
            {
                MessageBox.Show("Iteration must be Integer from 1 to " + maxScale);

                //return;
            }

            int time = Environment.TickCount;

            double[,] Red = new double[bmp.Width, bmp.Height];
            double[,] Green = new double[bmp.Width, bmp.Height];
            double[,] Blue = new double[bmp.Width, bmp.Height];

            int PixelSize = 3;
            BitmapData bmData = null;

            if (Safe)
            {
                Color c;

                for (int j = 0; j < bmp.Height; j++)
                {
                    for (int i = 0; i < bmp.Width; i++)
                    {
                        c = bmp.GetPixel(i, j);
                        Red[i, j] = (double)Scale(0, 255, -1, 1, c.R);
                        Green[i, j] = (double)Scale(0, 255, -1, 1, c.G);
                        Blue[i, j] = (double)Scale(0, 255, -1, 1, c.B);
                    }
                }
            }
            else
            {
                bmData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                unsafe
                {
                    for (int j = 0; j < bmData.Height; j++)
                    {
                        byte* row = (byte*)bmData.Scan0 + (j * bmData.Stride);
                        for (int i = 0; i < bmData.Width; i++)
                        {
                            Red[i, j] = (double)Scale(0, 255, -1, 1, row[i * PixelSize + 2]);
                            Green[i, j] = (double)Scale(0, 255, -1, 1, row[i * PixelSize + 1]);
                            Blue[i, j] = (double)Scale(0, 255, -1, 1, row[i * PixelSize]);
                        }
                    }
                }
            }

            if (Forward)
            {
                Forward2D(Red, Levels);
                Forward2D(Green, Levels);
                Forward2D(Blue, Levels);
            }
            else
            {
                Inverse2D(Red, Levels);
                Inverse2D(Green, Levels);
                Inverse2D(Blue, Levels);
            }

            if (Safe)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    for (int i = 0; i < bmp.Width; i++)
                    {
                        bmp.SetPixel(i, j, Color.FromArgb((int)Scale(-1, 1, 0, 255, Red[i, j]), (int)Scale(-1, 1, 0, 255, Green[i, j]), (int)Scale(-1, 1, 0, 255, Blue[i, j])));
                    }
                }
            }
            else
            {
                unsafe
                {
                    for (int j = 0; j < bmData.Height; j++)
                    {
                        byte* row = (byte*)bmData.Scan0 + (j * bmData.Stride);
                        for (int i = 0; i < bmData.Width; i++)
                        {
                            row[i * PixelSize + 2] = (byte)Scale(-1, 1, 0, 255, Red[i, j]);
                            row[i * PixelSize + 1] = (byte)Scale(-1, 1, 0, 255, Green[i, j]);
                            row[i * PixelSize] = (byte)Scale(-1, 1, 0, 255, Blue[i, j]);
                        }
                    }
                }

                bmp.UnlockBits(bmData);
            }

            //if (Forward)
            //{
            //    TransformedImage = new Bitmap(bmp);
            //}

            bmp2 = bmp;
            return bmp2;
        }


        public static double[,] WaveletCoeff(double[,] pixels, bool forward, int level)
        {
            double[,] p = pixels;
            if (forward == true)
            {
                Forward2D(p, level); //Forward Haar DWT
            }
            else
            {
                Inverse2D(p, level); //Inverse Haar DWT
            }

            return p;
        }

        public static double[,] GetWaveletCoeff(Bitmap bmp, int level)
        {
            double[,] p = new double[bmp.Height, bmp.Width];
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    p[y, x] = c.R;
                }
            }
            double[,] coeffs = WaveletCoeff(p, true, level);
            return coeffs;
        }

        public static double[,] InverseWaveletCoeff(Bitmap bmp, int level)
        {
            double[,] p = new double[bmp.Height, bmp.Width];
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    p[y, x] = c.R;
                }
            }
            double[,] coeffs = WaveletCoeff(p, true, level);
            double[,] InvertedCoeffs = WaveletCoeff(coeffs, false, level);
            return InvertedCoeffs;
        }



        #region Subbands of Decomposed Image
        //Subbands of Decomposed Image
        public static double[,] GetSubband(double[,] coeffs, int level, string subband)
        {
            if (level == 1 && subband == "LL1")
            {
                double[,] LL1 = new double[coeffs.GetLength(0) / 4, coeffs.GetLength(1) / 4];
                for (int i = 0; i < LL1.GetLength(0); i++)
                {
                    for (int j = 0; j < LL1.GetLength(1); j++)
                    {
                        LL1[i, j] = coeffs[i, j];
                    }
                }
                return LL1;
            }
            else if (level == 1 && subband == "LH1")
            {
                double[,] LH1 = new double[coeffs.GetLength(0) / 4, coeffs.GetLength(1) / 4];
                for (int i = 0; i < LH1.GetLength(0); i++)
                {
                    for (int j = 0; j < LH1.GetLength(1); j++)
                    {
                        LH1[i, j] = coeffs[i, j + (coeffs.GetLength(1) / 4)];
                    }
                }
                return LH1;
            }
            else if (level == 1 && subband == "HL1")
            {
                double[,] HL1 = new double[coeffs.GetLength(0) / 4, coeffs.GetLength(1) / 4];
                for (int i = 0; i < HL1.GetLength(0); i++)
                {
                    for (int j = 0; j < HL1.GetLength(1); j++)
                    {
                        HL1[i, j] = coeffs[i + (coeffs.GetLength(0) / 4), j];
                    }
                }
                return HL1;
            }
            else if (level == 1 && subband == "HH1")
            {
                double[,] HH1 = new double[coeffs.GetLength(0) / 4, coeffs.GetLength(1) / 4];
                for (int i = 0; i < HH1.GetLength(0); i++)
                {
                    for (int j = 0; j < HH1.GetLength(1); j++)
                    {
                        HH1[i, j] = coeffs[i + (coeffs.GetLength(0) / 4), j + (coeffs.GetLength(1) / 4)];
                    }
                }
                return HH1;
            }
            else if (level == 2 && subband == "LH2")
            {
                double[,] LH2 = new double[coeffs.GetLength(0) / 2, coeffs.GetLength(1) / 2];
                for (int i = 0; i < LH2.GetLength(0); i++)
                {
                    for (int j = 0; j < LH2.GetLength(1); j++)
                    {
                        LH2[i, j] = coeffs[i, j + (coeffs.GetLength(1) / 2)];
                    }
                }
                return LH2;
            }
            else if (level == 2 && subband == "HL2")
            {
                double[,] HL2 = new double[coeffs.GetLength(0) / 2, coeffs.GetLength(1) / 2];
                for (int i = 0; i < HL2.GetLength(0); i++)
                {
                    for (int j = 0; j < HL2.GetLength(1); j++)
                    {
                        HL2[i, j] = coeffs[i + (coeffs.GetLength(0) / 2), j];
                    }
                }
                return HL2;
            }
            else //if(level == 2 && subband == "HH2")
            {
                double[,] HH2 = new double[coeffs.GetLength(0) / 2, coeffs.GetLength(1) / 2];
                for (int i = 0; i < HH2.GetLength(0); i++)
                {
                    for (int j = 0; j < HH2.GetLength(1); j++)
                    {
                        HH2[i, j] = coeffs[i + (coeffs.GetLength(0) / 2), j + (coeffs.GetLength(1) / 2)];
                    }
                }
                return HH2;
            }
        }
        #endregion

        public static double VarianceOfSubband(double[,] subband)
        {
            List<double> s = new List<double>();
            
            for (int i = 0; i < subband.GetLength(0); i++)
            {
                for (int j = 0; j < subband.GetLength(1); j++)
                {
                    s.Add(subband[i, j]);
                }
            }

            double var = Statistic.Variance(s);
            return var;
        }

    }
}
