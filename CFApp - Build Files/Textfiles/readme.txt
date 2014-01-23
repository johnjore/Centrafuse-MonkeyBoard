This plugin is for the DAB / FM Monkeyboard and should be considered a work in progress.


Instructions:
	Best: Install and hope for the best...
	If install fails, unpack the .cfapp manually (its a zip file) and copy the files manually as per the cfapp.xml file


Known issues:
	None


For support use the CF forum: 
	http://forums.centrafuse.com/showthread.php?13314-DAB-DAB-FM-Digital-Radio-Board-(Aka-The-Monkeyboard)-Full-Plugin

	If I don't respond, you may send me an email to nudge me into action: j o h n ( a ) j o r e . n o

	If you email me directly without posting your support request on the forum I will ignore you.
	I get too many identical requests for help via email. Most of these are not related to the plugin
	and/or other forum users can help out by answering.


Revisions:
	00: Inital release
	01: If no DAB channels are stored on board, and no DAB channels are found after a rescan, the plugin ended up in an undefined state.
 	02: Fixed capturing of CENTRAFUSE.MAIN.FASTFORWARD and .REWIND events. Better handling of changing audio plugins.
	03: Fixed mute after DAB Scan.
	04: If config.xml is corrupt, its removed and replaced at startup
	    Status text during DAB Scanning now fits within the window
	    Prev/Next button: New or Legacy mode. Default is New (current mode)
	    	If in Legacy mode, the 2x top right buttons will scroll through favorites
	    Stereo mode selection is now persistent across restarts
	    Recovers (more) gracefully if non-valid FM frequency selected
	    Hibernation/sleep support
	05: Timing issue on Hibernation/Sleep
	    Internal cleanup & too many bug fixes to list
	    Added support for Radio Favorites Hotkeys 1 to 8 (See http://wiki.centrafuse.com/wiki/Hotkeys.ashx)
	    (Experimental) API Support for other plugins to query status:	Get MODE (DAB/FM/UNDEFINED)
										Get CHANNEL (Active Freq or Channel)
		Additonal queries can be added by requested
	06: Various bug fixes and internal changes
	    Added RadioDNS and RadioVIS support
	08: Fixed:
		Hotkeys broke after entering "Settings"
	    	Communication corruption when pause/unpause plugin
		Added integration with Garmin Mobile PC plugin
	09: Changes
		Added support for ATT and mapFactor Navigator plugin
		"Next/Prev" and steering wheel integration now requires only one button for each action for both mediaplayer and this plugin
		General cleanup and internal changes
