using System;
using System.Collections;
using GHI.Glide;
using GHI.Glide.UI;
using GHI.Glide.Display;
//using GHI.Glide.Geom;
using Microsoft.SPOT;



namespace NETMFBook1
{
    class Gauges
    {
        sealed public class ProgressBar : DisplayObjectContainer
        {
            String _Units = "";
            int _XLoc = 0, _YLoc = 0, _BarHeight = 0, _BarWidth = 0;
            int _CurrentValue = 0;
            int _MaxValue = 100;
            Microsoft.SPOT.Presentation.Media.Color _StartColor = Colors.Blue;
            Microsoft.SPOT.Presentation.Media.Color _EndColor = Colors.Red;
            Microsoft.SPOT.Presentation.Media.Color _EmptyColor = Colors.White;

            Microsoft.SPOT.Presentation.Media.Color _FontColor = Colors.White;

            private int PointsIncrease = 0;

            private Canvas _MainBar = new Canvas();
            private TextBlock _TextBlock = new TextBlock("text", 255, 0, 0, 10, 10);

            //
            // Summary:
            //     Maximum value
            public int MaxValue
            {
                get
                {
                    return _MaxValue;
                }
                set
                {
                    _MaxValue = value;
                    //    this.Invalidate();
                }
            }
            //
            // Summary:
            //     Value
            public int Value
            {
                get
                {
                    return _CurrentValue;
                }
                set
                {
                    _CurrentValue = value;
                    this.Invalidate();
                }
            }


            //Starting color
            public Microsoft.SPOT.Presentation.Media.Color StartColor
            {
                get
                {
                    return _StartColor;
                }
                set
                {
                    _StartColor = value;
                    this.Invalidate();
                }
            }

            //Ending color
            public Microsoft.SPOT.Presentation.Media.Color EndColor
            {
                get
                {
                    return _EndColor;
                }
                set
                {
                    _EndColor = value;
                    this.Invalidate();
                }
            }

            //Emptying color
            public Microsoft.SPOT.Presentation.Media.Color EmptyColor
            {
                get
                {
                    return _EmptyColor;
                }
                set
                {
                    _EmptyColor = value;
                    this.Invalidate();
                }
            }

            //Fonting color
            public Microsoft.SPOT.Presentation.Media.Color FontColor
            {
                get
                {
                    return _FontColor;
                }
                set
                {
                    _FontColor = value;
                    this.Invalidate();
                }
            }



            public ProgressBar(DisplayObjectContainer window, string GaugeName, string label, ushort alpha, int x, int y, short width, short height)
            {
                Name = GaugeName;
                Parent = window;
              
                X = 0;
                Y = 0;
                Width = window.Width;
                Height = window.Height;

                _BarHeight = height;
                _BarWidth = width;

                _XLoc = x;
                _YLoc = y;

                _Units = label;



                _MainBar = new Canvas { X = 0, Y = 0, Width = window.Width, Height = window.Height, Name = "BarGauge" };

                _MainBar.Parent = this;
                AddChild(_MainBar);

                _TextBlock.Parent = this;
                _TextBlock = new TextBlock("Text", 255, _XLoc, _YLoc - 15, _BarWidth, 15);
                _TextBlock.BackColor = Colors.Black;
                _TextBlock.ShowBackColor = true;
                _TextBlock.Font = FontManager.GetFont(FontManager.FontType.droid_reg10);
                _TextBlock.FontColor = _FontColor;
                _TextBlock.Text = _CurrentValue.ToString("d4") + _Units;
                AddChild(_TextBlock);

            }

            private void DrawGauge()
            {
                _MainBar.Clear();
                if (_CurrentValue == 0)
                {
                    PointsIncrease = 0;
                    _MainBar.DrawRectangle(Colors.White, 0, _XLoc + PointsIncrease, _YLoc, _BarWidth - PointsIncrease, _BarHeight, 0, 0, Colors.White, 0, 0, Colors.White, _BarWidth, _BarHeight, 65535);
                }
                else
                {
                    PointsIncrease = (int)((float)((float)(_BarWidth) / MaxValue) * Value);
                    _MainBar.DrawRectangle(Colors.White, 0, _XLoc, _YLoc, PointsIncrease, _BarHeight, 0, 0, _StartColor, _XLoc, _YLoc, _EndColor, _XLoc + _BarWidth, _YLoc + _BarHeight, 65535);
                    _MainBar.DrawRectangle(Colors.White, 0, _XLoc + PointsIncrease, _YLoc, _BarWidth - PointsIncrease, _BarHeight, 0, 0, Colors.White, 0, 0, Colors.White, _BarWidth, _BarHeight, 65535);
                }
            }

