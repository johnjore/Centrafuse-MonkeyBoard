/*
 * Copyright 2012, 2013, John Jore
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
 * This is the main CS file
 */

using centrafuse.Plugins;
using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Data;
using System.Threading;
using System.Xml;
using System.IO;
using System.Linq;
using CFControlsExtender.Base;      //Advanced ListView
using CFControlsExtender.Listview;  //Advanced ListView
using System.Reflection;            //Extra debug information

/*using System.Collections;
using Microsoft.Win32;
using System.Drawing;
using System.Xml.Serialization;
using System.Collections.Generic;*/

namespace DABFMMonkey
{
	/// <summary>
	/// DABFMMonkey plugin
	/// </summary>
    /// 
    [System.ComponentModel.DesignerCategory("Code")]
	public partial class DABFMMonkey : CFPlugin
	{

#region Variables
		private const string PluginName = "DABFMMonkey";
		private const string PluginPath = @"\plugins\" + PluginName + @"\";
		private const string PluginPathSkins = PluginPath + @"Skins\";
		private const string PluginPathLanguages = PluginPath + @"Languages\";
		private const string PluginPathIcons = PluginPath + @"Icons\";
		private const string ConfigurationFile = "config.xml";
        private const string FavoritesFile = "favorites.xml";
        private const string BlackListedFile = "blacklisted.xml";
        private const string BBEEQFile = "BBEEQ.xml";
		private const string LogFile= "DABFMMonkey.log";
        private static string LogFilePath = CFTools.AppDataPath + "\\Plugins\\" + PluginName + "\\" + LogFile; // we write the log to the appropriate users local appdata directory in the Plugins subfolder...

        //Max channels
        private const byte MAXDABChannels = 200;
        private const int FMSCANSleep = 7500; //milliseconds for sleeping while scanning for next FM
        
        //Audio startup
        private bool boolEnableAudio = false; // Start disabled.
        private bool boolFirstTime = true; //Flip in hide Used?!?       

        // threads
        Thread newthreadSCANFM; //Used when scanning for new active FM freq
        private bool newboolSCANFM = false;

        Thread threadIOComms; //Used for all I/O to the board, including getting RDS data information
        private bool boolIOCommsThread = true;

        Thread threadTimerButton; //Used to get button hold (long press)
        private const int intvolumeSleepTimer = 200; //Sleep duration
        private int intVolumeTimerButton = 0;
        private Volume DABVolumeDirection;

        //Buffer and input device (LineIn)
        private static bool _isBufferRadio = true;
        private static string _dabRadioLineDev =  "";
        private static string _dabRadioLine = "";

        //Used for Tune / Select font and size
        string strFontClass;
        byte fontSize;

#endregion

#region Construction

		/// <summary>
		/// Default constructor (creates the plugin and sets its properties).
		/// </summary>
		public DABFMMonkey()
		{
        }

#endregion

#region CFPlugin methods

		/// <summary>
		/// Initializes the plugin.  This is called from the main application
		/// when the plugin is first loaded.
		/// </summary>
		public override void CF_pluginInit()
		{
			try
			{
                // Call writeModuleLog() with the string startup() to keep only last 2 runtimes...                
                // Note CF_loadConfig() must be called before WriteLog() can be used
                WriteLog("CF_pluginInit() - start");

                // Check pluginConfig file is valid
                string ConfigFileName = CFTools.AppDataPath + PluginPath + ConfigurationFile;
                try
                {
                    if (File.Exists(ConfigFileName))
                    {
                        XmlDocument configxml = new XmlDocument();
                        WriteLog("App Config File: '" + ConfigFileName + "'");
                        configxml.Load(ConfigFileName);
                    }
                }
                catch
                {
                    this.CF_displayMessage("Configuration file is corrupt. Replacing.");
                    try
                    {
                        File.Delete(ConfigFileName);
                    }
                    catch { }
                }

                // CF3_initPlugin() Will configure pluginConfig and pluginLang automatically. All plugins must call this method once
                this.CF3_initPlugin("DABFMMonkey", true);

                //Log current version of DLL for debug purposes
                WriteLog("Assembly Version: '" + Assembly.GetExecutingAssembly().GetName().Version.ToString() + "'");

                //Initialise the advanced list
                try
                {
                    WriteLog("ADVLst_pluginInit - start");
                    // Now we need to set up our data variables. We will set up the listview itself in CF_localskinsetup
                    // The binding source is what links the listview to our data table
                    this.listBindingSource = new BindingSource();

                    // Tables that describes the data the lists will handle
                    FavDtList = new DataTable("Favorite");                          // Favorites
                    FavDtList.Columns.Add("DisplayName", typeof(System.String));    // DABLongName of radio channels

                    BlkDtList = new DataTable("Blacklist");                         // Blacklisted channels
                    BlkDtList.Columns.Add("DisplayName", typeof(System.String));    // DABLongName of radio channels

                    //Used for blacklisting
                    RadDtList = new DataTable("Radio");                             // All radio channels found on board
                    RadDtList.Columns.Add("DisplayName", typeof(System.String));    // DABLongName of radio channels
                    RadDtList.Columns.Add("Blacklisted", typeof(System.Boolean));   // If radio channel is in blacklisted, true, else false

                    // This timer can be used for the function where holding down the paging button makes appropriate action repeatedly
                    // You will need to handle the down event in the CML handler on the paging buttons and enable the paging timer if user does not release the button 
                    // (the click event) within a specified amount of time, usually abouta second or so...
                    this.pagingTimer = new System.Windows.Forms.Timer();
                    this.pagingTimer.Interval = 850;
                    this.pagingTimer.Enabled = false;
                    this.pagingTimer.Tick += new EventHandler(pagingTimer_Tick);

                    WriteLog("ADVLst_pluginInit - end");
                }
                catch (Exception errmsg) { CFTools.writeError(errmsg.ToString()); }


                // All controls should be created or Setup in CF_localskinsetup. 
                // This method is also called when the resolution or skin has changed.
                this.CF_localskinsetup();

                //From http://wiki.centrafuse.com/wiki/Application-Description.ashx
                this.CF_params.settingsDisplayDesc = this.pluginLang.ReadField("/APPLANG/SETUP/DESCRIPTION");

                //Get settings from XML files
                LoadSettings();
                
                // add event handlers for keyboard and power mode change
				this.KeyDown += new KeyEventHandler(DABFMMonkey_KeyDown);
                this.CF_events.CFPowerModeChanged += new CFPowerModeChangedEventHandler(OnPowerModeChanged); //Hibernation support
			}
			catch(Exception errmsg) { CFTools.writeError(errmsg.ToString()); }

            WriteLog("CF_pluginInit() - end");
		}
    
        private void enableplugin()
        {
            WriteLog("enableplugin() - start");
            //Let CF know we're an audio plugin
            this.CF_params.Media.isAudioPlugin = true;      // This is a audio source plugin
            this.CF_params.Media.bufferOn = _isBufferRadio; // Buffer usage is based on users settings                
            this.CF_params.Media.visibleOnly = false;       // Audio can continue to play, even if plugin is not visible
            this.CF_params.supportsRearScreen = false;      // No rear screen support

            //Init the board
            if (!init) init = InitializeRadio();

            //Get DAB Names
            if (WaitForBoard()) WriteLog("DAB Names completed: " + GetDABNames().ToString() + "'");

            //Rescan DAB if requested
            if (boolDABFMMonkeyReScan)
            {
                //Start the scan
                WriteLog("Start 'DABScanning'");

                ScanDAB(); // Scan and provide a status update
            }

            //Create the Scan thead
            newthreadSCANFM = new Thread(this.newsubSCANFM);

            WriteLog("enableplugin() - end");
        }

