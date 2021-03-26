## Spawning Objects

To spawn an object in the local and all remote clients at the same time, there is a NetworkSpawner.

There are two ways to spawn objects:

1. NetworkSpawner.Spawn(…)
 Use this for objects that are not meant to be persistent. Objects spawned through this function can only be seen by players that are already connected. A new player that joins later will not see them.
2. NetworkSpawner.SpawnPersistent(…)
 Use this for objects that are meant to be persistent. Objects spawned through this function will be stored by the server and the client of any new player receives a copy of this list to make sure the new player sees all the persistent objects already in the world.

**Note:**

The new object prefab needs to be known to the environment! For that, add it to the PrefabCatalogue of the SceneManager. If the scene manager does not have a catalogue yet, you can create one in the project window by right-clicking-\&gt;create-\&gt;Prefab catalogue
 You then have to drag it into the scene manager to use it.