# Implementation Plan: Stylized Stickman Party Character Rigs & Arena

**Feature ID**: `001-character-visuals`  
**Ref Spec**: [spec.md](./spec.md)

---

## 1. Technical Architecture & File Layout

```text
Assets/_Game/
├── Materials/
│   ├── Characters/
│   │   ├── Mat_Mother.mat
│   │   ├── Mat_Child.mat
│   │   ├── Mat_ConUt.mat
│   │   ├── Mat_Hunter.mat
│   │   ├── Mat_NonLa.mat
│   │   ├── Mat_Skin.mat
│   │   ├── Mat_EyeWhite.mat
│   │   ├── Mat_EyeBlack.mat
│   │   ├── Mat_Cheek.mat
│   │   └── Mat_Crown.mat
│   └── Environment/
│       ├── Mat_Grass.mat
│       └── Mat_Border.mat
├── Prefabs/
│   ├── Characters/
│   │   ├── Mother.prefab
│   │   ├── Child.prefab
│   │   └── Hunter.prefab
│   └── Environment/
│       └── Arena.prefab
└── Scenes/
    └── Character_Showcase.unity
```

---

## 2. Implementation Steps

1. **Materials Generation**: Ensure all 10 stylized URP Lit materials are compiled and verified.
2. **Mother Prefab Generation**: Build multi-part stylized mesh hierarchy (Torso, Head, Cartoon Eyes, Rosy Cheeks, Nón Lá, Curved Protective Arms, Feet, CharacterController).
3. **Child Prefab Generation**: Build chibi hierarchy (Head, Eyes, Cheeks, Chubby Torso, Feet, Golden Crown toggle).
4. **Hunter Prefab Generation**: Build craft healer hierarchy (Robe Torso, Head, Slanted Eyes, Nón Lá, Extended Grab Hand, Feet, CharacterController).
5. **Arena Prefab Generation**: Build circular flat grass ground with `MeshCollider` (flat top at Y=0) and 24-segment boundary fence with 3.5m tall collision barriers.
6. **Showcase Scene Setup**: Instantiate Arena, Mother at `(0, 0, 0)`, 3 Children in queue `(0, 0, -1.3..-3.9)`, Hunter at `(0, 0, 7.5)`, with URP Directional Light and Isometric Camera.

---

## 3. Verification Protocol (MANDATORY)
- Call `unity_get_console_logs` -> Verify 0 Errors, 0 Warnings.
- Call `test_status` -> Verify Mother Y = 0.0, Hunter Y = 0.0, Children Y = 0.5.
