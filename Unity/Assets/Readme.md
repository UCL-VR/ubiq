This project is the Unity-side implementation of the UCL CVE.

The expected use-case is for a team to branch this project and extend the code-base to build a CVE specific to their application.

The project has three parts:

**Common Code Base**
- Scripts and other assets specific to the UCL CVE that form the common platform. 
- Derivative projects will add to and modify these.
- Stored in the root Assets/ folder and subfolders other than dependencies or samples

**Dependencies**
- Re-usable components that handle message passing, time, book-keeping etc that support the common code base
- Loosely coupled with eachother, completely de-coupled from the platform
- Have contributions from other projects, but do *not* have their own repositories

**Samples** 
- Example mini-projects of various size, demonstrating how the common code base may be used.
