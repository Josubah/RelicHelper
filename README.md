Relic Helper
=======================================================

##Client config profile manager & helper for Relic Tibia Server

##Features

1. client configuration profile management (including hotkeys, vip lists, open channels),
1. shared map files between profiles,
1. cam files storage in one place, shared between profiles,
1. exp/h counter,
1. time to level,
1. few quick links including map viewer showing your current location,
1. optional informational overlay (XP, progress, and statistics).

## Will I get banned for using it?

1. The goal of the project is not and never will be cheating or gaining an advantage over other players.
1. Relic Helper is focused on organization, convenience, and quality-of-life features, always respecting the server rules.

## How do I start with pre-compiled binary?
1. Place the binaries and .dll files directly into the Relic Tibia client folder and run RelicHelper.exe. The game client should not be running!
1. Create your profile by clicking the ADD PROFILE button in the bottom-left corner of the window. Enter the desired profile name and confirm.
1. The new profile should now appear on the list. You can double-click it or select it and click START CLIENT.
1. That’s it! Configure your client and join the game.
1. Note that you can also edit or delete any profile by right-clicking it.

## How do I import/duplicate configuration files between profiles?

1. At the time of writing this tutorial, it is not possible to import or duplicate profiles using the GUI. This must be done manually. First, make sure you have created your profile as described above. If your client is running, please close it.
1. Find the .cfg file you wish to replace:
1. open the Relic Tibia client installation folder and go to the profiles subfolder,
1. edit the profiles.xml file,
1. search for your profile name in the Name attribute of the Profile tag,
1. the CfgId attribute is the equivalent of the .cfg file name in the profiles folder,
1. close the profiles.xml editor.
1. Make a copy of your source .cfg file:
1. the default Relic Tibia config file is located at: Relic/game/Relic.cfg,
1. you may also copy a .cfg file of another profile from the profiles folder.
1. Rename the file to match the desired CfgId and replace the existing file.

Start Relic Helper and enjoy!

##Contribution

1. Submit an issue — include as much detail as possible about the bug or feature request,
1. Fork the repository on GitHub,
1. Create a topic branch instead of working directly on the main branch,
1. Push your changes to your fork,
1. Submit a pull request to this repository.