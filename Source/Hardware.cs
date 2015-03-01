/*
 * Copyright 2012, 2013, 2014, John Jore
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

/*
 * All Hardware related functions
*/

//New CF_Actions.PAUSE and CF_Actions.PLAY (directly play and pause), 
//http://www.mp3car.com/centrafuse/146940-centrafuse-3-6-official-announcement-and-release-notes.html


namespace DABFMMonkey
{
    using System;
    using System.Runtime.InteropServices;
    using centrafuse.Plugins;
    using System.Management;
    using System.Data;
    using System.Drawing;
    using System.Net;
    using System.Windows.Forms;
    using System.Collections.Generic;

    public partial class DABFMMonkey
    {
        #region Variables
        private const Int16 constBufferSize = 400; //Buffersize for monkeyboard's string operations

        private const string DABFMMonkeydll = @"keystonecomm.dll";
        private const string DABXMLConfigFile = PluginPathLanguages + @"DABFMMonkey.xml";
       
        // Used for radio commands. Variable is named after DLL function or button function
        //private MonkeyCommand RadioCommand = MonkeyCommand.NONE; //Radio Command. Default is no command
        Queue<MonkeyCommand> RadioCommand = new Queue<MonkeyCommand>();
             
        //Global Variables for RadioCommand's
        RADIO_TUNE_BAND intDABFMMode = RADIO_TUNE_BAND.UNDEFINED;               // Current radio mode
        private UInt32 intCurrentStation = 999;                                 // Current/Active station
        private int intSignalStrength = -1;                                     // Current Signal strength
        private RADIO_TUNE_BAND intNewDABFMMode = RADIO_TUNE_BAND.DAB_BAND;      // Set to this mode
        private UInt32 intNewStation = 999;                                     // Set to this station (FM mode)
        private string strNewDABLongName = "";                                  // Set to this station (DAB mode)
        DABStatus intPlayStatus = DABStatus.Unknown;                            // Unknown board status
        UInt32 intTotalProgram = 999;                                           // Total channels on board
        private Mode STEREOMODE = Mode.UNDEFINED;                               // Auto or forced mono
        private bool boolEnableInternetUsage = false;                           // Allow Internet connectivity
        private bool boolEnableRadioVIS = false;                                // By default, don't use RadioVIS
        private static bool init = false;                                       // Is Radio initialized? Default is not initialized
        //private bool boolClearBoardBeforeScan = false;                        // Clear board's existing programs when performing a DAB scan?
        
        //RDS information
        private string strCachedProgramText = "";                               // Used to cache the ProgramText
        private string _stationText = "";
        private string _stationName = "";
       
        string DABFMMonkeyCOMPort = "";                                         //Used for COM Port override of autodetection

        // Read from XML Config file, DABXMLConfigFile. These are default values if we can't read from XML
        private Volume DABFMMonkeyVolume = Volume.Max;                          // min=0 / max=16
        private Volume DABFMMonkeyATTVolume = Volume.Max;                       // ATT volume level
        private string DABFMMonkeyUSBVID = "VID_04D8";        
        private string[] aryDABFMMonkeyUSBPID = new string[] {"PID_000A", "PID_F6E0"};
        private string[] aryProgramType = new string[] { "N/A","News","Current Affairs","Information","Sport","Education","Drama","Arts","Science","Talk",
            "Pop","Rock","Easy Listening","Light Classical","Classical","Other","Weather","Finance","Children's","Factual",
            "Religion","Phone In","Travel","Leisure","Jazz and Blues","Country","National","Oldies","Folk","Documentary","Alarm Test","Alarm" };
        private string[] aryDABStatus = new string[] { "Playing", "Searching", "Tuning", "Stop", "Sorting", "Reconfiguring" };
        private string[] aryDABChannelsLong = new string[] { "N/A" }; // Name of DAB channels stored on board (Long Name)

        public static bool boolDABFMMonkeyChinaMode = false; // By default, dont enable China DAB channels
        public static bool boolDABFMMonkeyLBandMode = false; // By default, dont enable LBand mode
        private byte DABFMMonkeyScanStartIndex, DABFMMonkeyScanEndIndex; // Start and End for DAB frequency Scanning
        private string DABFMMonkeyRegion = "";
        private DABNameMode boolDABLongName = DABNameMode.Short;

        //BBE / EQ Variable
        BBEEQ _BBEEQ = new BBEEQ();
                    
        //Default from config file, config.xml in users folder.
        private UInt32 intLASTDAB = 0;
        private UInt32 intLASTFM = 100000; 
        private UInt32 DABRadioChannel = 100000;
        private bool boolDABFMMonkeyReScan = false; // By default, don't rescan DAB channels
        private bool boolEnablePlugin = true; //By default, use the plugin to control the radio

        //Maps 0 to 40 for standard DAB frequencies
        public static string[] DABStandardFrequencyIndex = new string[] { "5A","5B","5C","5D","6A","6B","6C","6D","7A","7B","7C","7D","8A","8B","8C","8D","9A","9B","9C","9D","10A","10N","10B","10C","10D","11A","11N","11B","11C","11D","12A","12N","12B","12C","12D","13A","13B","13C","13D","13E","13F" };
        public static Double[] DABStandardFrequencyMHz = new double[] { 174.928, 176.64, 178.352, 180.064, 181.936, 183.648, 185.360, 187.072, 188.928, 190.64, 192.352, 194.064, 195.936, 197.648, 199.36, 201.072, 202.928, 204.64, 206.352, 208.064, 209.936, 210.096, 211.648, 213.36, 215.072, 216.928, 217.088, 218.64, 220.352, 222.064, 223.936, 224.096, 225.648, 227.36, 229.072, 230.784, 232.496, 234.208, 235.776, 237.488, 239.2 };

        // Maps 41 to 71 for China mode. All use of this must add 41 to the index
        public static string[] DABChinaFrequencyIndex = new string[] { "6A", "6B", "6C", "6D", "6N", "7A", "7B", "7C", "7D", "8A", "8B", "8C", "8D", "8N", "9A", "9B", "9C", "9D", "10A", "10B", "10C", "10D", "10N", "11A", "11B", "11C", "11D", "12A", "12B", "12C", "12D" };
        public static Double[] DABChinaFrequencyMHz = new double[] { 168.160, 169.872, 171.584, 173.296, 175.008, 176.720, 178.432, 180.144, 181.856, 184.160, 185.872, 187.584, 189.296, 191.008, 192.720, 194.432, 196.144, 197.856, 200.160, 201.872, 203.584, 205.296, 207.008, 208.720, 210.432, 212.144, 213.856, 216.432, 218.144, 219.856, 221.568 };

        // Maps 72 to 94 for L-Band. All use of this must add 72 to the index
        public static string[] DABLBandFrequencyIndex = new string[] { "LA", "LB", "LC", "LD", "LE", "LF", "LG", "LH", "LI", "LJ", "LK", "LL", "LM", "LN", "LO", "LP", "LQ", "LR", "LS", "LT", "LU", "LV", "LW" };
        public static Double[] DABLBandFrequencyMHz = new double[] { ﻿1452.960, 1454.672, 1456.384, 1458.096, 1459.808, 1461.520, 1463.232, 1464.944, 1466.656, ﻿1468.368, 1470.080, 1471.792, 1473.504, 1475.216, 1476.928, 1478.640, 1480.352, 1482.064, 1483.776, 1485.488, 1487.200, 1488.912, 1490.624 };

        # endregion

#region DLL Imports
        // Clear the DAB programs stored in the module's database
        [DllImport(DABFMMonkeydll)]
        private static extern bool ClearDatabase();

        //Deprecated?
        [DllImport(DABFMMonkeydll)]
        private static extern Int32 CommVersion();

        // Open the COM port of the radio and set mute behavior.
        [DllImport(DABFMMonkeydll)]
        private static extern bool OpenRadioPort(string com_port, bool usehardmute);

        // Hard reset the radio module by pulling the RESET pin LOW        
        [DllImport(DABFMMonkeydll)]
        private static extern bool HardResetRadio();

        // Close the COM port of the radio
        [DllImport(DABFMMonkeydll)]        
        private static extern bool CloseRadioPort();

        // Play radio stream in FM or DAB
        [DllImport(DABFMMonkeydll)]
        private static extern bool PlayStream(RADIO_TUNE_BAND mode, UInt32 channel);

        // Stop currently played FM or DAB stream
        [DllImport(DABFMMonkeydll)]
        private static extern bool StopStream();

        // Set the volume of the radio
        [DllImport(DABFMMonkeydll)]
        private static extern bool SetVolume(Volume volume);

        // Add one volume step to the current volume.
        [DllImport(DABFMMonkeydll)]
        private static extern sbyte VolumePlus();

        // Minus one volume step from the current volume
        [DllImport(DABFMMonkeydll)]
        private static extern sbyte VolumeMinus();

        // Mute the volume
        [DllImport(DABFMMonkeydll)]
        private static extern void VolumeMute();

        // Get the current volume
        [DllImport(DABFMMonkeydll)]
        private static extern Volume GetVolume();

        // Determine if the current mode is DAB or FM
        [DllImport(DABFMMonkeydll)]
        private static extern RADIO_TUNE_BAND GetPlayMode();

        // Forward to the next available stream in the current mode.  When radio is in DAB mode, the dabindex will be incremented and then played.
        // When the radio is in FM mode, search by increasing the FM frequency until a channel is found
        [DllImport(DABFMMonkeydll)]
        private static extern bool NextStream();

        // Backward to the previous available stream in the current mode.  When radio is in DAB mode, the dabindex will be decremented and then played.
        // When the radio is in FM mode, search by decresing the FM frequency until a channel is found.
        [DllImport(DABFMMonkeydll)]
        private static extern bool PrevStream();

        // Get the signal strengh of the current playing stream. biterror can't be utilized at this point in time
        [DllImport(DABFMMonkeydll)]
        private static extern sbyte GetSignalStrength(ref int biterror);

        // Get the current playing program type to be used to identify the genre
        [DllImport(DABFMMonkeydll)]
        private static extern sbyte GetProgramType(RADIO_TUNE_BAND mode, UInt32 dabIndex);

        // Get the RDS text of the current stream
        [DllImport(DABFMMonkeydll, CharSet = CharSet.Unicode)]
        private static extern sbyte GetProgramText(string strtextBuffer);

        // Get the current DAB data rate
        [DllImport(DABFMMonkeydll)]
        private static extern Int16 GetDataRate();
        
