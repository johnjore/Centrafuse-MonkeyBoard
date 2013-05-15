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
 * All RadioDNS, RadioVIS and RadioEPG functions
*/

namespace DABFMMonkey
{
    using System;
    using System.Threading;
    using System.Collections.Generic;
    using System.Net;

    using Apache.NMS; //STOMP
    //using Apache.NMS.Util; //STOMP 
    
    public partial class DABFMMonkey
    {
#region Variables
        private const string strSRVRadioVISRecordSTOMP = "radiovis";        // Service Record name for STOMP service
        private const string strSRVRadioVISRecordHTTP = "radiovis-http";    // Service Record name for HTTP service (Not implemented)
        private const string strSRVRadioEPGRecordSTOMP = "radioepg";        // Service Record name for STOMP service (Not implemented)
        private const string strSRVRadioEPGRecordHTTP = "radioepg-http";    // Service Record name for HTTP service (Not implemented)      
        private const string qdn = ".radiodns.org";                         // radiodns.org is always the domain
        string[] aryECCRegion = new string[0];                              // ECC values to check for
        private List<RadioDNS> _ProgramInfoList = new List<RadioDNS>();     // List with all station ID's for the connections
        protected static string[] aryMessage_Image;                         // Image messages
        protected static string strMessage_Text;                            // Text messages
        protected static IConnection connection;                            // STOMP connection
        protected static ISession session;                                  // STOMP session
        protected static bool boolRadioVISConfigured = false;               // Configured and running?
        protected static bool boolRadioEPGConfigured = false;               // Configured and running?
        
#endregion
        // Initialize & Enable RadioVIS
        private bool RadioVIS(UInt32 intDABIndex)
        {
            bool boolRadioVIS = false;               // Supported?
            bool result = false;
            string strCNAME = "";
            string strGCC = "";

            if (!boolRadioVISConfigured)
            {
                // If we have a CNAME record
                if (RadioDNS(intDABIndex, out strCNAME, out strGCC))
                {
                    // Locate SRV
                    string strSTOMPLocation = "";

                    // RadioVIS
                    boolRadioVIS = GetSRV(RadioDNSSRV.VIS, strCNAME, out strSTOMPLocation);
                    if (boolRadioVIS)
                    {
                        SubscribeVIS(strSTOMPLocation, strGCC, intDABIndex); 
                        boolRadioVISConfigured = true;
                        result = true;
                    }
                    else WriteLog("RadioVIS: No SRV record found for '" + strCNAME + "'");
                }
                else WriteLog("No CNAME found in RadioDNS");
            }
            else
            {
                CloseRadioVIS();
                RadioVIS(intDABIndex);
            }

            return result;
        }

        // Initialize & Enable RadioEPG
        private bool RadioEPG(UInt32 intDABIndex)
        {
            bool boolRadioEPG = false;               // Supported?
            bool result = false;
            string strCNAME = "";
            string strGCC = "";

            if (!boolRadioVISConfigured)
            {
                // If we have a CNAME record
                if (RadioDNS(intDABIndex, out strCNAME, out strGCC))
                {
                    // Locate SRV
                    string strSTOMPLocation = "";

                    // RadioEPG
                    boolRadioEPG = GetSRV(RadioDNSSRV.EPG, strCNAME, out strSTOMPLocation);
                    if (boolRadioEPG) 
                    {
                        SubscribeEPG(strSTOMPLocation, strGCC, intDABIndex);
                        boolRadioVISConfigured = true;
                        result = true;
                    }
                    else WriteLog("RadioEPG: No SRV record found for '" + strCNAME + "'");
                }
                else WriteLog("No CNAME found in RadioDNS");
            }
            else
            {
                CloseRadioEPG();

                RadioEPG(intDABIndex);
            }

            return result;
        }

        // Configure RadioDNS for the current DAB station
        private bool RadioDNS(UInt32 intDABIndex, out string CNAME, out string strGCC)
        {
            //Resolve the location of the STOMP server                                              
            string strCNAME = "";
            strGCC = "";

            //Find the CNAME for the radio station, try each ECC value
            foreach (string strECC in aryECCRegion)
            {
                strGCC = _ProgramInfoList[(Int32)intDABIndex].gcc + strECC;
                string strRadioDNS = _ProgramInfoList[(Int32)intDABIndex].scids + "." + _ProgramInfoList[(Int32)intDABIndex].sid + "." + _ProgramInfoList[(Int32)intDABIndex].eid + "." + strGCC + "." + _ProgramInfoList[(Int32)intDABIndex].type + qdn;
                WriteLog("Get CNAME for: '" + strRadioDNS + "'");

                if (GetCNAME(strRadioDNS, out strCNAME)) break;
            }

            CNAME = strCNAME;
            WriteLog("CNAME is: '" + CNAME + "'");            

            if (CNAME.Length > 0) return true; else return false;
        }

        // Triggered when new Image message arrives
        protected static void OnMessage_Image(IMessage receivedMsg)
        {
            try
            {
                ITextMessage msg = (ITextMessage)receivedMsg;
                receivedMsg.Acknowledge();

                ITextMessage message_image = receivedMsg as ITextMessage;
                aryMessage_Image = message_image.Text.Split(' ');
                aryMessage_Image[2] = aryMessage_Image[2].ToString().TrimEnd('\r', '\n'); //clean up
            }
            catch { aryMessage_Image = null; }
        }

