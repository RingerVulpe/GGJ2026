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

This is a Global Game Jam project.

It is not a reusable framework.

The goal of this file is to explain how the project is wired together,
not to teach Unity or justify design decisions made under time pressure.

If you are comfortable with Unity, this should be enough to orient you.
If you are not, this project is probably not a great learning reference.


------------------------------------------------------------
SCENES
------------------------------------------------------------

There are four scenes.

GameLoader
An intentionally minimal bootstrap scene containing GameLoader.

Its only responsibility is initializing persistent systems
(GameManager, AudioManager, etc.) before loading anything else.
Nothing interactive happens here.

Title
The main menu scene.

Contains the menu UI controller, which communicates with GameManager.
Pressing Play transitions into the Game scene.

Game
The main gameplay scene.

This scene must contain a GameObject holding the following core components:
- ClientSessionRunner
- GameScreenUI
- ReferenceBookUI
- MaskSelectionTravelFX
- PauseMenuUI

All references must be correctly assigned.
If something is missing or unassigned, the game will not function.

This setup assumes you already know how to create UI objects,
buttons, images, and TextMeshPro fields.

Credits
The final scene, reached after the end-of-game flow completes.


------------------------------------------------------------
DATA (SCRIPTABLEOBJECTS)
------------------------------------------------------------

The game is primarily data-driven via ScriptableObjects.

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
The mask library defines which masks are available to the player.

------------------------------------------------------------
CLIENT SESSION RUNNER
------------------------------------------------------------

ClientSessionRunner controls the overall flow of the game.

It:
- Pulls clients from the client queue
- Spawns client prefabs
- Slides clients in and out of view
- Tracks timing
- Handles scoring and progression

It requires:
- A ClientQueueSO
- A client prefab (ClientView)
- RectTransforms for start, center, and exit positions
- A parent transform for spawned clients

Many tuning values are exposed in the inspector.
They exist to allow rapid iteration during the jam
and are not individually documented.


------------------------------------------------------------
GAME SCREEN UI
------------------------------------------------------------

GameScreenUI is the primary gameplay UI controller.

It:
- Displays client request text
- Displays player feedback
- Handles confirm and next buttons
- Receives mask selections
- Manages final outcome and score presentation
- Transitions to the credits scene

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

The mask selection UI is split into two parts.

MaskGridUI
Attached to the grid container.

It:
- Creates mask slots
- Populates them with data
- Tracks selection state

MaskSlotUI
Attached to each individual slot.

Each slot:
- Displays a mask icon
- Handles click input
- Reports selection back to the grid


------------------------------------------------------------
MASK SELECTION TRAVEL FX
------------------------------------------------------------

MaskSelectionTravelFX is purely visual.

It handles the mask traveling from the grid
to the selected slot, including:
- Ghosting
- Arcing motion
- Scale pop
- Landing wobble

It requires:
- A reference to the main Canvas
- A RectTransform for the selected mask display

Many values are exposed because the animation
was tuned visually during development.
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
Nothing complicated is happening here.


------------------------------------------------------------
AUDIO SYSTEM
------------------------------------------------------------

Audio is handled by AudioManager and AudioLibrary.

AudioManager is created in the GameLoader scene
and registered through the ServiceLocator.
It persists across scene loads (same as GameManager).

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

Music behavior:
- Title music plays in the Title scene
- Game music replaces title music when entering the Game scene
- Music stops when entering the Credits scene

SFX behavior:
- UI clicks
- Client arrival and departure
- Mask selection and confirmation
SFX are layered using PlayOneShot and are never interrupted.

VO behavior:
- Each client can have:
  - One request VO
  - One response VO (fail / partial / success)
  - One exit VO
- Only one VO plays at a time
- New VO replaces existing VO via fades,
  never via hard stops


------------------------------------------------------------
FINAL NOTES
------------------------------------------------------------

This project assumes a working knowledge of Unity.

It does not explain:
- How to create UI
- How to create ScriptableObjects
- How to assign inspector references
- What every exposed tuning value does

Anything beyond that is intentionally out of scope.

Please approach it with the appropriate expectations.