            private void UpdateText()
            {
                _TextBlock.Text = _CurrentValue.ToString("d4") + _Units;

            }



            public override void Render()
            {
                DrawGauge();
                UpdateText();
                base.Render();
            }

        }





        sealed public class SlantedGauge : DisplayObjectContainer
        {
            String _Units = "";
            int _XLoc = 0, _YLoc = 0;
            int _CurrentValue = 0;
            int _MaxValue = 100;
            int length = 0,endx = 0, endy = 0;
            int startpointX = 0,startpointY = 0;
            float point = 0;

            Microsoft.SPOT.Presentation.Media.Color _StartColor = Colors.Green;
            Microsoft.SPOT.Presentation.Media.Color _EndColor = Colors.Red;
            Microsoft.SPOT.Presentation.Media.Color _EmptyColor = Colors.Black;

            Microsoft.SPOT.Presentation.Media.Color _UnitsFontColor = Colors.White;
            Microsoft.SPOT.Presentation.Media.Color _DigitsFontColor = Colors.White;
                   
           private GHI.Glide.UI.Image _GaugeImage = new Image("Gauge",255,0,0,10,10);
            Bitmap _GaugeBitmap = new Bitmap(10, 10);
            Bitmap _GaugeMaskBitmap = new Bitmap(10, 10);



            int TextWidth = 0, TextHeight = 0;
            private  Microsoft.SPOT.Font FontUnits; //FontManager.GetFont(FontManager.FontType.droid_reg10);
            private Microsoft.SPOT.Font FontDigits; //FontManager.GetFont(FontManager.FontType.droid_reg08);

            //
            // Summary:
            //     Maximum value
            public int MaxValue
            {
                get
                {
                    return _MaxValue;
                }
                set
                {
                    _MaxValue = value;
                    //    this.Invalidate();
                }
            }
            //
            // Summary:
            //     Value
            public int Value
            {
                get
                {
                    return _CurrentValue;
                }
                set
                {
                    if (value == _CurrentValue)
                    {
                        return;
                    }
                    if (value > _MaxValue)
                    {
                        _CurrentValue = _MaxValue;
                    }
                    else
                    {
                        _CurrentValue = value;                
                    }
                    this.Invalidate();
                }
            }


            //Starting color
            public Microsoft.SPOT.Presentation.Media.Color StartColor
            {
                get
                {
                    return _StartColor;
                }
                set
                {
                    _StartColor = value;
                    this.Invalidate();
                }
            }

            //Ending color
            public Microsoft.SPOT.Presentation.Media.Color EndColor
            {
                get
                {
                    return _EndColor;
                }
                set
                {
                    _EndColor = value;
                    this.Invalidate();
                }
            }

            //Emptying color
            public Microsoft.SPOT.Presentation.Media.Color EmptyColor
            {
                get
                {
                    return _EmptyColor;
                }
                set
                {
                    _EmptyColor = value;
                    this.Invalidate();
                }
            }

            //Fonting color
            public Microsoft.SPOT.Presentation.Media.Color UnitsFontColor
            {
                get
                {
                    return _UnitsFontColor;
                }
                set
                {
                    _UnitsFontColor = value;
                    this.Invalidate();
                }
            }

            //Fonting color
            public Microsoft.SPOT.Presentation.Media.Color DigitsFontColor
            {
                get
                {
                    return _DigitsFontColor;
                }
                set
                {
                    _DigitsFontColor = value;
                    this.Invalidate();
                }
            }


            public SlantedGauge(DisplayObjectContainer window, byte[] GaugeMask,Font SmallFont, Font BigFont, string GaugeName, string Units, ushort alpha, int x, int y)
            {
                
                Name = GaugeName;
                Parent = window;
              
                X = 0;
                Y = 0;
                Width = window.Width;
                Height = window.Height;

               _XLoc = x;
                _YLoc = y;

                _Units = Units;

                _GaugeMaskBitmap = new Bitmap(GaugeMask, Bitmap.BitmapImageType.Gif); //the mask thatll be applied
                _GaugeBitmap = new Bitmap(_GaugeMaskBitmap.Width, _GaugeMaskBitmap.Height);// GaugeGIF.Bitmap;

                FontUnits = SmallFont;
                FontDigits = BigFont;

                _GaugeImage = new Image("Gauge", 255, _XLoc, _YLoc, _GaugeBitmap.Width, _GaugeBitmap.Height);// GaugeGIF;
                _GaugeImage.Bitmap = _GaugeBitmap;// GaugeGIF.Bitmap;
                _GaugeImage.Parent = this;
                            
                AddChild(_GaugeImage);


            }

