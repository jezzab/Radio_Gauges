using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Media;
using GHI.Utilities;
using GHI.Processor;

namespace NETMFBook1
{
    class SmoothLine
    {
        /// <summary>
        /// Fast anti-alias line drawing method
        /// https://github.com/sachinruk/xiaolinwu/blob/master/xiaolinwu.m
        /// http://en.wikipedia.org/wiki/Xiaolin_Wu's_line_algorithm
        /// 
        /// </summary>
        /// 
        //Gamma correction lookup  gamma set to 2.2
        static byte[] gamma22n = new byte[] { 0, 21, 28, 34, 39, 43, 46, 50, 53, 56, 59, 61, 64, 66, 68, 70, 72, 74, 76, 78, 80, 82, 84, 85, 87, 89, 90, 92, 93, 95, 96, 98, 99, 101, 102, 103, 105, 106, 107, 109, 110, 111, 112, 114, 115, 116, 117, 118, 119, 120, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 144, 145, 146, 147, 148, 149, 150, 151, 151, 152, 153, 154, 155, 156, 156, 157, 158, 159, 160, 160, 161, 162, 163, 164, 164, 165, 166, 167, 167, 168, 169, 170, 170, 171, 172, 173, 173, 174, 175, 175, 176, 177, 178, 178, 179, 180, 180, 181, 182, 182, 183, 184, 184, 185, 186, 186, 187, 188, 188, 189, 190, 190, 191, 192, 192, 193, 194, 194, 195, 195, 196, 197, 197, 198, 199, 199, 200, 200, 201, 202, 202, 203, 203, 204, 205, 205, 206, 206, 207, 207, 208, 209, 209, 210, 210, 211, 212, 212, 213, 213, 214, 214, 215, 215, 216, 217, 217, 218, 218, 219, 219, 220, 220, 221, 221, 222, 223, 223, 224, 224, 225, 225, 226, 226, 227, 227, 228, 228, 229, 229, 230, 230, 231, 231, 232, 232, 233, 233, 234, 234, 235, 235, 236, 236, 237, 237, 238, 238, 239, 239, 240, 240, 241, 241, 242, 242, 243, 243, 244, 244, 245, 245, 246, 246, 247, 247, 248, 248, 249, 249, 249, 250, 250, 251, 251, 252, 252, 253, 253, 254, 254, 255, 255 };
        //Inverse of gamma correction loopup table
        static byte[] gamma22 = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 6, 6, 6, 6, 7, 7, 7, 8, 8, 8, 9, 9, 9, 10, 10, 11, 11, 11, 12, 12, 13, 13, 13, 14, 14, 15, 15, 16, 16, 17, 17, 18, 18, 19, 19, 20, 20, 21, 22, 22, 23, 23, 24, 25, 25, 26, 26, 27, 28, 28, 29, 30, 30, 31, 32, 33, 33, 34, 35, 35, 36, 37, 38, 39, 39, 40, 41, 42, 43, 43, 44, 45, 46, 47, 48, 49, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 73, 74, 75, 76, 77, 78, 79, 81, 82, 83, 84, 85, 87, 88, 89, 90, 91, 93, 94, 95, 97, 98, 99, 100, 102, 103, 105, 106, 107, 109, 110, 111, 113, 114, 116, 117, 119, 120, 121, 123, 124, 126, 127, 129, 130, 132, 133, 135, 137, 138, 140, 141, 143, 145, 146, 148, 149, 151, 153, 154, 156, 158, 159, 161, 163, 165, 166, 168, 170, 172, 173, 175, 177, 179, 181, 182, 184, 186, 188, 190, 192, 194, 196, 197, 199, 201, 203, 205, 207, 209, 211, 213, 215, 217, 219, 221, 223, 225, 227, 229, 231, 234, 236, 238, 240, 242, 244, 246, 248, 251, 253, 255 };

        static void swap(ref int a, ref int b)
        {
            int tmp = a;
            a = b;
            b = tmp;
        }

        static void swap(ref float a, ref float b)
        {
            float tmp = a;
            a = b;
            b = tmp;
        }

