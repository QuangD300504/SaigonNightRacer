# 🏍️ Saigon Night Racer

A pixel-art endless motorcycle racing game set in the neon-lit streets of Saigon. Race through procedurally generated terrain, avoid obstacles, collect power-ups, and survive as long as possible in this challenging arcade-style game.

## 🎮 How to Play

### Basic Controls
- **Arrow Keys / WASD**: Move left/right and accelerate/decelerate
- **Spacebar**: Jump
- **Shift**: Boost (temporary speed increase)
- **ESC**: Pause game / Open settings menu

### Objective
Survive as long as possible by avoiding obstacles while collecting points and power-ups. Your score increases based on distance traveled, obstacles avoided, and collectibles gathered.

## 🏗️ Game Systems

### 🛣️ Infinite Terrain Generation
The game features a sophisticated procedural terrain system that creates endless, varied landscapes:

- **Perlin Noise Generation**: Uses Perlin noise algorithms to create natural-looking hills, valleys, and slopes
- **Seamless Chunking**: Terrain is generated in chunks that seamlessly connect to prevent visual gaps
- **Dynamic Recycling**: Old terrain chunks are recycled and repositioned ahead of the player
- **Slope Physics**: The bike responds realistically to uphill and downhill terrain
  - **Uphill**: Reduces speed (up to 30% slower)
  - **Downhill**: Increases speed (up to 50% faster)
- **Road Overlay**: A road surface is generated on top of the terrain for visual consistency

### 🏍️ Bike Physics & Controls
Realistic motorcycle physics with arcade-style gameplay:

- **Speed System**: 
  - Base speed: 0 km/h (stationary)
  - Max speed: 45 km/h (can be increased by difficulty phases)
  - Speed display in real-time (km/h)
- **Acceleration/Deceleration**: Smooth speed changes with realistic physics
- **Jumping**: Spacebar to jump over obstacles
- **Boost System**: 
  - Temporary speed increase (1.8x multiplier)
  - Duration: 1.5 seconds
  - Cooldown: 2 seconds
  - Visual flame effects
- **Wheelie Control**: Anti-wheelie system prevents excessive front wheel lifting
- **Slope Adaptation**: Bike automatically adjusts to terrain slopes

### 🚧 Obstacle System
Three types of obstacles with unique behaviors:

#### 🚧 Traffic Cone
- **Type**: Static obstacle
- **Behavior**: Immediate collision detection
- **Effect**: 1 damage, visual flash effect
- **Sound**: Cone collision sound
- **Avoidance Points**: 5 points

#### 🚗 Car
- **Type**: Dynamic obstacle
- **Behavior**: 
  - Spawns above road and falls down
  - Switches to kinematic movement after landing
  - Moves toward player when within activation distance (5 units)
  - Follows road surface and rotates with slopes
- **Effect**: 1 damage
- **Sound**: Car collision sound
- **Avoidance Points**: 5 points

#### ☄️ Meteorite
- **Type**: Dynamic obstacle with smart targeting
- **Behavior**:
  - Falls from sky with physics
  - Smart targeting system predicts player movement
  - Adjusts trajectory based on player speed and direction
  - Creates dynamic shadows on ground
  - Rotates based on movement direction
- **Effect**: 1 damage
- **Sound**: Meteorite collision sound
- **Avoidance Points**: 5 points
- **Visual Effects**: Impact VFX, ground shadows, dynamic rotation

### 🍎 Collectible System
Two categories of collectibles with different effects:

#### 📊 Points Collectibles
- **🍎 Apple**: Medium score bonus (50 points)
- **💎 Diamond**: High score bonus (100 points)

#### ⚡ Buff Collectibles
- **❤️ Health**: Restores 1 life
- **🛡️ Shield**: Temporary invincibility (3 seconds)
- **⚡ Speed Boost**: Temporary speed increase (1.5x multiplier, 3 seconds)

### 📈 Progressive Difficulty System
The game features a sophisticated difficulty progression system that scales multiple game elements:

#### Phase Progression
- **Trigger**: Every 4,000 score points
- **Progression Type**: Score-based (can be switched to distance-based)

#### Speed Scaling
- **Player Speed**: Increases by 0.3x per phase
- **Max Speed Multiplier**: Up to 4.0x maximum
- **Obstacle Speed**: Increases by 0.2x per phase
- **Max Obstacle Speed**: Up to 2.5x maximum