            private void DrawGauge()
            {
             //   _GaugeImage.Bitmap.Clear();
             //   _GaugeImage.Bitmap.DrawImage(0, 0, _GaugeMaskBitmap, 0, 0, _GaugeMaskBitmap.Width, _GaugeMaskBitmap.Height); //_GaugeBitmap; //draw fresh gauge
            }

            private void UpdateText()
            {
                //Bar1.Bitmap.DrawText("RPM", smallfont, Colors.White, 0, 0);
                //Bar1.Bitmap.DrawText("" + RPM, bigfont, Colors.White, 70, 5);

                //Add units
           //     FontUnitsCalc.ComputeExtent(_Units, out TextWidth, out TextHeight);
               _GaugeImage.Bitmap.DrawText(_Units, FontUnits, _UnitsFontColor,0 ,0);
             
            //    //Add Digit display
            //   FontDigitsCalc.ComputeExtent(_CurrentValue.ToString(), out TextWidth, out TextHeight);
               _GaugeImage.Bitmap.DrawText(_CurrentValue.ToString(), FontDigits, _DigitsFontColor, (_GaugeImage.Width / 4), 5);
            }

           


            private void DrawColouredBox()
            {
                 endy = (startpointY + _GaugeImage.Height);
                 endx = (int)((float)_CurrentValue * ((float)((_GaugeImage.Width) / (float)_MaxValue)));

                 _GaugeImage.Bitmap.DrawRectangle(Colors.White, 0, startpointX, startpointY, _GaugeImage.Width, _GaugeImage.Height, 0, 0, _StartColor, startpointX, (_GaugeImage.Height / 2), _EndColor, _GaugeImage.Width, (_GaugeImage.Height / 2), 65535);
                 _GaugeImage.Bitmap.DrawRectangle(Colors.White, 0, endx, startpointY, _GaugeImage.Width - endx, _GaugeImage.Height, 0, 0, _EmptyColor, startpointX, startpointY, EmptyColor, _GaugeImage.Width, _GaugeImage.Height, 65535);
                                
              
            }

            private void DrawMask()
            {
                _GaugeImage.Bitmap.DrawImage(0, 0, _GaugeMaskBitmap, 0, 0, _GaugeMaskBitmap.Width, _GaugeMaskBitmap.Height);
            }
       
            public override void Render()
            {
               
                DrawGauge();
                DrawColouredBox();
                DrawMask();
                UpdateText();
                base.Render();
            }


            //private static void DrawBar(int data, int max, int startpointX, int startpointY, int height, int width, Bitmap gauge)
            //{
            //    int endpointY = (startpointY + height);
            //    float stepsize = (float)width / (float)max;
            //    float endx = ((float)data * stepsize);
            //    gauge.DrawRectangle(Colors.White, 0, startpointX, startpointY, width, height, 0, 0, Colors.Green, startpointX, (height / 2), Colors.Red, width, (height / 2), 65535);
            //    gauge.DrawRectangle(Colors.White, 0, (int)endx, startpointY, width - (int)endx, height, 0, 0, Colors.Black, startpointX, startpointY, Colors.Black, width, height, 65535);
            //    gauge.Flush();
            //}

        }







        sealed public class AnalogueGauge : DisplayObjectContainer
        {
            String _Units = "";
            int _XLoc = 0, _YLoc = 0;
            int _CurrentValue = 0;
            int _MaxValue = 100;
            int _MinValue = 0;
            bool _IsBigGauge = false;
            int Needlelength = 0, NeedleEndx = 0, NeedleEndy = 0;
            int NeedlestartpointX = 0, NeedlestartpointY = 0;
            float Needlepoint = 0;

            Microsoft.SPOT.Presentation.Media.Color _StartColor = Colors.Blue;
            Microsoft.SPOT.Presentation.Media.Color _EndColor = Colors.Red;
            Microsoft.SPOT.Presentation.Media.Color _EmptyColor = Colors.White;

