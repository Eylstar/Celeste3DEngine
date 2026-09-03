# Celeste3DEngine

`Celeste3DEngine` is a full 3D rendering and gameplay engine, running inside Celeste itself. Models, lighting, shadows, physics, UI, all of it, drawn on top of, behind, or entirely in place of Celeste's own 2D rendering.

It's built for modders, everything is done via code, and the API follows a pattern extremely close from Unity's component system. `GameObject`, `Behaviour`, `Collider3D`, if you've touched something like that before, this should feel familiar fast.

You can basically think of Celeste 3D Engine as **"Unity in Celeste"**.

<img width="850" height="460" src="https://github.com/user-attachments/assets/4f9d5481-9d82-4cab-a404-142549f7bf98" />

*A short 3D exploration game built entirely with the Engine for the SSC24, running inside Celeste, in pixel art*

---



## What It Can Do

Models load from both `GLTF` or `OBJ`, with skinned animation support for GLTF.

Scenes are lit with one directional light and any number of Scene `Light`, with real time shadows and tunable `LightingSettings`.

Physics comes from a `CollisionSystem` (box and sphere colliders, collision or trigger events) alongside `RayCasts`.

UI rendering supports two types of `UICanvas`, screen space HUD or World panels that live directly in the 3D world.

The Engine also supports custom fonts, custom audio source with 3D spatialization, and a persistence system that lets a scene survive Celeste room transitions.

<img width="850" height="460" src="https://github.com/user-attachments/assets/59fa2d7e-8fc1-4b21-a855-1710f6a54b3c" />

*A 3D overworld scene with a skinned and animated character, fog and realtime shadows, in HD rendering*

If you don't plan on making a fully 3D game, the engine can also be integrated directly into the Celeste rendering, for things like custom decoration or 3D background.

<img width="850" height="460" src="https://github.com/user-attachments/assets/a2501230-3eb2-4415-9b54-4c35a521b3ec" />

*A simple scene with 3D cubes and spheres looping around the Celeste gameplay, both over and under the Celeste rendering*



---


## Read the Doc !!

The best place to actually start is the official Documentation page :  https://eylstar.github.io/Celeste3DEngineDocumentation/ 

And follow `Your First Project`, a full walkthrough that builds a small working scene from nothing, camera, models, physics, UI and all, step by step.

<img width="850" height="460" alt="UI" src="https://github.com/user-attachments/assets/eac68aea-ac8d-4c37-a7b6-265e496e1125" />

*The simple scene you're going to learn how to do in the First Project tutorial*
