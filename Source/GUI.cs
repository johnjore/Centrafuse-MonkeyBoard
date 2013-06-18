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
 * All GUI related functions
*/

using System;
using System.IO;
using System.Windows.Forms;
using System.Data;
using centrafuse.Plugins;
using System.Threading;
using System.Xml.Serialization;
using System.Collections.Generic;

using CFControlsExtender.Base;      //Advanced ListView

/*using System.Xml;
using Microsoft.Win32;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using CFControlsExtender.Imaging;
using CFControlsExtender.ItemsBuilder;*/

namespace DABFMMonkey
{
    public partial class DABFMMonkey : CFPlugin
    {

#region Variables
        //Hotkeys for steering wheel controls
        private string strLaunchHotkey = "";
        private string strHotkeyLoadNextTrack = "";
        private string strHotkeyLoadPreviousTrack = "";
        private string strHotkeyRadioSeekForward = "";
        private string strHotkeyRadioSeekBack = "";
        private string strHotkeyFM = "";
        private string strHotkeyDAB = "";
        private string strHotkeyScan = "";
        private string strHotkeyToggleDABFM = "";
        private string[] aryHotkeyLoadPreset = new string[] { "CONTROL+F2", "CONTROL+F3", "CONTROL+F4", "CONTROL+F5", "CONTROL+F6", "CONTROL+F7", "CONTROL+F8", "CONTROL+F9" };

        //Default SLS Picture size to use
        private SLSLayout SLSSize = SLSLayout.Stretched;
        private string SLSID = "SLSPicture"; //ID of picture box

        //Rows to show in listbox
        private sbyte sbListRows = 4; // Default number of rows

        //Lists, favorites and blacklisted
        private List<Station> _favStationsList = new List<Station>();
        private List<BlackList> _blackList = new List<BlackList>();
        
        private System.Windows.Forms.Timer timerSlider = new System.Windows.Forms.Timer();  //Timer to keep Slider text updated with slider position
        private bool boolEQFirstPress = true; // First time the EQ button pressed?
        private bool boolButtonMode = false;    // Top, right buttons in legacy or new mode. New Mode = Scan for next/prev channel/freq. Legacy = next/prev fav
        private bool boolDABMinimal = false;    // Minimal GUI


        // Advanced List View
        delegate void SetListVisableCallback(bool vis);

        // The following variables are what we will use to set up our list view
        private static CFControls.CFAdvancedList listMain;
        private BindingSource listBindingSource;
        private DataTable FavDtList;                   // Make sure to include System.Data
        private DataTable BlkDtList;                   // Make sure to include System.Data
        private DataTable RadDtList;                   // Make sure to include System.Data
        private System.Windows.Forms.Timer pagingTimer;
        private int pagingCount = 0;
        private CFControls.CFListView.PagingDirection pagingDirection = CFControls.CFListView.PagingDirection.DOWN;

#endregion

#region GUIFunctions

        //Update screen with RDS information
        private void IOCommsThread()
        {
            WriteLog("Start of 'IOCommsThread()' thread");
            do
            {
                //Process commands and get data from board, if init
                if (init)
                {
                    IOCommsSub();
                }
                else Thread.Sleep(2000); //Sleep if not initialized                
            }
            while (boolIOCommsThread);
            WriteLog("End of 'IOCommsThread()' thread");
        }

        //Clear RDS information
        private void ClearRDSVars()
        {
            WriteLog("ClearRDSVars() - Start");

            strCachedProgramText = "";
            _stationText = "";
            _stationName = "";

            //Assume changing channels so hide SLS
            HideSLS();

            WriteLog("ClearRDSVars() - End");
        }

        //Show SlideShow Images and hide favorites
        private void ShowSLS(string strImageFilename)
        {
            WriteLog("ShowSLS() - Start");

            //If not in normal mode, don't show the image, else it will overlay deleting and blacklisting
            if (listMain.TemplateID != sbListRows.ToString() + "_default")
            {
                WriteLog("Not showing SLS image as list view is not in default mode.");
                return;
            }

            //Hide favlist. use thread safe function to set visible state for list
            SetListVisable(false);

            //Show picture
            try
            {                
                WriteLog("SLS FileName: '" + strImageFilename + "', show it. Size: '" + SLSSize.ToString() + "'");
                
                CFControls.CFPictureBox PictureBox = pictureboxArray[CF_getPictureBoxID(SLSID)];
                PictureBox.Bounds = base.CF_createRect(SkinReader.ParseBounds(SkinReader.GetControlAttribute("DABFMMonkey", SLSID, (SLSSize.ToString() + "_bounds").ToLower(), base.pluginSkinReader)));
                CF_setPictureImage(SLSID, CFTools.ImageFromFile(strImageFilename));
                CF_setPictureBoxEnableFlag(SLSID, true);
            }
            catch { }
                        
            //Remove picture file to keep filesystem tidy
            try
            {
                System.IO.File.Delete(strImageFilename);
                WriteLog("Removed File '" + strImageFilename + "'");
            }
            catch (System.IO.IOException e)
            {
                WriteLog("Failed to remove '" + strImageFilename + "' Error:" + e.Message);
            }

            WriteLog("ShowSLS() - End");
        }

        //Hide SlideShow Images and show favorites
        private void HideSLS()
        {
            WriteLog("HidewSLS() - Start");

            //Hide PictureBox
            CF_clearPictureImage(SLSID);
            CF_setPictureBoxEnableFlag(SLSID, false);

            // use thread safe function to set visible state for list
            SetListVisable(true);

            WriteLog("HidewSLS() - End");
        }

        // Switch between FM and DAB mode
        private void DABFMClick()
        {
            WriteLog("DABFMClick() - Start");

            switch (intDABFMMode)
            {
                case RADIO_TUNE_BAND.FM_BAND:
                    WriteLog("Selecting DAB");

                    //If there are programs in the board, we can switch to DAB
                    if (intTotalProgram > 0)
                    {
                        if (SetTuneBand(RADIO_TUNE_BAND.DAB_BAND)) WriteLog("Success Selecting DAB"); else WriteLog("Failed Selecting DAB");
                    }
                    else
                    {
                        WriteLog("No DAB Programs, will start a Scan");
                        if (init && RadioCommand == MonkeyCommand.NONE) RadioCommand = MonkeyCommand.SCANDAB;
                    }
                    break;
                case RADIO_TUNE_BAND.DAB_BAND:
                    WriteLog("Selecting FM");
                    //Swapping to FM, hide SLS
                    HideSLS();
                    if (SetTuneBand(RADIO_TUNE_BAND.FM_BAND)) WriteLog("Success Selecting FM"); else WriteLog("Failed Selecting FM");
                    break;
                case RADIO_TUNE_BAND.UNDEFINED:
                    WriteLog("Selecting FM, but only because we're undefined. Should never happen...");
                    if (SetTuneBand(RADIO_TUNE_BAND.FM_BAND)) WriteLog("Success Selecting FM"); else WriteLog("Failed Selecting FM");
                    //Swapping to FM, hide SLS
                    HideSLS();
                    break;
                default:
                    WriteLog("Unknown Mode: '" + intDABFMMode.ToString() + "'");
                    break;
            }

            WriteLog("DABFMClick() - End");
        }