		/// <summary>
		/// This is called to setup the skin.  This will usually be called in CF_pluginInit.  It will 
        /// also called by the system when the resolution has been changed.
		/// </summary>
		public override void CF_localskinsetup()
		{
            WriteLog("CF_localskinsetup() - start");

            //Default screen
            this.CF3_initSection("DABFMMonkey");
            
            //setup the controls for the advanced list
            WriteLog("ADVLst_localskinsetup - start");

            // Now set up our avanced list box
            listMain = this.advancedlistArray[CF_getAdvancedListID("MainPanel")];  // MainPanel is our list box's ID
            if (listMain != null)
            {
                // About linked items: linked items are little icons on each item that perform some action.
                // When a user clicks on that icon, the linked action fires. As an example, there is a linked
                // play action on the media selector page. Here, we are going to create a linked action to
                // delete an item from the list. In CML, you set a "linkId" attribute on the list view item and the
                // value of this attribute is the string that is passed to the handler
                listMain.LinkedItemOnEnter = "Delete";                                              // Our linked action is to delete the number
                listMain.LinkedItemClick += new EventHandler<LinkedItemArgs>(OnLinkedItemClick);    // This is the event that will fire for a linked action
                listMain.Click += new EventHandler<ThrowScrollPanelMouseEventArgs>(OnListClick);
                listMain.TemplateID = sbListRows.ToString() + "_default";                           // Current mode, +_default

                if (FavDtList != null)
                {
                    // Data bind to our data source
                    listBindingSource.DataSource = this.FavDtList.DefaultView.Table;
                    listMain.DataBinding = listBindingSource;
                }
            }
            WriteLog("ADVLst_localskinsetup - end");

         
            // Buttons not defined in the skin XML
            // DABFMMonkey
            WriteLog("Adding Button events not defined in XML");
            this.CF_createButtonClick("Scan", new MouseEventHandler(ScanClick)); //Scan for next station, wait, scan for next station
            this.CF_createButtonClick("BackFineTune", new MouseEventHandler(BackFineTuneClick)); // Go back
            this.CF_createButtonClick("ForwardFineTune", new MouseEventHandler(FwdFineTuneClick)); // Go forward
            this.CF_createButtonEvents("PageUp", new MouseEventHandler(PageUpClick), new MouseEventHandler(PageUpRelease)); //PageUp
            this.CF_createButtonEvents("PageDown", new MouseEventHandler(PageDownClick), new MouseEventHandler(PageDownRelease)); //Pagedown

            //Slider update timers
            timerSlider.Interval = 10; //in milliseconds
            timerSlider.Tick += new EventHandler(OnTimerSlider_Tick);
          
            WriteLog("CF_localskinsetup() - end");
        }

		/// <summary>
		/// This is called by the system when it exits or the plugin has been deleted.
		/// </summary>
		public override void CF_pluginClose()
		{
            WriteLog("CF_pluginClose() - start");

            ShutdownRadio(); // Shutdown

            ClearRDSVars(); //Clear the rds info bars

            //No longer an active audio plugin
            this.CF_params.Media.mediaPlaying = false;

            base.CF_pluginClose(); // calls form Dispose() method

            WriteLog("CF_pluginClose() - end");
		}

		/// <summary>
		/// This is called by the system when a button with this plugin action has been clicked.
		/// </summary>
		public override void CF_pluginShow()
        {
            WriteLog("CF_pluginShow() - start");

            // Only show the plugin GUI if we enable it. Users might be using the radio module instead
            if (!boolEnablePlugin)
            {
                this.CF_systemDisplayDialog(CF_Dialogs.OkBox, base.pluginLang.ReadField("/APPLANG/SETUP/ENABLEPLUGIN"));
                return;
            }

            try
            {
                if (!boolEnableAudio)
                {
                    boolEnableAudio = true; //Force true now as plugin is made active
                    //Active audio plugin
                    this.CF_params.Media.mediaPlaying = true;

                    StartAudio(); //Start the music
                }

                base.CF_pluginShow(); // sets form Visible property
            }
            catch { }

            WriteLog("CF_pluginShow() - end");
		}

        /// <summary>
        /// This is called by the system when this plugin is minimized/exited (when screen is left).
        /// </summary>
        public override void CF_pluginHide()
        {
            WriteLog("CF_pluginHide() - start");

            base.CF_pluginHide(); // sets form !Visible property

            WriteLog("CF_pluginHide() - end");
        }

		/// <summary>
		/// This is called by the system when the plugin setup is clicked.
		/// </summary>
		/// <returns>Returns the dialog result.</returns>
		public override DialogResult CF_pluginShowSetup()
		{
            WriteLog("CF_pluginShowSetup - start");
			
            // Return DialogResult.OK for the main application to update from plugin changes.
			DialogResult returnvalue = DialogResult.Cancel;

			try
			{
				// Creates a new plugin setup instance. If you create a CFDialog or CFSetup you must
				// set its MainForm property to the main plugins MainForm property.
				Setup setup = new Setup(this.MainForm, this.pluginConfig, this.pluginLang);
				returnvalue = setup.ShowDialog();

                if (returnvalue == DialogResult.OK)
                {
                    LoadSettings();
                }
                setup.Close();
                setup = null;
			}
			catch(Exception errmsg) { CFTools.writeError(errmsg.ToString()); }
            
            WriteLog("CF_pluginShowSetup - end");
			return returnvalue;
		}

        /// <summary>
		/// This method is called by the system when it pauses all audio.
		/// </summary>
		public override void CF_pluginPause()
		{
            WriteLog("CF_pluginPause() - start");

            //No longer an active audio plugin
            this.CF_params.Media.mediaPlaying = false;
            boolEnableAudio = false;

            //Save current status, but only if initialized (init)
            if (init)
            {
                WriteLog("SaveCurrentStatus()");
                SaveCurrentStatus();    // Save current status
            }

            //Stop everything
            if (init && RadioCommand == MonkeyCommand.NONE)
            {
                WriteLog("Send STOPSTREAM");
                RadioCommand = MonkeyCommand.STOPSTREAM;

                while (RadioCommand != MonkeyCommand.NONE) System.Threading.Thread.Sleep(100); //Allow time for Comms I/O thread to execute the command
            }

            WriteLog("ClearRDSVars()");
            ClearRDSVars();         // Clear the rds info bars

            //Stop thread for updating the GUI with RDS information
            WriteLog("Stop the IOComms Thread");
            boolIOCommsThread = false;
            threadIOComms.Abort(); // Kill the thread

            // Allow closing of file before exit
            System.Threading.Thread.Sleep(1000);

            WriteLog("CF_pluginPause() - end");
		}

		/// <summary>
		/// This is called by the system when it resumes all audio.
		/// </summary>
		public override void CF_pluginResume()
		{
            WriteLog("CF_pluginResume() - start");

            //Autostart audio when plugin starts if CF is set to startupAudio
            if (boolEnablePlugin)
            {
                //Active audio plugin
                this.CF_params.Media.mediaPlaying = true;

                boolEnableAudio = CF_getConfigFlag(CF_ConfigFlags.StartupAudio);
                WriteLog("Autostart audio :" + boolEnableAudio.ToString());
                if (boolEnableAudio)
                {
                    StartAudio();
                }
                // UnMute. Set the volume to preMute level
                RadioCommand = MonkeyCommand.SETVOLUME;
            }
            
            WriteLog("CF_pluginResume() - end");
		}

