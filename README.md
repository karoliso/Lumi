NOTE: [The latest version of Lumi is available on the Unity Asset Store](https://assetstore.unity.com/packages/slug/302308)

**This repository is no longer updated.**

# Lumi
Lumi is a Unity Engine light detector, created to facilitate stealth and horror genre games.

Tested to work with URP and linear colour space.

## Features
* Supports Directional, Point, and Spot realtime lights.
* Baked light support via light probes and lightmaps.
* Accounts for perceived brightness from coloured lights.
* Runs in editor for quick iteration.
* Multiple sample points per detector.
* Two sample point evaluation modes - Average and Max.

## Limitations
* When **Baked Light Sample Mode** is set to **Lightmap**, following apply:
  * The lightmap textures must be set to read/write enabled.
  * Lightmap should be in HDR (High Quality). See [Lightmap data format](https://docs.unity3d.com/Manual/Lightmaps-TechnicalInformation.html) for more information.
* Since realtime lights use ray casting, shadow caster mesh colliders need to be accurate.
* Does not account for soft shadow edges - a sample point is either in shadow or not.
* Cannot account for screen space Global Illumination solutions.
