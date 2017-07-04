# Centrafuse - MonkeyBoard

A Centrafuse full plugin for the DAB, DAB+ and FM radio Monkeyboard (Pro and Standard), http://www.monkeyboard.org/products/85-developmentboard/85-dab-dab-fm-digital-radio-development-board-pro is ready for testing. There may be bugs, there may be issues, it may crash. But hopefully, it all just works.

The plugin should work with CF 3.5 and up, as its compiled using .NET 3.5, but only tested on CF 4.3. Latest versions will be available from the CF Market. (When CF approves it)

Important: Use settings to set "Enable Plugin for Radio" to On. Else it will not show the GUI or work as a plugin as its other purpose is to provide configuration settings to the radio module. Make sure you do not enable the radio module and the plugin at the same time.

Important #2: Help me to help you and provide log files. Centrafuse.log, error.log and dabfmmonkey.log. Clean them out, start CF, reproduce the issue, close CF. Send me the log files and indicate where / when the error happened.

If you get the error message "Lost contact with radio", you're most likely running (active in CF) both the radio module and the plugin. Both are trying to control the radio and corrupting the serial communication. Or the USB cable has issues. Dodgy / cheap USB cables can cause connectivity issues.


## Features
* All of the boards functionality is supported.
* Hotkeys for steering wheel integration. See Settings for a list of hotkeys. Hotkeys can be changed by user. (Also supports Load Radio Preset 1 to 8 wiki.centrafuse.com/wiki/Hotkeys.ashx)
* COM port autodetection
* Fully supports language convertions (please send me your translated XML files)
* CF 3.5 and up
* Hibernation/sleep
* RadioDNS / RadioVIS (www.radiodns.org)

## Screenshots
todo... 

Note that only the Pro board supports SLS (both boards support RadioVIS on DAB supported radio stations)

For the most part, it mirrors the way CF radio modules work, and the graphics and layout have been copied from CF. Most buttons use both short press and long press to maximize GUI real estate usage, and to keep the plugin as identical to radio modules as possible.

## DAB Mode
* DAB/FM: Switches to FM Mode. Button text is updated to reflect the ensemble name
* Select: A scroll list of all DAB programs is shown to select from. Programs in Blacklist are suppressed
* Scan: Wipes all programs from the board (not favorites) and scans for DAB programs
* <: Go to first program: Anyone got a better suggestion for how to use this button?
* \>: Go to last program: Anyone got a better suggestion for how to use this button?
* <<: Previous program. Wraps around and skips blacklisted channels
* \>>: Next program. Wraps around and skips blacklisted channels
* Scrolling text: %/% = Signal Strength / Quality
* SLS/RadioVIS: When there's an image to show, it's overlaid the favorites list. Click on the image and it will disappear. If RadioVIS service is available, SLS is not used.
Note: If using RadioVIS, it must be explicitly enabled in Settings along with Internet usage. Default is off. 

## FM Mode 
* DAB/FM: Switches to DAB Mode
* Tune: A dialog box for entering the FM Frequency
* Scan: Searches up for next FM channel
* <: Changes frequency down by 50Hz
* \>: Changes frequency up by 50Hz
* \<<: Searches down for next FM channel
* \>>': Searches Up for next FM channel
* Scrolling text: % = Signal Strength

## Common
* DAB/FM: Long press: BBE/EQ configuration screen
* Select/Tune: Long press: Toggles between Forced Mono and Auto Stereo. If text is in italics, its in forced Mono mode
* +: Add current channel / frequency to favorites
* <: Long press: Changes the boards internal volume down
* \>: Long press: Changes the boards internal volume up
* Trashcan: Delete selected channel / frequency from favorites
* Trashcan: Long press: Blacklists DAB channels from appearing in lists
* CF's top right <<: Legacy mode: Previous favorite
* New mode: Works like "normal" << 
* CF's top right >>: Legacy mode: Next favorite
* New mode: Works like "normal" >>

## Settings
There are around 10 configuration settings + hotkey definitions. For the most part they are self explanatory, but a selection of settings are documented here:

* Enable Plugin for Radio: Unless its set to "On", the plugin will close at startup to avoid conflicting with the Radio Module. Make sure the Monkeyboard Radio module is NOT enabled in CF, else there will be conflicts over who controlls the radio.
* China Mode: Don't enable unless you're really in China as wrong DAB frequencies will be scanned
* Legacy Buttons: Changes the function of the top right << and >> buttons
* Internet / RadioVIS: Both options must be enabled to use RadioVIS and RadioVIS will supress DAB RDS text and images. Not all stations support RadioVIS and SLS/RDS text will be used if not available. Don't enable unless you really have internet access.
* ECC region: Set it to your area to speed up locating the RadioVIS image. Leaving blank will scan all ECC codes. Setting wrong value will prevent RadioVIS from finding any images.

## Credits
Paul C converted my Listview to Advanced listview. Without his assistance it would have taken another couple of weeks to complete.
CF forums for help during the initial radio module. A lot of code was copied across to this plugin.
CF for providing CF and an SDK (Even though the SDK documentation leaves a lot to be desired!)
Testers who provided feedback and/or PayPal donations.

## Quirks
Some settings can only be changed by restarting CF. One of these settings are hotkey definitions as they are read during startup and can't be freed up during runtime.
Error handing could be better. Period.
RadioVIS requires the correct ECC code to locate the image. Currently the KeyStone chip does not provide the ECC codes. Upto 20 DNS lookups are used to get around this.
RadioVIS does not work on FM as the KeyStone chip does not provide the "PI" code required to locate the image. Email Monkeyboard/KeyStone if you want this feature.

## Compile
Please note that the APIs to control the board can only be used with the MonkeyBoard and its DLL and no other KeyStone implementation.
To compile the source a copy of the Centrafuse SDK is required, www.centrafuse.com

## Bugs
Some Centrafuse versions must have "Auto Start Music" set to On, else audio plugins, including this one, won't open the GUI. Upgrading to latest CF does not fix the issue.

Please report any issues as well as suggestions and feature requests. Please leave "Log Events" on if you're reporting an issue. It contains a lot of information and might pinpoint the issue.