        // Triggered when new Text message arrives
        protected static void OnMessage_Text(IMessage receivedMsg)
        {
            try
            {
                ITextMessage msg = (ITextMessage)receivedMsg;
                receivedMsg.Acknowledge();

                ITextMessage message_text = receivedMsg as ITextMessage;

                //Clean up the message
                strMessage_Text = message_text.Text.TrimEnd('\r', '\n'); //remove special characters from end of the message
                strMessage_Text = strMessage_Text.Substring(5); //Remove the 'TEXT' command from message
            }
            catch { strMessage_Text = ""; }
        }

        // returns true if found a valid CNAME, returnes CNAME
        private bool GetCNAME(string strFQDN, out string strCNAME)
        {
            Random random = new Random();
            bool result = false;
            strCNAME = "";

            string[] s = nDnsQuery.GetCNAMERecords(strFQDN);
            if (s.Length > 0)
            {
                // Select CNAME
                strCNAME = s[random.Next(0, s.Length)];
                WriteLog("Using: '" + strCNAME + "'");               
                result = true;                
            }

            return result;
        }

        // returns true if service record is found
        private bool GetSRV(RadioDNSSRV srv, string strCNAME, out string strSTOMPLocation)
        {
            Random random = new Random();
            bool result = false;
            string strSRVLocation = "";
            strSTOMPLocation = "";

            switch (srv)
            {
                case RadioDNSSRV.VIS:
                    strSRVLocation = "_" + strSRVRadioVISRecordSTOMP + "._tcp." + strCNAME;
                    break;
                case RadioDNSSRV.EPG:
                    strSRVLocation = "_" + strSRVRadioEPGRecordSTOMP + "._tcp." + strCNAME;
                    break;
            }

            //Is the service record there?
            WriteLog("Looking for: " + strSRVLocation);
            string[] s = nDnsQuery.GetSRVRecords(strSRVLocation);

            //Found a server
            if (s.Length > 0)
            {
                //Change from hardcoded first instance to random of total returned
                strSTOMPLocation = s[random.Next(0, s.Length)];
                WriteLog("Server:Port '" + strSTOMPLocation + "'");
                result = true;
            }

            return result;
        }

        //Subscribe to RadioVIS
        private bool SubscribeVIS(string strSTOMPLocation, string strGCC, UInt32 intDABIndex)
        {
            bool result_image = false;
            bool result_text = false;

            // Connect to the STOMP Server/Service
            IConnectionFactory factory = new NMSConnectionFactory(new Uri("stomp:tcp://" + strSTOMPLocation));
            connection = factory.CreateConnection();
            session = connection.CreateSession();

            //Image
            try
            {
                string strTopic = ("topic://" + _ProgramInfoList[(Int32)intDABIndex].type + "/" + strGCC + "/" + _ProgramInfoList[(Int32)intDABIndex].eid + "/" + _ProgramInfoList[(Int32)intDABIndex].sid + "/" + _ProgramInfoList[(Int32)intDABIndex].scids + "/image").ToLower();
                WriteLog("Topic: '" + strTopic + "'");
                IDestination destination = session.GetDestination(strTopic);
                IMessageConsumer consumer_image = session.CreateConsumer(destination);

                WriteLog("Connecting...");
                connection.Start();

                consumer_image.Listener += new MessageListener(OnMessage_Image);
                WriteLog("Consumer started (Image), waiting for messages...");

                result_image = true;
            }
            catch { WriteLog("Exception during Image connection"); }


            //Text
            try
            {
                string strTopic = ("topic://" + _ProgramInfoList[(Int32)intDABIndex].type + "/" + strGCC + "/" + _ProgramInfoList[(Int32)intDABIndex].eid + "/" + _ProgramInfoList[(Int32)intDABIndex].sid + "/" + _ProgramInfoList[(Int32)intDABIndex].scids + "/text").ToLower();
                WriteLog("Topic: '" + strTopic + "'");
                IDestination destination = session.GetDestination(strTopic);
                IMessageConsumer consumer_text = session.CreateConsumer(destination);

                WriteLog("Connecting...");
                connection.Start();

                consumer_text.Listener += new MessageListener(OnMessage_Text);
                WriteLog("Consumer started (Text), waiting for messages...");

                result_text = true;
            }
            catch { WriteLog("Exception during connection"); }

            // Return
            if (result_image || result_text) return true; else return false;
        }

        //Unsubscribe and close VIS connection
        private bool CloseRadioVIS()
        {
            if (boolRadioVISConfigured )
            {
                WriteLog("Close RadioVIS session and connection");
                session.Close();
                connection.Close();
                boolRadioVISConfigured = false; //Not configured
            }
            else WriteLog("Close RadioVIS - Nothing to do");

            return true;
        }

        //Unsubscribe and close EPG connection
        private bool CloseRadioEPG()
        {
            if (boolRadioEPGConfigured)
            {
                WriteLog("Close RadioEPG session and connection");          
                boolRadioEPGConfigured = false; //Not configured
            }
            else WriteLog("Close RadioEPG - Nothing to do");

            return true;
        }

        //Subscribe to RadioEPG
        private bool SubscribeEPG(string strSTOMPLocation, string strGCC, UInt32 intDABIndex)
        {
            return false;
        }        
    }
}