        /// <summary>
        /// Antialiased line drawn in managed runtime
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="bmp"></param>
        /// <param name="Col"></param>
        /// <param name="alpha"></param>
        static public void drawLine(float x0, float y0, float x1, float y1, ref Bitmap bmp, Color Col, float alpha)
        {
            if (x0 == x1 & y0 == y1) return;    //not a line
            int intesityBits = 8; //log base 2 of NumLevels: the # of bits used to describe the intensity of the drawing color. 2**IntensityBits--NumLevels

            int FPbits = 15;
            int FPscale = (1 << FPbits) - 1; //2^16
            int FPscaleNOT = (~FPscale) & 0x7FFFFFFF; //2^16
            int FPscaleHalf = (1 << FPbits) - 1;

            int numLevels = 255;
            int srcColint;

            //Convert to fixed point representation
            int x0fp = (int)(x0 * FPscale);
            int x1fp = (int)(x1 * FPscale);


            int y0fp = (int)(y0 * FPscale);
            int y1fp = (int)(y1 * FPscale);

            bool steep = System.Math.Abs(y1fp - y0fp) > System.Math.Abs(x1fp - x0fp);
            //bool steep = System.Math.Abs(y1 - y0) > System.Math.Abs(x1 - x0);
            int alpha256FP = (int)(alpha * 256);

            if (steep)
            {
                swap(ref x0fp, ref y0fp);
                swap(ref x1fp, ref y1fp);
            }

            if (x0fp > x1fp)
            {
                swap(ref x0fp, ref x1fp);
                swap(ref y0fp, ref y1fp);
            }

            int dxfp = x1fp - x0fp;
            int dyfp = y1fp - y0fp;

            int grad = (int)((double)FPscale * ((double)dyfp / (double)dxfp)); //do it in floating point for accuracy//Need to look up a faster fixed point method

            int yAcc;//= y0fp + grad;

            int e;
            int re;
            int WeightingComplementMask = numLevels - 1;
            int intesityShift = FPbits - intesityBits;
            int yp;

            int Alpha;
            int AlphaNeg;
            int ag;
            int rb;
            int rgb;

            int agCol = ((int)Col & 0x0000FF00) >> 8;
            int rbCol = ((int)Col & 0x00FF00FF);

            //
            //DRAW START PIXEL  ----  AS A PAIR OF POINTS IN THE Y PLANE
            int xEnd = (x0fp) & FPscaleNOT;     //Round(x0fp)   Truncate works better. Error on Wiki and Rosetta code 
            //int xEnd = (x0fp + FPscaleHalf) & FPscaleNOT;     //Round(x0fp)   using add half and chop THIS ROUND IS NOT WORKING CORRECTLY
            int xm = ((xEnd - x0fp) * grad) >> FPbits;

            int yEnd = y0fp + xm;
            int xGap = (x0fp + FPscaleHalf) & FPscale; //round up and get the fractional part  
            xGap = (xGap >> intesityShift); // to 1 to 256  
            xGap = 256 - xGap; //Get remainder
            int ypxl1 = yEnd >> FPbits;
            int xpxl1 = xEnd >> FPbits;

            e = (yEnd & FPscale) >> intesityShift; //float part     as 0 to 255           
            re = e ^ WeightingComplementMask; //XOR to get the 1-e value
            e = (e * xGap) >> 8;
            re = (re * xGap) >> 8;
            e = gamma22n[e]; re = gamma22n[re];

            if (steep) { srcColint = (int)bmp.GetPixel(ypxl1, xpxl1); } else { srcColint = (int)bmp.GetPixel(xpxl1, ypxl1); };
            Alpha = alpha256FP * re >> 8;       //Fast 8bit mul approx =  (a255 +1) * b255 >>8 //or a256 * b255 >>8
            AlphaNeg = 256 - Alpha;
            ag = (srcColint & 0x0000FF00) >> 8; rb = srcColint & 0x00FF00FF;  //Set up colour so we can do a combined mult  //With this we can multiply in parralell. http://stereopsis.com/doubleblend.html
            ag = (Alpha * agCol) + (AlphaNeg * ag); rb = (Alpha * rbCol) + (AlphaNeg * rb); //Apply alpha blend
            ag = ag & 0x0000FF00; rb = rb >> 8 & 0x00FF00FF; rgb = ag | rb; //Merge colours back together00FF00FF;
            if (steep) { bmp.SetPixel(ypxl1, xpxl1, (Color)rgb); } else { bmp.SetPixel(xpxl1, ypxl1, (Color)rgb); };

            Alpha = alpha256FP * e >> 8;
            AlphaNeg = 256 - Alpha;   //Calculate amount of  source colour and background colour to blend
            if (steep) { srcColint = (int)bmp.GetPixel(ypxl1 + 1, xpxl1); } else { srcColint = (int)bmp.GetPixel(xpxl1, ypxl1 + 1); }   //Get underlying pixel colour for alpha blend
            ag = (srcColint & 0x0000FF00) >> 8; rb = srcColint & 0x00FF00FF;  //Set up colour so we can do a combined mult  //With this we can multiply in parralell. http://stereopsis.com/doubleblend.html
            ag = (Alpha * agCol) + (AlphaNeg * ag); rb = (Alpha * rbCol) + (AlphaNeg * rb); //Apply alpha blend
            ag = ag & 0x0000FF00; rb = rb >> 8 & 0x00FF00FF; rgb = ag | rb; //Merge colours back together
            if (steep) { bmp.SetPixel(ypxl1 + 1, xpxl1, (Color)rgb); } else { bmp.SetPixel(xpxl1, ypxl1 + 1, (Color)rgb); }

            yAcc = yEnd + grad;

            //DRAW END PIXEL  ----  AS A PAIR OF POINTS IN THE Y PLANE
            xEnd = (x1fp + FPscaleHalf) & FPscaleNOT;     //Round(x0fp)   using add half and chop
            ///
            xm = ((xEnd - x1fp) * grad) >> FPbits;
            yEnd = y1fp + xm;                            ///

            xGap = (x1fp + FPscaleHalf) & FPscale; //round up and get the fractional part

            xGap = (xGap >> intesityShift); // to 1 to 256
            int ypxl2 = yEnd >> FPbits;
            int xpxl2 = xEnd >> FPbits;

            e = (yEnd & FPscale) >> intesityShift; //float part     as 0 to 255
            re = e ^ WeightingComplementMask; //XOR to get the 1-e value
            e = (e * xGap) >> 8;
            re = (re * xGap) >> 8;
            e = gamma22n[e]; re = gamma22n[re];

            if (steep) { srcColint = (int)bmp.GetPixel(ypxl2, xpxl2); } else { srcColint = (int)bmp.GetPixel(xpxl2, ypxl2); }
            Alpha = alpha256FP * re >> 8;       //Fast 8bit mul approx =  (a255 +1) * b255 >>8 //or a256 * b255 >>8
            AlphaNeg = 256 - Alpha;
            ag = (srcColint & 0x0000FF00) >> 8; rb = srcColint & 0x00FF00FF;  //Set up colour so we can do a combined mult  //With this we can multiply in parralell. http://stereopsis.com/doubleblend.html
            ag = (Alpha * agCol) + (AlphaNeg * ag); rb = (Alpha * rbCol) + (AlphaNeg * rb); //Apply alpha blend
            ag = ag & 0x0000FF00; rb = rb >> 8 & 0x00FF00FF; rgb = ag | rb; //Merge colours back together00FF00FF;
            if (steep) { bmp.SetPixel(ypxl2, xpxl2, (Color)rgb); } else { bmp.SetPixel(xpxl2, ypxl2, (Color)rgb); }

            if (steep) { srcColint = (int)bmp.GetPixel(ypxl2 + 1, xpxl2); } else { srcColint = (int)bmp.GetPixel(xpxl2, ypxl2 + 1); }  //Get underlying pixel colour for alpha blend
            Alpha = alpha256FP * e >> 8;
            AlphaNeg = 256 - Alpha;   //Calculate amount of  source colour and background colour to blend
            ag = (srcColint & 0x0000FF00) >> 8; rb = srcColint & 0x00FF00FF;  //Set up colour so we can do a combined mult  //With this we can multiply in parralell. http://stereopsis.com/doubleblend.html
            ag = (Alpha * agCol) + (AlphaNeg * ag); rb = (Alpha * rbCol) + (AlphaNeg * rb); //Apply alpha blend
            ag = ag & 0x0000FF00; rb = rb >> 8 & 0x00FF00FF; rgb = ag | rb; //Merge colours back together
            if (steep) { bmp.SetPixel(ypxl2 + 1, xpxl2, (Color)rgb); } else { bmp.SetPixel(xpxl2, ypxl2 + 1, (Color)rgb); }

            //lOOP THROUGH DRAWING PAIRS OF POINTS IN THE X plane
            if (steep)
            {
                for (int x = xpxl1 + 1; x < xpxl2; x++)
                {
                    e = (yAcc & FPscale) >> intesityShift; //float part
                    re = e ^ WeightingComplementMask; //XOR to get the 1-e value
                    e = gamma22n[e]; re = gamma22n[re];
                    yp = yAcc >> FPbits;

                    srcColint = (int)bmp.GetPixel(yp, x);
                    Alpha = alpha256FP * re >> 8;
                    AlphaNeg = 256 - Alpha;      //Fast 8bit mul approx =  (a255 +1) * b255 >>8 //or a256 * b255 >>8

                    ag = (srcColint & 0x0000FF00) >> 8; rb = srcColint & 0x00FF00FF;       //With this we can multiply in parralell. http://stereopsis.com/doubleblend.html
                    ag = (Alpha * agCol) + (AlphaNeg * ag); rb = (Alpha * rbCol) + (AlphaNeg * rb);
                    ag = ag & 0x0000FF00; rb = rb >> 8 & 0x00FF00FF; rgb = ag | rb;

                    bmp.SetPixel(yp, x, (Color)rgb);

                    srcColint = (int)bmp.GetPixel(yp + 1, x);

                    Alpha = alpha256FP * e >> 8; AlphaNeg = 256 - Alpha;

                    ag = (srcColint & 0x0000FF00) >> 8; rb = srcColint & 0x00FF00FF;
                    ag = (Alpha * agCol) + (AlphaNeg * ag); rb = (Alpha * rbCol) + (AlphaNeg * rb);
                    ag = ag & 0x0000FF00; rb = rb >> 8 & 0x00FF00FF; rgb = ag | rb;

                    bmp.SetPixel(yp + 1, x, (Color)rgb);
                    yAcc += grad;
                }
            }
            else
            {
                for (int x = xpxl1 + 1; x < xpxl2; x++)
                {
                    e = (yAcc & FPscale) >> intesityShift; //float part
                    re = e ^ WeightingComplementMask; //XOR to get the 1-e value
                    e = gamma22n[e]; re = gamma22n[re];
                    yp = yAcc >> FPbits;

                    srcColint = (int)bmp.GetPixel(x, yp);
                    Alpha = alpha256FP * re >> 8;
                    AlphaNeg = 256 - Alpha;      //Fast 8bit mul approx =  (a255 +1) * b255 >>8 //or a256 * b255 >>8

                    ag = (srcColint & 0x0000FF00) >> 8; rb = srcColint & 0x00FF00FF;       //With this we can multiply in parralell. http://stereopsis.com/doubleblend.html
                    ag = (Alpha * agCol) + (AlphaNeg * ag); rb = (Alpha * rbCol) + (AlphaNeg * rb);
                    ag = ag & 0x0000FF00; rb = rb >> 8 & 0x00FF00FF; rgb = ag | rb;

                    bmp.SetPixel(x, yp, (Color)rgb);

                    srcColint = (int)bmp.GetPixel(x, yp + 1);

                    Alpha = alpha256FP * e >> 8; AlphaNeg = 256 - Alpha;

                    ag = (srcColint & 0x0000FF00) >> 8; rb = srcColint & 0x00FF00FF;
                    ag = (Alpha * agCol) + (AlphaNeg * ag); rb = (Alpha * rbCol) + (AlphaNeg * rb);
                    ag = ag & 0x0000FF00; rb = rb >> 8 & 0x00FF00FF; rgb = ag | rb;

                    bmp.SetPixel(x, yp + 1, (Color)rgb);
                    yAcc += grad;
                }
            }

        }