		/// <summary>
		/// Used for plugin to plugin communication. Parameters can be passed into CF_Main_systemCommands
		/// with CF_Actions.PLUGIN, plugin name, plugin command, and a command parameter.
		/// </summary>
		/// <param name="command">The command to execute.</param>
		/// <param name="param1">The first parameter.</param>
		/// <param name="param2">The second parameter.</param>
		public override void CF_pluginCommand(string command, string param1, string param2)
		{
            WriteLog("CF_pluginCommand: " + command + " " + param1 + ", " + param2);

            //Capture and act upon the hotkeys
            try
            {
                switch (command)
                {
                    case "LoadNextTrack":
                        WriteLog("LoadNextTrack");
                        FwdTuneClick();
                        break;
                    case "LoadPreviousTrack":
                        WriteLog("LoadPreviousTrack");
                        BackTuneClick();
                        break;
                    case "RadioSeekForward":
                        WriteLog("RadioSeekForward");
                        FwdFineTuneClick(null, null);
                        break;
                    case "RadioSeekBack":
                        WriteLog("RadioSeekBack");
                        BackFineTuneClick(null, null);
                        break;
                    case "FM":
                        WriteLog("FM");
                        if (SetTuneBand(RADIO_TUNE_BAND.FM_BAND)) WriteLog("Success Selecting FM"); else WriteLog("Failed Selecting FM");
                        break;
                    case "DAB":
                        WriteLog("DAB");
                        if (SetTuneBand(RADIO_TUNE_BAND.DAB_BAND)) WriteLog("Success Selecting DAB"); else WriteLog("Failed Selecting DAB");
                        break;
                    case "SCAN":
                        WriteLog("SCAN");
                        ScanClick(null, null);
                        break;
                    case "ToggleDABFM":
                        WriteLog("ToggleDABFM");
                        DABFMClick();
                        break;
                    case "LoadPreset1":
                        WriteLog("LOADPRESET1");
                        if (boolEnableAudio) PlayIndex(1);
                        break;
                    case "LoadPreset2":
                        WriteLog("LoadPreset2");
                        if (boolEnableAudio) PlayIndex(2);
                        break;
                    case "LoadPreset3":
                        WriteLog("LoadPreset3");
                        if (boolEnableAudio) PlayIndex(3);
                        break;
                    case "LoadPreset4":
                        WriteLog("LoadPreset4");
                        if (boolEnableAudio) PlayIndex(4);
                        break;
                    case "LoadPreset5":
                        WriteLog("LoadPreset5");
                        if (boolEnableAudio) PlayIndex(5);
                        break;
                    case "LoadPreset6":
                        WriteLog("LoadPreset6");
                        if (boolEnableAudio) PlayIndex(6);
                        break;
                    case "LoadPreset7":
                        WriteLog("LoadPreset7");
                        if (boolEnableAudio) PlayIndex(7);
                        break;
                    case "LoadPreset8":
                        WriteLog("LoadPreset8");
                        if (boolEnableAudio) PlayIndex(8);
                        break;
                    default:
                        WriteLog("Unknown command");
                        break;
                }
            }
            catch { }
		}

        public void PlayIndex(int favIndex)
        {
            //Sanity check if the fav is actually populated...
            if (favIndex <= listMain.Count)
            {
                // Update the list view
                listMain.SelectedIndex = favIndex - 1; //0 based
                listMain.Refresh();

                // Start playing
                SetSelectedValue(favIndex - 1); //0 based
            }
        }

		/// <summary>
		/// Used for retrieving information from plugins. You can run CF_getPluginData with a plugin name,
		///	command, and parameter to retrieve information from other plugins running on the system.
		/// </summary>
		/// <param name="command">The command to execute.</param>
		/// <param name="param">The parameter.</param>
		/// <returns>Returns whatever is appropriate.</returns>
		public override string CF_pluginData(string command, string param)
		{
            //WriteLog("CF_pluginData: " + command + " " + param);

            string retvalue = "";
            switch (command.ToUpper())
            {
                case "MODE":
                    retvalue = intDABFMMode.ToString().ToUpper();
                    //WriteLog("CF_pluginData: " + retvalue);
                    break;
                case "CHANNEL":
                    retvalue = intCurrentStation.ToString();
                    //WriteLog("CF_pluginData: " + retvalue);
                    break;
            }

            //WriteLog("CF_pluginData: " + retvalue);
            return retvalue;
		}

