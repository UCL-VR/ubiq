# Packaging


## Overview

The Unity side of the project can automatically be built for the Unity Package Manager by github actions. It can also be done manually.


## Releases

Release numbers use SemVer (major.minor.patch). The version is to be put in the filename and the package.json file.

The intention is that each release corresponds to a commit. To this end if you are making a new release version of the package:

1. Make sure you have no modified files
2. Add a tag on your current commit with the release number, push the tag to the origin repo


## Building (github action)

If you push the tag to the main origin repo, a github action will automatically build the corresponding UPM. This can be accessed in the package manager at  {origin-repo}#upm (e.g. https://github.com/UCL-VR/ubiq.git#upm). Specific versions of the package can be identified by their full tag (https://github.com/UCL-VR/ubiq.git#upm-unity-v1.0.0-pre.16).



## Building (Manual)

Building the package manually is quite painless. 

1. Check `git status` does not indicate any modified files
2. Make a new folder and copy over the Editor, Runtime and Samples folders and the package.json and package.json.meta files
3. Rename the Samples folder to Samples~ (this stops Unity importing it into the main package)
4. Zip it!  
5. Name the zipped file ubiq-{SemVer}.zip