            Microsoft.SPOT.Presentation.Media.Color _UnitsFontColor = Colors.Black;
            Microsoft.SPOT.Presentation.Media.Color _DigitsFontColor = Colors.White;
            Microsoft.SPOT.Presentation.Media.Color _DialFontColor = Colors.Black;

           
           private GHI.Glide.UI.Image _GaugeImage = new Image("Gauge",255,0,0,10,10);
            Bitmap _GaugeBitmap = new Bitmap(10, 10);
            Bitmap centerbig = new Bitmap(Resources.GetBytes(Resources.BinaryResources.center), Bitmap.BitmapImageType.Gif);
            Bitmap centersmall = new Bitmap(Resources.GetBytes(Resources.BinaryResources.centersmall), Bitmap.BitmapImageType.Gif);

            int TextWidth = 0, TextHeight = 0;
            private  Microsoft.SPOT.Font FontUnitsCalc = FontManager.GetFont(FontManager.FontType.droid_reg10);
            private Microsoft.SPOT.Font FontDigitsCalc = FontManager.GetFont(FontManager.FontType.droid_reg08);

            //
            // Summary:
            //     Maximum value
            public int MaxValue
            {
                get
                {
                    return _MaxValue;
                }
                set
                {
                    _MaxValue = value;
                    //    this.Invalidate();
                }
            }




            //
            // Summary:
            //     Minimum value
            public int MinValue
            {
                get
                {
                    return _MinValue;
                }
                set
                {
                    _MinValue = value;
                    //    this.Invalidate();
                }
            }

            //
            // Summary:
            //     Value
            public int Value
            {
                get
                {
                    return _CurrentValue;
                }
                set
                {
                    if (value == _CurrentValue)
                    {
                        return;
                    }
                    if (value > _MaxValue)
                    {
                        _CurrentValue = _MaxValue;
                    }
                    else if (value < _MinValue)
                    {
                        _CurrentValue = _MinValue;
                    }
                    else
                    {
                        _CurrentValue = value;
                    }
                    this.Invalidate();
                }
            }


            //Starting color
            public Microsoft.SPOT.Presentation.Media.Color StartColor
            {
                get
                {
                    return _StartColor;
                }
                set
                {
                    _StartColor = value;
                    this.Invalidate();
                }
            }

            //Ending color
            public Microsoft.SPOT.Presentation.Media.Color EndColor
            {
                get
                {
                    return _EndColor;
                }
                set
                {
                    _EndColor = value;
                    this.Invalidate();
                }
            }

            //Emptying color
            public Microsoft.SPOT.Presentation.Media.Color EmptyColor
            {
                get
                {
                    return _EmptyColor;
                }
                set
                {
                    _EmptyColor = value;
                    this.Invalidate();
                }
            }

            //Fonting color
            public Microsoft.SPOT.Presentation.Media.Color UnitsFontColor
            {
                get
                {
                    return _UnitsFontColor;
                }
                set
                {
                    _UnitsFontColor = value;
                    this.Invalidate();
                }
            }

            //Fonting color
            public Microsoft.SPOT.Presentation.Media.Color DigitsFontColor
            {
                get
                {
                    return _DigitsFontColor;
                }
                set
                {
                    _DigitsFontColor = value;
                    this.Invalidate();
                }
            }

            //Fonting color
            public Microsoft.SPOT.Presentation.Media.Color DialFontColor
            {
                get
                {
                    return _DialFontColor;
                }
                set
                {
                    _DialFontColor = value;
                    this.Invalidate();
                }
            }



            public AnalogueGauge(DisplayObjectContainer window, byte[] GaugeGIF, Font SmallFont, Font BigFont, string GaugeName, string Units, ushort alpha, int x, int y, bool IsBigGauge)
            {
                
                Name = GaugeName;
                Parent = window;
              
                X = 0;
                Y = 0;
                Width = window.Width;
                Height = window.Height;

               _XLoc = x;
                _YLoc = y;

                _Units = Units;

                _GaugeBitmap = new Bitmap(GaugeGIF, Bitmap.BitmapImageType.Gif);// GaugeGIF.Bitmap;

                _IsBigGauge = IsBigGauge;
                if (_IsBigGauge == false) { FontUnitsCalc = FontManager.GetFont(FontManager.FontType.droid_reg08); }


                _GaugeImage = new Image("Gauge", 255, _XLoc, _YLoc, _GaugeBitmap.Width, _GaugeBitmap.Height);// GaugeGIF;
                _GaugeImage.Bitmap = new Bitmap(GaugeGIF, Bitmap.BitmapImageType.Gif);// GaugeGIF.Bitmap;
                _GaugeImage.Parent = this;
                            
                AddChild(_GaugeImage);


            }

