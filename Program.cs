/* Holden VE Series 1 Radio and IQ Radio Gauge Data Display
 * Will output RGBS Video to a display and show
 * GMLAN and HSCAN GM Data similar to Motec EDI Unit
 * 
 * Resources are loading from a USB Stick
 * 
 * Jeremy Beall (JezzaB) (c)2015
 * Jason Martin (Tazzie) (c)2015
 * 
 */
using System;
using System.IO;
using System.Text;
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
    public static int Spark = 2;
    public static int ECT = 3;
    public static int IAT = 4;
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

        public static ControllerAreaNetwork.Message flowControl = new ControllerAreaNetwork.Message();
        public static ControllerAreaNetwork  can2 = new ControllerAreaNetwork(ControllerAreaNetwork.Channel.Two, ControllerAreaNetwork.Speed.Kbps500);

        static ControllerAreaNetwork.Message received = null;
        static string data = string.Empty;
        public static GHI.Glide.Geom.Point touches;
        public static int oldX;
        public static int oldY;
        public static int X;
        public static int Y;
        public static int RPM = 0, oldRPM = -1;
        public static int ECT = -1, oldECT = 0;
        public static int IAT = -1, oldIAT;
        public static int TPS, oldTPS = -1;
        public static int MAP, oldMAP = 0;
        public static int Boost, oldBoost = 0;
        public static int ETH, oldETH = -1;
        public static int SPKAdv, oldSPKAdv = -1;
        public static int VSS, oldVSS = 0;
        public static double KR, oldKR = 0;
        public static double AFR, oldAFR = 0;

        public static bool touchedOn = false;
        public static bool touchedOff = false;
        public static bool firstrun = true;
        public static bool IsS1 = true;                 //is this a S1 unit? If so send Nav enable packet
        public static bool IsAnalog = true;             //Analog or Bar display page default start
        public static long[] TimeNow = new long[9];
        public static long[] LastTime = new long[9];
        public static int UpdateRate = 25;              //Rate to update the display/drop frames
        public static int RPMTime;
        public static int x, y;                         //gauge X Y position
        public static bool BarGraph = false;            //set default display screen

        public static void Main()
        {
            // Overclock G120 w00t
            var EMCCLKSEL = new GHI.Processor.Register(0x400FC100);
            //EMCCLKSEL.ClearBits(1 << 0); 
            
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
            fileHandle = new FileStream(rootDirectory + @"\background.gif", FileMode.Open, FileAccess.Read);
            byte[] dataBackground = new byte[fileHandle.Length];
            fileHandle.Read(dataBackground, 0, dataBackground.Length);
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
            //var can2 = new ControllerAreaNetwork(ControllerAreaNetwork.Channel.Two, ControllerAreaNetwork.Speed.Kbps500);

            //CAN Bus Explicit Filters
            uint[] filter1 = { 0x102F8080, 0x102E0080 };            //GMLAN
            uint[] filter2 = { 0x7E8 };  //HSCAN
            //uint[] filter2 = { 0xC9, 0x4C1, 0x3FB, 0x7E8, 0x3E9 };  //HSCAN PPEI

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
            //Border.DrawRectangle(Colors.Red, 1, 5, 10, 370, 205, 0, 0, Colors.White, 6500, 6500, Colors.White, 6500, 6500, 0);      //S1
            
            int StartX1 = 0x27;
            int StartY1 = 0x8;
            int StartX2 = 0x4;
            int StartY2 = 135;
            Gauges.AnalogueGauge AnaGauge1 = new Gauges.AnalogueGauge(window, dataLargeDial, digitalfont_small, digitalfont_big, "AnaGauge1", "RPM", 255, StartX1, StartY1, true);
            window.AddChild(AnaGauge1);
            AnaGauge1.MaxValue = 8000;
            AnaGauge1.Value = 0;

            Gauges.AnalogueGauge AnaGauge2 = new Gauges.AnalogueGauge(window, dataLargeDial, digitalfont_small, digitalfont_big, "AnaGauge2", "MAP", 255, StartX1 + 147, StartY1, true);
            window.AddChild(AnaGauge2);
            AnaGauge2.MaxValue = 205;
            AnaGauge2.Value = 0;
            AnaGauge2.MinValue = 0;

            Gauges.AnalogueGauge AnaGauge3 = new Gauges.AnalogueGauge(window, dataSmallDial, digitalfont_small, digitalfont_big, "AnaGauge3", "SparkAdv", 255, StartX2, StartY2, false);
            window.AddChild(AnaGauge3);
            AnaGauge3.MaxValue = 64;
            AnaGauge3.MinValue = -64;
            AnaGauge3.Value = 0;

            Gauges.AnalogueGauge AnaGauge4 = new Gauges.AnalogueGauge(window, dataSmallDial, digitalfont_small, digitalfont_big, "AnaGauge4", "ECT", 255, StartX2 + 144, StartY2, false);
            window.AddChild(AnaGauge4);
            AnaGauge4.MaxValue = 100;
            AnaGauge4.MinValue = -40;
            AnaGauge4.Value = 0;

            Gauges.AnalogueGauge AnaGauge5 = new Gauges.AnalogueGauge(window, dataSmallDial, digitalfont_small, digitalfont_big, "AnaGauge5", "IAT", 255, StartX2 + 144 + 144, StartY2, false);
            window.AddChild(AnaGauge5);
            AnaGauge5.MaxValue = 100;
            AnaGauge5.Value = -40;

            Gauges.SlantedGauge SlantGauge1 = new Gauges.SlantedGauge(window2, bar_mask, smallfont, bigfont, "SlantGauge1", "RPM", 255, 5, 5);
            window2.AddChild(SlantGauge1);
            SlantGauge1.MaxValue = 8000;
            SlantGauge1.Value = 0;

            Gauges.SlantedGauge SlantGauge2 = new Gauges.SlantedGauge(window2, bar_mask, smallfont, bigfont, "SlantGauge2", "TPS", 255, 5, 75);
            window2.AddChild(SlantGauge2);
            SlantGauge2.MaxValue = 100;
            SlantGauge2.Value = 0;

            Gauges.SlantedGauge SlantGauge3 = new Gauges.SlantedGauge(window2, bar_mask, smallfont, bigfont, "SlantGauge3", "Boost", 255, 5, 145);
            window2.AddChild(SlantGauge3);
            SlantGauge3.MaxValue = 15;
            SlantGauge3.Value = 0;
            
            window.BackImage = new Bitmap(dataBackground, Bitmap.BitmapImageType.Gif);

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

            //Request Spark Advance PID 
            ControllerAreaNetwork.Message reqSpark = new ControllerAreaNetwork.Message();
            reqSpark.ArbitrationId = 0x7E0;
            reqSpark.Data = new byte[] { 0x03, 0x22, 0x00, 0x0E, 0xAA, 0xAA, 0xAA, 0xAA };
            reqSpark.Length = 8;
            reqSpark.IsExtendedId = false;
            //can2.SendMessage(reqSpark);

            //Request Manifold Pressure (MAP) PID
            ControllerAreaNetwork.Message reqMAP = new ControllerAreaNetwork.Message();
            reqMAP.ArbitrationId = 0x7E0;
            reqMAP.Data = new byte[] { 0x03, 0x22, 0x00, 0x0B, 0xAA, 0xAA, 0xAA, 0xAA };
            reqMAP.Length = 8;
            reqMAP.IsExtendedId = false;
            //can2.SendMessage(reqMAP);

            //Request Engine Speed (RPM) PID
            ControllerAreaNetwork.Message reqRPM = new ControllerAreaNetwork.Message();
            reqRPM.ArbitrationId = 0x7E0;
            reqRPM.Data = new byte[] { 0x03, 0x22, 0x00, 0x0C, 0xAA, 0xAA, 0xAA, 0xAA };
            reqRPM.Length = 8;
            reqRPM.IsExtendedId = false;

            //Request Knock Retard PID
            ControllerAreaNetwork.Message reqKNKRET = new ControllerAreaNetwork.Message();
            reqKNKRET.ArbitrationId = 0x7E0;
            reqKNKRET.Data = new byte[] { 0x03, 0x22, 0x12, 0xD9, 0xAA, 0xAA, 0xAA, 0xAA };
            reqKNKRET.Length = 8;
            reqKNKRET.IsExtendedId = false;

            //Request Vehicle Speed Sensor (VSS) PID
            ControllerAreaNetwork.Message reqVSS = new ControllerAreaNetwork.Message();
            reqVSS.ArbitrationId = 0x7E0;
            reqVSS.Data = new byte[] { 0x03, 0x22, 0x00, 0x0D, 0xAA, 0xAA, 0xAA, 0xAA };
            reqVSS.Length = 8;
            reqVSS.IsExtendedId = false;

            //Request Throttle Position Sensor PID (Pedal only)
            ControllerAreaNetwork.Message reqTPS = new ControllerAreaNetwork.Message();
            reqTPS.ArbitrationId = 0x7E0;
            reqTPS.Data = new byte[] { 0x03, 0x22, 0x12, 0xD9, 0xAA, 0xAA, 0xAA, 0xAA };
            reqTPS.Length = 8;
            reqTPS.IsExtendedId = false;

            //Request Engine Coolant Temp [ECT]
            ControllerAreaNetwork.Message reqECT = new ControllerAreaNetwork.Message();
            reqECT.ArbitrationId = 0x7E0;
            reqECT.Data = new byte[] { 0x03, 0x22, 0x00, 0x05, 0xAA, 0xAA, 0xAA, 0xAA };
            reqECT.Length = 8;
            reqECT.IsExtendedId = false;

            //Request Intake Air Temp [IAT]
            ControllerAreaNetwork.Message reqIAT = new ControllerAreaNetwork.Message();
            reqIAT.ArbitrationId = 0x7E0;
            reqIAT.Data = new byte[] { 0x03, 0x22, 0x00, 0x0F, 0xAA, 0xAA, 0xAA, 0xAA };
            reqIAT.Length = 8;
            reqIAT.IsExtendedId = false;

            //Request E38 AFR DMA PID
            ControllerAreaNetwork.Message reqAFR_DMA = new ControllerAreaNetwork.Message();
            reqAFR_DMA.ArbitrationId = 0x7E0;
            reqAFR_DMA.Data = new byte[] { 0x07, 0x23, 0x00, 0x24, 0x23, 0xFA, 0x00, 0x04 };
            reqAFR_DMA.Length = 8;
            reqAFR_DMA.IsExtendedId = false;

            //Send Flow Control
            flowControl.ArbitrationId = 0x7E0;
            flowControl.Data = new byte[] { 0x30, 0x00, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA };
            flowControl.Length = 3;
            flowControl.IsExtendedId = false;

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
                //Change screens using slide if button pressed
                if (BarGraph == true)
                    if (Glide.MainWindow == window) { Tween.SlideWindow(window, window2, Direction.Left); }
                    else { }

                if (BarGraph == false)
                    if (Glide.MainWindow == window2) { Tween.SlideWindow(window2, window, Direction.Right); }
                    else { }

                if (IsS1) //Nav RGBS input enable packet for Series1
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

                //CAN Request Timers
                if (((TimeNow[ModuleTimers.RPM] - LastTime[ModuleTimers.RPM]) / TimeSpan.TicksPerMillisecond) > 25)
                {
                    can2.SendMessage(reqRPM);
                    LastTime[ModuleTimers.RPM] = TimeNow[ModuleTimers.RPM];
                }
                else
                {
                    TimeNow[ModuleTimers.RPM] = System.DateTime.Now.Ticks;
                }
                if (((TimeNow[ModuleTimers.MAP] - LastTime[ModuleTimers.MAP]) / TimeSpan.TicksPerMillisecond) > 25)
                {
                    can2.SendMessage(reqMAP);
                    LastTime[ModuleTimers.MAP] = TimeNow[ModuleTimers.MAP];
                }
                else
                {
                    TimeNow[ModuleTimers.MAP] = System.DateTime.Now.Ticks;
                }
                if (((TimeNow[ModuleTimers.Spark] - LastTime[ModuleTimers.Spark]) / TimeSpan.TicksPerMillisecond) > 25)
                {
                    can2.SendMessage(reqSpark);
                    LastTime[ModuleTimers.Spark] = TimeNow[ModuleTimers.Spark];
                }
                else
                {
                    TimeNow[ModuleTimers.Spark] = System.DateTime.Now.Ticks;
                }
                if (((TimeNow[ModuleTimers.ECT] - LastTime[ModuleTimers.ECT]) / TimeSpan.TicksPerMillisecond) > 5000)
                {
                    can2.SendMessage(reqECT);
                    LastTime[ModuleTimers.ECT] = TimeNow[ModuleTimers.ECT];
                }
                else
                {
                    TimeNow[ModuleTimers.ECT] = System.DateTime.Now.Ticks;
                }
                if (((TimeNow[ModuleTimers.IAT] - LastTime[ModuleTimers.IAT]) / TimeSpan.TicksPerMillisecond) > 3000)
                {
                    can2.SendMessage(reqIAT);
                    LastTime[ModuleTimers.IAT] = TimeNow[ModuleTimers.IAT];
                }
                else
                {
                    TimeNow[ModuleTimers.IAT] = System.DateTime.Now.Ticks;
                }

                //Gauge Updates
                if (BarGraph == true)
                {
                    SlantGauge1.Value = RPM;
                    oldRPM = RPM;
                }
                else
                {
                    AnaGauge1.Value = RPM;
                    oldRPM = RPM;
                }

                if (BarGraph == true)
                {
                    SlantGauge2.Value = MAP;
                    oldMAP = MAP;
                }
                else
                {
                    AnaGauge2.Value = MAP;
                    oldMAP = MAP;
                }

                if (BarGraph == true)
                {
                    SlantGauge3.Value = SPKAdv;
                    oldSPKAdv = SPKAdv;
                }
                else
                {
                    AnaGauge3.Value = SPKAdv;
                    oldSPKAdv = SPKAdv;
                }

                if (BarGraph == true)
                {
                }
                else
                {
                    AnaGauge4.Value = ECT;
                    oldECT = ECT;
                }

                if (BarGraph == true)
                { }
                else
                {
                    AnaGauge4.Value = IAT;
                    oldIAT = IAT;
                }
                
                if (BarGraph == true)
                { }
                else
                {
                    // AnaGauge5.Value = ETH;
                    oldETH = ETH;
                }            
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
                        if (X < 100)
                            BarGraph = false;
                        if (X > 300)
                            BarGraph = true;

                    }
                }
                if (received.ArbitrationId == 0x102E0080)   //Nav buttons pressed on S1
                {
                    if (received.Data[2] == 0x01) //Button 1 Pressed
                    {
                        BarGraph = false;
                    }
                    if (received.Data[2] == 0x02) //Button 2 Pressed
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
                    oldMAP = MAP;
                    oldETH = ETH;
                    oldSPKAdv = SPKAdv;
                    oldVSS = VSS;
                    oldBoost = Boost;
                    oldAFR = AFR;
                    oldKR = KR;
                    
                    firstrun = false;
                }

                if (received.ArbitrationId == 0x7E8)
                {
                    if (received.Data[0] == 0x10)                               //Send flow control packet           
                        sender.SendMessage(flowControl);
                    if ((received.Data[0] == 0x10) && (received.Data[2] == 0x62)) //Mode23 DMA PID Frame 1
                        AFR = (float)received.Data[7] * (float)0.125;
                    if(received.Data[1] == 0x62)                                //Check is Mode22 Response
                    {
                        //if (received.Data[3] == 0x03)                         //Fuel System Status [CL or OL] Bit Encoded
                        //    FuelStatus = (received.Data[4];
                        if ((received.Data[2] == 0x00)&&(received.Data[3] == 0x05))                       //Engine Coolant Temp [ECT]
                            ECT = received.Data[4] - 40;
                        if ((received.Data[2] == 0x00)&&(received.Data[3] == 0x0B))                       //Manifold Pressure
                        {
                            MAP = (received.Data[4] + 14);
                            Boost = (int)(MAP * 0.145 - 14.5);                                            //Convert to Boost PSI
                        }
                        if ((received.Data[2] == 0x00)&&(received.Data[3] == 0x0C))                       //Engine Speed [RPM]
                            RPM = (((received.Data[4] * 0x100) + received.Data[5]) / 4);
                        if ((received.Data[2] == 0x00)&&(received.Data[3] == 0x0D))                       //Vehicle Speed [VSS]
                            VSS = received.Data[4];
                        if ((received.Data[2] == 0x00)&&(received.Data[3] == 0x0E))                       //Spark Advance
                            SPKAdv = (received.Data[4] / 2) - 64;
                        if ((received.Data[2] == 0x00)&&(received.Data[3] == 0x0F))                       //Intake Air Temp [IAT]
                            IAT = received.Data[4] - 40;
                        if ((received.Data[2] == 0x12)&&(received.Data[3] == 0xD9))                       //Knock Retard [KR]
                            KR = System.Math.Round(((double)received.Data[4] * 0.17578)*100.0)/100.0; 
                    }
                }
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

