# Log Collector Service

The LogCollectorService is an example NodeJs application that joins a room and writes all the Experiment Log Events (0x2) in that Room to disk.

The sample is located in the `Node/samples/logcollectorservice` directory.

## Use Case

The Log Collector Service can be used to automatically collect data from self-directed experiments.

To do this, an experimentor would create a build that automatically joined a pre-defined Room. Additionally, the Log Collector Service would be started on a server and configured to join the same Room.

As participants ran their builds and completed the experiment, they would all join the same room and forward their events to the `LogCollector` running in the Log Collector Service, which would write those events to disk on the server.


## Questionnaire Scene

A good way to try the Log Collector Service is to use the *Questionnaire* Sample (Samples/Single/Questionnaire).

Add the Join Room Component to the NetworkScene, and set the Room GUID to the same one as in the `logcollectorservice` app.

Be sure to generate a new GUID to avoid collisions with others potentially trying the same demonstration.

Then, start and stop the Questionnaire scene, submitting the Questionnaire a few times each run.

In the logcollectorservice directory, a number of log files should be created, with the Ids that the Peer took on each time it started.


## Configuration

The logcollectorservice application is configured by changing the source code of app.js. The two variables that are likely to change are the log event type, and the room GUID.