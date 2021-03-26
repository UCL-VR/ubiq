# Packaging

## Overview

The Unity side of the project can be built as a package for the Unity Package Manager.

## Releases

Release numbers use SemVer. The version is to be put in the filename and the package.json file.

The intention is that each release corresponds to a commit. To this end if you are making a new release version of the package:

1. Make sure you have no modified files (check your `git status` - the script does this for you)
2. Add a tag on your current commit with the release number, push the tag to the origin repo

## Building (Script)

There's a simple build script included in the project. It's integrated with the Unity Editor. Access it through the taskbar: 

`Ubik-dev -> Pack for Unity Package Manager` 

## Building (Manual)

Building the package manually is quite painless. 

1. Check `git status` does not indicate any modified files
2. Make a new folder and copy over the Editor, Runtime and Samples folders and the package.json and package.json.meta files
3. Rename the Samples folder to Samples~ (this stops Unity importing it into the main package)
4. Zip it!  
5. Name the zipped file ubik-{versionnumber}.zip