        // Get the ensemble name of the current program
        [DllImport(DABFMMonkeydll, CharSet = CharSet.Unicode)]
        private static extern bool GetEnsembleName(UInt32 dabIndex, DABNameMode namemode, string programName);

        // Get the preset DAB index or preset FM frequency.  The module is able to store 10 DAB and 10 FM preset
        [DllImport(DABFMMonkeydll, CharSet = CharSet.Unicode)]
        private static extern Int32 GetPreset(sbyte mode, sbyte presetindex);

        // Store program into preset location.
        [DllImport(DABFMMonkeydll, CharSet = CharSet.Unicode)]
        private static extern bool SetPreset(sbyte mode, sbyte presetindex, UInt32 channel);

        // Get the stereo reception status of the current playing stream
        [DllImport(DABFMMonkeydll)]
        private static extern sbyte GetStereo();

        // Get the index of current playing DAB stream or the current playing frequency
        [DllImport(DABFMMonkeydll)]
        private static extern UInt32 GetPlayIndex();

        // Get the currenct DAB frequency index in while DAB is auto searching
        [DllImport(DABFMMonkeydll)]
        private static extern sbyte GetFrequency();

        // Determine if the current radio status is playing, searching, tuning, stop sorting or reconfiguring
        [DllImport(DABFMMonkeydll, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern DABStatus GetPlayStatus();

        // Set radio to forced mono or auto detect stereo mode.
        [DllImport(DABFMMonkeydll)]
        private static extern bool SetStereoMode(Mode Mode);

        // Get the current stereo mode in the radio configuration
        [DllImport(DABFMMonkeydll)]
        private static extern Mode GetStereoMode();

        // Auto search DAB channels.  Current stored DAB channels will be cleared
        [DllImport(DABFMMonkeydll, CharSet = CharSet.Unicode)]
        private static extern bool DABAutoSearch(byte startindex, byte endindex);

        // Auto search DAB channels.  Current stored DAB channels will NOT be cleared
        [DllImport(DABFMMonkeydll)]
        private static extern bool DABAutoSearchNoClear(byte startindex, byte endindex);

        // Get the total number of DAB programs stored in the module
        [DllImport(DABFMMonkeydll)]
        private static extern UInt32 GetTotalProgram();

        // Check if the module is ready to receive command.
        [DllImport(DABFMMonkeydll)]
        private static extern bool IsSysReady();

        // Get the name of the current program
        [DllImport(DABFMMonkeydll, CharSet = CharSet.Unicode)]
        private static extern bool GetProgramName(RADIO_TUNE_BAND mode, UInt32 dabIndex, DABNameMode namemode, String programName);

        // Get the Service Component ID, Service ID and Ensemble ID for particular DAB station.
        [DllImport(DABFMMonkeydll)]
        private static extern bool GetProgramInfo(UInt32 dabIndex, ref Byte ServiceComponentID, ref UInt32 ServiceID, ref UInt16 EnsembleID);

        // Get the type of MOT application of the specified DAB channel.
        [DllImport(DABFMMonkeydll)]
        private static extern SByte GetServCompType(UInt32 dabIndex);
#endregion

#region 2nd Generation (Pro boards) DLL Imports

        //Get the type of MOT application of the specified DAB channel.
        //Return Values: 0 is MOT SlideShow, 1 is MOT BWS, 2 is TPEG, 3 is DGPS, 4 is TMC, 5 is EPG, 6 is DAB Java, 7 is DMB, 8 is Push Radio.
        [DllImport(DABFMMonkeydll)]
        private static extern ApplicationType GetApplicationType(UInt32 channel);
        
        //Get the filename of the SlideShow image.
        [DllImport(DABFMMonkeydll, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GetImage(string ImageFileName);

        //Query radio module for MOT data
        [DllImport(DABFMMonkeydll)]
        private static extern bool MotQuery();

        //Reset the MOT segment buffer
        [DllImport(DABFMMonkeydll)]
        private static extern void MotReset(MotMode mode);

        //Deprecated function as of December 2012
        //Get the signal quality of the current DAB channel.
        [DllImport(DABFMMonkeydll)]
        private static extern sbyte GetDABSignalQuality();

        //Set BBE HD Sound or Preset EQ
        [DllImport(DABFMMonkeydll)]
        private static extern bool SetBBEEQ(BBEStatus BBEOn, SByte EQMode, SByte BBELo, SByte BBEHi, SByte BBECFreq, Byte BBEMachFreq, SByte BBEMachGain, SByte BBEMachQ, SByte BBESurr, SByte BBEMp, Byte BBEHpF, SByte BBEHiMode);

        //Get parameters of BBE HD Sound or Mode of EQ.
        [DllImport(DABFMMonkeydll)]
        private static extern bool GetBBEEQ(ref BBEStatus BBEOn, ref SByte EQMode, ref SByte BBELo, ref SByte BBEHi, ref SByte BBECFreq, ref Byte BBEMachFreq, ref SByte BBEMachGain, ref SByte BBEMachQ, ref SByte BBESurr, ref SByte BBEMp, ref Byte BBEHpF, ref SByte BBEHiMode);

        //Set audio headroom.
        [DllImport(DABFMMonkeydll)]
        private static extern bool SetHeadroom (sbyte headroom);

        //Get the headroom volume
        [DllImport(DABFMMonkeydll)]
        private static extern sbyte GetHeadroom();

#endregion

        /// <summary>
        ///		Autodetect COM port
        /// </summary>
        /// <return>
        ///		Returns COM Port (Windows format), or empty if not found.
        /// </return>
        //Thread Safe. Only Called during init
        private string SetupCOMPortInformation()
        {
            WriteLog("SetupCOMPortInformation() - start");

            try
            {
                ManagementObjectSearcher searcherPNP = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity");
                foreach (ManagementObject mo in searcherPNP.Get())
                {
                    string name = mo["Name"].ToString();
                    // Name will have a substring like "(COM12)" in it.
                    if (name.Contains("(COM"))
                    {
                        try
                        {
                            string HardwareID = mo["DeviceID"].ToString();
                            WriteLog("Enumerating USBCOM Devices: '" + name + "' HardwareID: '" + HardwareID + "'");

                            //There can be multiple PIDs, search for them all
                            for (int j = 0; j < aryDABFMMonkeyUSBPID.Length; j++)
                            {
                                string DABFMMonkeyUSBVIDPID = "USB\\" + DABFMMonkeyUSBVID + "&" + aryDABFMMonkeyUSBPID[j];
                                WriteLog("Looking for: '" + DABFMMonkeyUSBVIDPID + "'");

                                if (HardwareID.IndexOf(DABFMMonkeyUSBVIDPID) == 0)
                                {
                                    try
                                    {
                                        WriteLog("Match. Found MonkeyBoard on: '" + name + "'");
                                        string strPort = name.Substring(name.LastIndexOf("(") + 1, name.LastIndexOf(")") - name.LastIndexOf("(") - 1);

                                        //Convert port to Windows format
                                        strPort = "\\\\.\\" + strPort;

                                        WriteLog("COM Port '" + strPort + "'");

                                        // Return port name
                                        return strPort;
                                    }
                                    catch (Exception ex) { WriteLog("FoundMatch, but failed: '" + ex.Message); }
                                }
                                else WriteLog("No Match on: " + name);
                            }
                        }
                        catch (Exception ex) { WriteLog("Processing COM port failed: '" + ex.Message); }
                    }
                }
            }
            catch (Exception ex) { WriteLog("SetupCOMPortInformation failed: '" + ex.Message); }


            WriteLog("SetupCOMPortInformation() - end");
            return "";
        }

        /// <summary>
        ///		Initialize radio.
        /// </summary>
        /// <return>
        ///		Returns if initialization is successful.
        /// </return>
        //Thread Safe. Only Called during init or main thread
        private bool InitializeRadio()
        {
            WriteLog("InitializeRadio() - start");            

            //Find the COM port the DABFMMonkey is using, if we're not using override from XML
            if (DABFMMonkeyCOMPort == "\\\\.\\")
            {
                DABFMMonkeyCOMPort = SetupCOMPortInformation();
            }
            else
            {
                WriteLog("COM Port Override Detected. Will use '" + DABFMMonkeyCOMPort + "'. Format is COM4 or COM22");
            }

            //Exit if Monkeyboard not found
            if (DABFMMonkeyCOMPort == "")
            {
                try
                {
                    WriteLog("Monkeyboard not found on any COM port and no override detected");
                    this.CF_systemDisplayDialog(CF_Dialogs.OkBox, base.pluginLang.ReadField("/APPLANG/SETUP/BOARDNOTFOUND"));
                }
                catch (Exception errmsg)
                {
                    WriteLog("Failed to find COM Port, " + errmsg.ToString());
                }
                
                return false;
            }
            WriteLog("Will initialize radio using: '" + DABFMMonkeyCOMPort + "'");

            try
            {
                init = OpenRadioPort(DABFMMonkeyCOMPort, false);
                if (!init)
                {
                    WriteLog("OpenRadioPort(" + DABFMMonkeyCOMPort + ") Failed, retrying 3 times...");
                    for (int i = 1; i <= 3 && init == false; i++)
                    {
                        WriteLog("OpenRadioPort(" + DABFMMonkeyCOMPort + ") Retry " + i.ToString() + "...");
                        init = OpenRadioPort(DABFMMonkeyCOMPort, true);
                    }
                }

                WriteLog("init = " + init.ToString());
                if (init)
                {
                    // CommVersion in use?
                    if (WaitForBoard()) WriteLog("Version: " + CommVersion().ToString());

                    // Set the volume
                    if (WaitForBoard()) WriteLog("Set Volume completed: '" + SetBoardVolume(DABFMMonkeyVolume) + "'");

                    // Set the mode
                    if (SetStereoMode(STEREOMODE))
                    {
                        Mode tmpSTEREOMODE = GetStereoMode();
                        if (tmpSTEREOMODE == STEREOMODE) WriteLog("Mode validated, '" + STEREOMODE + "'"); else WriteLog("Failed to set mode, '" + STEREOMODE + "'");
                    }
                    else
                    {
                        WriteLog("Failed to set Stero mode");
                    }

                    // Set BBE/EQ mode
                    StoreBBEEQ();
                }
            }
            catch (Exception errmsg)
            {
                WriteLog("Failed to Initialize Radio, SetVolume or SetMode, " + errmsg.ToString());
            }


            //Let the user know we didn't find the board
            if (!init)
            {
                WriteLog("Monkeyboard not initialized");
                this.CF_systemDisplayDialog(CF_Dialogs.OkBox, base.pluginLang.ReadField("/APPLANG/SETUP/BOARDNOTINITIALIZED"));
                return false;
            }

            WriteLog("InitializeRadio() - end");
            return init;
        }

        /// <summary>
        ///		Shutdown radio.
        /// </summary>
        //Thread Safe. Terminates threads prior to commands
        private void ShutdownRadio()
        {
            WriteLog("ShutdownRadio() - start");
            
            try
            {
                //Only shutdown if board is initialized
                if (init)
                {
                    //Stop the threads
                    newboolSCANFM = false;
                    boolIOCommsThread = false;

                    //Abort if running
                    if (threadIOComms != null)
                    {
                        this.threadIOComms.Abort();
                        this.threadIOComms = null;
                    }

                    //Save current status
                    //SaveCurrentStatus();

                    //Stop everything
                    if (WaitForBoard()) if (StopStream() == false) WriteLog("Error stopping stream"); else WriteLog("Stream Stopped");
                    if (WaitForBoard()) if (CloseRadioPort() == false) WriteLog("Error closing radio port"); else WriteLog("Radio port closed");

                    //Mixer
                    if (!(_isBufferRadio || !(CF_params.Media.recordDevice != "")))
                    {
                        this.WriteLog("CF_pluginClose():  Calling CF_setMixerMute(true)");
                        base.CF_setMixerMute(CF_params.Media.recordDevice, CF_params.Media.recordLine, false, true);
                    }

                    // Radio no longer initialized
                    try
                    {
                        this.BeginInvoke(new MethodInvoker(delegate { init = false; }));
                    }
                    catch (Exception ex)
                    {
                        WriteLog("Failed to call invoke, " + ex.ToString());
                    }
                }
            }
            catch (Exception errmsg)
            {
                WriteLog("Failed to ShutdownRadio(), " + errmsg.ToString());
            }
            
            WriteLog("ShutdownRadio() - end");
        }

        //Save current: status; FM or DAB, 
        //              active Freq/Channel
        //              Forced Mono or Auto mode
        //              Save BBE/EQ settings
        private void SaveCurrentStatus()
        {
            WriteLog("SaveCurrentStatus() - start");
            //Before stopping audio, get mode and channel/freq so we can start (PlayStream) the same band and channel
            if ((intCurrentStation != 999) && (intDABFMMode == RADIO_TUNE_BAND.DAB_BAND || intDABFMMode == RADIO_TUNE_BAND.FM_BAND))
            {
                WriteLog("Save current mode and channel: '" + intDABFMMode.ToString() + "' '" + intCurrentStation.ToString() + "'");
                base.pluginConfig.WriteField("/APPCONFIG/ACTIVEBAND", intDABFMMode.ToString(), true);

                switch (intDABFMMode)
                {
                    case RADIO_TUNE_BAND.DAB_BAND:
                        if (intCurrentStation != 999)
                        {
                            WriteLog("Save DAB channel: '" + intCurrentStation.ToString() + "'");
                            base.pluginConfig.WriteField("/APPCONFIG/LASTDAB", intCurrentStation.ToString(), true);
                        }
                        break;
                    case RADIO_TUNE_BAND.FM_BAND:
                        if (intCurrentStation != 999)
                        {
                            WriteLog("Save FM channel: '" + intCurrentStation.ToString() + "'");
                            base.pluginConfig.WriteField("/APPCONFIG/LASTFM", intCurrentStation.ToString(), true);
                        }
                        break;
                    default:
                        WriteLog("Failed to save last used frequency/mode: '" + intDABFMMode.ToString() + "' '" + intCurrentStation.ToString() + "'");
                        break;
                }

                //Write auto or forced mono
                this.pluginConfig.WriteField("/APPCONFIG/STEREOMODE", STEREOMODE.ToString(), true);

                //Save the BBE Values to disk
                SaveBBEEQ(_BBEEQ);
            }
            else WriteLog("Failed to save last used frequency/mode: '" + intDABFMMode.ToString() + "' '" + intCurrentStation.ToString() + "'");

            WriteLog("SaveCurrentStatus() - end");
        }

        //Return False if board is not ready, will wait upto ? before giving up, else returns True if board is ready for next command
        private bool WaitForBoard()
        {
            // If board is ready, exit now
            if (init && IsSysReady()) return true;

            //Loop until it is ready
            bool sysReady = false;
            for (int i = 1; i <= 10 && sysReady == false; i++)
            {
                WriteDebug("Waiting for Board to be ready: '" + i.ToString() + "...");
                System.Threading.Thread.Sleep(200);
                this.BeginInvoke(new MethodInvoker(delegate { init = sysReady = IsSysReady(); }));
            }

            //If still not initialized, let the user know
            if (!init) this.CF_systemCommand(CF_Actions.SHOWINFO, base.pluginLang.ReadField("/APPLANG/SETUP/BOARDLOSTCONTACT"), "AUTOHIDE");
            
            WriteDebug("Status: '" + init.ToString() + "'");
            return (init);
        }

        //Set, and validate, volume
        //Thread Safe. Only Called during init or main thread
        private bool SetBoardVolume(Volume intVolume)
        {
            WriteLog("SetBoardVolume() - start");
            Volume intVolumeValidate = Volume.Min;
            bool boolSetBoardVolume = true;

            if (init && WaitForBoard())
            {
                if (SetVolume(intVolume))
                {
                    WriteLog("Volume set success '" + intVolume.ToString() + "'");

                    if (WaitForBoard()) intVolumeValidate = GetVolume();
                    if (intVolumeValidate == intVolume)
                    {
                        WriteLog("Volume level is validated.");
                    }
                    else
                    {
                        boolSetBoardVolume = false;
                        WriteLog("Warning: Volume level is wrong: '" + intVolume.ToString() + "' / '" + intVolumeValidate.ToString() + "'");
                    }                
                }
                else
                {
                    WriteLog("Volume set failed: '" + intVolume.ToString() + "' / '" + intVolumeValidate.ToString() + "'");
                    boolSetBoardVolume = false;
                }
            }
            else boolSetBoardVolume = false;

            WriteLog("SetBoardVolume() - end");
            return boolSetBoardVolume;
        }

        //Thread Safe. Only Called during init or main thread
        private bool ScanDAB()
        {
            WriteLog("DAB Rescan starting, " + DABFMMonkeyScanStartIndex.ToString() + " " + DABFMMonkeyScanEndIndex.ToString());

            bool sysReady = false;
            if (WaitForBoard())
            {
                ClearRDSVars(); // Clear the text fields before setting the new value (and performs HideSLS)

                _stationName = "Scanning DAB Channels";
                _stationText = "";

                // Display a status box that will autohide
                // This is known to fail on several CF 4.x builds. Afaik, this was fixed in the CF 4.3 release version (not betas)
                this.CF_systemCommand(CF_Actions.SHOWINFO, base.pluginLang.ReadField("/APPLANG/SETUP/STARTSCANNING"), "AUTOHIDE");

                /**/ //Figure out a good use for this in the future
                // Start the Scan
                //if (boolClearBoardBeforeScan)
                //{
                    WriteLog("Clear Board for new DAB scan");
                    DABAutoSearch(DABFMMonkeyScanStartIndex, DABFMMonkeyScanEndIndex);
                //}
                /*else
                {
                    WriteLog("Do NOT clear Board for new DAB scan");
                    DABAutoSearchNoClear(DABFMMonkeyScanStartIndex, DABFMMonkeyScanEndIndex);
                }*/

                // Display a status box that will autohide
                // This is known to fail on several CF 4.x builds. Afaik, this was fixed in the CF 4.3 release version (not betas)
                this.CF_systemCommand(CF_Actions.SHOWINFO, base.pluginLang.ReadField("/APPLANG/SETUP/STARTSCANNING"), "AUTOHIDE");

                //loop until it is ready
                while (sysReady == false)
                {
                    intPlayStatus = GetPlayStatus(); // "Playing", "Searching", "Tuning", "Stop", "Sorting", "Reconfiguring" };
                    if (intPlayStatus == DABStatus.Searching || intPlayStatus == DABStatus.Unknown) // If not Searching or Unknown
                    {
                        //WriteLog("Waiting for Board to complete scanning: '" + i.ToString() + "... Status: " + intPlayStatus.ToString());
                        int intCurrentFrequency = GetFrequency();

                        //Calculate completed status in %
                        int iCurrent = DABFMMonkeyScanEndIndex - intCurrentFrequency;
                        int iTotal = DABFMMonkeyScanEndIndex - DABFMMonkeyScanStartIndex;
                        decimal iRes = 100 - Math.Round((decimal)iCurrent / iTotal * 100, 0);

                        //Standard or China Mode?
                        string strStatusMessage = "";
                        if (intCurrentFrequency >= 0 && (intCurrentFrequency <= DABStandardFrequencyIndex.Length || intCurrentFrequency <= DABChinaFrequencyIndex.Length))
                        {
                            strStatusMessage = DABStandardFrequencyIndex[intCurrentFrequency] + "/" + DABStandardFrequencyMHz[intCurrentFrequency] + "MHz (" + iRes.ToString() + "%)";
                        }
                        else
                        {
                            strStatusMessage = DABChinaFrequencyIndex[intCurrentFrequency] + "/" + DABChinaFrequencyMHz[intCurrentFrequency] + "MHz (" + iRes.ToString() + "%)";
                        }

                        //Display the message
                        this.CF_systemCommand(CF_Actions.SHOWINFO, strStatusMessage, "AUTOHIDE");
                        _stationText = strStatusMessage;

                        //Wait a little to allow the DAB board to progress
                        System.Threading.Thread.Sleep(500);
                    }
                    else
                    {
                        WriteLog("PlayStatus: " + intPlayStatus.ToString());
                        sysReady = true;
                    }
                }

                //After a DABAutoSearch() radio will default to Mono, set it to what the user wants
                if (SetStereoMode(STEREOMODE))
                {
                    Mode tmpSTEREOMODE = GetStereoMode();
                    if (tmpSTEREOMODE == STEREOMODE) WriteLog("Mode validated, '" + STEREOMODE + "'"); else WriteLog("Failed to set mode, '" + STEREOMODE + "'");
                }
                else
                {
                    WriteLog("Failed to set Stero mode");
                }

                //Update how many programs user has
                intTotalProgram = GetTotalProgram();

                //Get DAB Names
                GetDABNames();

                //Set Volume
                SetBoardVolume(DABFMMonkeyVolume);

                // Let the user know how it went
                if (intTotalProgram >= 0 && intTotalProgram < MAXDABChannels)
                {
                    this.CF_systemCommand(CF_Actions.SHOWINFO, base.pluginLang.ReadField("/APPLANG/SETUP/ENDSCANNING1"), "AUTOHIDE");
                    System.Threading.Thread.Sleep(1000);
                    this.CF_systemCommand(CF_Actions.SHOWINFO, base.pluginLang.ReadField("/APPLANG/SETUP/ENDSCANNING2") + " " + intTotalProgram.ToString() + " " + base.pluginLang.ReadField("/APPLANG/SETUP/STATIONS"), "AUTOHIDE");
                }
                else this.CF_systemCommand(CF_Actions.SHOWINFO, base.pluginLang.ReadField("/APPLANG/SETUP/SCANNINGFAILED"), "AUTOHIDE");
            }

            WriteLog("ScanDAB() - end");
            return sysReady;
        }

        //Get DAB Names and store them for later
        //Thread Safe. Only Called during init or main thread
        private bool GetDABNames()
        {
            WriteLog("GetDABNames() - start");
            bool boolResult = true;

            try
            {
                intTotalProgram = GetTotalProgram();
                if (intTotalProgram <= 0 || intTotalProgram > MAXDABChannels) return boolResult;
                WriteLog("TotalDABProgram: '" + intTotalProgram + "'");

                //Resize the Array
                Array.Resize<string>(ref aryDABChannelsLong, (Int32)intTotalProgram); //Converting intTotalProgram to Signed is probably safe as Max the board can handle is 100...

                //Clear the lists first
                _ProgramInfoList.Clear();
                RadDtList.Clear();
                for (UInt32 i = 0; i < intTotalProgram; i++)
                {
                    //Program Info
                    Byte ServiceComponentID = 0;
                    UInt32 ServiceID = 0;
                    UInt16 EnsembleID = 0;
                    
                    try
                    {
                        if (GetProgramInfo(i, ref ServiceComponentID, ref ServiceID, ref EnsembleID))
                        {
                            RadioDNS _RadioDNS = new RadioDNS();
                            _RadioDNS.dabIndex = i;
                            _RadioDNS.eid = EnsembleID.ToString("X");
                            _RadioDNS.scids = ServiceComponentID.ToString("X");
                            _RadioDNS.sid = ServiceID.ToString("X");
                            _RadioDNS.type = "dab"; // All are DAB stations
                            
                            //4 or 8 nibbles?
                            if (ServiceID > 0XFFFF)
                            {
                                _RadioDNS.gcc = ServiceID.ToString("X")[2].ToString() + ServiceID.ToString("X")[0].ToString() + ServiceID.ToString("X")[1].ToString();
                            }
                            else
                            {
                                _RadioDNS.gcc = ServiceID.ToString("X")[0].ToString();
                            }                            
                            
                            //Add to List
                            _ProgramInfoList.Add(_RadioDNS);
                        }
                        else WriteLog("Failed - GetProgramInfo");
                    }
                    catch (Exception errmsg)
                    {
                        WriteLog("Exception Thrown Getting ProgramInfo Data, " + errmsg.ToString());
                    }


                    //DAB Name
                    string strtextBuffer = new string(' ', constBufferSize); // Read buffer
                    if (GetProgramName(RADIO_TUNE_BAND.DAB_BAND, i, DABNameMode.Long, strtextBuffer))
                    {
                        aryDABChannelsLong[i] = strtextBuffer.Trim();
                        WriteLog("GetProgramName (Long): '" + i.ToString() + "' = '" + aryDABChannelsLong[i] + "'");
                        DataRow row = RadDtList.NewRow();
                        row["DisplayName"] = aryDABChannelsLong[i];

                        //Check if its blacklisted
                        bool boolBlackListed = false;
                        for (int j = 0; j < _blackList.Count; j++)
                        {
                            //If we find a match, update the bool so we can tell the world
                            if (aryDABChannelsLong[i] == _blackList[j].DABLongName)
                            {
                                boolBlackListed = true;
                                WriteLog("Current Name: '" + aryDABChannelsLong[i] + "' Blacklisted:'" + _blackList[j].DABLongName + "'");
                            }
                        }
                        if (boolBlackListed) row["Blacklisted"] = true; else row["Blacklisted"] = false;

                        //Add it
                        RadDtList.Rows.Add(row);
                    }
                    else
                    {
                        WriteLog("Failed - GetProgramName (Long)'" + i.ToString() + "'");
                        aryDABChannelsLong[i] = "Unknown";
                        boolResult = false;
                    }
                }
            }
            catch (Exception errmsg)
            {
                WriteLog("Failed to handle GetDABNames(), " + errmsg.ToString());
            }

            WriteLog("GetDABNames() - end");
            return boolResult;
        }

        /// <summary>
        ///		Tune frequency.
        /// </summary>
        /// <return>
        ///		Returns radio frequency.
        /// </return>
        private UInt32 TuneFreq(UInt32 Freq)
        {
            WriteLog("Start: TuneFreq(), Freq:'" + Freq.ToString() + "'");

            //Clear the RDS text
            WriteLog("Clear Cached Programtext");
            ClearRDSVars(); //Clear the text fields
            
            try
            {
                if (init && intDABFMMode != RADIO_TUNE_BAND.UNDEFINED)
                {
                    WriteLog("Mode: " + intDABFMMode.ToString());

                    //Sanetize values
                    WriteLog("Pre Freq : '" + Freq.ToString() + "'");
                    Freq = fixFreq(intDABFMMode, Freq);
                    WriteLog("Post Freq : '" + Freq.ToString() + "'");

                    //Set desired station, keep same mode
                    intNewStation = Freq;
                    intNewDABFMMode = intDABFMMode;
                    //WriteLog("Current Radio command value: " + RadioCommand.ToString());                    
                    //if (RadioCommand == MonkeyCommand.NONE) RadioCommand = MonkeyCommand.PLAYSTREAM;
                    RadioCommand.Enqueue(MonkeyCommand.PLAYSTREAM);
                    //WriteLog("Current Radio command value: " + RadioCommand.ToString());
                }
                else 
                    WriteLog("Failed to get current mode (FM/DAB)");                
            }
            catch (Exception errmsg)
            {
                WriteLog("Failed to TuneFreq(), " + errmsg.ToString());
            }


            WriteLog("End: TuneFreq(): Freq:" + Freq.ToString());
            return Freq;
        }

        //Sanity check, and fix abnormal, frequency values
        private UInt32 fixFreq(RADIO_TUNE_BAND band, UInt32 Freq)
        {
            WriteLog("fixFreq() - start - Freq:" + Freq.ToString());
            UInt32 result = 0;

            switch (band)
            {
                case RADIO_TUNE_BAND.DAB_BAND:
                    // How many channels did we find?
                    if (intTotalProgram >= 0 && intTotalProgram < MAXDABChannels)
                    {
                        if (Freq < 0) result = intTotalProgram - 1; //Freq can'be be less than 0
                        if (Freq > intTotalProgram) result = 0; //Freq can't be more than we've got stored on the board
                        result = Freq;
                    }
                    else result = 0;

                    break;
                case RADIO_TUNE_BAND.FM_BAND:
                    // Sanity check the FM freq range
                    while (Freq > 120000) { Freq = Freq / 10; }
                    while (Freq <  70000) { Freq = Freq * 10; }

                    //Final check
                    if (Freq > 120000 || Freq < 70000) Freq = 100000;
                   
                    result = Freq;
                    break;
            }

            WriteLog("fixFreq() - end - Freq:" + result.ToString());
            return result;
        }

        /// <summary>
        ///		Seek.
        /// </summary>
        /// <return>
        ///		Returns radio frequency, returns -1 if seek not available.
        ///		
        ///		Return -2 if you are using asynchronous seeking methods.
        /// </return>
        private void SeekFreq(RADIO_DIRECTION searchDirection)
        {
            WriteLog("SeekFreq() - start");

            // Clear the RDS text
            WriteLog("Clear Cached Programtext");
            ClearRDSVars(); //Clear the text fields
            
            try
            {
                if (init) // && RadioCommand == MonkeyCommand.NONE)
                {
                    //if (searchDirection == RADIO_DIRECTION.UP) RadioCommand = MonkeyCommand.NEXTSTREAM; else RadioCommand = MonkeyCommand.PREVSTREAM;
                    if (searchDirection == RADIO_DIRECTION.UP) RadioCommand.Enqueue(MonkeyCommand.NEXTSTREAM); else RadioCommand.Enqueue(MonkeyCommand.PREVSTREAM);
                }
            }
            catch (Exception errmsg)
            {
                WriteLog("Failed to SeekFreq(), " + errmsg.ToString());
            }

            WriteLog("End: SeekFreq()");
        }

        /// <summary>
        ///		Set tune band.
        /// </summary>
        /// <return>
        ///		Return false if multiple bands not supported.
        /// </return>
        private bool SetTuneBand(RADIO_TUNE_BAND Band)
        {
            WriteLog("SetTuneBand() - start, RADIO_TUNE_BAND: '" + Band + "'");

            //Nothing to do?
            if (Band == intDABFMMode) return true;

            try
            {
                if (init)
                {
                    // Clear the RDS texts
                    WriteLog("Clear Cached Programtext");
                    ClearRDSVars(); //Clear the text fields                    

                    //Set start value
                    intNewStation = 999;

                    //Before switching band, save channel/freq
                    if (intDABFMMode != RADIO_TUNE_BAND.UNDEFINED)
                    {
                        WriteLog("Save current channel: '" + intCurrentStation.ToString() + "' ' " + intDABFMMode.ToString() + "'");

                        switch (intDABFMMode) //What's the current mode
                        {
                            case RADIO_TUNE_BAND.DAB_BAND:                                                                
                                try
                                {
                                    if (intCurrentStation != 999) base.pluginConfig.WriteField("/APPCONFIG/LASTDAB", intCurrentStation.ToString(), true);
                                    intNewStation = UInt32.Parse(base.pluginConfig.ReadField("/APPCONFIG/LASTFM"));
                                    intNewStation = fixFreq(RADIO_TUNE_BAND.FM_BAND, intNewStation);
                                }
                                catch (Exception errmsg)
                                {
                                    intNewStation = 100000;
                                    WriteLog("Failed to get intNewStation when swithing to FM Mode, " + errmsg.ToString());
                                }
                                break;
                            case RADIO_TUNE_BAND.FM_BAND:
                                try 
                                {
                                    if (intCurrentStation != 999) base.pluginConfig.WriteField("/APPCONFIG/LASTFM", intCurrentStation.ToString(), true);
                                    intNewStation = UInt32.Parse(base.pluginConfig.ReadField("/APPCONFIG/LASTDAB"));
                                    intNewStation = fixFreq(RADIO_TUNE_BAND.DAB_BAND, intNewStation);
                                }
                                catch (Exception errmsg)
                                {
                                    intNewStation = 0;
                                    WriteLog("Failed to get intNewStation when swithing to DAB Mode, " + errmsg.ToString());
                                }
                                break;
                            default:
                                //Should never reach this
                                return false;
                        }
                    }
                    else
                    {
                        WriteLog("Failed to save last used frequency: '" + intCurrentStation.ToString() + "'");
                        return false;
                    }

                    WriteLog("intNewStation: '" + intNewStation.ToString() + "' RadioCommand: '" + RadioCommand.ToString() + "'");

                    //If we have a new mode, set it
                    if (intNewStation != 999) // && RadioCommand == MonkeyCommand.NONE)
                    {
                        intNewDABFMMode = Band;
                        WriteLog("PlayStream: '" + intNewDABFMMode.ToString() + "' '" + intNewStation.ToString() + "'");
                        //RadioCommand = MonkeyCommand.PLAYSTREAM;
                        RadioCommand.Enqueue(MonkeyCommand.PLAYSTREAM);

                        WriteLog("SetTuneBand() - end");
                        return true;
                    }

                    WriteLog("SetTuneBand() - end");
                    return false;
                }
            }
            catch (Exception errmsg)
            {
                WriteLog("Failed to SetTuneBand(), " + errmsg.ToString());
            }
            
            WriteLog("SetTuneBand() - end: Error, failed to set band");
            return false;
        }

        //Change board's Volume
        private bool DABVolume(Volume DABVolume)
        {
            WriteLog("CF_pluginCMLCommand Volume Direction: " + DABVolume.ToString());
            switch (DABVolume)
            {
                case Volume.Up:
                    //if (init && RadioCommand == MonkeyCommand.NONE) RadioCommand = MonkeyCommand.VOLUMEPLUS;
                    RadioCommand.Enqueue(MonkeyCommand.VOLUMEPLUS);
                    break;     
                case Volume.Down:
                    //if (init && RadioCommand == MonkeyCommand.NONE) RadioCommand = MonkeyCommand.VOLUMEMINUS;
                    RadioCommand.Enqueue(MonkeyCommand.VOLUMEMINUS);
                    break;
            }

            //Sanity check the volume numbers, and write to disk if safe
            if (DABFMMonkeyVolume < Volume.Min) 
            {
                DABFMMonkeyVolume = Volume.Min; //Sanity value
                threadTimerButton.Abort(); // We're at the extreme, end the thread?
                return false; //Not good
            }

            if (DABFMMonkeyVolume > Volume.Max) 
            {
                DABFMMonkeyVolume = Volume.Max; //Sanity value
                threadTimerButton.Abort(); //We're at the extreme, end the thread?
                return false; //Not good
            }
                
            //Commit to disk if all was ok
            try
            {
                WriteLog("CF_pluginCMLCommand " + DABFMMonkeyVolume.ToString());
                this.pluginConfig.WriteField("/APPCONFIG/VOLUME", DABFMMonkeyVolume.ToString(), true);
            }
            catch (Exception errmsg)
            {
                WriteLog("Failed to write VOLUME to disk, " + errmsg.ToString());
            }


            //Status to user, if at either extreme value, display text so user knows to let go of button
            if (DABFMMonkeyVolume != Volume.Min && DABFMMonkeyVolume != Volume.Max)
            {
                // From http://stackoverflow.com/questions/943398/enums-returning-int-value
                var intVol = Convert.ChangeType(DABFMMonkeyVolume, DABFMMonkeyVolume.GetTypeCode());
                this.CF_systemCommand(CF_Actions.SHOWINFO, base.pluginLang.ReadField("/APPLANG/SETUP/VOLUME") + " " + intVol.ToString(), "AUTOHIDE");                                 
            }
            else
            {                
                this.CF_systemCommand(CF_Actions.SHOWINFO, base.pluginLang.ReadField("/APPLANG/SETUP/VOLUME") + " " + DABFMMonkeyVolume.ToString(), "AUTOHIDE");
            }         
            
            return true;
        }

        //Thread safe. Only called by IOCommsSub()
        private bool RetrieveBBEEQ()
        {
            WriteLog("RetrieveBBEEQ() - start");
            
            BBEStatus BBEOn = _BBEEQ.BBEOn;
            SByte EQMode = _BBEEQ.EQMode;
            SByte BBELo = _BBEEQ.BBELo;
            SByte BBEHi = _BBEEQ.BBEHi;
            SByte BBECFreq = _BBEEQ.BBECFreq;
            Byte BBEMachFreq = _BBEEQ.BBEMachFreq;
            SByte BBEMachGain = _BBEEQ.BBEMachGain;
            SByte BBEMachQ = _BBEEQ.BBEMachQ;
            SByte BBESurr = _BBEEQ.BBESurr;
            SByte BBEMp = _BBEEQ.BBEMp;
            Byte BBEHpF = _BBEEQ.BBEHpF;
            SByte BBEHiMode = _BBEEQ.BBEHiMode;
            SByte HeadRoom = _BBEEQ.HeadRoom;

            bool boolResult = GetBBEEQ(ref BBEOn, ref EQMode, ref BBELo, ref BBEHi, ref BBECFreq, ref BBEMachFreq, ref BBEMachGain, ref BBEMachQ, ref BBESurr, ref BBEMp, ref BBEHpF, ref BBEHiMode);
            if (boolResult)
            {
                WriteLog("Success: " + _BBEEQ.BBEOn.ToString() + " " + _BBEEQ.EQMode.ToString() + " " + _BBEEQ.BBELo.ToString() + " " + _BBEEQ.BBEHi.ToString() + " " + _BBEEQ.BBECFreq.ToString() + " " + _BBEEQ.BBEMachFreq.ToString() + " " + _BBEEQ.BBEMachGain.ToString() + " " + _BBEEQ.BBEMachQ.ToString() + " " + _BBEEQ.BBESurr.ToString() + " " + _BBEEQ.BBEMp.ToString() + " " + _BBEEQ.BBEHpF.ToString() + " " + _BBEEQ.BBEHiMode.ToString());
            }
            else WriteLog("Failed to get BBEEQ values");

            _BBEEQ.HeadRoom = GetHeadroom();
            if (_BBEEQ.HeadRoom >= 0 && _BBEEQ.HeadRoom <= 12)
            {
                _BBEEQ.HeadRoom = (sbyte)(0 - _BBEEQ.HeadRoom);
                WriteLog("Success: Headroom:" + _BBEEQ.HeadRoom.ToString());
            }
            else
            {
                _BBEEQ.HeadRoom = 1;
                WriteLog("Failed to get HeadRoom values");
            }

            WriteLog("RetrieveBBEEQ() - end");
            return boolResult;
        }

        //Thread safe. Only called by IOCommsSub() or init
        private bool StoreBBEEQ()
        {
            WriteLog("StoreBEEQ() - start");

            WriteLog(_BBEEQ.BBEOn.ToString() + " " + _BBEEQ.EQMode.ToString() + " " + _BBEEQ.BBELo.ToString() + " " + _BBEEQ.BBEHi.ToString() + " " + _BBEEQ.BBECFreq.ToString() + " " + _BBEEQ.BBEMachFreq.ToString() + " " + _BBEEQ.BBEMachGain.ToString() + " " + _BBEEQ.BBEMachQ.ToString() + " " + _BBEEQ.BBESurr.ToString() + " " + _BBEEQ.BBEMp.ToString() + " " + _BBEEQ.BBEHpF.ToString() + " " + _BBEEQ.BBEHiMode.ToString());
            bool boolResultBBEEQ = SetBBEEQ(_BBEEQ.BBEOn, _BBEEQ.EQMode, _BBEEQ.BBELo, _BBEEQ.BBEHi, _BBEEQ.BBECFreq, _BBEEQ.BBEMachFreq, _BBEEQ.BBEMachGain, _BBEEQ.BBEMachQ, _BBEEQ.BBESurr, _BBEEQ.BBEMp, _BBEEQ.BBEHpF, _BBEEQ.BBEHiMode);
            if (boolResultBBEEQ) WriteLog("Success"); else WriteLog("Failed");

            bool boolResultHR = SetHeadroom(Math.Abs(_BBEEQ.HeadRoom));
            if (boolResultHR) WriteLog("Success: '" + _BBEEQ.HeadRoom.ToString() + "'"); else WriteLog("Failed: '" + _BBEEQ.HeadRoom.ToString() + "'");
            
            WriteLog("StoreBEEQ() - end");
            if (boolResultBBEEQ && boolResultHR) return true; else return false;
        }

        //Thread safe. Only called by IOCommsSub()
        private bool CurrentProgramBlackListed()
        {
            WriteLog("Start CurrentProgramBlackListed()");

            bool boolBlackListed = false; //Assume its not blacklisted

            // What are we playing? (Freq/Channel)
            UInt32 intPlayIndex = 999;
            if (WaitForBoard()) intPlayIndex = GetPlayIndex();
            WriteLog("PlayIndex: '" + intPlayIndex.ToString() + "'");

            // Get program name (Long)            
            string strtextBuffer = new string(' ', constBufferSize); // Read buffer            
            if (WaitForBoard() && GetProgramName(intDABFMMode, intPlayIndex, DABNameMode.Long, strtextBuffer))
            {
                string strTempName = strtextBuffer.Trim();
                WriteLog("ProgramName: '" + strTempName + "'");

                //Check if its blacklisted
                for (int j = 0; j < _blackList.Count; j++)
                {
                    //If we find a match, update the bool so we can tell the world
                    if (strTempName == _blackList[j].DABLongName)
                    {
                        boolBlackListed = true;
                        WriteLog("Current Name: '" + strTempName + "' Blacklisted:'" + _blackList[j].DABLongName + "'");
                    }
                }
            }

            return boolBlackListed;
        }

        /// <summary>
        ///		Perform board I/O and update RDS information on screen
        /// </summary>
        /// <return>
        ///		Nothing
        /// </return>
        private void IOCommsSub()
        {
            WriteDebug("Start IOCommsSub()");

            /**/
            //bool init1 = WaitForBoard();
            //WriteLog("init1 : " + init1.ToString());

            WriteDebug("init : " + init.ToString());

            if (init) // && WaitForBoard())
            {
                //We're playing audio
                this.CF_params.Media.mediaPlaying = true;


                //Start Board Commands
                //switch (RadioCommand)
                if (RadioCommand.Count > 0 && WaitForBoard())
                {
                    switch (RadioCommand.Dequeue())
                    {
                        case MonkeyCommand.SETATTVOLUME:
                            WriteLog("Start - SetATTVolume");
                            SetBoardVolume(DABFMMonkeyATTVolume);
                            WriteLog("End - SetATTVolume");
                            break;
                        case MonkeyCommand.SETVOLUME:
                            WriteLog("Start - SetVolume");
                            SetBoardVolume(DABFMMonkeyVolume);
                            WriteLog("End - SetVolume");
                            break;
                        case MonkeyCommand.VOLUMEPLUS:
                            WriteLog("Start - Volume Plus");
                            VolumePlus();
                            //Volume updated, get the new value
                            DABFMMonkeyVolume = GetVolume();
                            WriteLog("Start - Volume Plus");
                            break;
                        case MonkeyCommand.VOLUMEMINUS:
                            WriteLog("Start - Volume Down");
                            VolumeMinus();
                            //Volume updated, get the new value
                            DABFMMonkeyVolume = GetVolume();
                            WriteLog("End - Volume Down");
                            break;
                        case MonkeyCommand.VOLUMEMUTE:
                            WriteLog("Start - Mute'ing");
                            VolumeMute();
                            WriteLog("End - Mute'ing");
                            break;
                        case MonkeyCommand.STOPSTREAM:
                            WriteLog("Start - Stopstream");
                            if (StopStream() == false) WriteLog("Error stopping stream"); else WriteLog("Stream Stopped");                            
                            //Close RadioVIS
                            CloseRadioVIS();
                            WriteLog("End - Stopstream");
                            break;
                        case MonkeyCommand.PLAYSTREAM:
                            WriteLog("Start - Playstream");
                            WriteLog("PlayStream: '" + intNewDABFMMode.ToString() + "' '" + intNewStation.ToString() + "'");

                            //Changing channel, close RadioVIS
                            CloseRadioVIS();

                            if (PlayStream(intNewDABFMMode, intNewStation))
                            {
                                WriteLog("Success setting FM/DAB mode/frequency: '" + intNewDABFMMode.ToString() + "', ' " + intNewStation.ToString() + "'");
                                intDABFMMode = intNewDABFMMode;
                                intCurrentStation = intNewStation;
                            }
                            else
                            {
                                WriteLog("Failed to set FM/DAB mode/frequency: '" + intNewDABFMMode.ToString() + "', ' " + intNewStation.ToString() + "'");
                                intDABFMMode = RADIO_TUNE_BAND.UNDEFINED;
                                intCurrentStation = 999;
                            }

                            //If DAB do this:
                            if (intDABFMMode == RADIO_TUNE_BAND.DAB_BAND)
                            {
                                //Clear the MOT segment buffer after changing DAB channel
                                MotReset(MotMode.SlideShow);
                                MotReset(MotMode.EPG);

                                //RadioVIS
                                if (boolEnableInternetUsage && boolEnableRadioVIS) RadioVIS(intCurrentStation);
                            }

                            // Clear the RDS texts
                            ClearRDSVars();

                            //Clear the command early, as some sub's re-set this value, globally clearing it does not work
                            //RadioCommand = MonkeyCommand.NONE;
                            RadioCommand.Clear();

                            WriteLog("End - Playstream");
                            break;
                        case MonkeyCommand.NEXTSTREAM:
                            WriteLog("Start - Nextstream");
                            switch (intDABFMMode)
                            {
                                case RADIO_TUNE_BAND.DAB_BAND:
                                    //Changing channel, close RadioVIS
                                    CloseRadioVIS();

                                    //Loop until we find the next program not blacklisted
                                    bool boolBlackListed = false;
                                    do
                                    {
                                        if (NextStream())
                                        {
                                            WriteLog("Success NextStream()");

                                            //Is it blacklisted?
                                            boolBlackListed = CurrentProgramBlackListed();
                                        }
                                        else WriteLog("Failed NextStream()");
                                    }
                                    while (boolBlackListed);

                                    //Clear the MOT segment buffer after changing DAB channel as we need to start from scratch
                                    MotReset(MotMode.SlideShow);
                                    MotReset(MotMode.EPG);

                                    //RadioVIS
                                    if (boolEnableInternetUsage && boolEnableRadioVIS) RadioVIS(GetPlayIndex());

                                    break;
                                case RADIO_TUNE_BAND.FM_BAND:
                                    if (NextStream()) WriteLog("Success NextStream()"); else WriteLog("Failed NextStream()");
                                    break;
                            }
                            WriteLog("End - Nextstream");
                            break;
                        case MonkeyCommand.PREVSTREAM:
                            WriteLog("Start - Prevstream");
                            switch (intDABFMMode)
                            {
                                case RADIO_TUNE_BAND.DAB_BAND:
                                    //Changing channel, close RadioVIS
                                    CloseRadioVIS();

                                    //Loop until we find the next program not blacklisted
                                    bool boolBlackListed = false;
                                    do
                                    {
                                        if (PrevStream())
                                        {
                                            WriteLog("Success PrevStream()");

                                            //Is it blacklisted?
                                            boolBlackListed = CurrentProgramBlackListed();
                                        }
                                        else WriteLog("Failed PrevStream()");
                                    }
                                    while (boolBlackListed);

                                    //Clear the MOT segment buffer after changing DAB channel as we need to start from scratch
                                    MotReset(MotMode.SlideShow);
                                    MotReset(MotMode.EPG);

                                    //RadioVIS
                                    if (boolEnableInternetUsage && boolEnableRadioVIS) RadioVIS(GetPlayIndex());

                                    break;
                                case RADIO_TUNE_BAND.FM_BAND:
                                    if (PrevStream()) WriteLog("Success PrevStream()"); else WriteLog("Failed PrevStream()");
                                    break;
                            }
                            WriteLog("End - Nextstream");
                            break;
                        case MonkeyCommand.SCANDAB:
                            WriteLog("ScanDAB - Start");
                            //Scan for DAB channels
                            if (ScanDAB())
                            {
                                //Clear the command early, as TuneFreq will not launch as existing command is in progress
                                //RadioCommand = MonkeyCommand.NONE;
                                RadioCommand.Clear();

                                //After Scanning, tune to channel 0 as we dont know what the user wants to listen too
                                if (intTotalProgram > 0)
                                {
                                    intDABFMMode = RADIO_TUNE_BAND.DAB_BAND;
                                    TuneFreq(0);
                                }
                                else
                                {
                                    intCurrentStation = 999; //Do not save
                                    intDABFMMode = RADIO_TUNE_BAND.DAB_BAND; //Pretend we're in DAB mode so SetTuneBand works
                                    SetTuneBand(RADIO_TUNE_BAND.FM_BAND);
                                }
                            }
                            WriteLog("ScanDAB - End");
                            break;
                        case MonkeyCommand.ADDFAVBTNCLICK:
                            WriteLog("Start - AddFavBtnClick");
                            //Save station to favorites
                            AddFavorites();
                            WriteLog("End - AddFavBtnClick");
                            break;
                        case MonkeyCommand.TUNESELECT:
                            WriteLog("Start - TuneSelect");
                            TuneSelect();
                            WriteLog("End - TuneSelect");
                            break;
                        case MonkeyCommand.STEREOMODE:
                            WriteLog("Start - StereoMode");
                            SetStereoModeClick();
                            WriteLog("End - StereoMode");
                            break;
                        case MonkeyCommand.GETBBEEQ:
                            WriteLog("Start - GetBBEEQ");
                            RetrieveBBEEQ();
                            WriteLog("Check: " + _BBEEQ.BBEOn.ToString() + " " + _BBEEQ.EQMode.ToString() + " " + _BBEEQ.BBELo.ToString() + " " + _BBEEQ.BBEHi.ToString() + " " + _BBEEQ.BBECFreq.ToString() + " " + _BBEEQ.BBEMachFreq.ToString() + " " + _BBEEQ.BBEMachGain.ToString() + " " + _BBEEQ.BBEMachQ.ToString() + " " + _BBEEQ.BBESurr.ToString() + " " + _BBEEQ.BBEMp.ToString() + " " + _BBEEQ.BBEHpF.ToString() + " " + _BBEEQ.BBEHiMode.ToString());
                            WriteLog("End - GetBBEEQ");
                            break;
                        case MonkeyCommand.SETBBEEQ:
                            WriteLog("Start - SetBBEEQ");
                            StoreBBEEQ();
                            WriteLog("End - SetBBEEQ");
                            break;
                        case MonkeyCommand.CLOSERADIOPORT:
                            WriteLog("Start - CloseRadioPort");
                            CloseRadioPort();
                            init = false;
                            WriteLog("End - CloseRadioPort");
                            break;
                        default:
                            break;
                    }
                }

                /**/ //xxx
                /*
                //Clear the command, but not if its PLAYSTREAM, as its set in some sub's and can't be cleared here.
                //switch (RadioCommand)
                switch (RadioCommand.Dequeue())
                {
                    case MonkeyCommand.PLAYSTREAM:
                        break;
                    default:
                        //RadioCommand = MonkeyCommand.NONE;
                        RadioCommand.Clear();
                        break;
                }
                */

                //Current status. Do not remove, else hardware mute will be active, resulting in low volume                
                intPlayStatus = DABStatus.Unknown;
                try
                {
                    WriteDebug("ShowPlayStatus() - start");
                    if (WaitForBoard()) intPlayStatus = GetPlayStatus();

                    if ((int)intPlayStatus >= 0 && (int)intPlayStatus <= aryDABStatus.Length)                        
                    {
                        WriteDebug("Status: " + aryDABStatus[(int)intPlayStatus]);

                        /**/
                        // This is a workaround to restart audio if it stops for no reason
                        // Usually the "no reason" is when RadioVIS starts. Maybe it starves another thread of resources long enough to kill the board?
                        if ((boolEnableAudio) && (intPlayStatus == DABStatus.Stop))
                        {
                            WriteDebug("Error: Audio stopped for no reason. Starting it");
                            PlayStream(intDABFMMode, intCurrentStation); // Resume
                        }
                    }
                    else WriteDebug("Status: Unknown");

                    WriteDebug("ShowPlayStatus() - end");
                }
                catch
                {
                    WriteDebug("Status: Unknown");
                }

                //If Status is not "Playing", don't gather more information from board
                if (intPlayStatus != DABStatus.Playing) return;

                //Get current play mode
                RADIO_TUNE_BAND intDABFMModeTmp = RADIO_TUNE_BAND.UNDEFINED;
                try
                {
                    intDABFMModeTmp = GetPlayMode();
                    if ((intDABFMModeTmp == RADIO_TUNE_BAND.DAB_BAND) || (intDABFMModeTmp == RADIO_TUNE_BAND.FM_BAND)) intDABFMMode = intDABFMModeTmp;
                }
                catch (Exception errmsg)
                {
                    intDABFMMode = RADIO_TUNE_BAND.UNDEFINED;
                    WriteLog("Exception Thrown - GetPlayMode Data, " + errmsg.ToString());
                }
                
                //Get current freq / channel                
                try
                {
                    WriteDebug("GetPlayIndex - start");

                    //Initial value is "Not supported"
                    //intCurrentStation = 999;

                    UInt32 intPlayIndex = 999;
                    if (WaitForBoard()) intPlayIndex = GetPlayIndex(); // What are we playing? (Freq/Channel)

                    // Sanity Check the return value
                    switch (intDABFMMode)
                    {
                        case RADIO_TUNE_BAND.FM_BAND:
                            if ((intPlayIndex >= 87500) && (intPlayIndex <= 108000)) intCurrentStation = intPlayIndex;
                            WriteDebug("FM : " + intCurrentStation.ToString());
                            break;
                        case RADIO_TUNE_BAND.DAB_BAND:
                            intTotalProgram = GetTotalProgram();
                            if (intTotalProgram < 0 || intTotalProgram > MAXDABChannels) intTotalProgram = 0;
                            WriteDebug("TotalProgram() : '" + intTotalProgram.ToString() + "'");

                            if ((intPlayIndex >= 0) && (intPlayIndex < intTotalProgram)) intCurrentStation = intPlayIndex;
                            WriteDebug("DAB : " + intCurrentStation.ToString());
                            break;
                        default:
                            WriteDebug("Undefined Playmode");
                            break;
                    }
                    WriteDebug("GetPlayIndex - End");
                }
                catch (Exception errmsg)
                {
                    WriteLog("Failed to GetCurrent Freq / Channel, " + errmsg.ToString());
                }


                //Get Program Name (Long) if in DAB Mode
                strNewDABLongName = "";
                try
                {
                    if ((WaitForBoard()) && (intCurrentStation != 999) && (intDABFMMode == RADIO_TUNE_BAND.DAB_BAND))
                    {
                        // Defaults and variables
                        string strtextBuffer = new string(' ', constBufferSize); // Read buffer

                        if (GetProgramName(intDABFMMode, intCurrentStation, DABNameMode.Long, strtextBuffer))
                        {
                            strNewDABLongName = strtextBuffer.Trim();
                            WriteDebug("Success - GetProgramName (Long): '" + strtextBuffer + "' Trimmed: '" + strNewDABLongName + "'");
                        }
                        else WriteDebug("Failed - GetProgramName (Long)");
                    }
                    else WriteDebug("GetProgramName (Long) - Not ready or not DAB Mode");
                }
                catch (Exception errmsg)
                {
                    WriteLog("Failed to GetProgramName (long), " + errmsg.ToString());
                }
                
                //This is to allow commands to execute faster by returning now, and checking for a command
                //if (RadioCommand != MonkeyCommand.NONE) return;
                if (RadioCommand.Count > 0) return;


                //Get current stereo mode
                try
                {
                    WriteDebug("GetStereoMode() - start");
                    if (WaitForBoard())
                    {
                        Mode tmpSTEREOMODE = GetStereoMode();
                        if (tmpSTEREOMODE == STEREOMODE) WriteDebug("Mode validated, '" + tmpSTEREOMODE + "'"); else SetStereoMode(STEREOMODE);                         
                    }
                    WriteDebug("STEREOMODE: '" + STEREOMODE.ToString() + "'");

                    //Update GUI to match mode
                    string strTemp = "";
                    switch (intDABFMMode)
                    {
                        case RADIO_TUNE_BAND.FM_BAND:
                            strTemp = base.pluginLang.ReadField("/APPLANG/SETUP/FMTUNE");
                            break;
                        case RADIO_TUNE_BAND.DAB_BAND:
                            strTemp = base.pluginLang.ReadField("/APPLANG/SETUP/DABTUNE");
                            break;
                    }

                    //Update GUI to reflect the mode
                    Font CFFont;

                    switch (STEREOMODE)
                    {
                        case Mode.AUTO:
                            //Normal
                            CFFont = CF_getFont(strFontClass, fontSize, FontStyle.Regular | FontStyle.Bold);
                            buttonArray[CF_getButtonID("TuneSelect")].Font = CFFont;
                            this.Invalidate(); //Re-draw as CF does not know font has changed.

                            break;
                        case Mode.MONO:
                            //Italic
                            CFFont = CF_getFont("Button20", 20, FontStyle.Italic | FontStyle.Bold);
                            buttonArray[CF_getButtonID("TuneSelect")].Font = CFFont;
                            this.Invalidate(); //Re-draw as CF does not know font has changed.

                            break;
                    }
                    WriteDebug("GetStereoMode() - end");
                }
                catch (Exception errmsg)
                {
                    WriteLog("Failed to get current StereoMode, " + errmsg.ToString());
                }

                //Signal Strenght
                try
                {
                    WriteDebug("GetSignalStrength() - start");

                    if (init && WaitForBoard())
                    {
                        int bitError = 0; //Not working: biterror: [out] 0 if FM mode and bit error rate if DAB mode. Ignore this out value until API updated
                        int intSignal = -1;
                        intSignal = GetSignalStrength(ref bitError);
                        if (intSignal >= 0 && intSignal <= 100)
                        {
                            intSignalStrength = intSignal;
                            WriteDebug("Success - GetSignal: '" + intSignalStrength.ToString() + "'");
                        }
                        else
                        {
                            intSignalStrength = -1;
                            WriteDebug("Failed - GetSignalStrength");
                        }
                    }
                    else WriteDebug("Failed - GetSignalStrength");
                }
                catch (Exception errmsg)
                {
                    WriteLog("Exception Thrown - GetSignal Data, " + errmsg.ToString());
                }

                //This is to allow commands to execute faster by returning now, and checking for a command
                //if (RadioCommand != MonkeyCommand.NONE) return;
                if (RadioCommand.Count > 0) return;

                //Mono / Stereo
                string strStereoLock = "";
                try
                {
                    WriteDebug("StereoMode() - start");

                    if (WaitForBoard())
                    {
                        switch (GetStereo())
                        {
                            case 0:
                                strStereoLock = "Stereo";
                                break;
                            case 1:
                                strStereoLock = "Joint Stereo";
                                break;
                            case 2:
                                strStereoLock = "Dual Channel";
                                break;
                            case 3:
                                strStereoLock = "Mono";
                                break;
                            default:
                                strStereoLock = "";
                                break;
                        }
                        WriteDebug("Success - GetStereo(): '" + strStereoLock + "'");
                        WriteDebug("StereoMode() - end");
                    }
                }
                catch (Exception errmsg)
                {
                    WriteLog("Exception Thrown - StereoLock Data, " + errmsg.ToString());
                }

                //This is to allow commands to execute faster by returning now, and checking for a command
                //if (RadioCommand != MonkeyCommand.NONE) return;
                if (RadioCommand.Count > 0) return;

                //Get Program type classification (News etc)
                string strProgramType = "";
                try
                {
                    if (intCurrentStation != 999 && intDABFMMode != RADIO_TUNE_BAND.UNDEFINED && WaitForBoard())
                    {
                        int intProgramType = GetProgramType(intDABFMMode, intCurrentStation);
                        if (intProgramType > 0 && intProgramType <= 31)
                        {
                            strProgramType = aryProgramType[intProgramType];
                            WriteDebug("Success - GetProgramType: '" + intProgramType.ToString() + "' Type: '" + aryProgramType[intProgramType] + "'");
                        }
                        else
                        {
                            WriteDebug("GetProgramClassification() - Unknown: '" + intProgramType.ToString() + "'");
                            intProgramType = -1;
                            strProgramType = "";
                        }
                    }
                    else WriteDebug("Failed - GetProgramType");
                }
                catch (Exception errmsg)
                {
                    WriteLog("Exception Thrown - ProgramClassification Data, " + errmsg.ToString());
                }

                //This is to allow commands to execute faster by returning now, and checking for a command
                //if (RadioCommand != MonkeyCommand.NONE) return;
                if (RadioCommand.Count > 0) return;

                //Get Program Name
                string strProgramName = "";
                try
                {
                    if ((WaitForBoard()) && (intCurrentStation != 999) && (intDABFMMode != RADIO_TUNE_BAND.UNDEFINED))
                    {
                        // Defaults and variables
                        string strtextBuffer = new string(' ', constBufferSize); // Read buffer

                        if (GetProgramName(intDABFMMode, intCurrentStation, boolDABLongName, strtextBuffer))
                        {
                            strProgramName = strtextBuffer.Trim();
                            WriteDebug("Success - GetProgramName: '" + strtextBuffer + "' Trimmed: '" + strProgramName + "'");
                        }
                        else
                        {
                            WriteDebug("Failed - GetProgramName");
                        }
                    }
                    else
                    {
                        WriteDebug("GetProgramName - Not ready");
                    }
                }
                catch (Exception errmsg)
                {
                    WriteLog("Exception Thrown Getting Data, " +errmsg.ToString() );
                }

                //This is to allow commands to execute faster by returning now, and checking for a command
                //if (RadioCommand != MonkeyCommand.NONE) return;
                if (RadioCommand.Count > 0) return;

                //Get RDS Information. Set to old value as RadioVIS does not re-set the value
                string strRDSData = _stationName;
                try
                {
                    //If RadioVIS
                    if (boolRadioVISConfigured)
                    {
                        // Check if new message has arrived. 'Duplicate' of OnMessage, but required as OnMessage can't access WriteLog() or ShowSLS() etc
                        if (aryMessage_Image != null)
                        {
                            WriteDebug("RadioVIS Command: '" + aryMessage_Image[0] + "' URI: '" + aryMessage_Image[1] + "' When: '" + aryMessage_Image[2] + "'");

                            //No caching. As per spec, only show the image if 'show' and 'now' are set
                            if ((aryMessage_Image[0].ToLower() == "show".ToLower()) && (aryMessage_Image[2].ToLower() == "now".ToLower()))
                            {
                                //Get the image
                                WebClient webClient = new WebClient();                                
                                webClient.DownloadFile(aryMessage_Image[1], "RadioVIS.DABFMMonkey.jpg");

                                //Show and remove the image
                                ShowSLS("RadioVIS.DABFMMonkey.jpg");
                            }

                            //Clear fields to get next image
                            aryMessage_Image = null;
                        }

                        // New text message
                        if (strMessage_Text != null)
                        {
                            WriteDebug("RadioVIS: Received text: '" + strMessage_Text + "'");
                            strRDSData = strMessage_Text;
                            strMessage_Text = null;
                        }
                    }
                    else
                    {
                        if (WaitForBoard())
                        {
                            // Defaults and variables
                            string strtextBuffer = new string(' ', constBufferSize); // Read buffer

                            sbyte res = GetProgramText(strtextBuffer);
                            WriteDebug("RDS availability: '" + res.ToString() + "'");

                            switch (res)
                            {
                                case 0: // New RDS data. Cache it for future use
                                    strRDSData = strCachedProgramText = strtextBuffer.Trim();
                                    WriteDebug("RDSProgramText: '" + strtextBuffer + "' Trimmed: '" + strRDSData + "'");
                                    break;
                                case 1: // No new RDS data. Use cached data
                                    strRDSData = strCachedProgramText;
                                    WriteDebug("RDSProgramText - Cached data : '" + strRDSData + "'");
                                    break;
                                default:
                                    // Failed (-1)
                                    strRDSData = strCachedProgramText;
                                    WriteDebug("RDSProgramText Failed - Cached data: '" + strRDSData + "'");
                                    break;
                            }
                        }
                        else
                        {
                            WriteDebug("Not ready. Using cached data: '" + strRDSData + "'");
                            strRDSData = strCachedProgramText;
                        }
                    }
                }
                catch (Exception errmsg)
                {
                    WriteLog("Exception Thrown Getting Data, " + errmsg.ToString());
                }

                //This is to allow commands to execute faster by returning now, and checking for a command
                //if (RadioCommand != MonkeyCommand.NONE) return;
                if (RadioCommand.Count > 0) return;

                //Station information
                string temp_stationText = "";
                try
                {
                    // If neither FM or DAB mode, we're not "Playing". Not sure how we got ourselves into this state, but its not a good place to be...
                    if (intDABFMMode != RADIO_TUNE_BAND.FM_BAND && intDABFMMode != RADIO_TUNE_BAND.DAB_BAND)
                    {
                        base.CF_updateButtonText("DABFM", "N/A");
                        base.CF_updateButtonText("TuneSelect", "N/A");
                    }

                    //When in FM mode, this field is often blanked by screen refreshes
                    if (intDABFMMode == RADIO_TUNE_BAND.FM_BAND)
                    {
                        base.CF_updateButtonText("DABFM", base.pluginLang.ReadField("/APPLANG/SETUP/FM"));
                        base.CF_updateButtonText("TuneSelect", base.pluginLang.ReadField("/APPLANG/SETUP/FMTUNE"));
                    }

                    if (intDABFMMode == RADIO_TUNE_BAND.DAB_BAND)
                    {
                        //Select Text when in DAB mode
                        base.CF_updateButtonText("TuneSelect", base.pluginLang.ReadField("/APPLANG/SETUP/DABTUNE"));

                        //Get EnsembleName in DAB Mode
                        string strEnsembleName = "";
                        try
                        {
                            // Defaults and variables
                            string strtextBuffer = new string(' ', constBufferSize); // Read buffer
                            bool boolResult = false;

                            if ((intCurrentStation != 999) && WaitForBoard()) boolResult = GetEnsembleName(intCurrentStation, DABNameMode.Short, strtextBuffer);

                            if (boolResult)
                            {
                                strEnsembleName = strtextBuffer.Trim();

                                if (strEnsembleName == "") base.CF_updateButtonText("DABFM", base.pluginLang.ReadField("/APPLANG/SETUP/DAB"));
                                else base.CF_updateButtonText("DABFM", strEnsembleName);
                            }
                            else
                            {
                                base.CF_updateButtonText("DABFM", base.pluginLang.ReadField("/APPLANG/SETUP/DAB"));
                            }
                            WriteDebug("EnsembleName: '" + strEnsembleName + "'");
                        }
                        catch (Exception errmsg)
                        {
                            WriteLog("Exception Thrown - GetEnsembleName RDS Data, " + errmsg.ToString());
                        }

                        //GetApplicationType
                        ApplicationType AppType = ApplicationType.Unknown;
                        try
                        {
                            if ((intCurrentStation != 999) && WaitForBoard()) AppType = GetApplicationType(intCurrentStation);
                        }
                        catch (Exception errmsg)
                        {
                            WriteLog("Failed to set AppType, " + errmsg.ToString());
                        }
                        WriteDebug("MOT: GetApplicationType: '" + AppType.ToString() + "'");

                        //Get MotData if RadioVIS is not enabled
                        if ((AppType == ApplicationType.SlideShow) && !(boolRadioVISConfigured))
                        {
                            WriteDebug("MOT: SlideShow Channel");

                            //SLS mode. Any pictures?
                            if (MotQuery())
                            {
                                WriteDebug("SLS and MotQuery=True.");
                                string strtextBuffer = new string(' ', constBufferSize); // Read buffer                               
                                GetImage(strtextBuffer);

                                string strImageFilename = CFTools.StartupPath + "\\" + strtextBuffer.Trim();
                                WriteDebug("MOT: strImageFilename: '" + strImageFilename + "'");

                                ShowSLS(strImageFilename);
                            }
                            else WriteDebug("MOT: Nothing assembled yet.");
                        }
                        else WriteDebug("MOT: Not SlideShow Channel or RadioVIS enabled");

                        //Get DataRate
                        int intDataRate = -1;
                        try
                        {
                            WriteDebug("DataRate() - start");

                            if (WaitForBoard())
                            {
                                intDataRate = GetDataRate();
                                WriteDebug("Success - DataRate: '" + intDataRate.ToString() + "'");
                            }
                            else WriteDebug("Failed - DataRate");

                            WriteDebug("DataRate() - end");
                        }
                        catch (Exception errmsg)
                        {
                            WriteLog("Exception Thrown - DataRate, " + errmsg.ToString());
                        }

                        //Deprecated function
                        sbyte sbyteSignalQuality = -1;
                        try
                        {
                            if (WaitForBoard()) sbyteSignalQuality = GetDABSignalQuality();
                        }
                        catch (Exception errmsg)
                        {
                            WriteLog("Exception Thrown - GetSignalQuality Data, " + errmsg.ToString() );
                        }
                        WriteDebug("SignalQuality: '" + sbyteSignalQuality.ToString() + "'");

                        // Minimal GUI?
                        if (!boolDABMinimal)
                        {
                            //Unused: •
                            if (intCurrentStation != 999) temp_stationText = "[Ch:" + intCurrentStation.ToString() + " "; else temp_stationText = "[";
                            if (intDataRate != -1) temp_stationText = temp_stationText + intDataRate.ToString() + "kb/s ";
                            if (strStereoLock != "") temp_stationText = temp_stationText + strStereoLock;

                            //If not valid values, set to 0 for GUI.
                            if (intSignalStrength == -1) intSignalStrength = 0;
                            if (sbyteSignalQuality == -1) sbyteSignalQuality = 0;

                            //Signal Strength and Quality
                            temp_stationText = temp_stationText + " " + intSignalStrength.ToString() + "/" + sbyteSignalQuality.ToString() + "%]";
                            //Use this if SignalQuality is deprecated: temp_stationText = temp_stationText + intSignal.ToString() + "%]";

                            //Add program Name
                            temp_stationText = temp_stationText + " " + strProgramName;
                        }
                        else temp_stationText = strProgramName;

                        //Add program type
                        if (strProgramType != "") temp_stationText = temp_stationText + "/" + strProgramType;
                    }
                    else if (intDABFMMode == RADIO_TUNE_BAND.FM_BAND)
                    {
                        if (!boolDABMinimal)
                        {
                            decimal iFreq = 0.0M;
                            if (intCurrentStation != 999)
                            {
                                //Convert to something readable
                                try
                                {
                                    iFreq = Math.Round((decimal)intCurrentStation / 1000, 2);
                                    WriteDebug("iFreq " + iFreq.ToString());
                                    temp_stationText = "[" + iFreq.ToString("00.00") + "MHz";
                                }
                                catch (Exception errmsg)
                                {
                                    WriteLog("Failed to convert iFreq data, " + errmsg.ToString());
                                }
                            }
                            else
                                temp_stationText = "[";

                            if (strStereoLock != "") temp_stationText = temp_stationText + " " + strStereoLock;
                            if (intSignalStrength != -1) temp_stationText = temp_stationText + " " + intSignalStrength.ToString() + "%]"; else temp_stationText = temp_stationText + "]";

                            temp_stationText = temp_stationText + " " + strProgramName;
                        }
                        else temp_stationText = strProgramName;                    

                        if (strProgramType != "") temp_stationText = temp_stationText + " / " + strProgramType;
                    }
                }
                catch (Exception errmsg)
                {
                    WriteLog("Exception Thrown Getting RDS Data, " + errmsg.ToString());
                }

                //Set new text and information if changed
                if (_stationText != temp_stationText) _stationText = temp_stationText;                
                if (_stationName != strRDSData) _stationName = strRDSData;

                WriteDebug("_stationText : '" + _stationText + "'");
                WriteDebug("_stationName : '" + _stationName + "'");
                //WriteLog("Autostart audio :" + CF_ConfigFlags.StartupAudio.ToString());
            }
            else 
            {
                WriteLog("Board Not Ready");
                System.Threading.Thread.Sleep(1000);
            }

            WriteDebug("End IOCommsSub()");
        }
    }
}
