## Spawning Objects

You can create objects at runtime on all Peers using the NetworkSpawner.

![Network Spawner](images/e8cae779-d00b-47ac-81f2-28f14e6b8fd8.png)


Objects are spawned by calling Spawn() or SpawnPersistent(). These are static methods so are accessible anywhere in the code. However, they need a Unity GameObject to find the NetworkScene in which to spawn the object.

```
Spawn(
	behaviour,  // A Component in the Scene, e.g. the Component calling Spawn
	prefab 		// The Prefab to Spawn
)
```	
	
There are two Spawn methods: Spawn and SpawnPersistent.

Spawn is used for objects  that are not meant to be persistent. Objects spawned through this function can only be seen by players that are already connected. A new player that joins later will not see them.

SpawnPersistent will spawn objects that are stored by the server and the client of any new player receives a copy of this list to make sure the new player sees all the persistent objects already in the world.


Before a GameObject can be Spawned, it must be added to the PrefabCatalogue of the SceneManager.

Do this by adding a new entry to the Catalogue in the Inspector. Once the Prefab GameObject has been added it can be spawned by passing the same reference to Spawn or SpawnPersistent.

![Prefab Catalogue](images/740061ba-7bfe-4832-9c97-d75e85b9e26c.png)