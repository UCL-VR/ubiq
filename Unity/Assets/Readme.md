This project is the Unity-side implementation of Ubiq.

The expected use-case is for a team to branch this project and extend the code-base to build something specific to their application.

Assets are divided into four folders:

**Runtime and Editor**
- Scripts and other assets specific to the Ubiq that form the common platform
- Derivative projects will add to (and potentially modify) these
- Divided into runtime and editor to allow for easy packing as a UPM package

**Samples**
- Example mini-projects of various size, demonstrating how the common code base may be used.

**Local**
- Assets specific to this Unity project, including packaging tools and XR settings
- These need not be included in any other project and are not included in the UPM package