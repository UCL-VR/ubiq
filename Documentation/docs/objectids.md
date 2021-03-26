# Object and Component Ids

Network Object Ids are analogous to GameObject instance Ids, and are intended to be initialised late. For example, a spawner instantiates a prefab, then set its Id. Object Ids are represented by the `NetworkId` type.

Component Ids are analogous to class types and are intended to be fixed at design time. They should be unique within a single application. Component Ids are set by the NetworkComponentId Attribute, and so Ubiq can infer the Id from the type. Component Ids are `ushort`.

## NetworkId

Classes that are network objects implement the `INetworkObject` interface, which has one member, a get accessor for a `NetworkId`

`NetworkIds` can be generated or set at design time. They will almost always be generated however. While `NetworkId`s are mutable, they may be cached in different places (e.g. in lookup tables) by both Ubiq and user code, so changing them after the late-initialisation is strongly discouraged. By design `INetworkObject` only specifies the get accessor, so code that changes an object's `NetworkId` will need access to the full type - as it should if it is capable of safely changing the id.

The recommended pattern is to set the `NetworkId` when the interface is implemented,

```
public NetworkId Id { get; } = NetworkId.Unique();
```

This prevents any code from changing the Id, even within the class itself. 

There are other patterns however depending on need. 

For example, the Avatar class initialises a unique Id, but also implements the set accessor, as remote instances will need to have their Ids set to match their player's counterpart. Performing initialisation with the option to override before `Start()` allows users to build scenes with Avatar instances in the Editor, but also to instantiate prefabs dynamically.

```
public NetworkId Id { get; set; } = NetworkId.Unique();
```

The ISpawnable interface explicitly requires the set accessor, as spawned objects also need their Id set externally.

```
public interface ISpawnable
{
	NetworkId Id { set; }
	void OnSpawned(bool local);
}
```

In general, the guidelines are to:

1. Set `NetworkId`s before or during `Awake()`. 
2. Never read an object's `NetworkId` before `Start()`
3. Never change an unknown object's `NetworkId`.

If a `NetworkId` changes, it must be re-registerd with `NetworkScene::Register()`, however be aware that this may not be sufficient as other components may have cached the object beforehand.

## ComponentIds

Component Ids are fixed at design time and closely follow a particular type. 

If a Component Id is not explicitly set, it is automatically defined based on the full name of the type. For example, the full name of the NetworkSpawner as defined below is `Ubiq.Samples.NetworkSpawner`.

```
namespace Ubiq.Samples
{
    public class NetworkSpawner : MonoBehaviour, INetworkObject, INetworkComponent
    {
	}
}
	
```

The `ushort` is calculated using an MD5 hash, to ensure that the same value is resolved on different platforms.

Component Ids can also be set using the `NetworkComponentId` attribute.

Multiple Components may have the same Id, so long as the components will not be added to the same `NetworkObject`, though this is not recommended.


## Fixed Component Ids

In the same way as a known, fixed location is needed for clients to rendezvous, some services, such as the Room Server, need to be defined ahead of time. These are given unique Object and Component Ids.

## Multicasting Implementation

How fan-out is performed depends on the network architecture. In a mesh arrangement, all messages are transmitted on each connection by the Router object. In a client-server arrangement, such as with the Room Server, the server is responsible for forwarding messages to each peer in a room.

## Routing Implementation

Object and Component Ids are concepts. In practice, Component instances register themselves with a Scene. This registration process infers the Object and Component Ids for that instance, and store them in a Context. This context is used to route the messages. Object Ids are inferred from the closest antecedent that implements INetworkObject.