        /// <summary>
        /// Called on control clicks, down events, etc, if the control has a defined CML action parameter in the skin xml.
        /// </summary>
        /// <param name="id">The command to execute.</param>
        /// <param name="state">Button State.</param>
        /// <returns>Returns whatever is appropriate.</returns>
        public override bool CF_pluginCMLCommand(string id, string[] strparams, CF_ButtonState state, int zone)
        {
            WriteLog("CF_pluginCMLCommand id: '" + id + "' state: '" + state.ToString() + "' zone: '" + zone.ToString() + "'");

            try
            {
                switch (id.ToUpper())
                {
                    case "DABFM": //Swap FM/DAB
                        if (state == CF_ButtonState.Click) DABFMClick();
                        return true;
                    case "BBE": //BBE config screen if held
                        BBEClick();
                        return true;

                    case "ADD": //Add favorite
                        if (state == CF_ButtonState.Click && boolEnableAudio) AddfavBtnClick();
                        return true;
                    case "PAYPAL": //PayPal donation anyone?
                        PayPalClick();
                        return true;

                    case "BACKTUNE": //Back if clicked & VolDow if held
                        WriteLog("CF_pluginCMLCommand id: '" + id + "'");
                        if (state == CF_ButtonState.Click)
                        {
                            WriteLog("CF_pluginCMLCommand Duration: '" + intVolumeTimerButton.ToString() + "'");
                            threadTimerButton.Abort(); // Terminate timer

                            if (intVolumeTimerButton <= 1000) //Short click
                            {
                                WriteLog("CF_pluginCMLCommand Single click: '" + intVolumeTimerButton.ToString() + "'");
                                BackTuneClick();
                            }
                            return true;
                        }
                        else if (state == CF_ButtonState.Down)
                        {
                            WriteLog("CF_pluginCMLCommand Starting timer");

                            DABVolumeDirection = Volume.Down;
                            threadTimerButton = new Thread(this.TimerVolumeUpDown); //Create the timer
                            threadTimerButton.Start(); //Start the timer
                        } else threadTimerButton.Abort(); // Terminate timer
                        WriteLog("CF_pluginCMLCommand State: '" + state.ToString() + "' id: '" + id + "'");
                        return true;

                    case "FORWARDTUNE": //Forward if clicked & VolUp if held
                        WriteLog("CF_pluginCMLCommand id: '" + id + "'");
                        if (state == CF_ButtonState.Click)
                        {
                            WriteLog("CF_pluginCMLCommand Duration: '" + intVolumeTimerButton.ToString() + "'");
                            threadTimerButton.Abort(); // Terminate timer

                            if (intVolumeTimerButton <= 1000) //Short click
                            {
                                WriteLog("CF_pluginCMLCommand Single click: '" + intVolumeTimerButton.ToString() + "'");
                                FwdTuneClick();
                            }
                            return true;
                        }
                        else if (state == CF_ButtonState.Down)
                        {
                            WriteLog("CF_pluginCMLCommand Starting timer");

                            DABVolumeDirection = Volume.Up;
                            threadTimerButton = new Thread(this.TimerVolumeUpDown); //Create the timer
                            threadTimerButton.Start(); //Start the timer
                        }
                        else threadTimerButton.Abort(); // Terminate timer
                        WriteLog("CF_pluginCMLCommand State: '" + state.ToString() + "' id: '" + id + "'");
                        return true;

                    //CMLCommands happen wether the plugin is active or not. Make sure we only execute commands if boolEnableAudio is true for global CML commands
                    case "CENTRAFUSE.MAIN.FASTFORWARD":
                    case "CENTRAFUSE.CFACTIONS.NEXTSONG":
                        if (boolEnableAudio)
                        {
                            if (state == CF_ButtonState.Click)
                            {
                                if (boolButtonMode)
                                {
                                    // Get current location in listview
                                    int selection = GetSelectedValue() + 1;

                                    // Wrap around?
                                    if (selection >= listMain.Count) selection = 0;
                                    
                                    // Update the list view
                                    listMain.SelectedIndex = selection;
                                    listMain.Refresh();

                                    // Start playing
                                    SetSelectedValue(selection);
                                }
                                else
                                {
                                    FwdFineTuneClick(null, null);
                                }
                            }
                            return true;
                        }
                        else return false;

                    case "CENTRAFUSE.MAIN.REWIND":
                    case "CENTRAFUSE.CFACTIONS.PREVSONG":
                        if (boolEnableAudio)
                        {
                            if (state == CF_ButtonState.Click)
                            {
                                if (boolButtonMode)
                                {
                                    // Get current location in listview
                                    int selection = GetSelectedValue() - 1;

                                    // Wrap around?
                                    if (selection < 0) selection = listMain.Count-1;

                                    // Update the list view
                                    listMain.SelectedIndex = selection;
                                    listMain.Refresh();

                                    // Start playing
                                    SetSelectedValue(selection);                                    
                                }
                                else
                                {
                                    BackFineTuneClick(null, null);
                                }
                            }
                            return true;
                        }
                        else return false;
                    
                    //Not handled as used as there is no need to overwrite CF's native handling
                    /*case "CENTRAFUSE.MAIN.PLAYPAUSE":
                        if (boolEnableAudio)
                        {
                            return false;
                        }
                        else return false;
                    */
                    case "TUNESELECT":
                        if (state == CF_ButtonState.Click) TuneClick();
                        return true;

                    case "SETSTEREOMODE": // Set radio to forced mono or auto detect stereo mode
                        if (init && RadioCommand == MonkeyCommand.NONE) RadioCommand = MonkeyCommand.STEREOMODE;
                        return true;

                    case "DELETE":
                        if (state == CF_ButtonState.Click)
                        {
                            HideSLS();

                            if (FavDtList != null)
                            {
                                // Data bind to our data source
                                listBindingSource.DataSource = this.FavDtList.DefaultView.Table;
                                listMain.DataBinding = listBindingSource;
                            }
                            if (listMain.TemplateID == sbListRows.ToString() + "_default")
                            {
                                this.WriteLog("delete command click received");
                                listMain.TemplateID = sbListRows.ToString() + "_deletemode";
                                listMain.Refresh();
                                CF_setButtonOn("Delete");
                            }
                            else
                            {
                                this.WriteLog("delete command down received");
                                listMain.TemplateID = sbListRows.ToString() + "_default";
                                listMain.Refresh();
                                CF_setButtonOff("Delete");
                            }
                        }
                        return true;
                    case "BLACKLIST":
                        this.WriteLog("Blacklist command received");
                        HideSLS();
                        if (RadDtList != null)
                        {
                            // Data bind to our data source
                            listBindingSource.DataSource = this.RadDtList.DefaultView.Table;
                            listMain.DataBinding = listBindingSource;
                        }
                        if (state == CF_ButtonState.HoldClick)
                        {
                            CF_setButtonOn("Delete");
                            listMain.TemplateID = sbListRows.ToString() + "_blacklist";
                            listMain.Refresh();
                        }
                        return true;
                    case "MIXEROFF":
                        if (state == CF_ButtonState.Click)
                        {
                            this.WriteLog("MixerOff");
                            MixerOff();
                        }
                        return true;
                    case "MIXERBBE":
                        if (state == CF_ButtonState.Click)
                        {
                            this.WriteLog("MixerBBE");
                            MixerBBE();
                        }
                        return true;
                    case "MIXEREQ":
                        if (state == CF_ButtonState.Click)
                        {
                            this.WriteLog("MixerEq");
                            MixerEq();
                        }
                        return true;
                    case "MIXEREXIT":
                        if (state == CF_ButtonState.Click)
                        {
                            this.WriteLog("MixerExit");
                            timerSlider.Enabled = false;

                            //Save the BBE Values to disk
                            SaveBBEEQ(_BBEEQ);

                            CF_localskinsetup(); //Jump back to 'DABFMMonkey' screen
                        }
                        return true;
                }
            }
            catch (Exception ex) { CFTools.writeError(ex.ToString()); }
            return false;
        }
        
        public override string CF_pluginCMLData(CF_CMLTextItems textitem)
        {
            try
            {
                switch (textitem)
                {
                    case CF_CMLTextItems.MainTitle: // 0
                        return this._stationName;

                    case CF_CMLTextItems.MediaArtist: // 4
                        return this._stationText;

                    case CF_CMLTextItems.MediaAlbum: // 5
                        return "";

                    case CF_CMLTextItems.MediaTitle: // 6
                        return "";

                    case CF_CMLTextItems.MediaStation: // 7
                        return this._stationName;
                }
                return "";
            }
            catch (Exception exception)
            {
                CFTools.writeError(exception.Message, exception.StackTrace);
            }
            return "Return String";
        }

