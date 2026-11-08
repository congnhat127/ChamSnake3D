# Rồng Rắn Chăm Chăm 3D - Engineering Guidelines for AI Agents

Every AI agent working on this codebase MUST read and strictly adhere to the following principles. Any violation will be rejected.

---

## 1. Evidence-Based & Anti-Arbitrary Rationale (NO MAGIC NUMBERS)
- **Do not invent arbitrary numbers in code** (e.g. hardcoded `2.8f` arm span, `1.3f` follow distance).
- **Arm span / Barrier width:** MUST be derived from the character's skeletal rig (distance between `Hand_L` and `Hand_R` bones) or model bounds.
- **Child spacing:** MUST be derived from actual Collider bounds (`SphereCollider.radius` / `Collider.bounds.extents`) plus safety padding, or maintained via Physics Joints.
- **Grab range:** MUST match the actual visual reach of the attack animation swing.
- **Game balance parameters:** MUST be placed in `ScriptableObject` (`GameBalanceConfig.asset`), NEVER hardcoded in C# classes.
- **Scene Gizmos:** Use `OnDrawGizmosSelected()` so designers can visually see and tweak zones directly in the Unity Scene view.

---

## 2. Leverage Industry Standards (DO NOT REINVENT THE WHEEL)
Always use proven, official Unity packages and industry-standard solutions:
- **Pathfinding & AI:** Use **Unity NavMesh** (`NavMeshAgent` + dynamic `NavMeshObstacle` carving) instead of custom manual trigonometry / raycast steering.
- **Camera:** Use **Cinemachine 3.x** (`CinemachineCamera` + `CinemachineTargetGroup` + Damping) instead of custom manual lerping scripts.
- **Mobile Input:** Use **Unity New Input System** (`com.unity.inputsystem` with `OnScreenStick` & `OnScreenButton`) instead of raw pixel math.
- **Tweening & Polish:** Use **PrimeTween** / **DOTween** instead of custom coroutines with `Time.deltaTime`.
- **Decoupled Events:** Use C# `Action` / Observer pattern so systems remain completely decoupled (e.g. `OnConUtCaptured` event).

---

## 3. Prefab-First & Modular Architecture (NO PROCEDURAL PROGRAMMER PRIMITIVES)
- **All entities (Mother, Children, Hunter, Arena, Food Items) MUST be complete Prefabs** in `Assets/_Game/Prefabs/`.
- **STRICTLY FORBIDDEN:** Generating procedural primitive geometry (`GameObject.CreatePrimitive` cubes, cylinders, capsules) via C# code.
- **Accessories & Attachments:** Accessories (Nón Lá, Crown, Bow) must be 3D models (`.fbx`) attached to bone **Sockets** (`Socket_Head`, `Socket_Hand_R`) in Prefab mode, NOT generated procedurally.

---

## 4. Separation of Concerns (SRP)
- **Visual:** Model 3D + MeshRenderer + Animator + Sockets. Zero gameplay logic.
- **Motor:** `CharacterController` / `NavMeshAgent`. Translates velocity into physics-grounded movement.
- **Brain:** Player Input Controller or AI State Machine. Decisions only.
- **Game Manager:** High-level match states (`Ready`, `Chasing`, `Duel`, `Victory`, `GameOver`). No direct transform manipulation.

---

## 5. Verification Before Handoff
Never tell the user "ready to test" without programmatic verification:
1. Unity Console must be 100% clean (0 errors, 0 exceptions).
2. Check `Logs/Editor.log`.
3. Check MCP Bridge status (`unity_get_console_logs`, `unity_ping`).

---

## 6. Performance & Clean Unity 6 Code
- Code must reside inside namespace `RongRanChamCham` and assembly `RongRanChamCham.Runtime.asmdef`.
- **Zero GC Allocation in hot loops (`Update`, `FixedUpdate`):** No LINQ, no allocating new arrays/lists per frame. Target 60 FPS on mobile.

