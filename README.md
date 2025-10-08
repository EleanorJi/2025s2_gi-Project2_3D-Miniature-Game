# Antventure: Game Design Document

### Table of Contents

- [Game Overview](#game-overview)
  - [Core Concept](#core-concept)
  - [Related Genre](#related-genre)
  - [Target Audience](#target-audience)
  - [Unique Selling Points (USPs)](#unique-selling-points-usps)
- [Story and Narrative](#story-and-narrative)
  - [Backstory](#backstory)
  - [Characters](#characters)
- [Gameplay and Mechanics](#gameplay-and-mechanics)
  - [Player Perspective](#player-perspective)
  - [Controls](#controls)
  - [Progression](#progression)
  - [Gameplay Mechanics](#gameplay-mechanics)
- [Levels and World Design](#levels-and-world-design)
  - [Game World](#game-world)
  - [Objects](#objects)
  - [Physics](#physics)
- [Art and Audio](#art-and-audio)
  - [Art Style](#art-style)
  - [Sound and Music](#sound-and-music)
  - [Assets](#assets)
- [UI](#ui)
  - [UI/UX Flow](#uiux-flow)
  - [In-Game HUD (Heads-Up Display)](#in-game-hud-heads-up-display)
  - [Menu Details](#menu-details)
  - [Interaction & Feedback](#interaction--feedback)
- [Technology and Tools](#technology-and-tools)
- [Team Communication, Timelines and Task Assignment](#team-communication-timelines-and-task-assignment)
- [Possible Challenges](#possible-challenges)

---

## Game Overview

### Core Concept

The player takes on the role of a brave little ant on a quest to reclaim a stolen cookie from a greedy seagull. From the perspective of an insect, everyday environments become giant and dangerous playgrounds. Players must traverse kitchens, streets, and outdoor areas, overcoming hazards and using the unique ability to summon fellow ants to solve puzzles, cross obstacles, and ultimately confront the seagull in a final showdown. The game's title, *Antventure*, is a portmanteau of "Ant" and "Adventure," reflecting the grand journey of our tiny protagonist.

### Related Genre

1.  3D platformer + light puzzle adventure
2.  Inspirations include *Pikmin* (group coordination), *It Takes Two* (creative level design), and *Grounded* (miniature perspective).
3.  Unlike these titles, our game offers a short, focused experience (~10 minutes) built around the summoning mechanic and creative environmental puzzles from a miniature perspective.

### Target Audience

Primary audience is university students and young adults who enjoy fast-paced, light puzzle-solving and 3D platformer games.

### Unique Selling Points (USPs)

-   **Summoning Mechanic:** Collect cookie crumbs to summon different types of ants (workers for carrying, builders for bridges/ladders, soldiers for defense). This introduces strategy and variety within a short playtime.
-   **Creative Environmental Interactions:** Each level features unique obstacles: bubble machines in the kitchen, car traffic and shoe gaps on the street, puddles requiring bridges, and fishing lines or water spouts during the boss fight.
-   **Miniature Melbourne Setting:** The game is built with recognizable Melbourne landmarks (streets, cafés, seaside piers), turning the familiar into adventurous landscapes.

---

## Story and Narrative

### Backstory

In the hidden corners of Melbourne lies a world unnoticed by humans—the kingdom of ants. Within this miniature realm, a single ordinary cookie is like a legendary treasure bestowed by the gods, capable of altering the fate of an entire ant colony.

The story begins on a tranquil morning. The protagonist is a small ant, stubborn and brave. It stumbles upon a fallen cookie on the kitchen counter—a “golden feast” in ant terms. Just as it prepares to feast, a cunning pigeon swoops through the window, snatching the cookie and leaving scattered crumbs behind. To humans, this is an insignificant scene, but in the eyes of the little ant, it is a challenge from fate and a call to adventure.

In the ants' worldview, human kitchens, streets, cafes, and docks are not ordinary spaces, but towering labyrinths and turbulent battlefields. Tableware becomes colossal obstacles, human footsteps on the streets feel like impending doom, and a single drop of coffee could drown an entire squad.

To reclaim its precious cookie, the little ant embarks on a journey of pursuit and resistance. It is not alone: food scraps gathered along the way summon companions. Ants unite to carry burdens, build bridges, and ward off dangers. Through this journey, the protagonist evolves into a leader—no longer merely a food-seeking individual, but a vanguard guiding its colony toward glory.

The ultimate adversary is the tyrannical seagull—a “dragon” in the ant world, symbolizing the oppression and arrogance of the outside world. Only by defeating it can the little ant prove that even the most insignificant life can leave its own mark of victory in the vast world.

### Characters

-   **The Protagonist Ant (Player Character)**
   -   **Role:** The hero controlled by the player, on an adventure to reclaim the cookie.
   -   **Personality & Motivation:** Brave and determined, it fears nothing despite its tiny size. Its core drive is to reclaim the precious cookie stolen by the pigeon, a mission of honor for the entire ant colony.
   -   **Appearance:** A stylized, earth-toned (sandy brown) ant with simple texturing on its body for detail. Features a smooth, six-legged crawling animation, making its movement look natural and insect-like.
   -   **Abilities:** Can run, jump, climb walls, and grab (with the C key) small objects (e.g., sugar cubes). Also possesses the ability to summon specific helper ants.
-   **The Pigeon (Main Antagonist)**
    -   **Role:** The final boss and the greedy thief who stole the cookie.
    -   **Personality & Motivation:** Arrogant and possessive, it views the ant's world as its personal pantry, plundering at will.
    -   **Appearance:** From the ant's perspective, it is a massive and intimidating grey pigeon, with detailed feathers and threatening animations.
    -   **Role in Gameplay:** Appears as the boss in the dock level. Its attack patterns include: a fast pecking motion with its beak, and periodically flapping its wings to create strong gusts of wind that can push the ant back or create obstacles.
-   **Helper Ants (Worker, Builder, etc.)**
    -   **Role:** AI-controlled allies summoned by the protagonist ant, crucial for solving puzzles and overcoming level challenges. 
-   **Insects (Environmental Enemies)**
    -   **Role:** Hostile creatures encountered in the second level (Outdoor), acting as environmental hazards that the player must avoid or confront.
    -   **Scale & Design:** While small to humans, from the ant's perspective these insects are formidable in size and threat. They are designed to appear as large, intimidating adversaries.
    -   **Interaction:** These insects can be defeated by the protagonist's Venom Shot ability. Upon being hit, they are eliminated, clearing the path for the player.

---

## Gameplay and Mechanics

### Player Perspective

The game adopts a dynamic third-person perspective. The player need to control the ant character, and this character is always visible in the center of the screen. The camera system will adjust according to the environment. Provide the standard following view in flat terrain. In special scenarios (such as the vertical wall of a kitchen), the viewing angle will automatically rotate , thereby creating an immersive spatial experience. The character is designed in a polygonal form, which not only retains the basic features of an ant but also avoids any potentially uncomfortable realistic details through cartoonish treatment.

### Controls

-   **Movement control:** WASD keys control the character's movement in all directions.
-   **Jumping action:** Pressing the space will perform a normal jump.
-   **Pick up：** The "C" key is used for grabbing/picking up objects. 
-   **Environmental Interaction:** The "E" key is used for interacting with scene objects and summoning points.
-   **Launch attack:** The "P" key is used for shooting venom.
-   **Viewpoint control:** Adjust the camera direction by moving the mouse.
-   **Special operation:** The "J" key is used to escape from a trapped situation. The "V" key is used for some special gameplay methods.

### Progression

-   **Level structure:**
    -   **Tutorial Level: ~1 min:** Set in a simplified environment to teach the player the core controls: WASD movement, Space for jumping, and C for grabbing objects.
    -   **Teaching level (kitchen area) 1.5-2min:** Introduces environmental hazards and basic puzzles using the learned controls.
    -   **Main level (outdoor mixed environment) 4-6min :** Integrates shrubbery and street elements, and introduces a complete summoning mechanism
    -   **Ultimate Challenge (Dock Boss Battle) 2-4min:** Requires the comprehensive application of all the skills learned so far
-   **Difficulty assessment:**
    -   Gradual introduction of new mechanisms.
    -   Each mechanism offers ample opportunities for practice.
    -   The Boss battle focuses on strategy rather than operational difficulty.
-   **Failure and Renewal:**
    -   **Failure conditions:** Being attacked by enemies (such as being pecked by pigeons) or coming into contact with dangerous environments (such as falling into water)
    -   Using the checkpoint respawn system, after death, one can quickly restart from the most recent node.
    -   Simplify the health system and adopt a one-hit-death mechanism but combine it with quick respawn to maintain the game pace.
-   **Continuous play motivation comes from:**
    -   Collect food scraps to unlock the skin color of the new ant character.
    -   Set several hidden collectibles for each level to encourage exploration.
    -   The time record function for completing the game can be considered to encourage repeated challenges.

### Gameplay Mechanics

-   **Core mechanism:**
    -   **Basic movement system:** Running, jumping, wall-climbing movement
    -   **Environmental Interaction:**
        -   Interact with the preset trigger point
        -   Interacting with dynamic obstacles (such as moving chopsticks, dripping faucets)
    -   **Combat:** The player ant can perform a basic attack to defeat insects and clear a path.
    -   **Team collaboration:** Summon a limited number of ants to assist in completing the mission.
-   **Summoning Skill:**
    -   **Summoning Resources:** Food scraps need to be collected as the summoning energy. Each summoning consumes one scrap.
    -   **Type of helper ants:**
        -   **Wall-Crawlers:** Jet-black ants. They can defy gravity to scurry quickly across specific vertical surfaces (e.g., walls, cabinet sides), used to activate out-of-reach switches or open new paths for the protagonist.
        -   **Soldiers:** Jet-black ants, sturdier than workers. They are the primary units summoned for the final boss fight. Once summoned, Soldiers will automatically lock onto and charge towards the Pigeon boss to attack, providing constant distraction and damage without requiring player manual control.
    -   The summoning point is fixed at a specific location and requires sufficient debris to be gathered before it can be summoned.
-   **Physical system:**
    -   Implementing the standard gravity model and collision detection
    -   Obstacles move along the predetermined path.

---

## Levels and World Design

### Game World

The game is set in a fully 3D environment, but with level design that encourages movement and gameplay primarily on a two-dimensional plane, creating a 2.5D-style experience. Each level follows a linear progression structure, but there are a few branching paths for exploration. There is no mini map, but visual cues about the level layout assist in navigation.

### Objects

-   **player (ant):**
    <p align="center">
      <img src="images/ant_image.png" alt="Player Ant" width="400">
    </p>
-   **The first level (kitchen):**
    <p align="center">
      <img src="images/kitchen.png" alt="Kitchen Level 1" width="400">
    </p>
    
    -   **Checkpoint System:** A checkpoint is activated after overcoming each primary obstacle. Upon death, the player respawns at the most recently activated checkpoint.
    -   **Obstacles & Puzzles:**
        -   **Rolling Cucumber:** A cucumber rolling back and forth on a cutting board, which the player must time their jump to overcome.
        -   **Sugar Cube Puzzle**: The player must grab (C) a sugar cube and carry it to a designated slot. Placing the cube correctly extinguishes the flames on a stovetop, allowing passage. 
        -   **Milk Carton Maze:** A maze constructed from towering milk cartons. The player must navigate through it to find the exit.
    <p align="center">
      <img src="images/maze.png" alt="maze design Level 1" width="400">
    </p>
        -   **Cake Spatula Bridge:** A long cake spatula spans over a sink filled with water. The player must carefully cross this narrow bridge. Falling into the water results in failure.

-   **Second Level (Outdoor Comprehensive):**
    <p align="center">
      <img src="images/outdoor.png" alt="Outdoor Level" width="400">
    </p>
    
    -   **Collection item:** Food scraps
    -   **Summoning Point:** Fixed location, capable of summoning worker ants or assembly ants
    -   **Environmental challenges:**
        -   Small puddle: Requires assembly ants to build a bridge
        -   Mobility obstacle: Insects with simple path movement capabilities
-   **The third level (Boss battle at the dock):**
    <p align="center">
      <img src="images/dock.png" alt="Dock Level" width="400">
    </p>
    
    -   **Boss Attack Mode:**
        -   Wings flap the air: This phenomenon occurs periodically and requires hiding behind a fixed object.
    -   **Resource Management:** During the Boss battle, food crumbs will drop. Need to collect them in time to maintain the summoning ability.
    -   **Interaction mechanism:**
        -   Trap the boss: Press E to let the worker ants to trap.
        -   Water faucet: Worker ant interaction, triggers water spraying animation.

### Physics

-   Basic gravity simulation and collision detection
-   The jumping action is influenced by the gravitational acceleration.
-   The motion of objects follows simplified physical laws.

---

## Art and Audio

### Art Style

-   **Main Art Style:**
    - Stylized cartoon realism with semi-realistic textures to highlight the miniature perspective of ants in a human-scale world.
-   **Game outlook:**
    - 3D side-scroller platformer rendered with 3D assets; environments and hazards appear oversized compared to the ant protagonist, emphasizing scale contrast.
-   **Art**
    -   **Color**
        - **Kitchen**: warm tones (wood, metal gray, yellow lighting)
        - **Bushes**: lush greens, earthy browns, soft sunlight filtering
        - **Road**: dark asphalt gray with white/yellow lines, muted city colors
        - **Dock**: cool grays and blues, wooden planks, reflective water surfaces
    -   **Shape**
        - Rounded and exaggerated for safe/interactive objects; jagged and sharp for hazards like knives, claws, and debris.
    -   **Texture**
        - Hand-painted textures for food scraps and natural elements; semi-realistic surfaces for metal, stone, and water.
-   **Aesthetic:**
    - Each stage contrasts domestic, natural, and urban-industrial environments. The progression blends playful exploration with increasing danger, culminating in large-scale boss encounters.
-   **Concept art:**
    - Ant protagonist with customizable skins, oversized kitchen props as platforms, sewer dripping pipes, rooftop pigeon boss scene.
-   **Similar art styles:**
    - Grounded, Hollow Knight, Little Nightmares.

### Sound and Music

-   **Sound Design:**
    - Dynamic ambient layers (kitchen clinks, sewer drips, rooftop wind) combined with responsive character and hazard sounds.
-   **Basic sound effects:**
    - Footsteps, jumps, collisions, knife slashes, dripping water, bubble pops, insect buzzes, pigeon screeches.
-   **Fitness:**
    - SFX provide clear cues for danger (e.g., knife swing “whoosh”), reward feedback for collection, and tactile reinforcement for platforming.
-   **Music used:**
    - **Kitchen**: playful orchestral with plucked strings
    - **Bushes**: light woodwinds and ambient forest soundscape
    - **Road**: rhythmic percussion with low urban ambience, tense buildup
    - **Dock/Boss**: dramatic orchestral with heavy percussion and tension-filled crescendos
-   **Fitness:**
    - Adaptive music system that changes with player state (exploration, hazard, boss fight) to maintain immersion and tension.

### Assets

-   **Artistic assets:**
    - Ant character models (base + skins)
    - Kitchen props (knives, spoons, pots, bubbles)
    - Bush assets (leaves, branches, insect NPCs)
    - Road assets (cars, streetlights, traffic signs, trash)
    - Dock assets (wooden planks, crates, water surface, pigeon boss model)
    - Enemies (bees, rats, pigeon boss)
    - Collectibles (food crumbs, summon shards, glowing tokens)
    - VFX (bubble teleport, splash, wind gusts, feather scatter)
-   **Create source:**
    - Blender/Maya (3D models)
    - Substance Painter/Photoshop (textures)
    - Unity Particle System (VFX)
    - GrageBand
    - Musescore
-   **List of candidate assets**
    - Kitchen tableware, bush foliage, street props, dockside cranes, insect enemies, pigeon animations, hazard effects.


---

## UI

-   **UI tool：** Figma, Photoshop
-   **UI resource:**
    -   **icon:** [Vector Icons and Stickers - PNG, SVG, EPS, PSD and CSS](https://www.flaticon.com/free-icons/ant)
-   **UI Mockups:**
    <p align="center">
      <img src="images/interface.png" alt="UI Interface" width="500">
    </p>
    <p align="center">
      <img src="images/component.png" alt="UI Components" width="500">
    </p>

### UI/UX Flow
A visual or descriptive flowchart of the user's navigation through the game's interfaces.
*(A visual flowchart will be added here.)*
*   **Example Flow:** `Main Menu` -> `(Level Select)` -> `Loading Screen` -> `In-Game HUD` -> `Pause Menu` -> `Level Complete/Fail Screen` -> `Return to Main Menu`

### In-Game HUD (Heads-Up Display)
Core information displayed on-screen during gameplay.
*(A visual mockup of the HUD will be added here.)*
*   **Cookie Crumb Counter:** Clearly displays the quantity of the summoning resource.
*   **Summoned/Available Ants:** Shows the types and number of ants currently available.
*   **Ability Cooldowns:** Visual indicator for skills like Sprint.
*   **Interaction Prompts:** Contextual prompts like "Press E to Interact" when near objects.
*   **Objective Reminder:** A simple text reminder of the current goal (e.g., "Reclaim the cookie!").

### Menu Details
Detailed breakdown of what each menu contains.
*(Visual mockups for these menus will be added as they are designed.)*
*   **Pause Menu:**
    *   `Resume`
    *   `Restart Level`
    *   `Options`
    *   `Back to Main Menu`
*   **Options Menu:**
    *   `Audio Settings`: Sliders/toggles for master, music, and SFX volume.
    *   `Graphics Settings`: Options for quality (Low, Medium, High).
    *   `Controls`: Display of keybindings.
*   **Level Complete Screen:**
    *   `Time Taken`
    *   `Collectibles Found` (e.g., Total cookie crumbs)
    *   Buttons for `Next Level` or `Replay`.

### Interaction & Feedback
How UI elements respond to user input.
*(Examples of these states will be visualized later.)*
*   **Button States:** Visual changes for `Hover`, `Clicked`, and `Disabled` states.
*   **Feedback on Collection:** A brief animation or sound effect when picking up cookie crumbs.
*   **Summoning Feedback:** Visual and audio cues to confirm an ant has been successfully summoned.

---

## Technology and Tools

- **Game Engine**: Unity (C# scripting, 3D platforming workflow)
- **Physics & Animation**: Unity Physics3D, Mecanim Animator, Particle System
- **Art Tools**: Blender, Maya, Substance Painter, Photoshop, Illustrator
- **Audio Tools**: MuseScore, GrageBand
- **Collaboration Tools**: GitHub (version control), Monday (management), Slack/WeChat (team communication)
- **Extra Middleware**: FMOD or Wwise for adaptive sound; ProBuilder for rapid prototyping


---

## Team Communication, Timelines and Task Assignment

We primarily use WeChat for daily communication and quick updates, while Slack is used for structured discussions and file sharing. For task allocation and timeline tracking, we rely on Monday.com to assign responsibilities, set deadlines, and monitor progress.

-   **Communication Tool:**
    -   Wechat
    -   Slack
-   **Timeline Management:** [https://student493091.monday.com/boards/2072589284](https://student493091.monday.com/boards/2072589284)

---

## Possible Challenges

-   It's not easy to find a suitable model.

-   Time limit: too many idea need to complete




