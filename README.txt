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


GGJ 2026 – Project Setup Overview

This is a game jam project.

It is not a reusable framework.
It is not production-grade architecture.
This file exists because a README was required.

The purpose of this file is to explain how the project is wired together.
It is not a Unity tutorial.

If you are familiar with Unity, this should be enough to understand the setup.
If you are not, this project is probably not a good learning reference.


------------------------------------------------------------
SCENES
------------------------------------------------------------

There are four scenes.

GameLoader
An empty bootstrap scene containing GameLoader.
This scene exists solely to provide a clean entry point and initialize core systems before loading anything else.

Title
The main menu scene.
Contains the menu UI controller, which talks to GameManager.
Pressing Play loads the Game scene.

Game
The main gameplay scene.

This scene contains a GameObject holding the core gameplay components:
- ClientSessionRunner
- GameScreenUI
- ReferenceBookUI
- MaskSelectionTravelFX
- PauseMenuUI

All of these must be present and correctly referenced.
If something is missing or unassigned, the game will not function.

This setup assumes you already know how to create UI objects, buttons, images, and TextMeshPro fields.

Credits
The credits scene, reached at the end of the game.


------------------------------------------------------------
DATA (SCRIPTABLEOBJECTS)
------------------------------------------------------------

The game is driven primarily by ScriptableObjects.

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

Yes, this is a fair amount of setup.
No, this file does not walk through creating each asset.


------------------------------------------------------------
CLIENT SESSION RUNNER
------------------------------------------------------------

ClientSessionRunner controls the overall flow of clients.

It:
- Pulls clients from the client queue
- Spawns client prefabs
- Slides them in and out of view
- Tracks timing
- Handles scoring

It requires:
- A ClientQueueSO
- A client prefab (ClientView)
- RectTransforms for start, center, and exit positions
- A parent transform for spawned clients

There are many exposed tuning values in the inspector.
They exist so behavior could be tuned quickly during the jam.
They are not individually documented.


------------------------------------------------------------
GAME SCREEN UI
------------------------------------------------------------

GameScreenUI is the primary UI controller during gameplay.

It:
- Displays client request text
- Displays player feedback
- Handles confirm and next buttons
- Receives mask selections
- Transitions to the final outcome and score screens
- Triggers the credits scene at the end

It requires references to:
- MaskLibrary
- ClientSessionRunner
- MaskGridUI
- Multiple TextMeshPro text fields
- Buttons
- The selected mask image
- MaskSelectionTravelFX

All UI elements are expected to already exist in the scene.


------------------------------------------------------------
MASK GRID AND MASK SLOTS
------------------------------------------------------------

There is a grid container with MaskGridUI attached.
Each individual mask slot has MaskSlotUI.

The grid:
- Creates mask slots
- Populates them with data
- Tracks selection state

Each slot:
- Displays a mask icon
- Handles click input
- Reports selection back to the grid

Yes, the grid has a script and each slot has a script.
This is intentional.


------------------------------------------------------------
MASK SELECTION TRAVEL FX
------------------------------------------------------------

MaskSelectionTravelFX is visual-only.

It handles the mask flying from the grid to the selected slot, including:
- Ghosting
- Arcing motion
- Scale pop
- Landing wobble

It requires:
- A reference to the main Canvas
- A RectTransform for the selected mask display

Many values are exposed because the animation was tuned visually.
They are not individually explained.


------------------------------------------------------------
REFERENCE BOOK UI
------------------------------------------------------------

ReferenceBookUI controls the in-game reference book.

It:
- Displays two pages of text
- Handles page navigation
- Shows hover hints and navigation arrows

Only two text fields exist.
Page content is swapped dynamically as pages change.


------------------------------------------------------------
PAUSE MENU
------------------------------------------------------------

PauseMenuUI handles showing and hiding the pause menu.
It can optionally pause time by adjusting Time.timeScale.

Nothing complex is happening here.


------------------------------------------------------------
AUDIO SYSTEM
------------------------------------------------------------

Audio is handled by AudioManager and AudioLibrary.

AudioManager is created and initialized in the GameLoader scene and registered through the ServiceLocator.
It persists across scene loads.

AudioLibrary is a MonoBehaviour placed in the GameLoader scene.
It contains references to all audio clips used by the game.

Audio is split into three channels:
- Music
- SFX
- VO (voice-over)

Each channel has:
- Independent volume
- Priority
- Fade-in and fade-out timing

Hard audio cuts are intentionally avoided.
Music and VO always fade out before being replaced.
Rapid input (menu spam, fast client progression) will smoothly replace audio rather than abruptly stopping it.

Music behavior:
- Title music plays in the Title scene
- Game music replaces title music when entering the Game scene
- Music stops when entering the Credits scene

SFX behavior:
- UI clicks
- Client arrival and departure sounds
- Mask selection and confirmation sounds
SFX are layered using PlayOneShot and are never interrupted.

VO behavior:
- Each client can have:
  - One request VO
  - One response VO (fail / partial / success)
  - One exit VO
Only one VO plays at a time.
If a new VO is triggered while another is playing, the current VO fades out and the new one fades in quickly.

Audio tuning is controlled through AudioManagerConfig.
Values can be adjusted in code or via a ScriptableObject if desired.


------------------------------------------------------------
FINAL NOTES
------------------------------------------------------------

This project assumes a working knowledge of Unity.

It does not explain:
- How to create UI
- How to create ScriptableObjects
- How to assign inspector references
- What every exposed tuning value does

The game was built to be completed and submitted during a game jam.
It fulfills that purpose.

Anything beyond that is outside the scope of this file.

Please approach it with the appropriate expectations.
