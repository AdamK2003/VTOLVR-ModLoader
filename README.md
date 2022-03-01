## Original repo [here](https://gitlab.com/vtolvr-mods/ModLoader)
# VTOL VR Mod Loader

![Mod Loader Program](https://vtolvr-mods.com/static/files/modloader2.gif)

VTOL VR Modding aims to add more user-created content into the game as mods. With the mod loader, players can add custom code into the game to add extra features that they wanted.

## [Creating a mod](https://vtolvr-mods.com/modloader/creating-a-mod "Guide on creating a mod")

If you just want to create a mod for the mod loader. You can follow the guide at [vtolvr-mods.com](https://vtolvr-mods.com/modloader/creating-a-mod "Guide on how to create a mod") to get started with creating mods. To create a mod it requires some basic knowledge how [Unity](https://unity.com/ "Unity Game Engine") game engine works and C# but people have still managed to learn it on the go.

## Contents of this repository
### Builder
Builder is a simple console application in .NET framework 4.5. Its role is to build and package the client application and files for release.

99% of the time you won't need to touch this. Unless you're trying to some big change to the projects

### Core
Core is a basic class library in .NET Standard 2.0. Its goal is just to share some common code between the Launcher and Mod Loader projects. (This might be able to be turned into a shared library but have not figured out how)

### Launcher
Launcher is the main WPF application using .NET Core 5.0. This provides a place for users to update the mod loader, install and update mods or skins, create their own projects and view the changelog.

### Logo Files
This is just a directory to publicly store our logos / promotional art. These get stored in the format they were created in so they can easily be adapted for whatever platform they needed to be exported to.

### Mod Loader
Mod Loader is the class library that gets loaded into the game. This handles everything in the game and is built using .NET Framework 4.6.1.

### VTPatcher
VTPatcher is another class library that uses [Doorstep](https://github.com/NeighTools/UnityDoorstop) to run just before Unity is loaded when users run the game. The role of this class library is to patch the game's code to add in a function that loads the Mod Loader project and turns every method and variable into public and virtual to make modding much easier.

## Setup for contributing
To build this solution you require some of the game's files to be present in the `dll` folder.

These files can be found inside your games directory. Then head to ``VTOL VR\VTOLVR_Data\Managed``. Following the ``instructions.txt`` inside the `dll` folder will show you want ones you need to copy over.

## Contributors

A special thanks to all these people for their help in creating the mod loader to what it is today.

[Ketkev](https://github.com/ketkev "Ketkev's Github") for all his work on the website, hosting the website, maintaining the website and assistant with managing the project.

[Nebriv](https://github.com/nebriv "Nebriv's Github") for his early support to the mod loader, help with bug testing and help with setting up the new website.

[Temperz87](https://gitlab.com/Temperz87) for minor bug fixed in [different pull request](https://gitlab.com/vtolvr-mods/ModLoader/-/merge_requests?scope=all&state=merged&author_username=Temperz87)

[Yellowbluesky](https://gitlab.com/yellowbluesky) for a typo in pull request [!102](https://gitlab.com/vtolvr-mods/ModLoader/-/merge_requests/102)

[sgoodwin3105](https://gitlab.com/sgoodwin3105) for fixing a bug with skins in pull request [!83](https://gitlab.com/vtolvr-mods/ModLoader/-/merge_requests/83)
