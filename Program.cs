/* Holden VE Series 1 Radio and IQ Radio Gauge Data Display
 * Will output RGBS Video to a display and show
 * GMLAN and HSCAN GM Data similar to Motec EDI Unit
 * 
 * Resources are loading from a USB Stick
 * 
 * jezbeall@gmail.com (c)2015
 * Thanks to Jason Martin (Tazzie)
 * 
 */
using System;
using System.IO;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.IO;
using GHI.Processor;
using VideoOutModulePlainNETMF;
using GHI.IO;
using GHI.Glide;
using GHI.Glide.Display;
using GHI.Glide.UI;
using GHI.Glide.Geom;
using GHI.IO.Storage;
using GHI.Usb;
using GHI.Usb.Host;

//enum for Update Timers
public static class ModuleTimers
{
    public static int RPM = 0;
    public static int TPS = 1;
    public static int SWA = 2;
    public static int YAW = 3;
    public static int LAT = 4;
    public static int MAP = 5;
    public static int ETH = 6;
    public static int Nav = 7;
    public static int Switch = 8;
}
namespace NETMFBook1
{
    public partial class App
    {
        //Setup XML to Object stuff
        private static Image Bar1;
        private static Image Bar2;

        //Load up windows
        static Window window2 = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.window2));

        public static AutoResetEvent evt = new AutoResetEvent(false);
        public static MassStorage usb_storage;
        public static string rootDirectory;

        static ControllerAreaNetwork.Message received = null;
        static string data = string.Empty;
        public static GHI.Glide.Geom.Point touches;
        public static int oldX;
        public static int oldY;
        public static int X;
        public static int Y;
        public static int RPM = 0;                          //Setup all variables for the PIDs
        public static int oldRPM = -1;
        public static int ECT = -1;
        public static int oldECT = 0;
        public static int IAT = -1;
        public static int oldIAT;
        public static int TPS;
        public static int oldTPS = -1;
        public static int SWA;
        public static int oldSWA = 0;
        public static int LatAccel;
        public static int oldLatAccel = 0;
        public static int Yaw;
        public static int oldYaw = 0;
        public static int MAP;
        public static int oldMAP = 0;
        public static int ETH;
        public static int oldETH = -1;
        public static int SPKAdv;
        public static int oldSPKAdv = -1;
        public static bool touchedOn = false;
        public static bool touchedOff = false;
        public static bool firstrun = true;
        public static bool IsS1 = true;                 //is this a S1 unit? If so send Nav enable packet
        public static bool IsAnalog = true;             //Analog or Bar display page
        public static long[] TimeNow = new long[9];
        public static long[] LastTime = new long[9];
        public static int UpdateRate = 25;              //Rate to update the display/drop frames
        public static int RPMTime;
        public static int x, y;                         //gauge X Y position
        public static bool BarGraph = false;            //set default display screen

        public static void Main()
        {   
            //Look for USB.. setup event handles
            RemovableMedia.Insert += new InsertEventHandler(RemovableMedia_Insert); //event when inserted
            RemovableMedia.Eject += new EjectEventHandler(RemovableMedia_Eject); //event when ejected
            Controller.MassStorageConnected += (sender, massStorage) => //what it does when MS connected
            {
                usb_storage = massStorage;
                usb_storage.Mount();                            //Fires the USB insert event when finished
            };
            Controller.Start(); //Starts the event
            Debug.Print("Waiting for USB insertion...");
            evt.WaitOne();                                      //Wait here until mounting and initializing is finished

            FileStream fileHandle = new FileStream(rootDirectory + @"\GaugeBig.gif", FileMode.Open, FileAccess.Read);
            byte[] dataLargeDial = new byte[fileHandle.Length];
            fileHandle.Read(dataLargeDial, 0, dataLargeDial.Length);
            fileHandle.Close();
            fileHandle = new FileStream(rootDirectory + @"\GaugeSmall.gif", FileMode.Open, FileAccess.Read);
            byte[] dataSmallDial = new byte[fileHandle.Length];
            fileHandle.Read(dataSmallDial, 0, dataSmallDial.Length);
            fileHandle.Close();

            //Init the timing array (this is bullshit, must be an easier way. Will try the GHI Timer function in future)
            TimeNow[0] = System.DateTime.Now.Ticks;
            TimeNow[1] = System.DateTime.Now.Ticks;
            TimeNow[2] = System.DateTime.Now.Ticks;
            TimeNow[3] = System.DateTime.Now.Ticks;
            TimeNow[4] = System.DateTime.Now.Ticks;
            TimeNow[5] = System.DateTime.Now.Ticks;
            TimeNow[6] = System.DateTime.Now.Ticks;
            TimeNow[7] = System.DateTime.Now.Ticks;
            TimeNow[8] = System.DateTime.Now.Ticks;
            LastTime[0] = System.DateTime.Now.Ticks;
            LastTime[1] = System.DateTime.Now.Ticks;
            LastTime[2] = System.DateTime.Now.Ticks;
            LastTime[3] = System.DateTime.Now.Ticks;
            LastTime[4] = System.DateTime.Now.Ticks;
            LastTime[5] = System.DateTime.Now.Ticks;
            LastTime[6] = System.DateTime.Now.Ticks;
            LastTime[7] = System.DateTime.Now.Ticks;
            LastTime[8] = System.DateTime.Now.Ticks;
            Tween.NumSteps.SlideWindow = 25;

            if (IsS1) Debug.Print("Radio is a S1");

            //Load Jezzas wicked RGBS DLL file and init the display and pixel clocks
            Debug.Print("Setting RGBS Output and Pixel Clocks on Chrontel 7026B DAC...");
            VideoOutModulePlainNETMF.RGBSvideoOut.SetDisplayConfig();

            //Setup the CAN bus timings. Tazzie is the man for GMLAN/HSCAN timing settings
            GHI.IO.ControllerAreaNetwork.Timings GMLANTimings = new ControllerAreaNetwork.Timings(0, 0xF, 0x8, 0x4B, 1);
            GHI.IO.ControllerAreaNetwork.Timings HSCANTimings = new ControllerAreaNetwork.Timings(0, 14, 5, 10, 1);
            var can1 = new ControllerAreaNetwork(ControllerAreaNetwork.Channel.One, GMLANTimings);
            var can2 = new ControllerAreaNetwork(ControllerAreaNetwork.Channel.Two, ControllerAreaNetwork.Speed.Kbps500);

            //CAN Bus Explicit Filtersx
            uint[] filter1 = { 0x102F8080, 0x102E0080 };          //GMLAN
            uint[] filter2 = { 0xC9, 0x4C1, 0x3FB, 0x7E8 };       //HSCAN
            //uint[] filter2 = { 0xC9, 0x4C1, 0x1E5, 0x1E9 };     //HSCAN
            
            //Set screen dimensions
            int videoOutWidth = 395;
            int videoOutHeight = 240;
            Debug.Print("Setting up blank bitmap output container...");
            Bitmap LCD = new Bitmap(videoOutWidth, videoOutHeight); // This empty Bitmap object will be our output container
            Debug.Print("Displaying Glide Loading message...");
            Window window = new Window("window1", 395, 240);
            window.BackColor = Microsoft.SPOT.Presentation.Media.Color.Black;
            Window window2 = new Window("window2", 395, 240);//GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.window2));
            Debug.Print("Setting up Glide Touch system...");
            GlideTouch.Initialize();

            //Setup the fonts
            Debug.Print("Loading fonts...");
            Font bigfont = Resources.GetFont(Resources.FontResources.NinaB);
            Font smallfont = Resources.GetFont(Resources.FontResources.small);
            Font digitalfont_big = Resources.GetFont(Resources.FontResources.digital7_14pt);
            Font digitalfont_small = Resources.GetFont(Resources.FontResources.digital7_12pt);

            //Setup the bitmaps
            Debug.Print("Loading bitmaps...");
            byte[] bar_mask = Resources.GetBytes(Resources.BinaryResources.bar_mask); //new Bitmap(Resources.GetBytes(Resources.BinaryResources.bar_mask), Bitmap.BitmapImageType.Gif);

            Canvas Border = new Canvas();
            window.AddChild(Border);
            //Border.DrawRectangle(Colors.Red, 1, 5, 10, 370, 205, 0, 0, Colors.White, 6500, 6500, Colors.White, 6500, 6500, 0);    //IQ 
            Border.DrawRectangle(Colors.Red, 1, 5, 10, 370, 205, 0, 0, Colors.White, 6500, 6500, Colors.White, 6500, 6500, 0);      //S1
            
            int StartX1 = 0x27;
            int StartY1 = 0x8;
            int StartX2 = 0x4;
            int StartY2 = 135;
            Gauges.AnalogueGauge AnaGauge1 = new Gauges.AnalogueGauge(window, dataLargeDial,digitalfont_small,digitalfont_big, "AnaGauge1", "RPM", 255, StartX1, StartY1, true);
            window.AddChild(AnaGauge1);
            AnaGauge1.MaxValue = 8000;
            AnaGauge1.Value = 0;

            Gauges.AnalogueGauge AnaGauge2 = new Gauges.AnalogueGauge(window, dataLargeDial, digitalfont_small, digitalfont_big, "AnaGauge2", "TPS", 255, StartX1 + 147, StartY1, true);
            window.AddChild(AnaGauge2);
            AnaGauge2.MaxValue = 100;
            AnaGauge2.Value = 0;

            Gauges.AnalogueGauge AnaGauge3 = new Gauges.AnalogueGauge(window, dataSmallDial, digitalfont_small, digitalfont_big, "AnaGauge3", "ECT", 255, StartX2, StartY2, false);
            window.AddChild(AnaGauge3);
            AnaGauge3.MaxValue = 140;
            AnaGauge3.Value = 0;

            Gauges.AnalogueGauge AnaGauge4 = new Gauges.AnalogueGauge(window, dataSmallDial, digitalfont_small, digitalfont_big, "AnaGauge4", "IAT", 255, StartX2 + 144, StartY2, false);
            window.AddChild(AnaGauge4);
            AnaGauge4.MaxValue = 100;
            AnaGauge4.Value = 0;

            Gauges.AnalogueGauge AnaGauge5 = new Gauges.AnalogueGauge(window, dataSmallDial, digitalfont_small, digitalfont_big, "AnaGauge5", "ETH", 255, StartX2 + 144 + 144, StartY2, false);
            window.AddChild(AnaGauge5);
            AnaGauge5.MaxValue = 100;
            AnaGauge5.Value = 0;

            Gauges.SlantedGauge SlantGauge1 = new Gauges.SlantedGauge(window2, bar_mask, smallfont, bigfont, "SlantGauge1", "RPM",255,0,0);
            window2.AddChild(SlantGauge1);
            SlantGauge1.MaxValue = 8000;
            SlantGauge1.Value = 0;

            Gauges.SlantedGauge SlantGauge2 = new Gauges.SlantedGauge(window2, bar_mask, smallfont, bigfont, "SlantGauge2", "RPM",255,0,70);
            window2.AddChild(SlantGauge2);
            SlantGauge2.MaxValue = 100;
            SlantGauge2.Value = 0;

            //Image Bar1 = (Image)window2.GetChildByName("bar1");
            //Bar1.Bitmap = new Bitmap(Bar1.Width, Bar1.Height);
            //Image Bar2 = (Image)window2.GetChildByName("bar2");
            //Bar2.Bitmap = new Bitmap(Bar1.Width, Bar1.Height);
  

            //Draw the screen the first time
            //Debug.Print("Drawing gauges and labels...");
            //Bar1.Bitmap.DrawText("RPM", smallfont, Colors.White, 0, 0);
            //Bar2.Bitmap.DrawText("TPS", smallfont, Colors.White, 0, 0);

            //Setup CAN Events, enable CAN and Filters
            Debug.Print("Enabling HSCAN and GMLAN...");
            //can1.ErrorReceived += can_ErrorReceived;
            //can2.ErrorReceived += can_ErrorReceived;
            can1.MessageAvailable += can1_MessageAvailable;
            can2.MessageAvailable += can2_MessageAvailable;
            can1.Enabled = true;
            can2.Enabled = true;
            can1.SetExplicitFilters(filter1);
            can2.SetExplicitFilters(filter2);
            Debug.Print("CAN and Filters Enabled");

            //Define the Nav Enable packet for S1 Radio
            ControllerAreaNetwork.Message PingNav = new ControllerAreaNetwork.Message();
            PingNav.ArbitrationId = 0x102E2094;
            PingNav.Data = new byte[] { 0x25, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            PingNav.Length = 2;
            PingNav.IsExtendedId = true;
            
            //Request Spark Advane PID Test
            ControllerAreaNetwork.Message reqSpark = new ControllerAreaNetwork.Message();
            reqSpark.ArbitrationId = 0x7DF;
            reqSpark.Data = new byte[] { 0x02, 0x01, 0x0E, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA };
            reqSpark.Length = 8;
            reqSpark.IsExtendedId = false;
            can2.SendMessage(reqSpark);

            //send it the first time to fire things up if S1
            if (IsS1) can1.SendMessage(PingNav);

            if (BarGraph != true)
                Glide.MainWindow = window;
            else
                Glide.MainWindow = window2;
            Debug.Print("Program Started");
          
            //Run forever. 100 miles and running [NWA FTW]......
            while (true)
            {
                //RPM += 50;
                //TPS += 1;
                //ECT += 1;
                //IAT += 1;
                //ETH += 1;

                //can2.SendMessage(reqSpark);

                //Change screens using slide if button pressed
                if(BarGraph == true)
                    if (Glide.MainWindow == window) { Tween.SlideWindow(window, window2, Direction.Left); }
                     else { }
                
                if(BarGraph == false)
                    if (Glide.MainWindow == window2){Tween.SlideWindow(window2, window, Direction.Right);}
                    else { }

                if (IsS1) //NAV
                {
                    if (((TimeNow[ModuleTimers.Nav] - LastTime[ModuleTimers.Nav]) / TimeSpan.TicksPerMillisecond) > 5000) //send ping packet every 5sec
                    {
                        can1.SendMessage(PingNav);
                        LastTime[ModuleTimers.Nav] = TimeNow[ModuleTimers.Nav];
                    }
                    else
                    {
                        TimeNow[ModuleTimers.Nav] = System.DateTime.Now.Ticks;
                    }
                }

           
                if (oldRPM != RPM)
                {
                    if (BarGraph == true)
                    {
                        //DrawBar(RPM, 8000, 0, 4, 46, 352, Bar1.Bitmap);               //Unremark to draw new bars [TPS is fine]
                        //Bar1.Bitmap.Flush();
                        //Bar1.Bitmap.DrawImage(0, 0, bar_mask, 0, 0, 350,50);
                        //Bar1.Bitmap.DrawText("RPM", smallfont, Colors.White, 0, 0);
                        //Bar1.Bitmap.DrawText("" + RPM, bigfont, Colors.White, 70, 5);
                        //Bar1.Invalidate();
                        SlantGauge1.Value = RPM;
                        oldRPM = RPM;
                    }
                    else
                    {
                        AnaGauge1.Value = RPM;
                        oldRPM = RPM;
                    }
                }

                if (oldTPS != TPS)
                {
                    if (BarGraph == true)
                    {
                        //DrawBar(TPS, 100, 0, 4, 46, 352, Bar2.Bitmap);               //Unremark to draw new bars [TPS is fine]
                        //Bar2.Bitmap.Flush();
                        //Bar2.Bitmap.DrawImage(0, 0, bar_mask, 0, 0, 350, 50);
                        //Bar2.Bitmap.DrawText("TPS", smallfont, Colors.White, 0, 0);
                        //Bar2.Bitmap.DrawText("" + TPS, bigfont, Colors.White, 70, 5);
                        //Bar2.Invalidate();
                        SlantGauge2.Value = TPS;
                        oldTPS = TPS;
                    }
                    else
                    {
                        AnaGauge2.Value = TPS;
                        oldTPS = TPS;
                    }
                }

                if (oldECT != ECT)
                {
                    if (BarGraph == true)
                    { }
                    else
                    {
                        AnaGauge3.Value = ECT;
                        oldECT = ECT;
                    }
                }

                if (oldIAT != IAT)
                {
                    if (BarGraph == true)
                    { }
                    else
                    {
                        AnaGauge4.Value = IAT;
                        oldIAT = IAT;
                    }
                }

                if (oldETH != ETH)
                {
                    if (BarGraph == true)
                    { }
                    else
                    {
                        AnaGauge5.Value = ETH;
                        oldETH = ETH;
                    }
                }

                if (oldSPKAdv != SPKAdv)
                {

                    //Debug.Print("Spark Adv: " + SPKAdv);
                    //can2.SendMessage(reqSpark);
                }
                /*
                if (oldSWA != SWA)
                {
                    if (((TimeNow[ModuleTimers.SWA] - LastTime[ModuleTimers.SWA]) / TimeSpan.TicksPerMillisecond) > UpdateRate)
                    {
                        txtSWA.Text = SWA.ToString();
                        txtSWA.Invalidate();
                        can2.DiscardIncomingMessages();
                        LastTime[ModuleTimers.SWA] = TimeNow[ModuleTimers.SWA];
                    }
                    else
                    {
                        TimeNow[ModuleTimers.SWA] = System.DateTime.Now.Ticks;
                    }
                }
                if (oldYaw != Yaw)
                {
                    if (((TimeNow[ModuleTimers.YAW] - LastTime[ModuleTimers.YAW]) / TimeSpan.TicksPerMillisecond) > UpdateRate)
                    {
                        txtYaw.Text = Yaw.ToString();
                        txtYaw.Invalidate();
                        can2.DiscardIncomingMessages();
                        LastTime[ModuleTimers.YAW] = TimeNow[ModuleTimers.YAW];
                    }
                        else
                    {
                        TimeNow[ModuleTimers.YAW] = System.DateTime.Now.Ticks;
                    }
                }
                if (oldLatAccel != LatAccel)
                {
                    if (((TimeNow[ModuleTimers.LAT] - LastTime[ModuleTimers.LAT]) / TimeSpan.TicksPerMillisecond) > UpdateRate)
                    {
                        txtLatAccel.Text = LatAccel.ToString();
                        txtLatAccel.Invalidate();
                        can2.DiscardIncomingMessages();
                        LastTime[ModuleTimers.LAT] = TimeNow[(int)ModuleTimers.LAT];
                    }
                    else
                    {
                        TimeNow[ModuleTimers.LAT] = System.DateTime.Now.Ticks;
                    }
                } 
                 
                    if (((TimeNow[ModuleTimers.MAP] - LastTime[ModuleTimers.MAP]) / TimeSpan.TicksPerMillisecond) > UpdateRate)
                    {
                        //can2.SendMessage(req_map);
                        LastTime[ModuleTimers.MAP] = TimeNow[ModuleTimers.MAP];
                        Debug.Print("MAP: " + MAP);
                    }
                    else
                    {
                        TimeNow[ModuleTimers.MAP] = System.DateTime.Now.Ticks;
                    }
                  
                    */

            }
        }
        private static void can1_MessageAvailable(ControllerAreaNetwork sender, ControllerAreaNetwork.MessageAvailableEventArgs e)
        {
            //Debug.Print("CAN1 IRQ Called");
            while (sender.AvailableMessages > 0)
            {
                received = sender.ReadMessage();
                if (received.ArbitrationId == 0x102F8080)
                {
                    if ((received.Data[2] == 0x00) && (received.Data[3] == 0x00))
                    {
                        touches.X = oldX;
                        touches.Y = oldX;
                        GlideTouch.RaiseTouchUpEvent(null, new TouchEventArgs(touches));
                    }
                    else
                    {
                        X = received.Data[2] * 10;
                        Y = received.Data[3] * 10;
                        //Debug.Print("Touch On X" + X + " " + "Y" + Y);
                        touches.X = X;
                        touches.Y = Y;
                        oldX = X;
                        oldY = Y;
                        GlideTouch.RaiseTouchDownEvent(null, new TouchEventArgs(touches));
                    }
                }
                if (received.ArbitrationId == 0x102E0080)   //Nav buttons pressed on S1
                {
                    if (received.Data[2] == 0x01)
                    {
                        BarGraph = false;
                    }
                    if (received.Data[2] == 0x02)
                    {
                        BarGraph = true;
                    }
                    Debug.Print("Touched Nav Buttons");
                }
            }
        }
        private static void can2_MessageAvailable(ControllerAreaNetwork sender, ControllerAreaNetwork.MessageAvailableEventArgs e)
        {
            //Debug.Print("CAN2 IRQ Called"); 
            while (sender.AvailableMessages > 0)
            {
                received = sender.ReadMessage();
                if (firstrun == true) { }
                else
                {
                    oldRPM = RPM;
                    oldECT = ECT;
                    oldIAT = IAT;
                    oldTPS = TPS;
                    oldSWA = SWA;
                    oldYaw = Yaw;
                    oldMAP = MAP;
                    oldETH = ETH;
                    oldSPKAdv = SPKAdv;
                    firstrun = false;
                }
                if (received.ArbitrationId == 0x3FB)
                {
                    ETH = (received.Data[1] * 100) / 255;
                }
                if (received.ArbitrationId == 0xC9)
                {
                    RPM = (((received.Data[1] * 0x100) + received.Data[2]) / 4);
                    TPS = (received.Data[4] * 100) / 255;
                }
                if (received.ArbitrationId == 0x4C1)
                {
                    ECT = received.Data[2] - 40;
                    IAT = received.Data[3] - 40;
                }
                if ((received.ArbitrationId == 0x7E8) && (received.Data[2] == 0xB))
                {
                    MAP = received.Data[2] + 14;
                }
                if (received.ArbitrationId == 0x1E5)
                {
                    SWA = ((received.Data[5] * 0x100) + received.Data[6]);
                    if (SWA > 0x8000)
                    {
                        SWA = ~SWA;                         //process negative degrees
                    }
                    SWA = SWA / 16;
                }
                if (received.ArbitrationId == 0x1E9)
                {
                    Yaw = received.Data[4] / 16;
                    LatAccel = received.Data[0] / 64;       //metres per sec acceleration
                    //LatAccel = LatAccel / 9.8067;         //1 Gforce = 9.80665m/sec acceleration
                }
                if (received.ArbitrationId == 0x7E8)
                    if (received.Data[2] == 0xE)
                        SPKAdv = (received.Data[3] / 2) - 64;
            }
        }
        private static void can_ErrorReceived(ControllerAreaNetwork sender, ControllerAreaNetwork.ErrorReceivedEventArgs e)
        {
            // This event is fired by unmount
            Debug.Print("Error on CAN: " + e.Error.ToString());
        }
        static void RemovableMedia_Eject(object sender, MediaEventArgs e)
        {
            Debug.Print("USB unmounted, eject event fired");
        }
        static void RemovableMedia_Insert(object sender, MediaEventArgs e)
        {
            Debug.Print("Insert event fired; USB Storage mount is finished.");
            if (e.Volume.IsFormatted)
            {
                
                rootDirectory = e.Volume.RootDirectory;
                /*
                Debug.Print("Available folders:");
                string[] strs = Directory.GetDirectories(e.Volume.RootDirectory);
                for (int i = 0; i < strs.Length; i++)
                    Debug.Print(strs[i]);

                Debug.Print("Available files:");
                strs = Directory.GetFiles(e.Volume.RootDirectory);
                for (int i = 0; i < strs.Length; i++)
                    Debug.Print(strs[i]);
                */
            }
            evt.Set(); // proceed with other processing
        }
        private static void btn1_PressEvent(object sender)
        {
            Debug.Print("Button 1 tapped.");
            //Glide.MessageBoxManager.Show("This is the test message...", "Title", ModalButtons.OkCancel);
        }
        private static void btn2_PressEvent(object sender)
        {

            Debug.Print("Button 2 tapped.");
        }
        private static void btn3_PressEvent(object sender)
        {
            Debug.Print("Button 3 tapped.");
        }
      
        
        private static void DrawBar(int data, int max, int startpointX, int startpointY, int height, int width, Bitmap gauge)
        {
            int endpointY = (startpointY + height);
            float stepsize = (float)width / (float)max;
            float endx = ((float)data * stepsize);
            gauge.DrawRectangle(Colors.White, 0, startpointX, startpointY, width, height, 0, 0, Colors.Green, startpointX, (height / 2), Colors.Red, width, (height / 2), 65535);
            gauge.DrawRectangle(Colors.White, 0, (int)endx, startpointY, width - (int)endx, height, 0, 0, Colors.Black, startpointX, startpointY, Colors.Black, width, height, 65535);
            gauge.Flush();
        }
    }
}

