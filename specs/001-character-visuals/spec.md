# Feature Specification: Stylized Stickman Party Character Rigs & Arena Environment

**Feature ID**: `001-character-visuals`  
**Status**: In Progress  
**Target Platform**: Unity 6 (URP, Mobile/PC)

---

## 1. Problem Statement & User Need
Previous character prototypes were raw programmer primitives (single capsule, cylinders stuck through chests, isolated spheres) that looked amateurish, lacked identity, and caused severe physics glitches (e.g. cylinder capsule-colliders launching characters into orbit at Y=15.8m).
This feature establishes proper, high-quality Stylized Stickman Party character rigs with Vietnamese cultural cues (Nón Lá, Chibi proportions, Con Út crown) and a rock-solid flat circular Arena with airtight collision barriers.

---

## 2. Character Specifications

### 2.1 Rắn Mẹ (Mother Character Rig)
- **Art Style**: Chibi / Stickman Party (round, friendly, vibrant).
- **Proportions**:
  - Ground Anchor: Base at `Y = 0.0m`.
  - Torso: Rounded capsule, Emerald Green (`Mat_Mother`).
  - Head: Large Chibi sphere, Skin tone (`Mat_Skin`), positioned at `Y = 1.42m`.
  - Face: 2 large cartoon expressive eyes (White sclera + Black pupils looking forward) + rosy cheeks.
  - Hat: Traditional Vietnamese Nón Lá (layered conical bamboo hat) tilted slightly on head.
  - Arms: 2 curved outstretched protective arms spanning exactly 2.8m across the front to shield children.
  - Feet: 2 cute rounded cartoon feet on the ground.
  - Defense Aura: Golden semi-transparent ground disk indicating lock-facing stance.
- **Physics**:
  - `CharacterController`: height 1.7m, radius 0.5m, center (0, 0.85, 0).
  - Strictly grounded at `Y = 0`.

### 2.2 Đàn Con (Child Character Rigs)
- **Proportions**:
  - Chibi ratio 1:1 (large round head relative to body).
  - Head: Sphere, Skin tone, 2 big cartoon eyes, rosy cheeks.
  - Torso: Chubby rounded body.
  - Normal Children (Con 1 & Con 2): Warm Orange/Yellow (`Mat_Child`).
  - Con Út (Tail Child): Pastel Pink (`Mat_ConUt`) + Golden 3-point Crown (`Crown`).
- **Physics**:
  - `SphereCollider` (isTrigger = true, radius 0.45m) at `Y = 0.45m`.

### 2.3 Thầy Thuốc (Hunter Character Rig)
- **Proportions**:
  - Torso: Dark purple traditional robe (`Mat_Hunter`).
  - Head: Chibi head with crafty, slanted cartoon eyes.
  - Hat: Authentic Vietnamese Nón Lá (conical bamboo hat).
  - Hands: Left hand resting, Right hand extended forward ready for active grab.
  - VFX: Golden arc swipe wave attached to right hand (active during grab).
- **Physics**:
  - `CharacterController`: height 1.7m, radius 0.5m, center (0, 0.85, 0).

### 2.4 Đấu Trường (Circular Arena Environment)
- **Ground**:
  - Diameter 32m (Radius 16m).
  - Top surface strictly flat at `Y = 0.0m` with `MeshCollider` (NO CapsuleCollider).
  - Material: Vibrant cartoon grass (`Mat_Grass`).
- **Boundary Fence**:
  - Radius 15.0m.
  - 24 wooden posts & cross rails (`Mat_Border`).
  - Airtight 3.5m tall collision barrier surrounding the perimeter to physically prevent any character from ever falling or being shoved outside.

---

## 3. Acceptance Criteria
1. **Visual Fidelity**: Characters look like cute Stylized Stickman Party models with eyes, facial expressions, Nón Lá, and Con Út's crown.
2. **Grounding**: Mother, Hunter, and Arena floor are all situated at `Y = 0.0m`. Neither Mother nor Hunter floats in the air.
3. **Collision Integrity**: Arena bounds strictly confine all characters inside radius 15.0m.
4. **Zero Errors**: Unity Console reports 0 Errors and 0 Warnings upon building and loading the showcase scene.
