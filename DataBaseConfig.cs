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


namespace NETMFBook1
{
    public class DataBaseConfig
    {
     static      AutoResetEvent evt = new AutoResetEvent(false);
     static MassStorage usb_storage;

      public struct ScreenConfig
        {
            public int RadioType;
            public int DefaultWindow;
            public int BackgroundStatus;
            public int PID_BigLeft;
            public int PID_BigRight;
            public int PID_LittleLeft;
            public int PID_LittleRight;
            public int PID_TopBar;
            public int PID_MiddleBar;
            public int PID_BottomBar;
        }

      public struct GaugeConfig
        {
            public int ID;
            public string GaugeTitle;
            public int MaxValue;
            public int MinValue;
            public int UpdateRate;
            public string Units;
            public int ARBFilter;
        }


       public enum Gauge
      {
          BigLeft = 0,
          BigRight = 1,
          LittleLeft = 2,
          LittleRight = 3,
          TopBar = 4,
          MiddleBar = 5,
          BottomBar = 6
      }





     static public bool IsDataBaseAvailable(string rootDirectory)
        {
            try
            {
            
             //Throw fault if dosnt exist
                FileStream fileHandle = new FileStream(rootDirectory + @"\\config.dbs", FileMode.Open, FileAccess.Read);
                if (fileHandle.Length == 0) { return false; }
                fileHandle.Close();
                
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }


     static public bool CreateDefaultDatabase(string rootDirectory)
        {
            try
            {
              
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
                //  ResultSet result = myDatabase.ExecuteQuery("SELECT * FROM tblGauges WHERE ID = 2");

                // Get a copy of table data example
                //   ArrayList tabledata = result.Data;

                //object obj;
                //String row = "";
                //for (int j = 0; j < result.RowCount; j++)
                //{
                //    row = j.ToString() + " ";
                //    for (int i = 0; i < result.ColumnCount; i++)
                //    {
                //        obj = result[j, i];
                //        if (obj == null)
                //            row += "N/A";
                //        else
                //            row += obj.ToString();
                //        row += " |";
                //    }
                //    Debug.Print(row);
                //}
                myDatabase.Dispose();
              
                return true;
            }
            catch (Exception e)
            {
                return false;
            }

        }




     static public bool ReadConfig(string rootDirectory,ref ScreenConfig Config)
        {
            try
            {
               
                //Open database
                Database myDatabase = new Database(rootDirectory + @"\\config.dbs");

                //Select desired data
                ResultSet result = myDatabase.ExecuteQuery("SELECT * FROM tblConfig");


               //    ArrayList tabledata = result.Data;
            //    object obj = result[0, 0];
               //  Debug.Print(obj.ToString());

                 Config.RadioType = (int)Int32.Parse(result[0, 0].ToString());
                 Config.DefaultWindow = (int)Int32.Parse(result[0, 1].ToString());
                 Config.BackgroundStatus = (int)Int32.Parse(result[0, 2].ToString());
                 Config.PID_BigLeft = (int)Int32.Parse(result[0, 3].ToString());
                 Config.PID_BigRight = (int)Int32.Parse(result[0, 4].ToString());
                 Config.PID_LittleLeft = (int)Int32.Parse(result[0, 5].ToString());
                 Config.PID_LittleRight = (int)Int32.Parse(result[0, 6].ToString());
                 Config.PID_TopBar = (int)Int32.Parse(result[0, 7].ToString());
                 Config.PID_MiddleBar = (int)Int32.Parse(result[0, 8].ToString());
                 Config.PID_BottomBar = (int)Int32.Parse(result[0, 9].ToString());

                return true;
            }
            catch (Exception e)
            {
                return false;
            }

        }



     static public bool UpdateConfig(string rootDirectory,ScreenConfig Config)
        {
            try
            {
              

                //Open database
                Database myDatabase = new Database(rootDirectory + @"\\config.dbs");

                //Select desired data

                // ResultSet result = myDatabase.ExecuteQuery("Update * FROM tblConfig Limit 1");



                return true;
            }
            catch (Exception e)
            {
                return false;
            }

        }






     static public bool ReadPIDDatabase(string rootDirectory, ref ArrayList tabledata )
        {
            try
            {
              
                //Open database
                Database myDatabase = new Database(rootDirectory + @"\\config.dbs");

                //Select desired data
             //   ResultSet result = myDatabase.ExecuteQuery("SELECT * FROM tblGauges Where ID = " + SelectedGauge.ToString() + " Limit 1");
                 ResultSet result = myDatabase.ExecuteQuery("SELECT * FROM tblGauges");
               tabledata  = result.Data; 

               return true;
            }
            catch (Exception e)
            {
                return false;
            }

        }



     static public bool UpdateGauges()
        {
            try
            {
                return true;
            }
            catch (Exception e)
            {
                return false;
            }

        }


    }
}