        //Init board, set band, freq/channel, update GUI and start the music
        private bool StartAudio()
        {
            this.WriteLog("StartAudio() - start");
            bool result = false;

            try
            {
                if (!init)
                {
                    init = InitializeRadio();

                    //Get DAB Names
                    if (WaitForBoard()) WriteLog("DAB Names completed: " + GetDABNames().ToString() + "'");
                }

                //Only progress if success and CF_startup is enabled
                if (init && boolEnableAudio)
                {
                    //Play using settings from config file                   
                    try { intNewDABFMMode = (RADIO_TUNE_BAND)Enum.Parse(typeof(RADIO_TUNE_BAND), this.pluginConfig.ReadField("/APPCONFIG/ACTIVEBAND"), true); }
                    catch { }
                    WriteLog("XML ACTIVEBAND: '" + intNewDABFMMode.ToString() + "'");

                    try { 
                        intLASTDAB = UInt32.Parse(this.pluginConfig.ReadField("/APPCONFIG/LASTDAB"));
                        intLASTDAB = fixFreq(RADIO_TUNE_BAND.DAB_BAND, intLASTDAB);
                    }
                    catch { }
                    WriteLog("XML LASTDAB: '" + intLASTDAB.ToString() + "'");

                    try { 
                        intLASTFM = UInt32.Parse(this.pluginConfig.ReadField("/APPCONFIG/LASTFM"));
                        intLASTFM = fixFreq(RADIO_TUNE_BAND.FM_BAND, intLASTFM);
                    }
                    catch { }
                    WriteLog("XML LASTFM: '" + intLASTFM.ToString() + "'");

                    WriteLog("intTotalProgram: " + intTotalProgram.ToString());

                    //If user chooses DAB, then we must make sure there is something to tune to:
                    if (intNewDABFMMode == RADIO_TUNE_BAND.DAB_BAND)
                    {
                        if (intTotalProgram == 0)
                        {
                            WriteLog("No DAB channels. Switching to FM Mode");
                            intNewDABFMMode = RADIO_TUNE_BAND.FM_BAND;
                            DABRadioChannel = intLASTFM;
                        }
                        else DABRadioChannel = intLASTDAB;
                    }
                    else DABRadioChannel = intLASTFM;                
                    WriteLog("XML ACTIVE FREQ/CH: '" + DABRadioChannel.ToString() + "'");
                   
                    try
                    {
                        CF_params.Media.playbackDevice = pluginConfig.ReadField("/APPCONFIG/PLAYBACK").Split(new char[] { '|' })[0];
                        CF_params.Media.playbackLine = pluginConfig.ReadField("/APPCONFIG/PLAYBACK").Split(new char[] { '|' })[1];
                    }
                    catch
                    {
                        pluginConfig.WriteField("/APPCONFIG/PLAYBACK", "-1|-1", true);
                        CF_params.Media.playbackDevice = "";
                        CF_params.Media.playbackLine = "";                        
                    }

                    try
                    {
                        CF_params.Media.recordDevice = pluginConfig.ReadField("/APPCONFIG/LINEIN").Split(new char[] { '|' })[0];
                        CF_params.Media.recordLine = pluginConfig.ReadField("/APPCONFIG/LINEIN").Split(new char[] { '|' })[1];
                    }
                    catch
                    {
                        base.pluginConfig.WriteField("/APPCONFIG/LINEIN", "-1|-1", true);
                        _dabRadioLineDev = "";
                        _dabRadioLine = "";
                    }


                    if (!_isBufferRadio)
                    {
                        _dabRadioLineDev = CF_params.Media.playbackDevice;
                        _dabRadioLine = CF_params.Media.playbackLine;
                    }
                    else
                    {
                        _dabRadioLineDev = CF_params.Media.recordDevice;
                        _dabRadioLine = CF_params.Media.recordLine;
                    }


                    //Buffer or line?
                    WriteLog("Audio Buffer Enabled: '" + _isBufferRadio.ToString() + "'");
                    if (_isBufferRadio)
                    {
                        CF_updateBufferStatus(CF_params.pluginName, _isBufferRadio);
                        CF_setMixerMute(_dabRadioLineDev, _dabRadioLineDev, false, true);
                        CF_initRecord(_dabRadioLineDev, _dabRadioLine);
                        CF_clearRecordBuffer(true);
                    }
                    else
                    {
                        CF_clearRecordBuffer(false);
                        CF_setMixerMute(_dabRadioLineDev, _dabRadioLine, false, true);
                        CF_updateBufferStatus(CF_params.pluginName, _isBufferRadio);
                    }

                    //Start playing
                    WriteLog("Start Playing: '" + intNewDABFMMode.ToString() + "' '" + DABRadioChannel.ToString() + "'");
                    if (WaitForBoard() && PlayStream(intNewDABFMMode, DABRadioChannel))
                    {
                        WriteLog("Success setting FM/DAB mode/frequency: '" + DABRadioChannel.ToString() + "'");
                    }
                    else
                    {
                        WriteLog("Error setting channel/frequency: '" + DABRadioChannel.ToString() + "'");
                    }

                    //Clear the MOT segment buffer after changing DAB channel
                    WriteLog("Clear MOTs");
                    if (intNewDABFMMode == RADIO_TUNE_BAND.DAB_BAND)
                    {
                        MotReset(MotMode.SlideShow);
                        MotReset(MotMode.EPG);
                    }

                    //If RadioVIS enabled
                    if (boolEnableInternetUsage && boolEnableRadioVIS) RadioVIS(DABRadioChannel);
                    
                    //Start thread for updating the GUI with RDS information
                    WriteLog("Create and enable the IOComms Thread");
                    threadIOComms = new Thread(this.IOCommsThread);
                    boolIOCommsThread = true;
                    threadIOComms.Start();

                    //All good
                    result = true;
                }
            }
            catch { }

            this.WriteLog("StartAudio() - end");
            return result;
        }

        //Keep scanning when in FM mode (Scan up to next FM Freq every 5 seconds)
        private void newsubSCANFM()
        {
            WriteLog("Start of 'newsubSCANFM' thread");
            do
            {
                SeekFreq(RADIO_DIRECTION.UP);
                Thread.Sleep(FMSCANSleep); // Sleep before repeating scanning for next FM
            }
            while (newboolSCANFM);
            WriteLog("End of 'newsubSCANFM' thread");
        }

#endregion

#region System Functions

