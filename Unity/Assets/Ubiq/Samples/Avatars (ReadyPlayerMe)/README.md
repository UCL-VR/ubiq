This sample contains compatibility tools for Ready Player Me avatars in Ubiq. Currently support is only good for `HalfBody` avatars.

![rpm-avatar](https://github.com/UCL-VR/ubiq-avatars-readyplayerme/assets/33021110/f25c7f6f-7b65-49d6-a964-eb3a8e00837a)

## What's Included

An avatar for Ubiq's avatar system which can load a ReadyPlayerMe model at runtime. Features:
* Head and hand motion (from Ubiq Avatar Hints)
* Grip animation (from Ubiq Avatar Hints) 
* Speech indicator (from VOIP)
* Simple lip-sync (from VOIP)
* Eye movement and blinking (just flavor, not connected to tracking info)

At the time of writing, recent versions of the ReadyPlayerMe package (e.g., 6.2.1) have issues loading `HalfBody` avatars, so this package depends upon an old version, 1.3.3.

## Using a Ready Player Me avatar in Ubiq

The `RPM-Avatar` prefab can be added to the `AvatarCatalogue` on your `AvatarManager` to make it spawnable. To also make it the default, replace `avatarPrefab` on the `AvatarManager` with `RPM-Avatar`. See the `LoaderExample` scene in this sample for what this looks like when configured.

## Changing the avatar model

Avatar models can be designed with Ready Player Me's web interface. An url is provided once the model has been created. Note before you start that this package currently only supports `HalfBody` avatars! The web interface for `HalfBody` avatars can be found [here](https://vr.readyplayer.me).

Avatar loading is done at runtime by the `UbiqReadyPlayerMeAvatarLoader` script on the `RPM-Avatar` prefab. Different models can be loaded by changing the `avatarUrl` variable on this script. Urls can also be supplied at runtime by calling the `Load` method on the script.

## Help! I'm seeing error CS0234

This version of the ReadyPlayerMe package manages its dependencies in code. If there are compilation errors at the time it is imported, it'll never run this import code and won't correctly install its dependencies. Here's the fix:

1. Delete this sample from your project folder
2. In the package manager, remove the `Ready Player Me Core` package
3. Ensure you have no compilation errors in your project
4. Import this sample again