        //Bring up PayPal Web screen... 
        private void PayPalClick()
        {
            WriteLog("PayPalClick() - Start");

            CF_systemCommand(CF_Actions.PLUGIN, "WEB", "BROWSE", "http://www.paypal.com", "FULLSCREEN");
            CF_systemCommand(CF_Actions.PLUGIN, "WEB");
            this.CF_displayMessage("If you find the plugin useful, feel free to make a donation to john@jore.no");

            WriteLog("PayPalClick() - End");
        }

        //Clicked on the SLS Picture
        private void SLSClick(object sender, MouseEventArgs e)
        {
            WriteLog("SLSClick() - Start");

            //Hide the SLS Picture
            HideSLS();

            WriteLog("SLSClick() - End");
        }

        // Scan for next FM, or do a full DAB frequency scan
        private void ScanClick(object sender, MouseEventArgs e)
        {
            WriteLog("ScanClick() - Start");

            try
            {
                switch (intDABFMMode)
                {
                    case RADIO_TUNE_BAND.DAB_BAND:
                        if (init && RadioCommand == MonkeyCommand.NONE) RadioCommand = MonkeyCommand.SCANDAB;
                        break;
                    case RADIO_TUNE_BAND.FM_BAND:
                        if (newboolSCANFM)
                        {
                            //Set button state (not pressed)
                            CF_setButtonOff("SCAN");

                            //Stop the thread
                            newboolSCANFM = false;
                        }
                        else
                        {
                            //Set button state (pressed)
                            CF_setButtonOn("SCAN");

                            //Start FM thread for scanning freq
                            newboolSCANFM = true;
                            newthreadSCANFM.Start();
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception errmsg)
            {
                CFTools.writeError(errmsg.ToString());
            }

            WriteLog("ScanClick() - End");
        }

        //Force Mono or Auto mode
        private void SetStereoModeClick()
        {
            WriteLog("SetStereoModeClick() - Start");
            
            if (STEREOMODE != Mode.UNDEFINED)
            {
                switch (STEREOMODE)
                {
                    case Mode.AUTO:
                        STEREOMODE = Mode.MONO;
                        break;
                    case Mode.MONO:
                        STEREOMODE = Mode.AUTO;
                        break;
                }

                if (SetStereoMode(STEREOMODE))
                {
                    Mode TempMode = GetStereoMode();
                    if (TempMode == STEREOMODE) 
                    {
                        WriteLog("Stero mode validated: '" + STEREOMODE.ToString() + "'");
                        this.CF_systemCommand(CF_Actions.SHOWINFO, base.pluginLang.ReadField("/APPLANG/SETUP/STEREOMODE" + STEREOMODE.ToString()), "AUTOHIDE");
                    }
                    else WriteLog("Stero mode wrong");                    
                }
                else WriteLog("Failed to set Stero mode");
            }
            else
            {
                WriteLog("STEREOMODE undefined");
            }

            WriteLog("SetStereoModeClick() - End");
        }

        private void TuneClick()
        {
            WriteLog("TuneClick() - Start"); //Text is "Select" when in DAB mode

            try
            {
                if (_isBufferRadio)
                {
                    this.WriteLog("Calling CF_clearRecordBuffer(true)");
                    base.CF_clearRecordBuffer(true);
                }

                switch (intDABFMMode)
                {
                    case RADIO_TUNE_BAND.FM_BAND:
                        try
                        {
                            string resultval, resulttext;
                            base.CF_systemDisplayDialog(CF_Dialogs.NumberPad, "Enter Frequency MHz", out resultval, out resulttext);

                            resulttext = resulttext.Replace("*", ".").Replace("#", ".");
                            string _stationFreq = resulttext;
                            double freq = Convert.ToDouble(resulttext) * 1000.0;
                            UInt32 iFreq = Convert.ToUInt32(freq);
                            
                            //Sanity check the value
                            iFreq = fixFreq(RADIO_TUNE_BAND.FM_BAND, iFreq);

                            this.WriteLog("TUNE_Click: resulttext= " + resulttext + " iFreq Hz= " + iFreq.ToString());
                            TuneFreq(iFreq);
                        }
                        catch { }

                        break;
                    case RADIO_TUNE_BAND.DAB_BAND:
                        if (init && RadioCommand == MonkeyCommand.NONE) RadioCommand = MonkeyCommand.TUNESELECT;
                        break;
                    default:
                        this.WriteLog("Undefined Mode");
                        break;
                }
            }
            catch (Exception errmsg)
            {
                CFTools.writeError(errmsg.ToString());
            }

            WriteLog("TuneClick() - End"); //Text is "Select" when in DAB mode
        }
        
        private void TuneSelect()
        {
            WriteLog("TuneSelect() - Start");
            try
            {
                object resultobject;
                string resultvalue, resulttext;

                // Array's can be resized in C#, figure out the total - blacklist = intCounter
                // Populate the list with the programs
                bool boolBlackListed = false;
                int i = 0;
                int intCounter = 0;
                foreach (string s in aryDABChannelsLong)
                {
                    WriteLog("Channel Name (Long): '" + s + "'");
                    boolBlackListed = false;
                    for (int j = 0; j < _blackList.Count; j++)
                    {
                        //If we find a match, blacklisted it
                        if (s == _blackList[j].DABLongName) boolBlackListed = true;
                    }

                    if (!boolBlackListed) intCounter++;
                }

                //Create the array
                CFControls.CFListViewItem[] textoptions = new CFControls.CFListViewItem[intCounter];
                boolBlackListed = false;
                i = 0;
                int intTempStation = -1;
                foreach (string s in aryDABChannelsLong)
                {
                    WriteLog("Channel Name (Long): '" + s + "'");
                    boolBlackListed = false;
                    for (int j = 0; j < _blackList.Count; j++)
                    {
                        //If we find a match, blacklisted it
                        if (s == _blackList[j].DABLongName) boolBlackListed = true;
                    }

                    if (!boolBlackListed)
                    {
                        WriteLog("Channel Name: '" + s + "'");
                        textoptions[i++] = new CFControls.CFListViewItem(s, i.ToString(), -1, false);
                      
                        if (strNewDABLongName == s)
                        {
                            WriteLog("Found a match at: '" + i.ToString() + "' Array: '" + s + "'");
                            intTempStation = i;
                        }
                    }
                    else
                    {
                        WriteLog("Blacklisted: '" + s + "'");
                    }
                }            
                

                //If nothing found, then start at the beginning
                if (intTempStation < 0) intTempStation = 1;

                // Display the options
                if (this.CF_systemDisplayDialog(CF_Dialogs.FileBrowser,
                    base.pluginLang.ReadField("/APPLANG/SETUP/SELECTSTATION"),
                    base.pluginLang.ReadField("/APPLANG/SETUP/SELECTSTATION"),
                    intTempStation.ToString(), out resultvalue, out resulttext, out resultobject, textoptions, false, false, false, false, false, false, 1) == DialogResult.OK)
                {
                    //Tune to new radio channel
                    WriteLog("Resultvalue / text: '" + resultvalue + "' / '" + resulttext + "'");
                    try {
                        //Clear the command early, as TuneFreq(), launched from TuneSelect() will not launch as existing command is in progress
                        RadioCommand = MonkeyCommand.NONE;
                        
                        //Find the board Index # for the users selection
                        for (UInt32 j = 0; j < aryDABChannelsLong.Length; j++)
                        {
                            WriteLog("Looking for '" + resulttext + "'   Channel Name (Long): '" + aryDABChannelsLong[j] + "'");

                            //If a match is found, note our position and exit out
                            if (resulttext == aryDABChannelsLong[j])
                            {
                                WriteLog("Found a match at: '" + j.ToString() + "' Array: '" + aryDABChannelsLong[j] + "'");
                                // Update intNewStation with our results
                                intNewStation = j;
                                break; //Found it, so exit
                            }
                        }

                        TuneFreq(intNewStation);
                        WriteLog("Current Radio command value: " + RadioCommand.ToString());
                    }
                    catch { }
                }
            }
            catch (Exception errmsg) { CFTools.writeError(errmsg.Message, errmsg.StackTrace); }

            WriteLog("TuneSelect() - End");
        }

        private void FwdTuneClick()
        {
            WriteLog("FwdTuneClick() - Start");

            try
            {
                if (intDABFMMode == RADIO_TUNE_BAND.DAB_BAND)
                {
                    this.WriteLog("Forward Fine tune DAB");
                    if (intTotalProgram > 0 && intTotalProgram < MAXDABChannels) TuneFreq(intTotalProgram - 1);
                }
                else if (intDABFMMode == RADIO_TUNE_BAND.FM_BAND)
                {
                    this.WriteLog("Forward tune FM");
                    TuneFreq(intCurrentStation + 50);
                }
            }
            catch (Exception errmsg)
            {
                CFTools.writeError(errmsg.ToString());
            }

            WriteLog("FwdTuneClick() - End");
        }

        private void FwdFineTuneClick(object sender, MouseEventArgs e)
        {
            WriteLog("FwdFineTuneClick() - Start");

            try
            {
                SeekFreq(RADIO_DIRECTION.UP);
            }
            catch (Exception errmsg)
            {
                CFTools.writeError(errmsg.ToString());
            }

            WriteLog("FwdFineTuneClick() - End");
        }

        private void BackTuneClick()
        {
            WriteLog("BackTuneClick() - Start");

            try
            {
                if (intDABFMMode == RADIO_TUNE_BAND.DAB_BAND)
                {
                    this.WriteLog("Back Fine tune DAB");
                    TuneFreq(0);
                }
                else if (intDABFMMode == RADIO_TUNE_BAND.FM_BAND)
                {
                    this.WriteLog("Back tune FM");
                    TuneFreq(intCurrentStation - 50);
                }
            }
            catch (Exception errmsg)
            {
                CFTools.writeError(errmsg.ToString());
            }

            WriteLog("BackTuneClick() - End");
        }

        private void BackFineTuneClick(object sender, MouseEventArgs e)
        {
            this.WriteLog("BackFineTuneClick() - Start");
            try
            {
                SeekFreq(RADIO_DIRECTION.DOWN);
            }
            catch (Exception errmsg)
            {
                CFTools.writeError(errmsg.ToString());
            }

            this.WriteLog("BackFineTuneClick() - End");
        }

        //Add to favorites
        private void AddfavBtnClick()
        {
            this.WriteLog("AddfavBtnClick() - Start");

            if (init && RadioCommand == MonkeyCommand.NONE) RadioCommand = MonkeyCommand.ADDFAVBTNCLICK;

            this.WriteLog("AddfavBtnClick() - End");
        }
    
        //Thread Safe. Only Called during init or main thread
        private void AddFavorites()
        {
            this.WriteLog("AddFavorites() - Start");
            string resultvalue, resulttext;

            try
            {
                //Get Program Name (Long)
                string strProgramName = "";
                try
                {
                    if ((intCurrentStation != 999) && (intDABFMMode != RADIO_TUNE_BAND.UNDEFINED))
                    {
                        string strtextBuffer = new string(' ', constBufferSize); // Read buffer
                        if (GetProgramName(intDABFMMode, intCurrentStation, DABNameMode.Long, strtextBuffer))
                        {
                            strProgramName = strtextBuffer.Trim();
                            WriteLog("Success - GetProgramName: '" + strtextBuffer + "' Trimmed: '" + strProgramName + "'");
                        }
                        else WriteLog("Failed - GetProgramName");
                    }
                    else WriteLog("GetProgramName - Not ready");
                }
                catch { WriteLog("Exception Thrown Getting Data"); }

                //Get Program Name (Long) if in DAB mode as we'll use it instead of board's Channel index
                string strDABLongName = "";
                if (intDABFMMode == RADIO_TUNE_BAND.DAB_BAND)
                {
                    try
                    {
                        if ((intCurrentStation != 999) && (intDABFMMode != RADIO_TUNE_BAND.UNDEFINED))
                        {
                            string strtextBuffer = new string(' ', constBufferSize); // Read buffer
                            if (GetProgramName(intDABFMMode, intCurrentStation, DABNameMode.Long, strtextBuffer))
                            {
                                strDABLongName = strtextBuffer.Trim();
                                WriteLog("Success - GetProgramName: '" + strtextBuffer + "' Trimmed: '" + strDABLongName + "'");
                            }
                            else WriteLog("Failed - GetProgramName");
                        }
                        else WriteLog("GetProgramName - Not ready");
                    }
                    catch { WriteLog("Exception Thrown Getting Data"); }
                }

                //If no program name, use current freq
                if ((strProgramName == "") && (intCurrentStation != 999)) strProgramName = intCurrentStation.ToString();
                

                //Append band
                //DAB mode
                if (intDABFMMode == RADIO_TUNE_BAND.DAB_BAND)
                {                  
                    //Get Service Type (DAB, DAB+, PacketData or DMB)
                    ServCompType intServCompType = ServCompType.Undefined;
                    try
                    {
                        if ((intCurrentStation != 999) && WaitForBoard()) intServCompType = (ServCompType)GetServCompType(intCurrentStation);
                    }
                    catch { WriteLog("Exception Thrown Getting Service Type"); }

                    // We should only be in DAB or DAB+ mode as we dont know how to process the other modes...
                    switch (intServCompType)
                    {
                        case ServCompType.DAB_plus: strProgramName = strProgramName + " (" + base.pluginLang.ReadField("/APPLANG/SETUP/DAB") + "+)";
                            break;
                        default: strProgramName = strProgramName + " (" + base.pluginLang.ReadField("/APPLANG/SETUP/DAB") + ")";
                            break;
                    }
                }

                //FM Mode
                if (intDABFMMode == RADIO_TUNE_BAND.FM_BAND) strProgramName = strProgramName + " (" + base.pluginLang.ReadField("/APPLANG/SETUP/FM") + " / " + Math.Round((decimal)intCurrentStation / 1000, 2).ToString() + ")";

                //If all ok, ask user for fav name
                if ((intCurrentStation != 999) && (intDABFMMode != RADIO_TUNE_BAND.UNDEFINED))
                {
                    WriteLog("Current mode and channel: '" + intDABFMMode.ToString() + "' '" + intCurrentStation.ToString() + "'");

                    //Add to favorites list if all ok                    
                    if (CF_systemDisplayDialog(CF_Dialogs.OSK, base.pluginLang.ReadField("/APPLANG/SETUP/STATIONNAME"), strProgramName, out resultvalue, out resulttext) == DialogResult.OK)
                    {
                        WriteLog("Result: '" + resultvalue + "'");

                        Station _favStation = new Station();
                        _favStation.FavoriteName = resultvalue;
                        _favStation.Frequency = intCurrentStation;
                        _favStation.Band = intDABFMMode;
                        _favStation.DABLongName = strDABLongName;
                        WriteLog("FavStation '" + _favStation.FavoriteName + "' '" + _favStation.Frequency.ToString() + "' '" + _favStation.Band + "' '" + _favStation.DABLongName + "'");

                        //Add to favorites list to save to disk
                        _favStationsList.Add(_favStation);

                        // Save favorites to disk
                        SaveFavourites(_favStationsList);

                        //Update datatable
                        DataRow row = FavDtList.NewRow();
                        row["DisplayName"] = _favStation.FavoriteName;
                        FavDtList.Rows.Add(row);

                        //Refresh screen
                        listMain.Refresh();
                    }
                }
            }
            catch
            {
                WriteLog("Failed to add to favorites");
            }

            this.WriteLog("AddFavorites() - End");
        }

        // add station to blacklist file
        private void BlacklistClick(int selected)
        {
            WriteLog("BlacklistClick - Start");
            try
            {
                bool boolDuplicate = false;
                BlackList blk = new BlackList();
                blk.DABLongName = RadDtList.Rows[selected].ItemArray[0].ToString();

                // Check for duplicate
                for (int i=0; i < _blackList.Count; i++)
                {
                    //Duplicate?
                    if (blk.DABLongName == _blackList[i].DABLongName)
                    {
                        //Make sure we don't add channel to file (again)
                        boolDuplicate = true;

                        //Un-blacklist?
                        if (this.CF_systemDisplayDialog(CF_Dialogs.YesNo, base.pluginLang.ReadField("/APPLANG/SETUP/REMOVEBLACKLISTING") + " '" + blk.DABLongName + "' ?") == DialogResult.OK)
                        {
                            _blackList.RemoveAt(i);
                            SaveBlacklist(_blackList);

                            //Update the datatable with the new value
                            RadDtList.Rows[selected][1] = false;

                            //Refresh the row that changed
                            listMain.Invalidate();
                        }
                    }
                }

                //If not duplicate
                if (!boolDuplicate) if (this.CF_systemDisplayDialog(CF_Dialogs.YesNo, base.pluginLang.ReadField("/APPLANG/SETUP/ADDBLACKLISTING") + " '" + blk.DABLongName + "' ?") == DialogResult.OK)
                {
                    _blackList.Add(blk);
                    SaveBlacklist(_blackList);

                    //Update the datatable with the new value
                    RadDtList.Rows[selected][1] = true;

                    //Refresh the row that changed
                    listMain.Invalidate();
                }

            }
            catch (Exception exception)
            {
                CFTools.writeError(exception.Message, exception.StackTrace);
            }
            WriteLog("BlacklistClick - end");
        }

        //Remove current favorite
        private void DeleteClick(int selection)
        {
            WriteLog("DeleteClick() - start");
            try
            {
                //Get current favorite and remove it
                string strFavoriteName = _favStationsList[selection].FavoriteName;

                if (this.CF_systemDisplayDialog(CF_Dialogs.YesNo, base.pluginLang.ReadField("/APPLANG/SETUP/REMOVE") + " '" + strFavoriteName + "' ?") == DialogResult.OK)
                {
                    _favStationsList.RemoveAt(selection);
                    FavDtList.Rows.RemoveAt(selection);

                    // Save modified favorites list to disk
                    SaveFavourites(_favStationsList);

                    // Refresh the datatable
                    listMain.Refresh();

                }
            }
            catch
            {
                WriteLog("Failed to remove a favorite");
            }

            WriteLog("DeleteClick() - end");
        }

        //Scroll up
        private void PageUpClick(object sender, MouseEventArgs e)
        {
            WriteLog("PageUpClick() - start");

            HideSLS(); //Make sure no SLS image is visible, else scrolling is hidden
            OnPageUpClick();
            WriteLog("PageUpClick() - end");
        }

        private void PageUpRelease(object sender, MouseEventArgs e)
        {
            WriteLog("PageUpRelease() - start");
            WriteLog("PageUpRlease() - end");
        }

        //Scroll down
        private void PageDownClick(object sender, MouseEventArgs e)
        {
            WriteLog("PageDownClick() - start");

            HideSLS(); //Make sure no SLS image is visible, else scrolling is hidden
            OnPageDownClick();
            WriteLog("PageDownClick() - end");
        }

        private void PageDownRelease(object sender, MouseEventArgs e)
        {
            WriteLog("PageDownRelase() - start");
            WriteLog("PageDownRelase() - end");
        }

        //Load favorites from disk
        public List<Station> LoadFavourites()
        {
            WriteLog("LoadFavourites() - start");

            //File name for Favorites
            string FavFileName = CFTools.AppDataPath + PluginPath + FavoritesFile;

            List<Station> lfs = new List<Station>();
            if (File.Exists(FavFileName))
            {
                try
                {
                    WriteLog("LoadFavourites() Favorites - deserialize");
                    XmlSerializer SerializerObj = new XmlSerializer(typeof(List<Station>));
                    Stream ReadFileStream = new FileStream(FavFileName, FileMode.Open);
                    lfs = (List<Station>)SerializerObj.Deserialize(ReadFileStream);
                    ReadFileStream.Close();
                }
                catch (Exception exception1)
                {
                    Exception ex = exception1;
                    WriteLog("LoadFavourites() EXCEPTION: " + ex.ToString());
                }
            }

            this.WriteLog("LoadFavourites() - end");
            return lfs;
        }

        //Load Blacklisted from disk
        public List<BlackList> LoadBlacklisted()
        {
            WriteLog("LoadBlacklisted() - start");

            //File name for Blacklisted
            string BlacklistFileName = CFTools.AppDataPath + PluginPath + BlackListedFile;

            List<BlackList> lfs = new List<BlackList>();
            if (File.Exists(BlacklistFileName))
            {
                try
                {
                    WriteLog("LoadBlacklisted()  - deserialize");
                    XmlSerializer SerializerObj = new XmlSerializer(typeof(List<BlackList>));
                    Stream ReadFileStream = new FileStream(BlacklistFileName, FileMode.Open);
                    lfs = (List<BlackList>)SerializerObj.Deserialize(ReadFileStream);
                    ReadFileStream.Close();
                }
                catch (Exception exception1)
                {
                    Exception ex = exception1;
                    WriteLog("LoadBlacklisted() EXCEPTION: " + ex.ToString());
                }
            }

            this.WriteLog("LoadBlacklisted() - end");
            return lfs;
        }
        
        //Save favorites to disk
        public void SaveFavourites(List<Station> fs)
        {
            this.WriteLog("SaveFavourites() - start");

            try
            {
                XmlSerializer SerializerObj = new XmlSerializer(typeof(List<Station>));
                string FavFileName = CFTools.AppDataPath + PluginPath + FavoritesFile;
                TextWriter WriteFileStream = new StreamWriter(FavFileName);
                SerializerObj.Serialize(WriteFileStream, fs);
                WriteFileStream.Close();
            }
            catch (Exception ex)
            {
                this.WriteLog("SaveFavourites() EXCEPTION: " + ex.ToString());
            }

            this.WriteLog("SaveFavourites() - end");
        }

        //Save blacklist to disk
        public void SaveBlacklist(List<BlackList> fs)
        {
            this.WriteLog("SaveBlacklist() - start");

            try
            {
                XmlSerializer SerializerObj = new XmlSerializer(typeof(List<BlackList>));
                string BlacklListFileName = CFTools.AppDataPath + PluginPath + BlackListedFile;
                TextWriter WriteFileStream = new StreamWriter(BlacklListFileName);
                SerializerObj.Serialize(WriteFileStream, fs);
                WriteFileStream.Close();
            }
            catch (Exception ex)
            {
                this.WriteLog("SaveBlacklist() EXCEPTION: " + ex.ToString());
            }

            this.WriteLog("SaveBlacklist() - end");
        }

        //Load BBEEQ from disk
        public BBEEQ LoadBBEEQ()
        {
            WriteLog("LoadBBEEQ() - start");

            //File name for Blacklisted
            string BBEEQConfigurationFileName = CFTools.AppDataPath + PluginPath + BBEEQFile;

            BBEEQ _BBEEQSettings = new BBEEQ();
            if (File.Exists(BBEEQConfigurationFileName))
            {
                try
                {
                    WriteLog("LoadBBEEQ()  - deserialize");
                    XmlSerializer SerializerObj = new XmlSerializer(typeof(BBEEQ));
                    Stream ReadFileStream = new FileStream(BBEEQConfigurationFileName, FileMode.Open);
                    _BBEEQSettings = (BBEEQ)SerializerObj.Deserialize(ReadFileStream);
                    ReadFileStream.Close();
                }
                catch (Exception exception1)
                {
                    Exception ex = exception1;
                    WriteLog("LoadBBEEQ() EXCEPTION: " + ex.ToString());
                }
            }

            this.WriteLog("LoadBBEEQ() - end");
            return _BBEEQSettings;
        }

        //Save BBEEQ to disk
        public void SaveBBEEQ(BBEEQ fs)
        {
            this.WriteLog("SaveBBEEQ() - start");

            try
            {
                XmlSerializer SerializerObj = new XmlSerializer(typeof(BBEEQ));
                string BBEEQFileName = CFTools.AppDataPath + PluginPath + BBEEQFile;
                TextWriter WriteFileStream = new StreamWriter(BBEEQFileName);
                SerializerObj.Serialize(WriteFileStream, fs);
                WriteFileStream.Close();
            }
            catch (Exception ex)
            {
                this.WriteLog("SaveBBEEQ() EXCEPTION: " + ex.ToString());
            }

            this.WriteLog("SaveBBEEQ() - end");
        }

        //Timer for holding forward or back button in for ajusting boards volume
        private void TimerVolumeUpDown()
        {
            WriteLog("TimerUpdDown() - start");

            intVolumeTimerButton = 0; // Set to 0 each time the timer is initialized            

            while (true) //Keep looping until killed by the change of ButtonState
            {
                //WriteLog("CF_pluginCMLCommand Timer Duration: '" + intTimerUpDown.ToString() + "'");
                intVolumeTimerButton = intVolumeTimerButton + intvolumeSleepTimer;

                if (intVolumeTimerButton > 1000) //Button held, change volume
                {
                    WriteLog("CF_pluginCMLCommand CHANGE VOLUME");
                    DABVolume(DABVolumeDirection); //Change the volume in the direction set by the variable
                }

                Thread.Sleep(intvolumeSleepTimer); // Sleep, else we'll ajust volume to max or min too fast
            }

            //WriteLog("TimerUpdDown() - end");
        }

#endregion

#region MixerFunctions

        //Bring up BBE Config screen
        private void BBEClick()
        {
            WriteLog("BBEClick() - Start");

            this.CF3_initSection("Mixer");

            //Button text
            base.CF_updateButtonText("Off", this.pluginLang.ReadField("/APPLANG/SETUP/OFF"));
            base.CF_updateButtonText("BBE", this.pluginLang.ReadField("/APPLANG/SETUP/BBE"));
            base.CF_updateButtonText("EQ", this.pluginLang.ReadField("/APPLANG/SETUP/EQ"));
            base.CF_updateButtonText("Exit", this.pluginLang.ReadField("/APPLANG/SETUP/EXIT"));
            
            //Get current BBEEQ status from board and setup the buttons
            if (init && RadioCommand == MonkeyCommand.NONE) RadioCommand = MonkeyCommand.GETBBEEQ;

            //Wait until we have the BBEEQ Values
            WriteLog("Waiting...");
            do { Thread.Sleep(20); } while (RadioCommand != MonkeyCommand.NONE);

            WriteLog("Success: " + _BBEEQ.BBEOn.ToString() + " " + _BBEEQ.EQMode.ToString() + " " + _BBEEQ.BBELo.ToString() + " " + _BBEEQ.BBEHi.ToString() + " " + _BBEEQ.BBECFreq.ToString() + " " + _BBEEQ.BBEMachFreq.ToString() + " " + _BBEEQ.BBEMachGain.ToString() + " " + _BBEEQ.BBEMachQ.ToString() + " " + _BBEEQ.BBESurr.ToString() + " " + _BBEEQ.BBEMp.ToString() + " " + _BBEEQ.BBEHpF.ToString() + " " + _BBEEQ.BBEHiMode.ToString());

            //Set button status
            switch (_BBEEQ.BBEOn)
            {
                case BBEStatus.Off:
                    WriteLog("On,Off,Off");
                    CF_setButtonOn("Off");
                    CF_setButtonOff("BBE");
                    CF_setButtonOff("EQ");
                    
                    //No BBE items
                    BBEObjects(false); // Do not show BBE GUI items
                    timerSlider.Enabled = false; // Do not update from sliders

                    //No EQ items
                    boolEQFirstPress = false;

                    break;
                case BBEStatus.BBE:
                    WriteLog("Off,On,Off");

                    //Set button status
                    CF_setButtonOff("Off");
                    CF_setButtonOn("BBE");
                    CF_setButtonOff("EQ"); 

                    //Enable BBE items
                    BBEObjects(true); // Show BBE GUI items
                    timerSlider.Enabled = true; // Update from slider values

                    //No EQ items
                    boolEQFirstPress = false;

                    break;
                case BBEStatus.EQ:
                    WriteLog("Off,Off,On");
                    CF_setButtonOff("Off");
                    CF_setButtonOff("BBE");
                    CF_setButtonOn("EQ");
                    
                    //No BBE items
                    BBEObjects(false); // Do not show BBE GUI items
                    timerSlider.Enabled = false; // Do not update from sliders

                    //Set button text to current value                   
                    string[] aryEQList = this.pluginLang.ReadField("/APPLANG/SETUP/EQMODES").Split(',');
                    base.CF_updateButtonText("MixerEQ", aryEQList[_BBEEQ.EQMode]);

                    break;
            }

            WriteLog("BBEClick() - End");
        }

        void OnTimerSlider_Tick(object sender, EventArgs e)
        {
            //WriteLog("Timer Updates() - start");

            //WriteLog("BBELo");
            _BBEEQ.BBELo = (sbyte)buttonArray[CF_getButtonID("Slider_BBELo")].sliderValue;
            if (_BBEEQ.BBELo <= 0) _BBEEQ.BBELo = 0;
            if (_BBEEQ.BBELo >= 24) _BBEEQ.BBELo = 24;
            base.CF_updateText("Value_BBELo", Math.Round((decimal)_BBEEQ.BBELo / 2, 1).ToString() + "dB");

            //WriteLog("BBEHi");
            _BBEEQ.BBEHi = (sbyte)buttonArray[CF_getButtonID("Slider_BBEHi")].sliderValue;
            if (_BBEEQ.BBEHi <= 0) _BBEEQ.BBEHi = 0;
            if (_BBEEQ.BBEHi >= 24) _BBEEQ.BBEHi = 24;
            base.CF_updateText("Value_BBEHi", Math.Round((decimal)_BBEEQ.BBEHi / 2, 1).ToString() + "dB");

            //WriteLog("BBECFreq");
            _BBEEQ.BBECFreq = (sbyte)buttonArray[CF_getButtonID("Slider_BBECFreq")].sliderValue;
            if (_BBEEQ.BBECFreq < 1) _BBEEQ.BBECFreq = 0; else _BBEEQ.BBECFreq = 1;
            if (_BBEEQ.BBECFreq == 1) base.CF_updateText("Value_BBECFreq", "1KHz"); else base.CF_updateText("Value_BBECFreq", "595Hz");

            //WriteLog("BBEMachFreq");
            _BBEEQ.BBEMachFreq = (byte)((buttonArray[CF_getButtonID("Slider_BBEMachFreq")].sliderValue * 30) + 60); //60, 90, 120, 150
            if (_BBEEQ.BBEMachFreq <= 60) _BBEEQ.BBEMachFreq = 60;
            if (_BBEEQ.BBEMachFreq >= 150) _BBEEQ.BBEMachFreq = 150;
            base.CF_updateText("Value_BBEMachFreq", _BBEEQ.BBEMachFreq + "Hz");

            //WriteLog("BBEMachGain");
            _BBEEQ.BBEMachGain = (sbyte)(buttonArray[CF_getButtonID("Slider_BBEMachGain")].sliderValue * 4); //0, 4, 8, 12
            if (_BBEEQ.BBEMachGain <= 0) _BBEEQ.BBEMachGain = 0;
            if (_BBEEQ.BBEMachGain >= 12) _BBEEQ.BBEMachGain = 12;
            base.CF_updateText("Value_BBEMachGain", _BBEEQ.BBEMachGain.ToString() + "dB");

            //WriteLog("BBEMachQ");
            _BBEEQ.BBEMachQ = (sbyte)(buttonArray[CF_getButtonID("Slider_BBEMachQ")].sliderValue); // 1 or 3
            if (_BBEEQ.BBEMachQ < 1) _BBEEQ.BBEMachQ = 1; else _BBEEQ.BBEMachQ = 3;
            base.CF_updateText("Value_BBEMachQ", _BBEEQ.BBEMachQ.ToString());

            //WriteLog("BBESurr");
            _BBEEQ.BBESurr = (sbyte)(buttonArray[CF_getButtonID("Slider_BBESurr")].sliderValue);
            if (_BBEEQ.BBESurr <= 0) _BBEEQ.BBESurr = 0;
            if (_BBEEQ.BBESurr >= 10) _BBEEQ.BBESurr = 10;
            base.CF_updateText("Value_BBESurr", _BBEEQ.BBESurr.ToString() + "dB");

            //WriteLog("BBEMp");
            _BBEEQ.BBEMp = (sbyte)(buttonArray[CF_getButtonID("Slider_BBEMp")].sliderValue);
            if (_BBEEQ.BBEMp <= 0) _BBEEQ.BBEMp = 0;
            if (_BBEEQ.BBEMp >= 10) _BBEEQ.BBEMp = 10;
            base.CF_updateText("Value_BBEMp", _BBEEQ.BBEMp.ToString() + "dB");

            //WriteLog("BBEHpF");
            _BBEEQ.BBEHpF = (byte)(buttonArray[CF_getButtonID("Slider_BBEHpF")].sliderValue * 10);
            if (_BBEEQ.BBEHpF <= 20) _BBEEQ.BBEHpF = 20;
            if (_BBEEQ.BBEHpF >= 250) _BBEEQ.BBEHpF = 250;
            base.CF_updateText("Value_BBEHpF", _BBEEQ.BBEHpF.ToString() + "Hz");

            //WriteLog("BBEHiMode");
            _BBEEQ.BBEHiMode = (sbyte)(buttonArray[CF_getButtonID("Slider_BBEHiMode")].sliderValue);
            if (_BBEEQ.BBEHiMode <= 0) _BBEEQ.BBEHiMode = 0;
            if (_BBEEQ.BBEHiMode >= 10) _BBEEQ.BBEHiMode = 10;
            base.CF_updateText("Value_BBEHiMode", _BBEEQ.BBEHiMode.ToString() + "dB");

            //WriteLog("Headroom");
            _BBEEQ.HeadRoom = (sbyte)(buttonArray[CF_getButtonID("Slider_HeadRoom")].sliderValue);
            if (_BBEEQ.HeadRoom <= -12) _BBEEQ.HeadRoom = -12;
            if (_BBEEQ.HeadRoom >= 0) _BBEEQ.HeadRoom = 0;
            base.CF_updateText("Value_HeadRoom", _BBEEQ.HeadRoom.ToString() + "dB");

            //WriteLog("Save BBE Settings");
            if (init && RadioCommand == MonkeyCommand.NONE) RadioCommand = MonkeyCommand.SETBBEEQ;

            //WriteLog("Timer Updates() - start");
        }

        private void BBEObjects(bool boolStatus)
        {
            WriteLog("BBEObjects() - start");

            //Move to init?
            string[] aryBBEGUIItems = new string[] { "BBELo", "BBEHi", "BBECFreq", "BBEMachFreq", "BBEMachGain", "BBEMachQ", "BBESurr", "BBEMp", "BBEHpF", "BBEHiMode", "HeadRoom" };
            string[] aryBBEGUILang = this.pluginLang.ReadField("/APPLANG/SETUP/BBETEXT").Split(',');

            //Show the sliders with header and values
            for (sbyte i = 0; i < aryBBEGUIItems.Length; i++)
            {
                WriteLog("Update '" + i.ToString() + "' '" + aryBBEGUIItems[i] + "' '" + aryBBEGUILang[i] + "'");

                //Enable Header and Value labels
                CF_setLabelEnableFlag("Label_" + aryBBEGUIItems[i], boolStatus);
                CF_setLabelEnableFlag("Value_" + aryBBEGUIItems[i], boolStatus);
                CF_updateText("Label_" + aryBBEGUIItems[i], aryBBEGUILang[i]);

                //Slider status
                base.CF_setButtonEnableFlag("Slider_" + aryBBEGUIItems[i], boolStatus);
                buttonArray[CF_getButtonID("Slider_" + aryBBEGUIItems[i])].sliderControl = boolStatus;
            }

            WriteLog("Success: " + _BBEEQ.BBEOn.ToString() + " " + _BBEEQ.EQMode.ToString() + " " + _BBEEQ.BBELo.ToString() + " " + _BBEEQ.BBEHi.ToString() + " " + _BBEEQ.BBECFreq.ToString() + " " + _BBEEQ.BBEMachFreq.ToString() + " " + _BBEEQ.BBEMachGain.ToString() + " " + _BBEEQ.BBEMachQ.ToString() + " " + _BBEEQ.BBESurr.ToString() + " " + _BBEEQ.BBEMp.ToString() + " " + _BBEEQ.BBEHpF.ToString() + " " + _BBEEQ.BBEHiMode.ToString());

            //Set Slider values and captions
            WriteLog("Slider values");
            base.CF_setSlider("Slider_BBELo", _BBEEQ.BBELo);
            WriteLog("BBELo: " + _BBEEQ.BBELo.ToString());

            base.CF_setSlider("Slider_BBEHi", _BBEEQ.BBEHi);
            WriteLog("BBEHi: " + _BBEEQ.BBEHi.ToString());

            if (_BBEEQ.BBECFreq == 0) base.CF_setSlider("Slider_BBECFreq", 0); else base.CF_setSlider("Slider_BBECFreq", 2);
            WriteLog("BBECFreq: " + _BBEEQ.BBECFreq.ToString());

            base.CF_setSlider("Slider_BBEMachFreq", ((_BBEEQ.BBEMachFreq - 60) / 30));
            WriteLog("BBEMachFreq: " + _BBEEQ.BBEMachFreq.ToString());

            base.CF_setSlider("Slider_BBEMachGain", (_BBEEQ.BBEMachGain / 4));
            WriteLog("BBEMachGain: " + _BBEEQ.BBEMachGain.ToString());

            if (_BBEEQ.BBEMachQ <= 1) base.CF_setSlider("Slider_BBEMachQ", 0); else base.CF_setSlider("Slider_BBEMachQ", 2);
            WriteLog("BBEMachQ: " + _BBEEQ.BBEMachQ.ToString());

            base.CF_setSlider("Slider_BBESurr", _BBEEQ.BBESurr);
            WriteLog("BBESurr: " + _BBEEQ.BBESurr.ToString());

            base.CF_setSlider("Slider_BBEMp", _BBEEQ.BBEMp);
            WriteLog("BBEMp: " + _BBEEQ.BBEMp.ToString());

            base.CF_setSlider("Slider_BBEHpF", (_BBEEQ.BBEHpF / 10));
            WriteLog("BBEHpF: " + _BBEEQ.BBEHpF.ToString());

            base.CF_setSlider("Slider_BBEHiMode", _BBEEQ.BBEHiMode);
            WriteLog("BBEHiMode: " + _BBEEQ.BBEHiMode.ToString());

            base.CF_setSlider("Slider_HeadRoom", _BBEEQ.HeadRoom);
            WriteLog("HeadRoom: " + _BBEEQ.HeadRoom.ToString());

            WriteLog("BBEObjects() - end");
        }

        //Set BBE
        private void MixerBBE()
        {
            //Set to BBE mode
            _BBEEQ.BBEOn = BBEStatus.BBE;
           
            //Write to board
            if (init && RadioCommand == MonkeyCommand.NONE) RadioCommand = MonkeyCommand.SETBBEEQ;
            do { Thread.Sleep(20); } while (RadioCommand != MonkeyCommand.NONE);

            //Re-draw the Mixer screen with new button statuses
            BBEClick();
        }
        
        //Mixer Eq button
        private void MixerEq()
        {
            if (!boolEQFirstPress)
            {
                boolEQFirstPress = true;

                // Set to EQ Mode
                _BBEEQ.BBEOn = BBEStatus.EQ;

                //Write to board
                if (init && RadioCommand == MonkeyCommand.NONE) RadioCommand = MonkeyCommand.SETBBEEQ;
                do { Thread.Sleep(20); } while (RadioCommand != MonkeyCommand.NONE);
            }
            else
            {
                try
                {
                    object tempobject;
                    string resultvalue, resulttext;
                    string[] aryEQList = this.pluginLang.ReadField("/APPLANG/SETUP/EQMODES").Split(',');

                    // Create a listview with the number of items in the Array
                    CFControls.CFListViewItem[] textoptions = new CFControls.CFListViewItem[aryEQList.Length];

                    // Populate the list with the options                
                    for (int i = 0; i < aryEQList.Length; i++)
                    {
                        WriteLog("EQ Mode: '" + aryEQList[i].ToString() + "'");
                        textoptions[i] = new CFControls.CFListViewItem(aryEQList[i].ToString(), i.ToString(), -1, false);
                    }

                    // Display the options
                    if (this.CF_systemDisplayDialog(CF_Dialogs.FileBrowser,
                       this.pluginLang.ReadField("/APPLANG/SETUP/MIXER"),
                       this.pluginLang.ReadField("/APPLANG/SETUP/MIXER"),
                       _BBEEQ.EQMode.ToString(), out resultvalue, out resulttext, out tempobject, textoptions, false, false, false, false, false, false, 1) == DialogResult.OK)
                    {
                        WriteLog("Result text and value: " + resulttext + " " + resultvalue);
                        _BBEEQ.BBEOn = BBEStatus.EQ;
                        _BBEEQ.EQMode = sbyte.Parse(resultvalue);

                        //Set EQ mode
                        if (init && RadioCommand == MonkeyCommand.NONE) RadioCommand = MonkeyCommand.SETBBEEQ;
                        do { Thread.Sleep(20); } while (RadioCommand != MonkeyCommand.NONE);
                    }

                    //MixerEQ button text matches current EQ
                    base.CF_updateButtonText("MixerEQ", resulttext);
                }
                catch (Exception errmsg) { CFTools.writeError(errmsg.Message, errmsg.StackTrace); }
            }

            //Re-draw the Mixer screen with new button statuses
            BBEClick();
        }

        //No BBE or EQ
        private void MixerOff()
        {
            WriteLog("MixerOff() - start");

            try
            {
                _BBEEQ.BBEOn = BBEStatus.Off;
                
                //Set new BBE EQ Status
                if (init && RadioCommand == MonkeyCommand.NONE) RadioCommand = MonkeyCommand.SETBBEEQ;
                do { Thread.Sleep(10); } while (RadioCommand != MonkeyCommand.NONE);

            }
            catch (Exception errmsg) { CFTools.writeError(errmsg.Message, errmsg.StackTrace); }

            //Re-draw the Mixer screen with new button statuses
            BBEClick();

            WriteLog("MixerEq() - end");
        }

#endregion

#region AdvancedListView
        
        private void OnPageUpClick()
        {
            WriteLog("OnPageUpClick - Start");
            listMain.PageUp();
            WriteLog("OnPageUpClick - end");
        }

        private void OnPageDownClick()
        {
            WriteLog("OnPageDownClick - Start");
            listMain.PageDown();
            WriteLog("OnPageDownClick - end");
        }

        //Play what the user selected
        private void OnListClick(object sender, ThrowScrollPanelMouseEventArgs e)
        {
            WriteLog("OnListClick - Start");

            try
            {
                //Only do this if we're in default mode (Not _blacklisted or _delete)
                //This could/should probably be fixed the CML command area if we're pedantic and want it done "proper", or cleanly...
                if (listMain.TemplateID.ToUpper() == (sbListRows.ToString() + "_default").ToUpper())
                {
                    // Grab our currently selected item and show its details (attached to the Value DataTable column)
                    int selection = GetSelectedValue();
                    WriteLog("FavLstSingleClick()- Index=" + selection.ToString());

                    SetSelectedValue(selection);
                }
            }
            catch (Exception exception)
            {
                CFTools.writeError(exception.Message, exception.StackTrace);
            }

            WriteLog("OnListClick - end");
        }

        // Helper method to set the channel/freq
        private void SetSelectedValue(int selection)
        {
            WriteLog("SetSelectedValue - Start");

            if (selection != -1)
            {
                intNewDABFMMode = _favStationsList[selection].Band;
                string strFindProgram = _favStationsList[selection].DABLongName;
                WriteLog("FavLstSingleClick()- Band/Freq=" + intNewDABFMMode.ToString() + "/" + intNewStation.ToString() + "/" + strFindProgram);

                //If DAB mode, find the correct index, if FM, just set the frequency
                intNewStation = 999;
                switch (intNewDABFMMode)
                {
                    case RADIO_TUNE_BAND.DAB_BAND:
                        //Find the board Index # for DABLongName
                        for (UInt32 j = 0; j < aryDABChannelsLong.Length; j++)
                        {
                            WriteLog("Looking for '" + strFindProgram + "'   Channel Name (Long): '" + aryDABChannelsLong[j] + "'");

                            //If a match is found, note our position and exit out
                            if (strFindProgram == aryDABChannelsLong[j])
                            {
                                WriteLog("Found a match at: '" + j.ToString() + "' Array: '" + aryDABChannelsLong[j] + "'");
                                // Update intNewStation with our results
                                intNewStation = j;
                                break; //Found it, so exit
                            }
                        }
                        break;
                    case RADIO_TUNE_BAND.FM_BAND:
                        intNewStation = _favStationsList[selection].Frequency;
                        break;
                }

                //If new frequency is not valid, DAB mode failed to find the program
                if (intNewStation != 999)
                {
                    //Play it
                    if (init && RadioCommand == MonkeyCommand.NONE) RadioCommand = MonkeyCommand.PLAYSTREAM;
                }
                else
                {
                    //No match is found.
                    this.CF_systemDisplayDialog(CF_Dialogs.OkBox, base.pluginLang.ReadField("/APPLANG/SETUP/FAVNOTFOUND"));
                }
            }

            WriteLog("SetSelectedValue - End");
        }


        // Helper method to get the currently selected value
        private int GetSelectedValue()
        {
            WriteLog("GetSelectedValue - Start");

            if (listMain.SelectedItems.Count <= 0) return -1;

            int nSelected = listMain.SelectedItems[0];
            if (nSelected < 0) return -1;

            WriteLog("GetSelectedValue - End");
            return nSelected;
        }

        // this function needed to make the list control visible function thread safe
        private void SetListVisable(bool vis)
        {
            WriteLog("SetListVisable - Start");
            if (listMain.InvokeRequired)
            {
                SetListVisableCallback d = new SetListVisableCallback(SetListVisable);
                this.Invoke(d, new object[] { vis });

            }
            else
            {
                listMain.Visible = vis;
            }
            WriteLog("SetListVisable - End");
        }

        // Event to keep paging up/down while the button is held
        private void pagingTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                pagingCount++;
                if (pagingDirection == CFControls.CFListView.PagingDirection.DOWN)
                    listMain.PageDown();
                else
                    listMain.PageUp();
            }
            catch (Exception errmsg) { CFTools.writeError(errmsg.Message, errmsg.StackTrace); }
        }

#endregion

    }
}