#### Obstacle Spawning
- **Spawn Rate**: Increases by 0.1x per phase
- **Base Interval**: 3.0 seconds
- **Minimum Interval**: 0.8 seconds
- **Interval Decrease**: 0.998x per spawn

#### Obstacle Probabilities (Change per phase)
- **Traffic Cone**: 40% → 20% (decreases)
- **Car**: 30% → 40% (increases)
- **Meteorite**: 30% → 40% (increases)

#### Collectible Scaling
- **Score Multiplier**: Increases per phase
- **Buff Duration**: Increases per phase
- **Visual Effect Intensity**: Increases per phase
- **Obstacle Scale**: Increases per phase

#### Boost System Scaling
- **Boost Power**: Increases per phase
- **Boost Duration**: Increases per phase
- **Boost Cooldown**: Decreases per phase

### 🎵 Audio System
Comprehensive audio system with multiple sound categories:

#### 🎵 Music
- **Background Music**: Continuous atmospheric music
- **Volume Control**: Adjustable music volume

#### 🔊 Sound Effects
- **Engine Sounds**:
  - Idle engine sound (when stationary)
  - Rev engine sound (when moving)
  - Smooth transitions between idle and rev
- **Player Actions**:
  - Jump landing sound
  - Boost activation sound
- **Collectibles**:
  - Point collect sound (Apple, Diamond)
  - Buff collect sound (Health, Shield, Speed Boost)
- **Collisions**:
  - Car collision sound
  - Cone collision sound
  - Meteorite collision sound
  - Death sound
- **Game State**:
  - Game over sound
- **UI Sounds**:
  - Button click sound
  - Button hover sound
  - Menu open/close sounds

#### 🔊 Audio Features
- **Volume Control**: Separate music and SFX volume sliders
- **Persistent Audio**: AudioManager persists across scenes
- **Smooth Transitions**: Engine sounds transition smoothly based on speed
- **Quieter Effects**: Collectible and collision sounds are 40% quieter for better balance

### 🎨 Visual Effects & UI
- **Pixel Art Style**: Consistent 16-bit pixel art aesthetic
- **HUD Elements**:
  - Speed display (km/h) with color-coded feedback
  - Score and high score with dynamic colors
  - Health hearts with clear active/inactive states
  - Buff status with unique colors and flashing animation
- **Visual Feedback**:
  - Color-coded speed values (white → cyan → yellow → red)
  - Flashing buff indicators
  - Impact VFX for collisions
  - Collection effects for items
- **Terrain Effects**:
  - Dynamic shadows for meteorites
  - Road surface overlay
  - Slope-based visual adjustments

## 🎯 Scoring System
- **Distance Traveled**: Continuous score increase based on distance
- **Obstacle Avoidance**: 5 points per obstacle avoided
- **Collectibles**:
  - Apple: 50 points
  - Diamond: 100 points
- **Difficulty Multipliers**: Score values increase with difficulty phases
- **High Score**: Persistent high score tracking

## 🎮 Cheat Codes
Debug features for testing and development:

### 🛡️ Invincible Mode (I Key)
- **Effect**: Player becomes invincible to all obstacles
- **Behavior**: 
  - No damage taken from obstacles
  - No collision sounds or VFX
  - Obstacles still get destroyed when touched
  - Player can pass through obstacles without effects
- **Visual**: "INVINCIBLE MODE" appears in HUD with flashing animation

### ❤️ Direct Health Reduction (H Key)
- **Effect**: Directly reduces player health by 1
- **Behavior**: Bypasses all protection (shield, invincibility frames)
- **Use**: For testing game over conditions

## 🏆 Game Progression
- **Lives System**: Start with 5 lives
- **Health Restoration**: Collect health items to restore lives
- **Difficulty Scaling**: Game becomes progressively more challenging
- **Endless Gameplay**: Continue until all lives are lost
- **High Score Challenge**: Beat your previous best score

## 🔧 Development Notes
- **Modular Design**: Each system is independently designed and can be modified
- **Extensible**: Easy to add new obstacles, collectibles, or difficulty features
- **Performance Optimized**: Efficient object recycling and memory management
- **Debug Friendly**: Comprehensive logging and cheat systems for testing

---

**Enjoy racing through the neon-lit streets of Saigon!** 🏍️✨
