# Network and Component Types

## Network Ids

Network Ids are 64-bit identifiers. They are represented by the `Ubiq.Messaging.NetworkId` structure in C# and the `Messaging.NetworkId` class in Javascript.

In C# Ids are value types, and the equality operators are overridden. In Javascript they are reference types and must be explicitly compared using the static `NetworkId.Compare` method.

```
NetworkId.Compare(message.objectId, server.objectId)
```

Internally, the types are represented by two 32 bit integers. This is an implementation detail and should not be relied upon. The reason for using two 32 bit integers rather than one 64 bit long, is that Javascript only supports 53 bit integers.

### Binary vs Json

In Javascript, `NetworkId`s must be handled in both their binary and Json forms. This is because Ubiq messages include the `NetworkId` in their binary header, while some Javascript services, such as the Room Server, accept `NetworkId`s as arguments. 

For example, a Json message would arrive containing the Id of an object that the Javascript code should send a message to. The Javascript code will need to convert that into a binary representation in order to build the header.

Binary `NetworkId`s are converted to `NetworkId` class instances by the `Message` wrapper. From this point on any Javascript code can work with the object in its Json representation.

`NetworkId` instances and generic Json objects representing a network Id can interoperate.
