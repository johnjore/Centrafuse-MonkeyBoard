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
 * Classes used for variables
*/

namespace DABFMMonkey
{
    using System;

    [Serializable]
    //Used to keep track of stations and their properties
    public class Station
    {
        private RADIO_TUNE_BAND _band;
        private UInt32 _frequency;
        private string _favoriteName;
        private string _DABLongName;

        public RADIO_TUNE_BAND Band
        {
            get { return this._band; }
            set { this._band = value; }
        }

        public UInt32 Frequency
        {
            get { return this._frequency; }
            set { this._frequency = value; }
        }

        public string FavoriteName
        {
            get { return this._favoriteName; }
            set { this._favoriteName = value; }
        }

        public string DABLongName
        {
            get { return this._DABLongName; }
            set { this._DABLongName = value; }
        }
    }

    //Used to keep track of blacklisted DAB channels
    public class BlackList
    {
        private string _DABLongName;

        public string DABLongName
        {
            get { return this._DABLongName; }
            set { this._DABLongName = value; }
        }
    }

    //BBE/EQ Settings
    public class BBEEQ
    {
        private BBEStatus _BBEOn;
        private SByte _EQMode;
        private SByte _BBELo;
        private SByte _BBEHi;
        private SByte _BBECFreq;
        private Byte _BBEMachFreq;
        private SByte _BBEMachGain;
        private SByte _BBEMachQ;
        private SByte _BBESurr;
        private SByte _BBEMp;
        private Byte _BBEHpF;
        private SByte _BBEHiMode;
        private SByte _HeadRoom;
        
        public BBEStatus BBEOn
        {
            get { return this._BBEOn; }
            set { this._BBEOn = value; }
        }

        public SByte EQMode
        {
            get { return this._EQMode; }
            set { this._EQMode = value; }
        }

        public SByte BBELo
        {
            get { return this._BBELo; }
            set { this._BBELo = value; }
        }

        public SByte BBEHi
        {
            get { return this._BBEHi; }
            set { this._BBEHi = value; }
        }

        public SByte BBECFreq
        {
            get { return this._BBECFreq; }
            set { this._BBECFreq = value; }
        }

        public Byte BBEMachFreq
        {
            get { return this._BBEMachFreq; }
            set { this._BBEMachFreq = value; }
        }

        public SByte BBEMachGain
        {
            get { return this._BBEMachGain; }
            set { this._BBEMachGain = value; }
        }

        public SByte BBEMachQ
        {
            get { return this._BBEMachQ; }
            set { this._BBEMachQ = value; }
        }

        public SByte BBESurr
        {
            get { return this._BBESurr; }
            set { this._BBESurr = value; }
        }

        public SByte BBEMp
        {
            get { return this._BBEMp; }
            set { this._BBEMp = value; }
        }

        public Byte BBEHpF
        {
            get { return this._BBEHpF; }
            set { this._BBEHpF = value; }
        }

        public SByte BBEHiMode
        {
            get { return this._BBEHiMode; }
            set { this._BBEHiMode = value; }
        }

        public SByte HeadRoom
        {
            get { return this._HeadRoom; }
            set { this._HeadRoom = value; }
        }        
    }

    //RadioDNS attributes
    public class RadioDNS
    {
        private UInt32 _dabIndex;
        private String _gcc;
        private String _eid;
        private String _sid;
        private String _scids;
        private String _type;

        public UInt32 dabIndex
        {
            get { return this._dabIndex; }
            set { this._dabIndex = value; }
        }

        public String gcc
        {
            get { return this._gcc; }
            set { this._gcc = value; }
        }

        public String eid
        {
            get { return this._eid; }
            set { this._eid = value; }
        }

        public String sid
        {
            get { return this._sid; }
            set { this._sid = value; }
        }

        public String scids
        {
            get { return this._scids; }
            set { this._scids = value; }
        }

        public String type
        {
            get { return this._type; }
            set { this._type = value; }
        }
    }
}
