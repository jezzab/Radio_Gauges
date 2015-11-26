using System;
using System.Threading;
using System.IO;
using System.Collections;
using Microsoft.SPOT;
using Microsoft.SPOT.IO;
using GHI.SQLite;
using GHI.Usb;
using GHI.Usb.Host;
using GHI.IO;
using GHI.IO.Storage;

public class Program
{
    public static AutoResetEvent evt = new AutoResetEvent(false);
    public static MassStorage usb_storage;
    public static string rootDirectory;

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

        Database myDatabase = new Database(rootDirectory + @"\\config.dbs");


          
        myDatabase.ExecuteNonQuery("CREATE Table tblGauges" +
        " (ID INTEGER, GaugeText TEXT, MaxValue INTEGER, MinValue INTEGER, UpdateRate INTEGER, Units TEXT, ARBFilter INTEGER)");
        //add rows to table
        myDatabase.ExecuteNonQuery("INSERT INTO tblGauges (ID, GaugeText, MaxValue, MinValue, UpdateRate, Units, ARBFilter)" +
        " VALUES (1,'RPM', 8000, 0, 25, 'RPM', 201)");
        myDatabase.ExecuteNonQuery("INSERT INTO tblGauges (ID, GaugeText, MaxValue, MinValue, UpdateRate, Units, ARBFilter)" +
        " VALUES (2,'Throttle', 100, 0, 25, '%', 201)");
        myDatabase.ExecuteNonQuery("INSERT INTO tblGauges (ID, GaugeText, MaxValue, MinValue, UpdateRate, Units, ARBFilter)" +
        " VALUES (3,'Coolant', 140, -40, 1000, 'oC', 1217)");
        myDatabase.ExecuteNonQuery("INSERT INTO tblGauges (ID, GaugeText, MaxValue, MinValue, UpdateRate, Units, ARBFilter)" +
        " VALUES (4,'Intake', 140, -40, 1000, 'oC', 1217)");
        myDatabase.ExecuteNonQuery("INSERT INTO tblGauges (ID, GaugeText, MaxValue, MinValue, UpdateRate, Units, ARBFilter)" +
        " VALUES (5,'Ethanol', 100, -0, 5000, '%', 1019)");
        myDatabase.ExecuteNonQuery("INSERT INTO tblGauges (ID, GaugeText, MaxValue, MinValue, UpdateRate, Units, ARBFilter)" +
        " VALUES (6,'MAP', 210, 0, 25, 'KPA', 2024)");

        myDatabase.ExecuteNonQuery("CREATE Table tblConfig" +
        " (RadioType INTEGER, DefaultWindow INTEGER, Background INTEGER, BigLeft INTEGER, BigRight INTEGER, LittleLeft INTEGER, LittleMiddle INTEGER, LittleRight INTEGER, TopBar INTEGER, MiddleBar INTEGER, BottomBar, INTEGER)");
        //add row to table
        myDatabase.ExecuteNonQuery("INSERT INTO tblConfig (RadioType, DefaultWindow, Background, BigLeft, BigRight, LittleLeft, LittleMiddle, LittleRight, TopBar, MiddleBar, BottomBar)" +
" VALUES (0,0,1,1,2,3,4,5,1,2,3)");
        

        // Process SQL query and save returned records in SQLiteDataTable
        ResultSet result = myDatabase.ExecuteQuery("SELECT * FROM tblGauges WHERE ID = 2");
        // Get a copy of table data example
        ArrayList tabledata = result.Data;

        object obj;
        String row = "";
        for (int j = 0; j < result.RowCount; j++)
        {
            row = j.ToString() + " ";
            for (int i = 0; i < result.ColumnCount; i++)
            {
                obj = result[j, i];
                if (obj == null)
                    row += "N/A";
                else
                    row += obj.ToString();
                row += " |";
            }
            Debug.Print(row);
        }
        myDatabase.Dispose();
        usb_storage.Unmount();
        while (true) { } 
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

}