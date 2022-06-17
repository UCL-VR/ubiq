# Event Logging Unit Tests

The LoggingDiagnostics scene (Samples/Single/Logging) performs stress testing of the Logging system.

This scene contains a Component, LoggingDiagnostics, and corresponding UI in the scene to control it outside the Editor.

The Component creates a number of LogEmitters that generate arbitrary, deterministic events. Additionally, the Component will instruct the Peer's LogCollector to become the primary Collector at random.

When multiple instances of this Peer are running, each will create and forward events to eachother as the primary mode moves between them. 

As events are deterministic, the log files can be checked once all Peers have shut down to verify that all events from all peers were logged correctly, despite changing the primary Collector and starting at different times.


The function to check the logs is also in the LoggingDiagnostics Component and is invoked in the Inspector in the Editor.

This code assumes that all test Peers were running on the same machine, as it will check all files in the systems persistent data path.

If some Peers were running elsewhere (e.g. on an Oculus Quest), those log files should be copied to the persistent data path first so their events will be included.

The user should quit the peers using the Finish & Exit UI button in the scene. Pressing this button will shut down all Peers in the Peer Group running the LoggingDiagnostics scene. This mechanism must be used as it delays the shutdown for a few seconds after the last log messages have been transmitted, to ensure enough time for the data to be logged and preventing false negatives when checking the data for integrity.