        static RuntimeLoadableProcedures.NativeFunction draw_line_antialiasFixAlpha1;
        /// <summary>
        /// Draw a antialiased line. No alpha blending will be performed. Only really works on black background.
        /// </summary>
        /// <param name="fx0"></param>
        /// <param name="fy0"></param>
        /// <param name="fx1"></param>
        /// <param name="fy1"></param>
        /// <param name="bmp"></param>
        /// <param name="Col"></param>
        static public void drawLineRLPFix(float fx0, float fy0, float fx1, float fy1, ref Bitmap bmp, Color Col)
        {
            uint width = (uint)bmp.Width;
            uint height = (uint)bmp.Height;

            int rval = draw_line_antialiasFixAlpha1.Invoke(width, height, bmp, fx0, fy0, fx1, fy1, (uint)Col);
        }

        static RuntimeLoadableProcedures.NativeFunction draw_line_antialiasFix;
        /// <summary>
        /// Draw Antialised line using RLP. Need to call init RLP first
        /// </summary>
        /// <param name="fx0"></param>
        /// <param name="fy0"></param>
        /// <param name="fx1"></param>
        /// <param name="fy1"></param>
        /// <param name="bmp"></param>
        /// <param name="Col"></param>
        /// <param name="alpha"></param>
        static public void drawLineRLPFix(float fx0, float fy0, float fx1, float fy1, ref Bitmap bmp, Color Col, float alpha)
        {
            uint width = (uint)bmp.Width;
            uint height = (uint)bmp.Height;

            int rval = draw_line_antialiasFix.Invoke(width, height, bmp, fx0, fy0, fx1, fy1, (uint)Col, alpha);//, dd, ddLen, ff, ffLen);


        }

        static RuntimeLoadableProcedures.NativeFunction NoArgumentsProcedure;
        /// <summary>
        /// Load the Runtime Loadable Procedures
        /// </summary>
        static public void initRLP()
        {
            byte[] elfBuffer =  Resources.GetBytes(Resources.BinaryResources.smoothLineG120_43);
            var elfImage = new RuntimeLoadableProcedures.ElfImage(elfBuffer);

            NoArgumentsProcedure = elfImage.FindFunction("NoArgumentsProcedure");
            draw_line_antialiasFix = elfImage.FindFunction("draw_line_antialiasFix");
            draw_line_antialiasFixAlpha1 = elfImage.FindFunction("draw_line_antialiasFixAlpha1");
            int result = NoArgumentsProcedure.Invoke();
            Debug.Print("This should be 13 if RLP initialised = " + result.ToString());

        }







    }
}
