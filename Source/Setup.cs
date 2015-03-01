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
 * All CF Setup related functions
*/

using System;
using System.Windows.Forms;
using centrafuse.Plugins;

namespace DABFMMonkey
{
    [System.ComponentModel.DesignerCategory("Code")]
    public class Setup : CFSetup
    {        
#region Variables
        private const string PluginName = "DABFMMonkey";
        private const string PluginPath = @"plugins\DABFMMonkey\";
        private const string PluginPathLanguages = PluginPath + @"Languages\";
        //private const string ConfigurationFile = "config.xml";
        //private const string ConfigSection = "/APPCONFIG/";
        //private const string LanguageSection = "/APPLANG/SETUP/";
        //private const string LanguageControlSection = "/APPLANG/DABFMMONKEY/";
#endregion

#region Construction
        // The setup constructor will be called each time this plugin's setup is opened from the CF Setting Page
        // This setup is opened as a dialog from the CF_pluginShowSetup() call into the main plugin application form.
        public Setup(ICFMain mForm, ConfigReader config, LanguageReader lang)
        {
            // Total configuration pages for each mode
            const sbyte NormalTotalPages = 3;
            const sbyte AdvancedTotalPages = 6;

            // MainForm must be set before calling any Centrafuse API functions
            this.MainForm = mForm;
           
            // pluginConfig and pluginLang should be set before calling CF_initSetup() so this CFSetup instance 
            // will internally save any changed settings.
            this.pluginConfig = config;
            this.pluginLang = lang;

            // When CF_initSetup() is called, the CFPlugin layer will call back into CF_setupReadSettings() to read the page
            // Note that this.pluginConfig and this.pluginLang must be set before making this call
            CF_initSetup(NormalTotalPages, AdvancedTotalPages);

            // Update the Settings page title
            this.CF_updateText("TITLE", this.pluginLang.ReadField("/APPLANG/SETUP/TITLE"));
        }

#endregion

#region CFSetup
        public override void CF_setupReadSettings(int currentpage, bool advanced)
        {
            /*
             * Number of configuration pages is defined in two constsants in Setup(...)
             * const sbyte NormalTotalPages = ;
             * const sbyte AdvancedTotalPages = ;
             */

            try
            {
                int i = CFSetupButton.One;
                
                if (currentpage == 1)
                {
                    // TEXT BUTTONS (1-4)
                    ButtonHandler[i] = new CFSetupHandler(SetPort);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/PORT");
                    ButtonValue[i++] = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/PORT");

                    ButtonHandler[i] = new CFSetupHandler(SetRegion);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/REGION");
                    ButtonValue[i++] = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/REGION");

                    ButtonHandler[i] = new CFSetupHandler(SetVolume);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/VOLUME");
                    ButtonValue[i++] = this.pluginConfig.ReadField("/APPCONFIG/VOLUME");

                    ButtonHandler[i] = new CFSetupHandler(SetSLSLayout);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/SLSLAYOUT");
                    string[] arySLSLayout = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/SLSLAYOUT").ToString().Split(',');
                    ButtonValue[i++] = arySLSLayout[(sbyte)Enum.Parse(typeof(SLSLayout), this.pluginConfig.ReadField("/APPCONFIG/SLSLAYOUT"))];

                    // BOOL BUTTONS (5-8)
                    ButtonHandler[i] = new CFSetupHandler(SetLogEvents);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/LOGEVENTS");
                    ButtonValue[i++] = this.pluginConfig.ReadField("/APPCONFIG/LOGEVENTS");

                    ButtonHandler[i] = new CFSetupHandler(SetEnablePlugin);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/ENABLEPLUGIN");
                    ButtonValue[i++] = this.pluginConfig.ReadField("/APPCONFIG/ENABLEPLUGIN");

                    ButtonHandler[i] = new CFSetupHandler(SetChinaMode);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/CHINAMODE");
                    ButtonValue[i++] = this.pluginConfig.ReadField("/APPCONFIG/CHINAMODE");

                    ButtonHandler[i] = new CFSetupHandler(SetLBandMode);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/LBANDMODE");
                    ButtonValue[i++] = this.pluginConfig.ReadField("/APPCONFIG/LBANDMODE");
                }
                else if (currentpage == 2)
                {
                    ButtonHandler[i] = new CFSetupHandler(SetScanStart);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/SCANSTART");
                    ButtonValue[i++] = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/SCANSTART");

                    ButtonHandler[i] = new CFSetupHandler(SetScanEnd);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/SCANEND");
                    ButtonValue[i++] = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/SCANEND");

                    ButtonHandler[i] = new CFSetupHandler(SetPlayBack);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/PLAYBACK");
                    ButtonValue[i++] = this.pluginConfig.ReadField("/APPCONFIG/PLAYBACK");

                    ButtonHandler[i] = new CFSetupHandler(SetLineIn);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/LINEIN");
                    ButtonValue[i++] = this.pluginConfig.ReadField("/APPCONFIG/LINEIN");

                    // BOOL BUTTONS (5-8)
                    ButtonHandler[i] = new CFSetupHandler(SetDABTextMode);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/DABTEXTMODE");
                    ButtonValue[i++] = this.pluginConfig.ReadField("/APPCONFIG/DABTEXTMODE");

                    ButtonHandler[i] = new CFSetupHandler(SetEnableBuffer);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/BUFFERRADIO");
                    ButtonValue[i++] = this.pluginConfig.ReadField("/APPCONFIG/BUFFERRADIO");

                    ButtonHandler[i] = new CFSetupHandler(SetButtonMode);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/BUTTONMODE");
                    ButtonValue[i++] = this.pluginConfig.ReadField("/APPCONFIG/BUTTONMODE");

                    ButtonHandler[i] = new CFSetupHandler(SetGUIMode);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/MINISTATUS");
                    ButtonValue[i++] = this.pluginConfig.ReadField("/APPCONFIG/MINISTATUS");
                }
                else if (currentpage == 3)
                {
                    ButtonHandler[i] = new CFSetupHandler(SetDisplayName);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/DISPLAYNAME");
                    ButtonValue[i++] = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/DISPLAYNAME");

                    ButtonHandler[i] = new CFSetupHandler(ECCRegion);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/ECCREGION");
                    ButtonValue[i++] = this.pluginConfig.ReadField("/APPCONFIG/ECCREGION");

                    ButtonHandler[i] = new CFSetupHandler(SetPID);
                    ButtonText[i] = this.pluginLang.ReadField("APPLANG/SETUP/PID");
                    ButtonValue[i++] = this.pluginLang.ReadField("APPLANG/DABFMMONKEY/PID");

                    ButtonHandler[i] = new CFSetupHandler(SetListRows);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/LISTROWS");
                    ButtonValue[i++] = this.pluginConfig.ReadField("/APPCONFIG/LISTROWS");
                  
                    // BOOL BUTTONS (5-8)
                    ButtonHandler[i] = new CFSetupHandler(EnableInternetUsage);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/ENABLEINTERNETUSAGE");
                    ButtonValue[i++] = this.pluginConfig.ReadField("/APPCONFIG/ENABLEINTERNETUSAGE");

                    ButtonHandler[i] = new CFSetupHandler(EnableRadioVIS);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/ENABLERADIOVIS");
                    ButtonValue[i++] = this.pluginConfig.ReadField("/APPCONFIG/ENABLERADIOVIS");

                    ButtonHandler[i] = new CFSetupHandler(SetDebugEvents);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/DEBUGEVENTS");
                    ButtonValue[i++] = this.pluginConfig.ReadField("/APPCONFIG/DEBUGEVENTS");

                    ButtonHandler[i] = new CFSetupHandler(SetRescanEvents);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/RESCAN");
                    ButtonValue[i++] = this.pluginConfig.ReadField("/APPCONFIG/RESCAN");

                    /*
                    ButtonHandler[i] = new CFSetupHandler(ClearBoardBeforeScan);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/CLEARBOARDBEFORESCAN");
                    ButtonValue[i++] = this.pluginConfig.ReadField("/APPCONFIG/CLEARBOARDBEFORESCAN");
                    */
                }
                else if (currentpage == 4 && advanced == true)
                {
                    ButtonHandler[i] = new CFSetupHandler(SetHotkeyLOADNEXTTRACK);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/HOTKEYLOADNEXTTRACK");
                    ButtonValue[i++] = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/HOTKEYLOADNEXTTRACK");

                    ButtonHandler[i] = new CFSetupHandler(SetHotkeyLOADPREVIOUSTRACK);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/HOTKEYLOADPREVIOUSTRACK");
                    ButtonValue[i++] = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/HOTKEYLOADPREVIOUSTRACK");

                    ButtonHandler[i] = new CFSetupHandler(SetHotkeyRADIOSEEKFORWARD);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/HOTKEYRADIOSEEKFORWARD");
                    ButtonValue[i++] = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/HOTKEYRADIOSEEKFORWARD");

                    ButtonHandler[i] = new CFSetupHandler(SetHotkeyRADIOSEEKBACK);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/HOTKEYRADIOSEEKBACK");
                    ButtonValue[i++] = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/HOTKEYRADIOSEEKBACK");

                    // BOOL BUTTONS (5-8)
                    ButtonHandler[i] = null; ButtonText[i] = ""; ButtonValue[i++] = "";
                    ButtonHandler[i] = null; ButtonText[i] = ""; ButtonValue[i++] = "";
                    ButtonHandler[i] = null; ButtonText[i] = ""; ButtonValue[i++] = "";
                    ButtonHandler[i] = null; ButtonText[i] = ""; ButtonValue[i++] = "";
                }
                else if (currentpage == 5 && advanced == true)
                {
                    ButtonHandler[i] = new CFSetupHandler(SetHotkeyFM);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/HOTKEYFM");
                    ButtonValue[i++] = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/HOTKEYFM");

                    ButtonHandler[i] = new CFSetupHandler(SetHotkeyDAB);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/HOTKEYDAB");
                    ButtonValue[i++] = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/HOTKEYDAB");

                    ButtonHandler[i] = new CFSetupHandler(SetHotkeySCAN);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/HOTKEYSCAN");
                    ButtonValue[i++] = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/HOTKEYSCAN");

                    ButtonHandler[i] = new CFSetupHandler(SetHotkeyTOGGLEDABFM);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/HOTKEYTOGGLEDABFM");
                    ButtonValue[i++] = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/HOTKEYTOGGLEDABFM");

                    // BOOL BUTTONS (5-8)
                    ButtonHandler[i] = null; ButtonText[i] = ""; ButtonValue[i++] = "";
                    ButtonHandler[i] = null; ButtonText[i] = ""; ButtonValue[i++] = "";
                    ButtonHandler[i] = null; ButtonText[i] = ""; ButtonValue[i++] = "";
                    ButtonHandler[i] = null; ButtonText[i] = ""; ButtonValue[i++] = "";
                }
                else if (currentpage == 6 && advanced == true)
                {
                    ButtonHandler[i] = new CFSetupHandler(SetHotkeyLAUNCH);
                    ButtonText[i] = this.pluginLang.ReadField("/APPLANG/SETUP/HOTKEYLAUNCH");
                    ButtonValue[i++] = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/HOTKEYLAUNCH");

                    ButtonHandler[i] = new CFSetupHandler(SetVID);
                    ButtonText[i] = this.pluginLang.ReadField("APPLANG/SETUP/VID");
                    ButtonValue[i++] = this.pluginLang.ReadField("APPLANG/DABFMMONKEY/VID");                                       
                    
                    ButtonHandler[i] = null; ButtonText[i] = ""; ButtonValue[i++] = "";
                    ButtonHandler[i] = null; ButtonText[i] = ""; ButtonValue[i++] = "";

                    // BOOL BUTTONS (5-8)
                    ButtonHandler[i] = null; ButtonText[i] = ""; ButtonValue[i++] = "";
                    ButtonHandler[i] = null; ButtonText[i] = ""; ButtonValue[i++] = "";
                    ButtonHandler[i] = null; ButtonText[i] = ""; ButtonValue[i++] = "";
                    ButtonHandler[i] = null; ButtonText[i] = ""; ButtonValue[i++] = "";
                }
            }
            catch (Exception errmsg) 
            {
                CFTools.writeError(PluginName + ": Failed to handle CF_setupReadSettings(), " + errmsg.ToString());
            }
        }
#endregion

#region User Input Events

