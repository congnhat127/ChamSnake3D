---
description: Core engineering principles, evidence-based development, and Unity best practices for RongRanChamCham3D
globs: ["**/*.cs", "**/*.unity", "**/*.prefab", "**/*.asset"]
---

# RongRanChamCham3D Engineering Standards

1. **Evidence-Based (No Magic Numbers):**
   - Arm span and hitboxes MUST be derived from model rig bones (`Hand_L` to `Hand_R`), not arbitrary numbers.
   - Child follow distances MUST be derived from actual Collider extents/radius.
   - Gameplay balance values belong in `ScriptableObject` (`GameBalanceConfig`), never hardcoded in scripts.

2. **Leverage Unity Standards:**
   - Use `NavMeshAgent` and `NavMeshObstacle` for AI.
   - Use `Cinemachine` (CinemachineTargetGroup) for cameras.
   - Use Unity New Input System (`OnScreenStick`, `OnScreenButton`) for touch controls.
   - Use Event-Driven architecture (C# events/actions) for decoupled communication.

3. **Prefab-First (No Primitive Geometry in Code):**
   - Never use `GameObject.CreatePrimitive` to construct characters, arms, or accessories in code.
   - Use complete Prefabs with sockets attached to skeletal bones.

4. **Zero GC Allocation in Update Loops:**
   - No LINQ, no `new` allocations inside `Update()` / `FixedUpdate()`. Target 60 FPS mobile.