        private void LoadSettings()
        {
            WriteLog("LoadSettings() - start");
           
            #region Get Config settings
            //Get Configuration
            try
            {
                WriteLog("App Load Config File");

                try { boolDABMinimal = bool.Parse(this.pluginConfig.ReadField("/APPCONFIG/MINISTATUS")); }
                catch { boolDABMinimal = false; }
                WriteLog("XML MiniStatus: " + boolDABMinimal.ToString());

                try { boolDABFMMonkeyReScan = bool.Parse(this.pluginConfig.ReadField("/APPCONFIG/RESCAN")); }
                catch { boolDABFMMonkeyReScan = false; }
                WriteLog("XML Rescan: " + boolDABFMMonkeyReScan.ToString());

                try { boolDABFMMonkeyChinaMode = bool.Parse(this.pluginConfig.ReadField("/APPCONFIG/CHINAMODE")); }
                catch { boolDABFMMonkeyChinaMode = false; }
                WriteLog("XML China Mode: " + boolDABFMMonkeyChinaMode.ToString());

                try { boolEnableInternetUsage = bool.Parse(this.pluginConfig.ReadField("/APPCONFIG/ENABLEINTERNETUSAGE")); }
                catch { boolEnableInternetUsage = false; }
                WriteLog("XML DAB Enable Internet Usage: " + boolEnableInternetUsage.ToString());

                try { boolEnableRadioVIS = bool.Parse(this.pluginConfig.ReadField("/APPCONFIG/ENABLERADIOVIS")); }
                catch { boolEnableRadioVIS = false; }
                WriteLog("XML DAB Enable RadioVIS: " + boolEnableRadioVIS.ToString());

                /*
                try { boolClearBoardBeforeScan = bool.Parse(this.pluginConfig.ReadField("/APPCONFIG/CLEARBOARDBEFORESCAN")); }
                catch { boolClearBoardBeforeScan = false; }
                WriteLog("XML DAB Clear Before Scan: " + boolClearBoardBeforeScan.ToString());
                */

                try 
                { 
                    _isBufferRadio = bool.Parse(this.pluginConfig.ReadField("/APPCONFIG/BUFFERRADIO"));

                    //Only do this if we entered Setup after plugin has started
                    if (!boolFirstTime)
                    {
                        ShutdownRadio(); // Shutdown
                        StartAudio(); //Start
                    }
                }
                catch { _isBufferRadio = false; }
                WriteLog("XML Audio Buffer Enabled: " + _isBufferRadio.ToString());

                try { boolDABLongName = (DABNameMode)sbyte.Parse(this.pluginConfig.ReadField("/APPCONFIG/DABTEXTMODE")); }
                catch { boolDABLongName = DABNameMode.Short; }
                WriteLog("XML DAB Long Station Name: " + boolDABLongName.ToString());

                try { boolEnablePlugin = bool.Parse(this.pluginConfig.ReadField("/APPCONFIG/ENABLEPLUGIN")); }
                catch { boolEnablePlugin = false;  }
                WriteLog("XML Plugin enabled as Radio: " + boolEnablePlugin.ToString());

                try { boolButtonMode = bool.Parse(this.pluginConfig.ReadField("/APPCONFIG/BUTTONMODE")); }
                catch { boolButtonMode = false; }
                WriteLog("XML Next/Prev in Legacy Mode: " + boolEnablePlugin.ToString());

                //Make sure a volume value is defined
                try 
                {
                    //As no value is set, write the default value (Max) to disk, but use numbers
                    if (this.pluginConfig.ReadField("/APPCONFIG/VOLUME") == "") this.pluginConfig.WriteField("/APPCONFIG/VOLUME", ((sbyte)Volume.Max).ToString(), true);

                    DABFMMonkeyVolume = (Volume)sbyte.Parse(this.pluginConfig.ReadField("/APPCONFIG/VOLUME"));
                    if (init && RadioCommand == MonkeyCommand.NONE) RadioCommand = MonkeyCommand.SETVOLUME;
                }
                catch { DABFMMonkeyVolume = Volume.Max;  }
                WriteLog("XML Volume: '" + DABFMMonkeyVolume.ToString() + "'");

                //Make sure a listrows value is defined
                try
                {
                    //As no value is set, write the default value to disk, but use numbers
                    if (this.pluginConfig.ReadField("/APPCONFIG/LISTROWS") == "") this.pluginConfig.WriteField("/APPCONFIG/LISTROWS", sbListRows.ToString(), true);
                    sbListRows = (SByte)sbyte.Parse(this.pluginConfig.ReadField("/APPCONFIG/LISTROWS"));
                }
                catch { sbListRows = 4; }
                WriteLog("XML ListRows: '" + sbListRows.ToString() + "'");

                //Define the advancedlistview template to use
                listMain.TemplateID = sbListRows.ToString() + "_default";

                //Make sure a mono/stereo mode is defined
                try
                {
                    //If no value is set, write the default value (Auto) to disk.
                    if (this.pluginConfig.ReadField("/APPCONFIG/STEREOMODE") == "") this.pluginConfig.WriteField("/APPCONFIG/STEREOMODE", Mode.AUTO.ToString(), true);

                    //Get the mode
                    STEREOMODE = (Mode)Enum.Parse(typeof(Mode), this.pluginConfig.ReadField("/APPCONFIG/STEREOMODE"));
                }
                catch { STEREOMODE = Mode.AUTO; }
                WriteLog("XML STEREOMODE: '" + STEREOMODE.ToString() + "'");

                //Make sure a layout value is defined
                try {
                    //As no value is set, write the default value (Stretched) to disk.
                    if (this.pluginConfig.ReadField("/APPCONFIG/SLSLAYOUT") == "") this.pluginConfig.WriteField("/APPCONFIG/SLSLAYOUT", SLSSize.ToString(), true);

                    ///Get the layout size and define the click event action
                    SLSLayout SLSSize_Temp = (SLSLayout)Enum.Parse(typeof(SLSLayout), this.pluginConfig.ReadField("/APPCONFIG/SLSLAYOUT"), true); //Update with new size
                    this.CF_createPictureBoxClick(SLSID, new MouseEventHandler(SLSClick)); //set Click event

                    //If changed (or different from default), hide the current picture
                    if (SLSSize_Temp != SLSSize) HideSLS();

                    SLSSize = SLSSize_Temp;
                }             
                catch { SLSSize = SLSLayout.Stretched; }
                WriteLog("XML SLSPicture Layout: '" + SLSSize.ToString() + "'");

                try { intNewDABFMMode = (RADIO_TUNE_BAND)Enum.Parse(typeof(RADIO_TUNE_BAND), this.pluginConfig.ReadField("/APPCONFIG/ACTIVEBAND"), true); }
                catch { }
                WriteLog("XML ACTIVEBAND: '" + intNewDABFMMode.ToString() + "'");

                try { 
                    intLASTDAB = UInt32.Parse(this.pluginConfig.ReadField("/APPCONFIG/LASTDAB"));
                    intLASTDAB = fixFreq(RADIO_TUNE_BAND.DAB_BAND, intLASTDAB);
                }
                catch { }
                WriteLog("XML LASTDAB: '" + intLASTDAB.ToString() + "'");

                try { 
                    intLASTFM = UInt32.Parse(this.pluginConfig.ReadField("/APPCONFIG/LASTFM"));                    
                    intLASTFM = fixFreq(RADIO_TUNE_BAND.FM_BAND, intLASTFM);
                }
                catch { }
                WriteLog("XML LASTFM: '" + intLASTFM.ToString() + "'");

                if (intNewDABFMMode == RADIO_TUNE_BAND.DAB_BAND) DABRadioChannel = intLASTDAB; else DABRadioChannel = intLASTFM;
                WriteLog("XML ACTIVE FREQ/CH: '" + DABRadioChannel.ToString() + "'");

                //ECC Region set?
                WriteLog("XML ECCREGION");
                try
                {
                    string strECCRegion = this.pluginConfig.ReadField("/APPCONFIG/ECCREGION").ToUpper();

                    if (strECCRegion.Length != 0)
                    {
                        WriteLog("ECCRegion : '" + strECCRegion + "'");
                        aryECCRegion = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/ECC_" + strECCRegion).Split(',');
                    }
                    else
                    {
                        //Add all
                        string[] aryECCList = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/ECCREGIONLIST").ToUpper().Split(',');
                        string[] aryECCTemp = new string[0];
                        foreach (string s in aryECCList)
                        {
                            string[] x = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/ECC_" + s).Split(',');
                            string[] z = new string[aryECCTemp.Length + x.Length];
                            
                            // Add to array
                            aryECCTemp .CopyTo(z, 0);
                            x.CopyTo(z, aryECCTemp.Length);
                            aryECCTemp = z;
                        }                      

                        //Remove Duplicates
                        aryECCRegion = aryECCTemp.Distinct().ToArray();
                        WriteLog("No Region Selected. Adding all ECC values");
                    }
                }
                catch { }                
                foreach (string s in aryECCRegion) WriteLog("XML ECC Regions: '" + s + "'");

            }
            catch { }

            // If XML exists with values, then use them, else default (hardcoded) values will be used
            try
            {
                //Get Configuration
                WriteLog("XML Config File");

                this.CF_params.displayName = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/DISPLAYNAME");
                WriteLog("XML DisplayName : " + this.CF_params.displayName);

                //Define Launch hotkey
                string strHotkeyTemp = "";
                try { strHotkeyTemp = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/HOTKEYLAUNCH"); }
                catch { strHotkeyTemp = ""; }
                if (strLaunchHotkey != strHotkeyTemp && strHotkeyTemp != "" && strHotkeyTemp != null)
                {
                    strLaunchHotkey = strHotkeyTemp;
                    WriteLog("Launch Hotkey is '" + strLaunchHotkey + "'");
                    CF_loadHotkey(strLaunchHotkey, "Centrafuse.CFActions.Plugin:" + PluginName);
                }
                else WriteLog("XML Launch Hotkey: Unchanged or not set");

                //Define strHotkeyLoadNextTrack hotkey
                try { strHotkeyTemp = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/HOTKEYLOADNEXTTRACK"); }
                catch { strHotkeyTemp = ""; }
                if (strHotkeyLoadNextTrack != strHotkeyTemp && strHotkeyTemp != "" && strHotkeyTemp != null)
                {
                    strHotkeyLoadNextTrack = strHotkeyTemp;
                    WriteLog("Load Next Track Hotkey is '" + strHotkeyLoadNextTrack + "'");
                    CF_loadHotkey(strHotkeyLoadNextTrack, "Centrafuse.CFActions.Plugin:" + PluginName + ",LoadNextTrack");
                }
                else WriteLog("XML Load Next Track Hotkey: Unchanged or not set");

                //Define strHotkeyLoadPreviousTrack hotkey                          
                try { strHotkeyTemp = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/HOTKEYLOADPREVIOUSTRACK"); }
                catch { strHotkeyTemp = ""; }
                if (strHotkeyLoadPreviousTrack != strHotkeyTemp && strHotkeyTemp != "" && strHotkeyTemp != null)
                {
                    strHotkeyLoadPreviousTrack = strHotkeyTemp;
                    WriteLog("Load Previous Track Hotkey is '" + strHotkeyLoadPreviousTrack + "'");
                    CF_loadHotkey(strHotkeyLoadPreviousTrack, "Centrafuse.CFActions.Plugin:" + PluginName + ",LoadPreviousTrack");
                }
                else WriteLog("XML Load Previous Track Hotkey: Unchanged or not set");

                //Define strHotkeyRadioSeekForward hotkey                            
                try { strHotkeyTemp = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/HOTKEYRADIOSEEKFORWARD"); }
                catch { strHotkeyTemp = ""; }
                if (strHotkeyRadioSeekForward != strHotkeyTemp && strHotkeyTemp != "" && strHotkeyTemp != null)
                {
                    strHotkeyRadioSeekForward = strHotkeyTemp;
                    WriteLog("Radio Seek Forward Hotkey is '" + strHotkeyRadioSeekForward + "'");
                    CF_loadHotkey(strHotkeyRadioSeekForward, "Centrafuse.CFActions.Plugin:" + PluginName + ",RadioSeekForward");
                }
                else WriteLog("XML Radio Seek Forward Hotkey: Unchanged or not set");

                //Define strHotkeyRadioSeekBack hotkey                            
                try { strHotkeyTemp = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/HOTKEYRADIOSEEKBACK"); }
                catch { strHotkeyTemp = ""; }
                if (strHotkeyRadioSeekBack != strHotkeyTemp && strHotkeyTemp != "" && strHotkeyTemp != null)
                {
                    strHotkeyRadioSeekBack = strHotkeyTemp;
                    WriteLog("Radio Seek Back Hotkey is '" + strHotkeyRadioSeekBack + "'");
                    CF_loadHotkey(strHotkeyRadioSeekBack, "Centrafuse.CFActions.Plugin:" + PluginName + ",RadioSeekBack");
                }
                else WriteLog("XML Radio Seek Back Hotkey: Unchanged or not set");

                //Define strHotkeyFM hotkey                            
                try { strHotkeyTemp = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/HOTKEYFM"); }
                catch { strHotkeyTemp = ""; }
                if (strHotkeyFM != strHotkeyTemp && strHotkeyTemp != "" && strHotkeyTemp != null)
                {
                    strHotkeyFM = strHotkeyTemp;
                    WriteLog("FM Hotkey is '" + strHotkeyFM + "'");
                    CF_loadHotkey(strHotkeyFM, "Centrafuse.CFActions.Plugin:" + PluginName + ",FM");
                }
                else WriteLog("XML FM Hotkey: Unchanged or not set");

                //Define strHotkeyDAB hotkey                            
                try { strHotkeyTemp = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/HOTKEYDAB"); }
                catch { strHotkeyTemp = ""; }
                if (strHotkeyDAB != strHotkeyTemp && strHotkeyTemp != "" && strHotkeyTemp != null)
                {
                    strHotkeyDAB = strHotkeyTemp;
                    WriteLog("DAB Hotkey is '" + strHotkeyDAB + "'");
                    CF_loadHotkey(strHotkeyDAB, "Centrafuse.CFActions.Plugin:" + PluginName + ",DAB");
                }
                else WriteLog("XML DAB Hotkey: Unchanged or not set");

                //Define strHotkeyScan hotkey                            
                try { strHotkeyTemp = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/HOTKEYSCAN"); }
                catch { strHotkeyTemp = ""; }
                if (strHotkeyScan != strHotkeyTemp && strHotkeyTemp != "" && strHotkeyTemp != null)
                {
                    strHotkeyScan = strHotkeyTemp;
                    WriteLog("Scan Hotkey is '" + strHotkeyScan + "'");
                    CF_loadHotkey(strHotkeyScan, "Centrafuse.CFActions.Plugin:" + PluginName + ",SCAN");
                }
                else WriteLog("XML Scan Hotkey: Unchanged or not set");

                //Define strHotkeyToggleDABFM hotkey                            
                try { strHotkeyTemp = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/HOTKEYTOGGLEDABFM"); }
                catch { strHotkeyTemp = ""; }
                if (strHotkeyToggleDABFM != strHotkeyTemp && strHotkeyTemp != "" && strHotkeyTemp != null)
                {
                    strHotkeyToggleDABFM = strHotkeyTemp;
                    WriteLog("Toggle DAB / FM Hotkey is '" + strHotkeyToggleDABFM + "'");
                    CF_loadHotkey(strHotkeyToggleDABFM, "Centrafuse.CFActions.Plugin:" + PluginName + ",ToggleDABFM");
                }
                else WriteLog("XML Toggle DAB / FM Hotkey: Unchanged or not set");
                
                //Define HotkeyLoadPreset1-8 hotkeys                                
                for (int p = 0; p < 8; p++)
                {                
                    try {
                        strHotkeyTemp = this.pluginLang.ReadField("/APPCONFIG/HOTKEYPRESET" + (p+1).ToString());
                    }
                    catch { strHotkeyTemp = ""; }

                    if (aryHotkeyLoadPreset[p] != strHotkeyTemp && strHotkeyTemp != "" && strHotkeyTemp != null)
                    {
                        aryHotkeyLoadPreset[p] = strHotkeyTemp;
                        WriteLog("Load Preset " + (p+1).ToString() + " Hotkey '" + aryHotkeyLoadPreset[p] + "'");
                        CF_loadHotkey(aryHotkeyLoadPreset[p], "Centrafuse.CFActions.Plugin:" + PluginName + ",LoadPreset" + (p+1).ToString());
                    }
                    else WriteLog("aryHotkeyLoadPreset" + (p+1).ToString() + ": Unchanged or not set");
                }

                try { DABFMMonkeyUSBVID = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/VID"); }
                catch { }
                WriteLog("XML VID: '" + DABFMMonkeyUSBVID + "'");
                
                try { aryDABFMMonkeyUSBPID = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/PID").Split(','); }
                catch { }
                foreach (string s in aryDABFMMonkeyUSBPID) WriteLog("XML PID: '" + s + "'");
                                
                try {                    
                    DABFMMonkeyCOMPort = "\\\\.\\" + this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/PORT"); 
                }
                catch { }
                WriteLog("XML PORT: '" + DABFMMonkeyCOMPort + "'");

                try { aryProgramType = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/PROGRAMTYPES").Split(','); }
                catch { }
                // Log the 32 types:
                foreach (string s in aryProgramType) WriteLog("XML PROGRAMTYPES: '" + s + "'");

                WriteLog("XML STATUS MESSAGES");
                try { aryDABStatus = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/STATUS").Split(','); }
                catch { }
                // Log the differet status messages
                foreach (string s in aryDABStatus) WriteLog("XML STATUS: '" + s + "'");

                // If all else fails, use these values
                string startDAB = "";
                string endDAB = "";
                try
                {
                    startDAB = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/SCANSTART");
                    endDAB = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/SCANEND");
                }
                catch { }
                WriteLog("XML SCANSTART & SCANEND values: '" + startDAB + "' '" + endDAB + "'");

                //Region set?
                WriteLog("XML REGION");
                try
                {
                    DABFMMonkeyRegion = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/REGION").ToUpper();

                    if (DABFMMonkeyRegion.Length != 0)
                    {
                        WriteLog("Region : '" + DABFMMonkeyRegion + "'");

                        string[] aryDABRegion = new string[0];
                        aryDABRegion = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/" + DABFMMonkeyRegion).Split(',');

                        startDAB = aryDABRegion[0];
                        endDAB = aryDABRegion[1];

                        WriteLog("XML SCANSTART & SCANEND values: '" + startDAB + "' '" + endDAB + "'");
                    }
                    else
                    {
                        WriteLog("No Region Selected");
                        WriteLog("XML SCANSTART & SCANEND values: '" + startDAB + "' '" + endDAB + "'");
                    }
                }
                catch { }

                // Figure out the real Start and End
                try
                {
                    if (boolDABFMMonkeyChinaMode)
                    {
                        WriteLog("Using China DAB frequencies");
                        for (DABFMMonkeyScanStartIndex = 0; DABFMMonkeyScanStartIndex < DABChinaFrequencyIndex.Length; DABFMMonkeyScanStartIndex++)
                            if (DABChinaFrequencyIndex[DABFMMonkeyScanStartIndex].IndexOf(startDAB) >= 0) break;

                        //Convert to Chinese index
                        DABFMMonkeyScanStartIndex = (byte)(DABFMMonkeyScanStartIndex + 41);
                    }
                    else
                    {
                        WriteLog("Using standard DAB frequencies");
                        for (DABFMMonkeyScanStartIndex = 0; DABFMMonkeyScanStartIndex < DABStandardFrequencyIndex.Length; DABFMMonkeyScanStartIndex++)
                            if (DABStandardFrequencyIndex[DABFMMonkeyScanStartIndex].IndexOf(startDAB) >= 0) break;
                    }
                }
                catch { }
                WriteLog("XML Real DAB Start Scan '" + DABFMMonkeyScanStartIndex.ToString() + "' '" + startDAB + "'");

                try
                {
                    if (boolDABFMMonkeyChinaMode)
                    {
                        WriteLog("Using China DAB frequencies");
                        for (DABFMMonkeyScanEndIndex = 0; DABFMMonkeyScanEndIndex < DABChinaFrequencyIndex.Length; DABFMMonkeyScanEndIndex++)
                            if (DABChinaFrequencyIndex[DABFMMonkeyScanEndIndex].IndexOf(endDAB) >= 0) break;

                        //Convert to Chinese index
                        DABFMMonkeyScanEndIndex = (byte)(DABFMMonkeyScanEndIndex + 41);
                    }
                    else
                    {
                        WriteLog("Using standard DAB frequencies");
                        for (DABFMMonkeyScanEndIndex = 0; DABFMMonkeyScanEndIndex < DABStandardFrequencyIndex.Length; DABFMMonkeyScanEndIndex++)
                            if (DABStandardFrequencyIndex[DABFMMonkeyScanEndIndex].IndexOf(endDAB) >= 0) break;
                    }
                }
                catch { }
                WriteLog("XML Real DAB End Scan '" + DABFMMonkeyScanEndIndex.ToString() + "' '" + endDAB + "'");

            }
            catch { WriteLog("No, or corrupt, XML Configuration file"); }
            #endregion
            
            // Get the favorites
            _favStationsList.Clear();
            _favStationsList = LoadFavourites();
            WriteLog("_favStationsList loaded with " + this._favStationsList.Count.ToString() + " favourite stations.");

            // Get the blacklisted
            _blackList.Clear();
            _blackList = LoadBlacklisted();
            WriteLog("_blackList loaded with " + this._blackList.Count.ToString() + " blacklisted stations.");

            //Get the BBE/EQ Settings
            _BBEEQ = LoadBBEEQ();
            WriteLog("_BBEEQ Loaded");

            //Clear the list first
            FavDtList.Clear();
            foreach (Station favouriteStation in this._favStationsList)
            {
                DataRow row = FavDtList.NewRow();
                row["DisplayName"] = favouriteStation.FavoriteName;
                FavDtList.Rows.Add(row);
            }

            //Clear the list first
            BlkDtList.Clear();
            foreach (BlackList BlacklistStation in this._blackList)
            {
                DataRow row = BlkDtList.NewRow();
                row["DisplayName"] = BlacklistStation.DABLongName;
                BlkDtList.Rows.Add(row);
            }

            // Make sure to refresh the list, or it will appear blank!
            listMain.Refresh();

            //Get Font used for Tune/Select
            strFontClass = SkinReader.GetControlAttribute("DABFMMonkey", "TuneSelect", "fontclass".ToLower(), base.pluginSkinReader);
            fontSize = Byte.Parse(strFontClass.Substring(strFontClass.Length - 2));

            //CF Config:
            WriteLog("StartupAudio: " + CF_getConfigFlag(CF_ConfigFlags.StartupAudio).ToString());

            //Enable the plugin to control the radio?
            if (boolEnablePlugin && !init) enableplugin();

            WriteLog("LoadSettings() - end");
        }

        private void WriteLog(string msg)
        {
            try
            {
                if (Boolean.Parse(this.pluginConfig.ReadField("/APPCONFIG/LOGEVENTS")))
                {
                    if (msg.Equals("startup"))
                        CFTools.writeModuleLog(msg, LogFilePath);
                    else
                        CFTools.writeModuleLog((new StackTrace(true)).GetFrame(1).GetMethod().Name + ": " + msg, LogFilePath);
                }
            }
            catch { }
        }

#endregion

#region CF events

        // Fired when the power mode of the operating system changes
        private void OnPowerModeChanged(object sender, CFPowerModeChangedEventArgs e)
        {
            WriteLog("OnPowerModeChanged - start()");
            WriteLog("OnPowerModeChanged '" + e.Mode.ToString() + "'");

            CFTools.writeLog(PluginName, "OnPowerModeChanged", e.Mode.ToString());
           
            //If suspending
            if (e.Mode == CFPowerModes.Suspend)
            {
                //Save current status
                SaveCurrentStatus();

                WriteLog("Send STOPSTREAM");
                if (init) RadioCommand = MonkeyCommand.STOPSTREAM;
                while (RadioCommand != MonkeyCommand.NONE) System.Threading.Thread.Sleep(100); //Allow time for Comms I/O thread to execute the command

                WriteLog("Send CLOSERADIOPORT");
                if (init) RadioCommand = MonkeyCommand.CLOSERADIOPORT;
                while (RadioCommand != MonkeyCommand.NONE) System.Threading.Thread.Sleep(100); //Allow time for Comms I/O thread to execute the command

                // Radio no longer initialized
                threadIOComms.Abort(); // Kill the thread
                init = false;
                WriteLog("init '" + init.ToString() + "'");
            }

            //If resuming from sleep
            if (e.Mode == CFPowerModes.Resume)
            {
                enableplugin();
            }

            WriteLog("OnPowerModeChanged - end()");
            return;
        }

        // If the plugin uses back/forward buttons, we need to catch the left/right keyboard commands too...
		private void DABFMMonkey_KeyDown(object sender, KeyEventArgs e)
		{
			e.Handled = true;
		}

#endregion

#region AdvancedListView

        private void OnLinkedItemClick(object sender, LinkedItemArgs e)
        {
            WriteLog("OnLinkedItemClick - Start");

            // You could have other linked actions, and can test for which on it is by checking the LinkId property
            if (e.LinkId == "Delete")
            {
                int nSelected = e.ItemId;
                if (nSelected < 0) return;
                DeleteClick(nSelected);
            }
            else if (e.LinkId == "Blacklist")
            {
                int nSelected = e.ItemId;
                if (nSelected < 0) return;
                BlacklistClick(nSelected);
            }
            WriteLog("OnLinkedItemClick - end");
        }


#endregion

    }
}