        private void SetDisplayName(ref object value)
        {
            try
            {
                object tempobject;
                string resultvalue, resulttext;

                // Display OSK for user to type display name
                if (this.CF_systemDisplayDialog(CF_Dialogs.OSK, this.pluginLang.ReadField("/APPLANG/SETUP/DISPLAYNAME"), ButtonValue[(int)value], null, out resultvalue, out resulttext, out tempobject, null, true, true, true, true, false, false, 1) == DialogResult.OK)
                {
                    //Update with new value
                    this.pluginLang.WriteField("/APPLANG/DABFMMONKEY/DISPLAYNAME", resultvalue);

                    // Display new value on Settings Screen button
                    ButtonValue[(int)value] = resultvalue;
                }
            }
            catch (Exception errmsg) 
            {
                CFTools.writeError(PluginName + ": Failed to handle SetDisplayName(), " + errmsg.ToString());
            }
        }

        private void SetRegion(ref object value)
        {
            try
            {
                object tempobject;
                string resultvalue, resulttext;
                string[] aryRegionList= new string[0];

                //Get the regions and extand the Array with the values
                try
                {
                    aryRegionList = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/REGIONLIST").Split(',');
                }
                catch (Exception errmsg)
                {
                    CFTools.writeError(PluginName + ": Failed to split REGIONLIST, " + errmsg.ToString());
                }


                // Create a listview with the number of items in the Array
                CFControls.CFListViewItem[] textoptions = new CFControls.CFListViewItem[aryRegionList.Length+1]; // Add one for the "none" / "blank" entry

                // Populate the list with the options
                textoptions[0] = new CFControls.CFListViewItem(this.pluginLang.ReadField("/APPLANG/SETUP/NONE"), this.pluginLang.ReadField("/APPLANG/SETUP/NONE"), -1, false);
                int i = 1;
                foreach (string s in aryRegionList)
                {
                    CFTools.writeLog(PluginName + ": Regions: '" + s + "'");
                    textoptions[i++] = new CFControls.CFListViewItem(s, s, -1, false);
                }                

                // Display the options
                if (this.CF_systemDisplayDialog(CF_Dialogs.FileBrowser,
                   this.pluginLang.ReadField("/APPLANG/SETUP/REGION"),
                   this.pluginLang.ReadField("/APPLANG/SETUP/REGION"),
                   ButtonValue[(int)value], out resultvalue, out resulttext, out tempobject, textoptions, true, true, true, false, false, false, 1) == DialogResult.OK)
                {
                    //Blank?
                    if (resultvalue.IndexOf(this.pluginLang.ReadField("/APPLANG/SETUP/NONE"), StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        this.pluginLang.WriteField("/APPLANG/DABFMMONKEY/REGION", "");
                        ButtonValue[(int)value] = "";
                    }
                    else
                    {
                        this.pluginLang.WriteField("/APPLANG/DABFMMONKEY/REGION", resultvalue);
                        ButtonValue[(int)value] = resultvalue;
                    }
                }
            }
            catch (Exception errmsg) 
            {
                CFTools.writeError(PluginName + ": Failed to handle SetRegion(), " + errmsg.ToString());
            }
        }

        private void ECCRegion(ref object value)
        {
            try
            {
                object tempobject;
                string resultvalue, resulttext;
                string[] aryRegionList = new string[0];

                //Get the regions and extand the Array with the values
                try { aryRegionList = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/ECCREGIONLIST").Split(','); }
                catch (Exception errmsg)
                {
                    CFTools.writeError(PluginName + ": Failed to split ECC RegionList, " + errmsg.ToString());
                }

                // Create a listview with the number of items in the Array
                CFControls.CFListViewItem[] textoptions = new CFControls.CFListViewItem[aryRegionList.Length + 1]; // Add one for the "none" / "blank" entry

                // Populate the list with the options
                textoptions[0] = new CFControls.CFListViewItem(this.pluginLang.ReadField("/APPLANG/SETUP/NONE"), this.pluginLang.ReadField("/APPLANG/SETUP/NONE"), -1, false);
                int i = 1;
                foreach (string s in aryRegionList)
                {
                    CFTools.writeLog(PluginName + ": ECC Regions: '" + s + "'");
                    textoptions[i++] = new CFControls.CFListViewItem(s, s, -1, false);
                }

                // Display the options
                if (this.CF_systemDisplayDialog(CF_Dialogs.FileBrowser,
                   this.pluginLang.ReadField("/APPLANG/SETUP/REGION"),
                   this.pluginLang.ReadField("/APPLANG/SETUP/REGION"),
                   ButtonValue[(int)value], out resultvalue, out resulttext, out tempobject, textoptions, true, true, true, false, false, false, 1) == DialogResult.OK)
                {
                    //Blank?
                    if (resultvalue.IndexOf(this.pluginLang.ReadField("/APPLANG/SETUP/NONE"), StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        this.pluginConfig.WriteField("/APPCONFIG/ECCREGION", "");
                        ButtonValue[(int)value] = "";
                    }
                    else
                    {
                        this.pluginConfig.WriteField("/APPCONFIG/ECCREGION", resultvalue);
                        ButtonValue[(int)value] = resultvalue;
                    }
                }
            }
            catch (Exception errmsg) 
            {
                CFTools.writeError(PluginName + ": Failed to handle ECCRegion()" + errmsg.ToString());
            }
        }

        private void SetVolume(ref object value)
        {
            try
            {
                string resultvalue, resulttext;

                if (this.CF_systemDisplayDialog(CF_Dialogs.NumberPad, this.pluginLang.ReadField("/APPLANG/SETUP/VOLUMESELECTION"), out resultvalue, out resulttext) == DialogResult.OK)
                {
                    //Parse the value
                    int intTemp = int.Parse(resultvalue);

                    //Sanity check it and set to its extremes.
                    if (intTemp > 16) intTemp = 16;
                    if (intTemp < 0) intTemp = 0;

                    //Value is scrubbed, write it
                    this.pluginConfig.WriteField("/APPCONFIG/VOLUME", intTemp.ToString());

                    // Display new value on Settings Screen button
                    ButtonValue[(int)value] = intTemp.ToString();
                }
            }
            catch (Exception errmsg) 
            {
                CFTools.writeError(PluginName + ": Failed to handle Setvolume(), " + errmsg.ToString());
            }
        }

        private void SetPort(ref object value)
        {
            try
            {
                string resultvalue, resulttext;

                if (this.CF_systemDisplayDialog(CF_Dialogs.NumberPad, this.pluginLang.ReadField("/APPLANG/SETUP/PORTSELECTION"), out resultvalue, out resulttext) == DialogResult.OK)
                {
                    //Parse the value
                    int intTemp = int.Parse(resultvalue);

                    //Sanity check it and set to its extremes.
                    if (intTemp > 255) intTemp = 255;
                    if (intTemp < 0) intTemp = 0;

                    //Make it pretty
                    resultvalue = "COM" + intTemp.ToString();

                    //Value is scrubbed, write it
                    this.pluginLang.WriteField("/APPLANG/DABFMMONKEY/PORT", resultvalue);

                    // Display new value on Settings Screen button
                    ButtonValue[(int)value] = resultvalue;
                }
            }
            catch (Exception errmsg) 
            {
                CFTools.writeError(PluginName + ": Failed to handle SetPort(), " + errmsg.ToString());
            }
        }

        private void SetVID(ref object value)
        {
            try
            {
                object tempobject;
                string resultvalue, resulttext;

                // Display OSK for user to type display name
                if (this.CF_systemDisplayDialog(CF_Dialogs.OSK, this.pluginLang.ReadField("/APPLANG/SETUP/VID"), ButtonValue[(int)value], null, out resultvalue, out resulttext, out tempobject, null, true, true, true, true, false, false, 1) == DialogResult.OK)
                {
                    // save user value, note this does not save to file yet, as this should only be done when user confirms settings
                    // being overwritten when they click the "Save" button.  Saving is done internally by the CFSetup instance if
                    // pluginConfig and pluginLang were properly set before callin CF_initSetup().
                    this.pluginLang.WriteField("/APPLANG/DABFMMONKEY/VID", resultvalue);

                    // Display new value on Settings Screen button
                    ButtonValue[(int)value] = resultvalue;
                }
            }
            catch (Exception errmsg) 
            {
                CFTools.writeError(PluginName + ": Failed to handle SetVID(), " + errmsg.ToString());
            }
        }

        private void SetPID(ref object value)
        {
            try
            {
                object tempobject;
                string resultvalue, resulttext;

                // Display OSK for user to type display name
                if (this.CF_systemDisplayDialog(CF_Dialogs.OSK, this.pluginLang.ReadField("/APPLANG/SETUP/PID"), ButtonValue[(int)value], null, out resultvalue, out resulttext, out tempobject, null, true, true, true, true, false, false, 1) == DialogResult.OK)
                {
                    // save user value, note this does not save to file yet, as this should only be done when user confirms settings
                    // being overwritten when they click the "Save" button.  Saving is done internally by the CFSetup instance if
                    // pluginConfig and pluginLang were properly set before callin CF_initSetup().
                    this.pluginLang.WriteField("/APPLANG/DABFMMONKEY/PID", resultvalue);

                    // Display new value on Settings Screen button
                    ButtonValue[(int)value] = resultvalue;
                }
            }
            catch (Exception errmsg)
            {
                CFTools.writeError(PluginName + ": Failed to handle SetPID(), " + errmsg.ToString());
            }
        }

        private void SetScanStart(ref object value)
        {
            try
            {
                object tempobject;
                string resultvalue, resulttext;

                try { DABFMMonkey.boolDABFMMonkeyChinaMode = bool.Parse(this.pluginConfig.ReadField("/APPCONFIG/CHINAMODE")); }
                catch { DABFMMonkey.boolDABFMMonkeyChinaMode = false; }

                try { DABFMMonkey.boolDABFMMonkeyLBandMode = bool.Parse(this.pluginConfig.ReadField("/APPCONFIG/LBANDMODE")); }
                catch { DABFMMonkey.boolDABFMMonkeyLBandMode = false; }
                
                //Depending on mode, populate list with different values
                CFControls.CFListViewItem[] textoptions = null;
                if (DABFMMonkey.boolDABFMMonkeyChinaMode)
                {
                    textoptions = new CFControls.CFListViewItem[DABFMMonkey.DABChinaFrequencyIndex.Length];

                    // Populate the list with the options
                    for (sbyte i = 0; i < DABFMMonkey.DABChinaFrequencyIndex.Length; i++)
                    {
                        CFTools.writeLog(PluginName + ": Layout Option='" + DABFMMonkey.DABChinaFrequencyIndex[i] + "'");
                        textoptions[i] = new CFControls.CFListViewItem(DABFMMonkey.DABChinaFrequencyIndex[i] + " (" + DABFMMonkey.DABChinaFrequencyMHz[i] + "MHz) ", i.ToString(), -1, false);
                    }
                }
                else if (DABFMMonkey.boolDABFMMonkeyLBandMode)
                {
                    textoptions = new CFControls.CFListViewItem[DABFMMonkey.DABLBandFrequencyIndex.Length];

                    // Populate the list with the options
                    for (sbyte i = 0; i < DABFMMonkey.DABLBandFrequencyIndex.Length; i++)
                    {
                        CFTools.writeLog(PluginName + ": Layout Option='" + DABFMMonkey.DABLBandFrequencyIndex[i] + "'");
                        textoptions[i] = new CFControls.CFListViewItem(DABFMMonkey.DABLBandFrequencyIndex[i] + " (" + DABFMMonkey.DABLBandFrequencyMHz[i] + "MHz) ", i.ToString(), -1, false);
                    }

                }
                else
                {
                    textoptions = new CFControls.CFListViewItem[DABFMMonkey.DABStandardFrequencyIndex.Length];

                    // Populate the list with the options
                    for (sbyte i = 0; i < DABFMMonkey.DABStandardFrequencyIndex.Length; i++)
                    {
                        CFTools.writeLog(PluginName + ": Layout Option='" + DABFMMonkey.DABStandardFrequencyIndex[i] + "'");
                        textoptions[i] = new CFControls.CFListViewItem(DABFMMonkey.DABStandardFrequencyIndex[i] + " (" + DABFMMonkey.DABStandardFrequencyMHz[i] + "MHz) ", i.ToString(), -1, false);
                    }
                }

                // Display the options
                if (this.CF_systemDisplayDialog(CF_Dialogs.FileBrowser,
                this.pluginLang.ReadField("/APPLANG/SETUP/SCANSTART"),
                this.pluginLang.ReadField("/APPLANG/SETUP/SCANSTART"),
                ButtonValue[(int)value], out resultvalue, out resulttext, out tempobject, textoptions, false, false, false, false, false, false, 1) == DialogResult.OK)
                {
                    //Strip out the MHz text
                    this.pluginLang.WriteField("/APPLANG/DABFMMONKEY/SCANSTART", resulttext.Substring(0, resulttext.IndexOf(' ')));
                    ButtonValue[(int)value] = resulttext.Substring(0, resulttext.IndexOf(' '));
                }
            }
            catch (Exception errmsg) 
            {
                CFTools.writeError(PluginName + ": Failed to handle SetScanStart(), " + errmsg.ToString());
            }
        }

        private void SetScanEnd(ref object value)
        {
            try
            {
                object tempobject;
                string resultvalue, resulttext;


                try { DABFMMonkey.boolDABFMMonkeyChinaMode = bool.Parse(this.pluginConfig.ReadField("/APPCONFIG/CHINAMODE")); }
                catch { DABFMMonkey.boolDABFMMonkeyChinaMode = false; }

                try { DABFMMonkey.boolDABFMMonkeyLBandMode = bool.Parse(this.pluginConfig.ReadField("/APPCONFIG/LBANDMODE")); }
                catch { DABFMMonkey.boolDABFMMonkeyLBandMode = false; }

                //Depending on mode, populate list with different values
                CFControls.CFListViewItem[] textoptions = null;
                if (DABFMMonkey.boolDABFMMonkeyChinaMode)
                {
                    textoptions = new CFControls.CFListViewItem[DABFMMonkey.DABChinaFrequencyIndex.Length];

                    // Populate the list with the options
                    for (sbyte i = 0; i < DABFMMonkey.DABChinaFrequencyIndex.Length; i++)
                    {
                        CFTools.writeLog(PluginName + ": Layout Option='" + DABFMMonkey.DABChinaFrequencyIndex[i] + "'");
                        textoptions[i] = new CFControls.CFListViewItem(DABFMMonkey.DABChinaFrequencyIndex[i] + " (" + DABFMMonkey.DABChinaFrequencyMHz[i] + "MHz) ", i.ToString(), -1, false);
                    }
                }
                else if (DABFMMonkey.boolDABFMMonkeyLBandMode)
                {
                    textoptions = new CFControls.CFListViewItem[DABFMMonkey.DABLBandFrequencyIndex.Length];

                    // Populate the list with the options
                    for (sbyte i = 0; i < DABFMMonkey.DABLBandFrequencyIndex.Length; i++)
                    {
                        CFTools.writeLog(PluginName + ": Layout Option='" + DABFMMonkey.DABLBandFrequencyIndex[i] + "'");
                        textoptions[i] = new CFControls.CFListViewItem(DABFMMonkey.DABLBandFrequencyIndex[i] + " (" + DABFMMonkey.DABLBandFrequencyMHz[i] + "MHz) ", i.ToString(), -1, false);
                    }

                }
                else
                {
                    textoptions = new CFControls.CFListViewItem[DABFMMonkey.DABStandardFrequencyIndex.Length];

                    // Populate the list with the options
                    for (sbyte i = 0; i < DABFMMonkey.DABStandardFrequencyIndex.Length; i++)
                    {
                        CFTools.writeLog(PluginName + ": Layout Option='" + DABFMMonkey.DABStandardFrequencyIndex[i] + "'");
                        textoptions[i] = new CFControls.CFListViewItem(DABFMMonkey.DABStandardFrequencyIndex[i] + " (" + DABFMMonkey.DABStandardFrequencyMHz[i] + "MHz) ", i.ToString(), -1, false);
                    }
                }

                // Display the options
                if (this.CF_systemDisplayDialog(CF_Dialogs.FileBrowser,
                this.pluginLang.ReadField("/APPLANG/SETUP/SCANEND"),
                this.pluginLang.ReadField("/APPLANG/SETUP/SCANEND"),
                ButtonValue[(int)value], out resultvalue, out resulttext, out tempobject, textoptions, false, false, false, false, false, false, 1) == DialogResult.OK)
                {
                    //Strip out the MHz text
                    this.pluginLang.WriteField("/APPLANG/DABFMMONKEY/SCANEND", resulttext.Substring(0, resulttext.IndexOf(' ')));
                    ButtonValue[(int)value] = resulttext.Substring(0, resulttext.IndexOf(' '));
                }
            }
            catch (Exception errmsg) 
            {
                CFTools.writeError(PluginName + ": Failed to Handle SetScanEnd(), " + errmsg.ToString());
            }
        }

        private void SetLogEvents(ref object value)
        {
            this.pluginConfig.WriteField("/APPCONFIG/LOGEVENTS", value.ToString());
        }

        private void SetDebugEvents(ref object value)
        {
            this.pluginConfig.WriteField("/APPCONFIG/DEBUGEVENTS", value.ToString());
        }

        private void SetRescanEvents(ref object value)
        {
            this.pluginConfig.WriteField("/APPCONFIG/RESCAN", value.ToString());
        }

        private void SetChinaMode(ref object value)
        {
            this.pluginConfig.WriteField("/APPCONFIG/CHINAMODE", value.ToString(), true);

            //Can't have LBand and Chine enabled at the same time
            if ((bool)value == true) this.pluginConfig.WriteField("/APPCONFIG/LBANDMODE", false.ToString(), true);
        }

        private void SetLBandMode(ref object value)
        {
            this.pluginConfig.WriteField("/APPCONFIG/LBANDMODE", value.ToString(), true);

            //Can't have LBand and Chine enabled at the same time
            if ((bool)value == true) this.pluginConfig.WriteField("/APPCONFIG/CHINAMODE", false.ToString(), true);
        }

        private void SetEnablePlugin(ref object value)
        {
            this.pluginConfig.WriteField("/APPCONFIG/ENABLEPLUGIN", value.ToString());            
        }

        private void SetEnableBuffer(ref object value)
        {
            // radio buffer on or off
            this.pluginConfig.WriteField("/APPCONFIG/BUFFERRADIO", value.ToString());
        }

        private void SetButtonMode(ref object value)
        {
            // Top right buttons in new or legacy mode
            this.pluginConfig.WriteField("/APPCONFIG/BUTTONMODE", value.ToString());
        }

        // Minimal GUI
        private void SetGUIMode(ref object value)
        {
            this.pluginConfig.WriteField("/APPCONFIG/MINISTATUS", value.ToString());
        }


        private void EnableInternetUsage(ref object value)
        {
            // Enable Internet traffic
            this.pluginConfig.WriteField("/APPCONFIG/ENABLEINTERNETUSAGE", value.ToString());
        }

        private void EnableRadioVIS(ref object value)
        {
            // Enable Internet traffic
            this.pluginConfig.WriteField("/APPCONFIG/ENABLERADIOVIS", value.ToString());
        }

        /*private void ClearBoardBeforeScan(ref object value)
        {
            // Clear board when scanning for channels, yes / no
            this.pluginConfig.WriteField("/APPCONFIG/CLEARBOARDBEFORESCAN", value.ToString());
        }*/

        private void SetDABTextMode(ref object value)
        {
            // Long DAB station name? (true = Long; False = Short (Abbreviated)
            this.pluginConfig.WriteField("/APPCONFIG/DABTEXTMODE", value.ToString());
        }

        private void SetListRows(ref object value)
        {
            try
            {
                object tempobject;
                string resultvalue, resulttext;
                sbyte[] aryRowList = new sbyte[] {4, 6, 8}; // Values here MUST match the template definitions in the skin

                // Create a listview with the items in the Array
                CFControls.CFListViewItem[] textoptions = new CFControls.CFListViewItem[aryRowList.Length];

                //Populate the CFListView
                for (int i=0; i < aryRowList.Length; i++)
                {
                    CFTools.writeLog(PluginName + ": Row Option: '" + aryRowList[i].ToString() + "'");
                    textoptions[i] = new CFControls.CFListViewItem(aryRowList[i].ToString(), aryRowList[i].ToString(), -1, false);
                }

                // Display the options
                if (this.CF_systemDisplayDialog(CF_Dialogs.FileBrowser,
                   this.pluginLang.ReadField("/APPLANG/SETUP/LISTROWS"),
                   this.pluginLang.ReadField("/APPLANG/SETUP/LISTROWS"),
                   ButtonValue[(int)value], out resultvalue, out resulttext, out tempobject, textoptions, false, false, false, false, false, false, 1) == DialogResult.OK)
                {
                    this.pluginConfig.WriteField("/APPCONFIG/LISTROWS", resultvalue);
                    ButtonValue[(int)value] = resultvalue;
                }
            }
            catch (Exception errmsg) 
            {
                CFTools.writeError(PluginName + ": Failed to handle SetListRows(), " + errmsg.ToString());
            }
        }

        private void SetPlayBack(ref object value)
        {
            try
            {
                object tempobject;
                string resultvalue, resulttext;
                MixerDeviceInput[] aryPlaybackDevices = {};
                int i;
                
                //Get the input devices and extend the Array with the values
                aryPlaybackDevices = this.CF_getPlaybackDevices();

                // Create a listview with the number of items in the Array
                CFControls.CFListViewItem[] textoptions = new CFControls.CFListViewItem[aryPlaybackDevices.Length + 1]; // Add one for the "none" / "blank" entry

                // Populate the list with the options
                textoptions[0] = new CFControls.CFListViewItem(this.pluginLang.ReadField("/APPLANG/SETUP/NONE"), this.pluginLang.ReadField("/APPLANG/SETUP/NONE"), -1, false);                
                for(i = 0; i < aryPlaybackDevices.Length; i++)
                {
                    CFTools.writeLog(PluginName + ": Device='" + aryPlaybackDevices[i].MixerDevice + "' Input='" + aryPlaybackDevices[i].MixerInput + "' Device Name='" + aryPlaybackDevices[i].Name + "'");
                    textoptions[i + 1] = new CFControls.CFListViewItem(aryPlaybackDevices[i].Name, aryPlaybackDevices[i].Name, -1, false);
                }

                // Display the options
                if (this.CF_systemDisplayDialog(CF_Dialogs.FileBrowser,
                this.pluginLang.ReadField("/APPLANG/SETUP/RADIOLINETEXT"),
                this.pluginLang.ReadField("/APPLANG/SETUP/RADIOLINETEXT"),
                ButtonValue[(int)value], out resultvalue, out resulttext, out tempobject, textoptions, true, true, true, false, false, false, 1) == DialogResult.OK)
                {
                    //Blank?
                    if (resultvalue.IndexOf(this.pluginLang.ReadField("/APPLANG/SETUP/NONE"), StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        this.pluginConfig.WriteField("/APPCONFIG/PLAYBACK", "");
                        ButtonValue[(int)value] = "";
                    }
                    else
                    {
                        this.pluginConfig.WriteField("/APPCONFIG/PLAYBACK", resultvalue);
                        ButtonValue[(int)value] = resultvalue;
                    }

                }
            }
            catch (Exception errmsg) 
            {
                CFTools.writeError(PluginName + ": Failed to handle SetPlayBack(), " + errmsg.ToString());
            }
        }

        private void SetLineIn(ref object value)
        {
            try
            {
                object tempobject;
                string resultvalue, resulttext;
                MixerDeviceInput[] aryRecordDevices = { };
                int i;

                //Get the input devices and extend the Array with the values
                aryRecordDevices = base.CF_getRecordDevices();

                // Create a listview with the number of items in the Array
                CFControls.CFListViewItem[] textoptions = new CFControls.CFListViewItem[aryRecordDevices.Length + 1]; // Add one for the "none" / "blank" entry

                // Populate the list with the options
                textoptions[0] = new CFControls.CFListViewItem(this.pluginLang.ReadField("/APPLANG/SETUP/NONE"), this.pluginLang.ReadField("/APPLANG/SETUP/NONE"), -1, false);
                for (i = 0; i < aryRecordDevices.Length; i++)
                {
                    CFTools.writeLog(PluginName + ": Device='" + aryRecordDevices[i].MixerDevice + "' Input='" + aryRecordDevices[i].MixerInput + "' Device Name='" + aryRecordDevices[i].Name + "'");
                    textoptions[i + 1] = new CFControls.CFListViewItem(aryRecordDevices[i].Name, aryRecordDevices[i].Name, -1, false);
                }

                // Display the options
                if (this.CF_systemDisplayDialog(CF_Dialogs.FileBrowser,
                this.pluginLang.ReadField("/APPLANG/SETUP/RADIOLINETEXT"),
                this.pluginLang.ReadField("/APPLANG/SETUP/RADIOLINETEXT"),
                ButtonValue[(int)value], out resultvalue, out resulttext, out tempobject, textoptions, true, true, true, false, false, false, 1) == DialogResult.OK)
                {
                    //Blank?
                    if (resultvalue.IndexOf(this.pluginLang.ReadField("/APPLANG/SETUP/NONE"), StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        this.pluginConfig.WriteField("/APPCONFIG/LINEIN", "");
                        ButtonValue[(int)value] = "";
                    }
                    else
                    {
                        this.pluginConfig.WriteField("/APPCONFIG/LINEIN", resultvalue);
                        ButtonValue[(int)value] = resultvalue;
                    }
                }
            }
            catch (Exception errmsg) 
            {
                CFTools.writeError(PluginName + ": Failed to handle SetLineIn(), " + errmsg.ToString());
            }
        }
        
        private void SetSLSLayout(ref object value)
        {
            try
            {
                object tempobject;
                string resultvalue, resulttext;
                string[] arySLSLayout = new string[0];
                
                //Get the SLS layout options and populate the array with the values
                arySLSLayout = this.pluginLang.ReadField("/APPLANG/DABFMMONKEY/SLSLAYOUT").ToString().Split(',');

                // Create a listview with the number of items in the Array
                CFControls.CFListViewItem[] textoptions = new CFControls.CFListViewItem[arySLSLayout.Length];

                // Populate the list with the options
                for (sbyte i = 0; i < arySLSLayout.Length; i++)
                {
                    CFTools.writeLog(PluginName + ": Layout Option='" + arySLSLayout[i] + "'");
                    textoptions[i] = new CFControls.CFListViewItem(arySLSLayout[i], i.ToString(), -1, false);
                }

                // Display the options
                if (this.CF_systemDisplayDialog(CF_Dialogs.FileBrowser,
                    this.pluginLang.ReadField("/APPLANG/SETUP/SLSLAYOUT"),
                    this.pluginLang.ReadField("/APPLANG/SETUP/SLSLAYOUT"),
                    ButtonValue[(int)value], out resultvalue, out resulttext, out tempobject, textoptions, false, false, false, false, false, false, 1) == DialogResult.OK)
                {
                    //Write the selected option
                    SLSLayout MyStatus = (SLSLayout)Enum.Parse(typeof(SLSLayout), resultvalue, true);
                                                          
                    this.pluginConfig.WriteField("/APPCONFIG/SLSLAYOUT", MyStatus.ToString());
                    ButtonValue[(int)value] = arySLSLayout[Convert.ToSByte(resultvalue)];
                }
            }
            catch (Exception errmsg) 
            {
                CFTools.writeError(PluginName + ": Failed to handle SetSLSLayout(), " + errmsg.ToString());
            }
        }

        private void SetHotkeyLAUNCH(ref object value)
        {
            try
            {
                object tempobject;
                string resultvalue, resulttext;

                // Display OSK for user to type display name
                if (this.CF_systemDisplayDialog(CF_Dialogs.OSK, this.pluginLang.ReadField("/APPLANG/SETUP/HOTKEYLAUNCH"), ButtonValue[(int)value], null, out resultvalue, out resulttext, out tempobject, null, true, true, true, true, false, false, 1) == DialogResult.OK)
                {
                    // save user value, note this does not save to file yet, as this should only be done when user confirms settings
                    // being overwritten when they click the "Save" button.  Saving is done internally by the CFSetup instance if
                    // pluginConfig and pluginLang were properly set before callin CF_initSetup().
                    this.pluginLang.WriteField("/APPLANG/DABFMMONKEY/HOTKEYLAUNCH", resultvalue);

                    // Display new value on Settings Screen button
                    ButtonValue[(int)value] = resultvalue;
                }
            }
            catch (Exception errmsg) 
            {
                CFTools.writeError(PluginName + ": Failed to handle SetHotkeyLAUNCH(), " + errmsg.ToString());
            }
        }

        private void SetHotkeyLOADNEXTTRACK(ref object value)
        {
            try
            {
                object tempobject;
                string resultvalue, resulttext;

                // Display OSK for user to type display name
                if (this.CF_systemDisplayDialog(CF_Dialogs.OSK, this.pluginLang.ReadField("/APPLANG/SETUP/HOTKEYLOADNEXTTRACK"), ButtonValue[(int)value], null, out resultvalue, out resulttext, out tempobject, null, true, true, true, true, false, false, 1) == DialogResult.OK)
                {
                    // save user value, note this does not save to file yet, as this should only be done when user confirms settings
                    // being overwritten when they click the "Save" button.  Saving is done internally by the CFSetup instance if
                    // pluginConfig and pluginLang were properly set before callin CF_initSetup().
                    this.pluginLang.WriteField("/APPLANG/DABFMMONKEY/HOTKEYLOADNEXTTRACK", resultvalue);

                    // Display new value on Settings Screen button
                    ButtonValue[(int)value] = resultvalue;
                }
            }
            catch (Exception errmsg) 
            {
                CFTools.writeError(PluginName + ": Failed to handle SetHotkeyLOADNEXTTRACK(), " + errmsg.ToString());
            }
        }

        private void SetHotkeyLOADPREVIOUSTRACK(ref object value)
        {
            try
            {
                object tempobject;
                string resultvalue, resulttext;

                // Display OSK for user to type display name
                if (this.CF_systemDisplayDialog(CF_Dialogs.OSK, this.pluginLang.ReadField("/APPLANG/SETUP/HOTKEYLOADPREVIOUSTRACK"), ButtonValue[(int)value], null, out resultvalue, out resulttext, out tempobject, null, true, true, true, true, false, false, 1) == DialogResult.OK)
                {
                    // save user value, note this does not save to file yet, as this should only be done when user confirms settings
                    // being overwritten when they click the "Save" button.  Saving is done internally by the CFSetup instance if
                    // pluginConfig and pluginLang were properly set before callin CF_initSetup().
                    this.pluginLang.WriteField("/APPLANG/DABFMMONKEY/HOTKEYLOADPREVIOUSTRACK", resultvalue);

                    // Display new value on Settings Screen button
                    ButtonValue[(int)value] = resultvalue;
                }
            }
            catch (Exception errmsg) 
            {
                CFTools.writeError(PluginName + ": Failed to handle SetHotkeyLOADPREVIOUSTRACK(), " + errmsg.ToString());
            }

        }

        private void SetHotkeyRADIOSEEKFORWARD(ref object value)
        {
            try
            {
                object tempobject;
                string resultvalue, resulttext;

                // Display OSK for user to type display name
                if (this.CF_systemDisplayDialog(CF_Dialogs.OSK, this.pluginLang.ReadField("/APPLANG/SETUP/HOTKEYRADIOSEEKFORWARD"), ButtonValue[(int)value], null, out resultvalue, out resulttext, out tempobject, null, true, true, true, true, false, false, 1) == DialogResult.OK)
                {
                    // save user value, note this does not save to file yet, as this should only be done when user confirms settings
                    // being overwritten when they click the "Save" button.  Saving is done internally by the CFSetup instance if
                    // pluginConfig and pluginLang were properly set before callin CF_initSetup().
                    this.pluginLang.WriteField("/APPLANG/DABFMMONKEY/HOTKEYRADIOSEEKFORWARD", resultvalue);

                    // Display new value on Settings Screen button
                    ButtonValue[(int)value] = resultvalue;
                }
            }
            catch (Exception errmsg)
            {
                CFTools.writeError(PluginName + ": Failed to handle SetHotkeyRADIOSEEKFORWARD(), " + errmsg.ToString());
            }
        }

        private void SetHotkeyRADIOSEEKBACK(ref object value)
        {
            try
            {
                object tempobject;
                string resultvalue, resulttext;

                // Display OSK for user to type display name
                if (this.CF_systemDisplayDialog(CF_Dialogs.OSK, this.pluginLang.ReadField("/APPLANG/SETUP/HOTKEYRADIOSEEKBACK"), ButtonValue[(int)value], null, out resultvalue, out resulttext, out tempobject, null, true, true, true, true, false, false, 1) == DialogResult.OK)
                {
                    // save user value, note this does not save to file yet, as this should only be done when user confirms settings
                    // being overwritten when they click the "Save" button.  Saving is done internally by the CFSetup instance if
                    // pluginConfig and pluginLang were properly set before callin CF_initSetup().
                    this.pluginLang.WriteField("/APPLANG/DABFMMONKEY/HOTKEYRADIOSEEKBACK", resultvalue);

                    // Display new value on Settings Screen button
                    ButtonValue[(int)value] = resultvalue;
                }
            }
            catch (Exception errmsg)
            {
                CFTools.writeError(PluginName + ": Failed to handle SetHotkeyRADIOSEEKBACK(), " + errmsg.ToString());
            }
        }

        private void SetHotkeyFM(ref object value)
        {
            try
            {
                object tempobject;
                string resultvalue, resulttext;

                // Display OSK for user to type display name
                if (this.CF_systemDisplayDialog(CF_Dialogs.OSK, this.pluginLang.ReadField("/APPLANG/SETUP/HOTKEYFM"), ButtonValue[(int)value], null, out resultvalue, out resulttext, out tempobject, null, true, true, true, true, false, false, 1) == DialogResult.OK)
                {
                    // save user value, note this does not save to file yet, as this should only be done when user confirms settings
                    // being overwritten when they click the "Save" button.  Saving is done internally by the CFSetup instance if
                    // pluginConfig and pluginLang were properly set before callin CF_initSetup().
                    this.pluginLang.WriteField("/APPLANG/DABFMMONKEY/HOTKEYFM", resultvalue);

                    // Display new value on Settings Screen button
                    ButtonValue[(int)value] = resultvalue;
                }
            }
            catch (Exception errmsg)
            {
                CFTools.writeError(PluginName + ": Failed to handle SetHotkeysFM(), " + errmsg.ToString());
            }

        }

        private void SetHotkeyDAB(ref object value)
        {
            try
            {
                object tempobject;
                string resultvalue, resulttext;

                // Display OSK for user to type display name
                if (this.CF_systemDisplayDialog(CF_Dialogs.OSK, this.pluginLang.ReadField("/APPLANG/SETUP/HOTKEYDAB"), ButtonValue[(int)value], null, out resultvalue, out resulttext, out tempobject, null, true, true, true, true, false, false, 1) == DialogResult.OK)
                {
                    // save user value, note this does not save to file yet, as this should only be done when user confirms settings
                    // being overwritten when they click the "Save" button.  Saving is done internally by the CFSetup instance if
                    // pluginConfig and pluginLang were properly set before callin CF_initSetup().
                    this.pluginLang.WriteField("/APPLANG/DABFMMONKEY/HOTKEYDAB", resultvalue);

                    // Display new value on Settings Screen button
                    ButtonValue[(int)value] = resultvalue;
                }
            }
            catch (Exception errmsg)
            {
                CFTools.writeError(PluginName + ": Failed to handle SetHotkeydab(), " + errmsg.ToString());
            }

        }

        private void SetHotkeySCAN(ref object value)
        {
            try
            {
                object tempobject;
                string resultvalue, resulttext;

                // Display OSK for user to type display name
                if (this.CF_systemDisplayDialog(CF_Dialogs.OSK, this.pluginLang.ReadField("/APPLANG/SETUP/HOTKEYSCAN"), ButtonValue[(int)value], null, out resultvalue, out resulttext, out tempobject, null, true, true, true, true, false, false, 1) == DialogResult.OK)
                {
                    // save user value, note this does not save to file yet, as this should only be done when user confirms settings
                    // being overwritten when they click the "Save" button.  Saving is done internally by the CFSetup instance if
                    // pluginConfig and pluginLang were properly set before callin CF_initSetup().
                    this.pluginLang.WriteField("/APPLANG/DABFMMONKEY/HOTKEYSCAN", resultvalue);

                    // Display new value on Settings Screen button
                    ButtonValue[(int)value] = resultvalue;
                }
            }
            catch (Exception errmsg)
            {
                CFTools.writeError(PluginName + ": Failed to handle SetHotkeySCAN(), " + errmsg.ToString());
            }
        }

        private void SetHotkeyTOGGLEDABFM(ref object value)
        {
            try
            {
                object tempobject;
                string resultvalue, resulttext;

                // Display OSK for user to type display name
                if (this.CF_systemDisplayDialog(CF_Dialogs.OSK, this.pluginLang.ReadField("/APPLANG/SETUP/HOTKEYTOGGLEDABFM"), ButtonValue[(int)value], null, out resultvalue, out resulttext, out tempobject, null, true, true, true, true, false, false, 1) == DialogResult.OK)
                {
                    // save user value, note this does not save to file yet, as this should only be done when user confirms settings
                    // being overwritten when they click the "Save" button.  Saving is done internally by the CFSetup instance if
                    // pluginConfig and pluginLang were properly set before callin CF_initSetup().
                    this.pluginLang.WriteField("/APPLANG/DABFMMONKEY/HOTKEYTOGGLEDABFM", resultvalue);

                    // Display new value on Settings Screen button
                    ButtonValue[(int)value] = resultvalue;
                }
            }
            catch (Exception errmsg)
            {
                CFTools.writeError(PluginName + ": Failed to handle SetHotkeyTOGGLEDABFM(), " + errmsg.ToString());
            }
        }
    
#endregion

    }
}