            private void DrawGauge()
            {
             //   _GaugeImage.Bitmap.Clear();
          
                _GaugeImage.Bitmap.DrawImage(0, 0, _GaugeBitmap, 0, 0, _GaugeBitmap.Width, _GaugeBitmap.Height); //_GaugeBitmap; //draw fresh gauge
            }

            private void UpdateText()
            {
                //Add units
                FontUnitsCalc.ComputeExtent(_Units, out TextWidth, out TextHeight);
                _GaugeImage.Bitmap.DrawText(_Units, FontUnitsCalc, _UnitsFontColor, (_GaugeImage.Width / 2) - (TextWidth / 2), (_GaugeImage.Height / 3) -4);
             
                //Add Digit display
                FontDigitsCalc.ComputeExtent(_CurrentValue.ToString(), out TextWidth, out TextHeight);
                _GaugeImage.Bitmap.DrawText(_CurrentValue.ToString(), FontDigitsCalc, _DigitsFontColor, (_GaugeImage.Width / 2) - (TextWidth / 2), (_GaugeImage.Height - (_GaugeImage.Height / 3)));
            }

            private void DrawDialNumbers()
            {
                //Add units
               // FontSizeCalc.ComputeExtent(_Units, out TextWidth, out TextHeight);
               // _GaugeImage.Bitmap.DrawText(_Units, FontSizeCalc, _UnitsFontColor, (_GaugeImage.Width / 2) - (TextWidth / 2), (_GaugeImage.Height - (_GaugeImage.Height / 3)) - TextHeight);
            }


            Bitmap _TempGaugeBitmap = new Bitmap(10, 10);
            private void DrawNeedle()
            {
                NeedlestartpointX = _GaugeImage.Width / 2;
                NeedlestartpointY = _GaugeImage.Height / 2;
                Needlelength = _GaugeImage.Width / 3;


            


                float TopVal = _CurrentValue + System.Math.Abs(_MinValue);
                float bottomVal = _MaxValue - _MinValue; //so that is the total range
                bottomVal = bottomVal / 245;
              
               // Needlepoint = (float)(System.Math.Abs(_CurrentValue)) / ((float)(((System.Math.Abs(_MinValue)) + _MaxValue) / 245));         //245deg max sweep max=max units (step size calc)
                Needlepoint = TopVal / bottomVal;
                
                //short needle for small gauge
                float angle = 149 + Needlepoint;                                       //153deg is start point angle
                float radians;
                if (angle > 360)
                    angle -= 360;
                radians = angle * (float)System.Math.PI / 180;
                NeedleEndx = (int)(Needlelength * System.Math.Cos(radians));      //[Jez]eyes glazed over, answer comes out.....
                NeedleEndy = (int)(Needlelength * System.Math.Sin(radians));
                NeedleEndx += NeedlestartpointX;                                   //center point
                NeedleEndy += NeedlestartpointY;                                   //center point

            //    _TempGaugeBitmap = _GaugeImage.Bitmap;
             //   SmoothLine.drawLineRLPFix((float)NeedlestartpointX, (float)NeedlestartpointY, (float)NeedleEndx, (float)NeedleEndy, ref  _TempGaugeBitmap, Colors.Red, (float)1);
             //   _GaugeImage.Bitmap = _TempGaugeBitmap;

                _GaugeImage.Bitmap.DrawLine(Colors.Red, 1, NeedlestartpointX, NeedlestartpointY, NeedleEndx, NeedleEndy);

                if (_IsBigGauge == true) { _GaugeImage.Bitmap.DrawImage(NeedlestartpointX - (centerbig.Width / 2), NeedlestartpointY - (centerbig.Height / 2), centerbig, 0, 0, centerbig.Width, centerbig.Height); }
                else { _GaugeImage.Bitmap.DrawImage(NeedlestartpointX - (centersmall.Width / 2), NeedlestartpointY - (centersmall.Height / 2), centersmall, 0, 0, centersmall.Width, centersmall.Height); }

            }
       
            public override void Render()
            {
               
                DrawGauge();
                UpdateText();
                DrawDialNumbers();
                DrawNeedle();
                base.Render();
            }



        }







     

    }

}