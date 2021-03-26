# Versioning

There is no such thing as an "authoritative" server in Ubiq. Components such as the Room Server implement services to fulfil a particular goal. They behave as traditional servers through some concepts:

1. They have pre-defined Ids
2. In some cases, such as the actual RoomServer Node App, they process messages themselves and change the messaing behaviour.


Such services use pre-defined Ids and can make assumptions about how they will be used, but always at least assume that both themselves and their users are in a Ubiq mesh. Components like the RoomClient will bootstrap this by creating those initial connections.

Each service then is responsible for maintaing, or not, compatability and version control.

