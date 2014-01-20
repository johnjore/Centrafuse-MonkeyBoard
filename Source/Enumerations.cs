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
 * All string to number translations
*/


namespace DABFMMonkey
{
    using System;

    public enum MonkeyCommand : sbyte
    {
        NONE = -1,
        SETVOLUME = 0,
        VOLUMEPLUS = 1,
        VOLUMEMINUS = 2,
        VOLUMEMUTE = 3,
        STOPSTREAM = 4,
        PLAYSTREAM = 5,
        NEXTSTREAM = 6,
        PREVSTREAM = 7,
        SCANDAB = 8,
        ADDFAVBTNCLICK = 9,
        TUNESELECT = 10,
        STEREOMODE = 11,
        GETBBEEQ = 12,
        SETBBEEQ = 13,
        CLOSERADIOPORT = 14,
        SETATTVOLUME = 15
    }

    public enum RADIO_TUNE_BAND : sbyte
    {
        UNDEFINED = -1,
        DAB_BAND = 0,
        FM_BAND = 1        
    }

    public enum DABNameMode : sbyte
    {
        Short = 0,
        Long = 1
    }

    public enum DABStatus : sbyte
    {
        Unknown = -1,
        Playing = 0,
        Searching = 1,
        Tuning = 2,
        Stop = 3,
        Sorting = 4,
        Reconfiguring = 5
    }

    public enum Mode : sbyte
    {
        MONO = 0,
        AUTO = 1,
        UNDEFINED = -1
    }

    public enum MotMode : sbyte
    {
        SlideShow = 0,
        EPG = 1
    }

    public enum ApplicationType : sbyte
    {
        Unknown = -1,
        SlideShow = 0,
        BWS = 1,
        TPEG = 2,
        DGPS = 3,
        TMC = 4,
        EPG = 5,
        DABJava = 6,
        DMB = 7,
        PushRadio = 8
    }

    public enum Volume : sbyte
    {
        Up = 1,
        Down = 2,
        Undefined = 3,
        Min = 0,
        Max = 16
    }

    public enum SLSLayout : sbyte
    {
        Normal = 0,
        Centered = 1,
        Stretched = 2
    }

    public enum BBEStatus : sbyte
    {
        Undefined = -1,
        Off = 0,
        BBE = 1,
        EQ = 2
    }

    public enum RadioDNSSRV : sbyte
    {
        Undefined = -1,
        VIS = 0,
        EPG = 1,
        TAG = 2
    }

    public enum RadioDNSVIS : sbyte
    {
        Undefined = -1,
        Text = 0,
        Image = 1
    }

    public enum ServCompType : sbyte
    {
        Undefined = -1,
        DAB = 0,
        DAB_plus = 1,
        PacketData = 2,
        DMB = 3
    }
}
