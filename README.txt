        [ ]
       (- -)
         |
   ______|______
  |               |
  |  requirement  |
  |  satisfied    |
  |_______________|
         |
        / \


GGJ 2026 – How This Project Is Set Up

This is not a reusable framework.
This is not production-quality architecture.
This README exists because it was required.

No, I am not detailing every single step or inspector field. I have more important things to do than write a Unity tutorial for a game jam project.

If you know Unity, this should be enough to understand how the project is wired.
If you don’t… honestly, I’m not sure why you’re here.


Scenes

There are four scenes. Yes, four.

GameLoader
An empty bootstrap scene with GameLoader on it. It exists so the game has a clean entry point. That’s it.

Title
Main menu scene. Contains the menu UI controller that talks to GameManager. Pressing Play loads the game scene.

Game
This is the main scene and where everything happens.

There is a GameObject here that holds the core gameplay components:
- ClientSessionRunner
- GameScreenUI
- ReferenceBookUI
- MaskSelectionTravelFX
- PauseMenuUI

All of these need to be present and correctly hooked up. If something is missing, the game will not work.

This assumes you already know how to create UI objects, buttons, images, and TextMeshPro fields.

Credits
Credits scene. Reached at the end of the game.


Data (ScriptableObjects)

The game is driven by ScriptableObjects.

You are expected to create:
- Clients
- Masks
- Colors
- Shapes
- Styles
- A client queue
- A mask library

Each client is a ClientDefinitionSO.
Each mask is a MaskDefinitionSO.
The client queue defines the order clients appear.
The mask library defines which masks are available.

Yes, this is a lot of setup.
No, this README is not walking you through creating every asset. Deal with it.


Client Session Runner

ClientSessionRunner controls the overall flow of clients.

It pulls from the client queue, spawns client prefabs, slides them in and out, handles timing, and handles scoring.

It requires:
- A ClientQueueSO
- A client prefab (ClientView)
- RectTransforms for start, center, and exit positions
- A parent transform for spawned clients

There are a lot of exposed motion and tuning values in the inspector. They exist so things could be tuned quickly during the jam. They are not individually documented.


Game Screen UI

GameScreenUI is the main UI controller during gameplay.

It displays request text, player feedback, handles confirm and next buttons, receives mask selections, and transitions to the score screen and credits.

It needs references to:
- MaskLibrary
- ClientSessionRunner
- MaskGridUI
- Various TMP text fields
- Buttons
- The selected mask image
- MaskSelectionTravelFX

All UI elements are expected to already exist in the scene.


Mask Grid and Mask Slots

There is a grid container with MaskGridUI on it.
Each individual mask slot has MaskSlotUI.

The grid handles creating slots, populating them with data, and tracking selection.
Each slot displays a mask icon, handles clicks, and reports selection back to the grid.

Yes, the grid has a script and every slot has a script.
This is intentional.


Mask Selection Travel FX

MaskSelectionTravelFX is visual-only.

It handles the mask flying from the grid to the selected slot, including ghosting, arcs, scale pop, and landing wobble.

It needs a reference to the main Canvas and a RectTransform for the selected mask display.

There are a lot of exposed values because the animation was tuned visually.
No, they are not all explained.


Reference Book UI

ReferenceBookUI handles the in-game reference book.

It displays two pages of text, handles page navigation, and shows hover hints and arrows.
Only two text fields exist. Page content is swapped in and out.


Pause Menu

PauseMenuUI shows and hides the pause menu and optionally pauses time.
Nothing fancy.


Final Notes

This project assumes you know how to use Unity.

It does not explain:
- How to make UI
- How to create ScriptableObjects
- How to assign references
- What every inspector slider does

The game was built to ship during a jam.
It does that.

Anything beyond that is outside the scope of this README.

Now wobble away from my stuff.
