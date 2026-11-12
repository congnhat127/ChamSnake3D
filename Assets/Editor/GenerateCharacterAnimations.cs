#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace RongRanChamCham.Editor
{
    public static class GenerateCharacterAnimations
    {
        private const string AnimDir = "Assets/_Game/Animations/Characters";
        private const string PrefabsDir = "Assets/_Game/Prefabs/Characters";

        [InitializeOnLoadMethod]
        private static void AutoInit()
        {
            string marker = "Temp/AnimGen_AutoRun.tmp";
            if (!File.Exists(marker))
            {
                File.WriteAllText(marker, "done");
                EditorApplication.delayCall += () => Execute();
            }
        }

        [MenuItem("Tools/Rong Ran/Generate Full Native 3D Animation Suite")]
        public static void Execute()
        {
            Debug.Log("[AnimationGenerator] Bắt đầu khởi tạo trọn bộ Animation Clips (.anim) và Animator Controllers...");

            if (!Directory.Exists(AnimDir))
            {
                Directory.CreateDirectory(AnimDir);
            }

            // =========================================================
            // 1. GENERATE MOTHER ANIMATION CLIPS
            // =========================================================
            var clipMotherIdle = CreateMotherIdle();
            var clipMotherRun = CreateMotherRun();
            var clipMotherGuard = CreateMotherGuard();
            var clipMotherVictory = CreateMotherVictory();

            // =========================================================
            // 2. GENERATE CHILD ANIMATION CLIPS
            // =========================================================
            var clipChildIdle = CreateChildIdle();
            var clipChildRun = CreateChildRun();
            var clipChildVictory = CreateChildVictory();
            var clipChildDodgeLeft = CreateChildDodgeLeft();
            var clipChildDodgeRight = CreateChildDodgeRight();
            var clipChildDuck = CreateChildDuck();
            var clipChildLookUp = CreateChildLookUp();

            // =========================================================
            // 3. GENERATE HUNTER ANIMATION CLIPS
            // =========================================================
            var clipHunterStalk = CreateHunterStalk();
            var clipHunterRun = CreateHunterRun();
            var clipHunterLunge = CreateHunterLunge();
            var clipHunterAngry = CreateHunterAngry();
            var clipHunterPointL = CreateHunterPointLeft();
            var clipHunterPointC = CreateHunterPointCenter();
            var clipHunterPointR = CreateHunterPointRight();
            var clipHunterPointD = CreateHunterPointDown();

            AssetDatabase.SaveAssets();

            // =========================================================
            // 4. BUILD ANIMATOR CONTROLLERS & STATE MACHINES
            // =========================================================
            BuildMotherController(clipMotherIdle, clipMotherRun, clipMotherGuard, clipMotherVictory);
            BuildChildController(clipChildIdle, clipChildRun, clipChildVictory, clipChildDodgeLeft, clipChildDodgeRight, clipChildDuck, clipChildLookUp);
            BuildHunterController(clipHunterStalk, clipHunterRun, clipHunterLunge, clipHunterAngry, clipHunterPointL, clipHunterPointC, clipHunterPointR, clipHunterPointD);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // =========================================================
            // 5. ATTACH ANIMATORS TO PREFABS
            // =========================================================
            AttachAnimatorToPrefab($"{PrefabsDir}/Mother.prefab", $"{AnimDir}/Ctrl_Mother.controller");
            AttachAnimatorToPrefab($"{PrefabsDir}/Child.prefab", $"{AnimDir}/Ctrl_Child.controller");
            AttachAnimatorToPrefab($"{PrefabsDir}/Hunter.prefab", $"{AnimDir}/Ctrl_Hunter.controller");

            AssetDatabase.SaveAssets();
            Debug.Log("[AnimationGenerator] HOÀN TẤT 100%! Trọn bộ 14 animation clips và 3 máy trạng thái Animator đã sẵn sàng hoạt động!");
        }

        // ==============================================================================
        // HELPER: Float Curve Builder
        // ==============================================================================
        private static void SetCurve(AnimationClip clip, string relativePath, Type type, string propertyName, AnimationCurve curve)
        {
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(relativePath, type, propertyName), curve);
        }

        private static AnimationCurve SineCurve(float duration, float minVal, float maxVal, float freq = 1f, float phase = 0f)
        {
            int keyCount = 25;
            Keyframe[] keys = new Keyframe[keyCount];
            float mid = (minVal + maxVal) * 0.5f;
            float amp = (maxVal - minVal) * 0.5f;

            for (int i = 0; i < keyCount; i++)
            {
                float t = (float)i / (keyCount - 1) * duration;
                float val = mid + amp * Mathf.Sin((t / duration) * Mathf.PI * 2f * freq + phase);
                keys[i] = new Keyframe(t, val);
            }
            var curve = new AnimationCurve(keys);
            for (int i = 0; i < keyCount; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
            }
            return curve;
        }

        // ==============================================================================
        // 1. MOTHER CLIPS
        // ==============================================================================
        private static AnimationClip CreateMotherIdle()
        {
            var clip = new AnimationClip { name = "Mother_Idle" };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            float dur = 2.0f;
            // Gentle breathing bobbing
            SetCurve(clip, "Model_Body", typeof(Transform), "m_LocalPosition.y", SineCurve(dur, 0.0f, 0.035f, 1f));
            SetCurve(clip, "Socket_Head", typeof(Transform), "m_LocalPosition.y", SineCurve(dur, 1.34f, 1.36f, 1f));
            SetCurve(clip, "Socket_Head", typeof(Transform), "localEulerAnglesRaw.x", SineCurve(dur, -1.5f, 2.0f, 1f));

            // Arms resting comfortably
            SetCurve(clip, "Socket_Hand_L", typeof(Transform), "m_LocalPosition.x", SineCurve(dur, -1.30f, -1.33f, 1f));
            SetCurve(clip, "Socket_Hand_R", typeof(Transform), "m_LocalPosition.x", SineCurve(dur, 1.30f, 1.33f, 1f));

            SaveClip(clip, "Mother_Idle.anim");
            return clip;
        }

        private static AnimationClip CreateMotherRun()
        {
            var clip = new AnimationClip { name = "Mother_Run" };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            float dur = 0.65f;
            // Forward tilt & bounce
            SetCurve(clip, "Model_Body", typeof(Transform), "m_LocalPosition.y", SineCurve(dur, 0.0f, 0.065f, 2f));
            SetCurve(clip, "Model_Body", typeof(Transform), "localEulerAnglesRaw.x", SineCurve(dur, 6f, 9f, 2f));
            SetCurve(clip, "Model_Body", typeof(Transform), "localEulerAnglesRaw.z", SineCurve(dur, -4f, 4f, 1f));

            // Feet stride
            SetCurve(clip, "Socket_Foot_L", typeof(Transform), "m_LocalPosition.z", SineCurve(dur, -0.15f, 0.18f, 1f, 0f));
            SetCurve(clip, "Socket_Foot_R", typeof(Transform), "m_LocalPosition.z", SineCurve(dur, -0.15f, 0.18f, 1f, Mathf.PI));
            SetCurve(clip, "Socket_Foot_L", typeof(Transform), "m_LocalPosition.y", SineCurve(dur, 0.07f, 0.18f, 1f, 0f));
            SetCurve(clip, "Socket_Foot_R", typeof(Transform), "m_LocalPosition.y", SineCurve(dur, 0.07f, 0.18f, 1f, Mathf.PI));

            SaveClip(clip, "Mother_Run.anim");
            return clip;
        }

        private static AnimationClip CreateMotherGuard()
        {
            var clip = new AnimationClip { name = "Mother_Guard" };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            float dur = 1.0f;
            // Solid lowered defensive stance
            SetCurve(clip, "Model_Body", typeof(Transform), "m_LocalPosition.y", SineCurve(dur, -0.06f, -0.04f, 1f));
            SetCurve(clip, "Model_Body", typeof(Transform), "localEulerAnglesRaw.x", SineCurve(dur, 5f, 8f, 1f));

            // Arms spread WIDE to maximum barrier extent (-1.45m to +1.45m)
            SetCurve(clip, "Socket_Hand_L", typeof(Transform), "m_LocalPosition.x", SineCurve(dur, -1.45f, -1.42f, 1f));
            SetCurve(clip, "Socket_Hand_R", typeof(Transform), "m_LocalPosition.x", SineCurve(dur, 1.42f, 1.45f, 1f));
            SetCurve(clip, "Socket_Hand_L", typeof(Transform), "m_LocalPosition.y", SineCurve(dur, 0.85f, 0.88f, 1f));
            SetCurve(clip, "Socket_Hand_R", typeof(Transform), "m_LocalPosition.y", SineCurve(dur, 0.85f, 0.88f, 1f));

            SaveClip(clip, "Mother_Guard.anim");
            return clip;
        }

        private static AnimationClip CreateMotherVictory()
        {
            var clip = new AnimationClip { name = "Mother_Victory" };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            float dur = 0.85f;
            // High celebratory hop
            Keyframe[] hopKeys = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.42f, 0.40f),
                new Keyframe(0.85f, 0f)
            };
            SetCurve(clip, "Model_Body", typeof(Transform), "m_LocalPosition.y", new AnimationCurve(hopKeys));
            SetCurve(clip, "Model_Body", typeof(Transform), "localEulerAnglesRaw.z", SineCurve(dur, -10f, 10f, 2f));

            // Arms raised high waving
            SetCurve(clip, "Socket_Hand_L", typeof(Transform), "m_LocalPosition.y", SineCurve(dur, 1.40f, 1.55f, 2f));
            SetCurve(clip, "Socket_Hand_R", typeof(Transform), "m_LocalPosition.y", SineCurve(dur, 1.40f, 1.55f, 2f));
            SetCurve(clip, "Socket_Hand_L", typeof(Transform), "localEulerAnglesRaw.z", SineCurve(dur, 25f, 45f, 2f));
            SetCurve(clip, "Socket_Hand_R", typeof(Transform), "localEulerAnglesRaw.z", SineCurve(dur, -45f, -25f, 2f));

            SaveClip(clip, "Mother_Victory.anim");
            return clip;
        }

        // ==============================================================================
        // 2. CHILD CLIPS
        // ==============================================================================
        private static AnimationClip CreateChildIdle()
        {
            var clip = new AnimationClip { name = "Child_Idle" };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            float dur = 1.4f;
            SetCurve(clip, "Model_Body", typeof(Transform), "m_LocalPosition.y", SineCurve(dur, 0.0f, 0.025f, 1f));
            SetCurve(clip, "Socket_Head", typeof(Transform), "localEulerAnglesRaw.x", SineCurve(dur, -2f, 3f, 1f));

            SaveClip(clip, "Child_Idle.anim");
            return clip;
        }

        private static AnimationClip CreateChildRun()
        {
            var clip = new AnimationClip { name = "Child_Run" };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            float dur = 0.50f;
            SetCurve(clip, "Model_Body", typeof(Transform), "m_LocalPosition.y", SineCurve(dur, 0.0f, 0.045f, 2f));
            SetCurve(clip, "Model_Body", typeof(Transform), "localEulerAnglesRaw.x", SineCurve(dur, 8f, 12f, 1f));
            SetCurve(clip, "Model_Body", typeof(Transform), "localEulerAnglesRaw.z", SineCurve(dur, -5f, 5f, 1f));

            SaveClip(clip, "Child_Run.anim");
            return clip;
        }

        private static AnimationClip CreateChildVictory()
        {
            var clip = new AnimationClip { name = "Child_Victory" };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            float dur = 0.70f;
            Keyframe[] hopKeys = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.35f, 0.35f),
                new Keyframe(0.70f, 0f)
            };
            SetCurve(clip, "Model_Body", typeof(Transform), "m_LocalPosition.y", new AnimationCurve(hopKeys));
            SetCurve(clip, "Model_Body", typeof(Transform), "localEulerAnglesRaw.y", SineCurve(dur, -25f, 25f, 1f));

            SaveClip(clip, "Child_Victory.anim");
            return clip;
        }

        private static AnimationClip CreateChildDodgeLeft()
        {
            var clip = new AnimationClip { name = "Child_Dodge_Left" };
            Keyframe[] dodgeX = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.20f, -0.65f),
                new Keyframe(0.45f, -0.50f),
                new Keyframe(0.60f, 0f)
            };
            Keyframe[] rotZ = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.20f, -22f),
                new Keyframe(0.60f, 0f)
            };
            SetCurve(clip, "Model_Body", typeof(Transform), "m_LocalPosition.x", new AnimationCurve(dodgeX));
            SetCurve(clip, "Socket_Head", typeof(Transform), "localEulerAnglesRaw.z", new AnimationCurve(rotZ));

            SaveClip(clip, "Child_Dodge_Left.anim");
            return clip;
        }

        private static AnimationClip CreateChildDodgeRight()
        {
            var clip = new AnimationClip { name = "Child_Dodge_Right" };
            Keyframe[] dodgeX = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.20f, 0.65f),
                new Keyframe(0.45f, 0.50f),
                new Keyframe(0.60f, 0f)
            };
            Keyframe[] rotZ = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.20f, 22f),
                new Keyframe(0.60f, 0f)
            };
            SetCurve(clip, "Model_Body", typeof(Transform), "m_LocalPosition.x", new AnimationCurve(dodgeX));
            SetCurve(clip, "Socket_Head", typeof(Transform), "localEulerAnglesRaw.z", new AnimationCurve(rotZ));

            SaveClip(clip, "Child_Dodge_Right.anim");
            return clip;
        }

        private static AnimationClip CreateChildDuck()
        {
            var clip = new AnimationClip { name = "Child_Duck" };
            Keyframe[] duckY = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.22f, -0.35f),
                new Keyframe(0.45f, -0.30f),
                new Keyframe(0.60f, 0f)
            };
            SetCurve(clip, "Model_Body", typeof(Transform), "m_LocalPosition.y", new AnimationCurve(duckY));
            SetCurve(clip, "Socket_Head", typeof(Transform), "localEulerAnglesRaw.x", SineCurve(0.6f, 0f, 25f, 1f));

            SaveClip(clip, "Child_Duck.anim");
            return clip;
        }

        private static AnimationClip CreateChildLookUp()
        {
            var clip = new AnimationClip { name = "Child_Look_Up" };
            Keyframe[] hopY = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.20f, 0.28f),
                new Keyframe(0.45f, 0.20f),
                new Keyframe(0.60f, 0f)
            };
            Keyframe[] headUp = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.20f, -28f),
                new Keyframe(0.45f, -25f),
                new Keyframe(0.60f, 0f)
            };
            SetCurve(clip, "Model_Body", typeof(Transform), "m_LocalPosition.y", new AnimationCurve(hopY));
            SetCurve(clip, "Socket_Head", typeof(Transform), "localEulerAnglesRaw.x", new AnimationCurve(headUp));

            SaveClip(clip, "Child_Look_Up.anim");
            return clip;
        }

        // ==============================================================================
        // 3. HUNTER CLIPS
        // ==============================================================================
        private static AnimationClip CreateHunterStalk()
        {
            var clip = new AnimationClip { name = "Hunter_Stalk" };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            float dur = 1.2f;
            SetCurve(clip, "Model_Body", typeof(Transform), "m_LocalPosition.y", SineCurve(dur, -0.04f, 0.02f, 1f));
            SetCurve(clip, "Model_Body", typeof(Transform), "localEulerAnglesRaw.x", SineCurve(dur, 6f, 10f, 1f));

            // Right claw hand hovering
            SetCurve(clip, "Socket_Hand_R", typeof(Transform), "m_LocalPosition.z", SineCurve(dur, 0.45f, 0.58f, 1f));
            SetCurve(clip, "Socket_Hand_R", typeof(Transform), "m_LocalPosition.y", SineCurve(dur, 0.80f, 0.88f, 1f));

            SaveClip(clip, "Hunter_Stalk.anim");
            return clip;
        }

        private static AnimationClip CreateHunterRun()
        {
            var clip = new AnimationClip { name = "Hunter_Run" };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            float dur = 0.60f;
            SetCurve(clip, "Model_Body", typeof(Transform), "m_LocalPosition.y", SineCurve(dur, 0.0f, 0.06f, 2f));
            SetCurve(clip, "Model_Body", typeof(Transform), "localEulerAnglesRaw.x", SineCurve(dur, 10f, 14f, 1f));
            SetCurve(clip, "Model_Body", typeof(Transform), "localEulerAnglesRaw.z", SineCurve(dur, -6f, 6f, 1f));

            SaveClip(clip, "Hunter_Run.anim");
            return clip;
        }

        private static AnimationClip CreateHunterLunge()
        {
            var clip = new AnimationClip { name = "Hunter_Lunge" };
            Keyframe[] lungeZ = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.20f, -0.15f), // windup pullback
                new Keyframe(0.40f, 0.95f),  // sudden forward thrust!
                new Keyframe(0.65f, 0.80f),
                new Keyframe(0.85f, 0f)
            };
            Keyframe[] handReach = new Keyframe[]
            {
                new Keyframe(0f, 0.48f),
                new Keyframe(0.20f, 0.25f),
                new Keyframe(0.40f, 1.15f),
                new Keyframe(0.85f, 0.48f)
            };
            SetCurve(clip, "Model_Body", typeof(Transform), "m_LocalPosition.z", new AnimationCurve(lungeZ));
            SetCurve(clip, "Socket_Hand_R", typeof(Transform), "m_LocalPosition.z", new AnimationCurve(handReach));

            SaveClip(clip, "Hunter_Lunge.anim");
            return clip;
        }

        private static AnimationClip CreateHunterAngry()
        {
            var clip = new AnimationClip { name = "Hunter_Angry" };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            float dur = 2.2f;
            // Furious stomping
            SetCurve(clip, "Model_Body", typeof(Transform), "m_LocalPosition.y", SineCurve(dur, 0.0f, 0.08f, 6f));
            SetCurve(clip, "Socket_Head", typeof(Transform), "localEulerAnglesRaw.y", SineCurve(dur, -28f, 28f, 5f));
            SetCurve(clip, "Socket_Head", typeof(Transform), "localEulerAnglesRaw.x", SineCurve(dur, 10f, 22f, 3f));

            SaveClip(clip, "Hunter_Angry.anim");
            return clip;
        }

        private static AnimationClip CreateHunterPointLeft()
        {
            var clip = new AnimationClip { name = "Hunter_Point_Left" };
            Keyframe[] bodyRotY = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.18f, -38f),
                new Keyframe(1.80f, -35f),
                new Keyframe(2.20f, 0f)
            };
            Keyframe[] bodyRotZ = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.18f, -14f),
                new Keyframe(1.80f, -12f),
                new Keyframe(2.20f, 0f)
            };
            Keyframe[] handRotY = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.18f, -70f),
                new Keyframe(1.80f, -70f),
                new Keyframe(2.20f, 0f)
            };
            SetCurve(clip, "Socket_Hand_R", typeof(Transform), "localEulerAnglesRaw.y", new AnimationCurve(handRotY));
            SetCurve(clip, "Model_Body", typeof(Transform), "localEulerAnglesRaw.y", new AnimationCurve(bodyRotY));
            SetCurve(clip, "Model_Body", typeof(Transform), "localEulerAnglesRaw.z", new AnimationCurve(bodyRotZ));

            SaveClip(clip, "Hunter_Point_Left.anim");
            return clip;
        }

        private static AnimationClip CreateHunterPointCenter()
        {
            var clip = new AnimationClip { name = "Hunter_Point_Center" };
            Keyframe[] bodyRotX = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.18f, -22f),
                new Keyframe(1.80f, -20f),
                new Keyframe(2.20f, 0f)
            };
            Keyframe[] handRotX = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.18f, -60f),
                new Keyframe(1.80f, -60f),
                new Keyframe(2.20f, 0f)
            };
            SetCurve(clip, "Socket_Hand_R", typeof(Transform), "localEulerAnglesRaw.x", new AnimationCurve(handRotX));
            SetCurve(clip, "Model_Body", typeof(Transform), "localEulerAnglesRaw.x", new AnimationCurve(bodyRotX));

            SaveClip(clip, "Hunter_Point_Center.anim");
            return clip;
        }

        private static AnimationClip CreateHunterPointRight()
        {
            var clip = new AnimationClip { name = "Hunter_Point_Right" };
            Keyframe[] bodyRotY = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.18f, 38f),
                new Keyframe(1.80f, 35f),
                new Keyframe(2.20f, 0f)
            };
            Keyframe[] bodyRotZ = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.18f, 14f),
                new Keyframe(1.80f, 12f),
                new Keyframe(2.20f, 0f)
            };
            Keyframe[] handRotY = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.18f, 70f),
                new Keyframe(1.80f, 70f),
                new Keyframe(2.20f, 0f)
            };
            SetCurve(clip, "Socket_Hand_R", typeof(Transform), "localEulerAnglesRaw.y", new AnimationCurve(handRotY));
            SetCurve(clip, "Model_Body", typeof(Transform), "localEulerAnglesRaw.y", new AnimationCurve(bodyRotY));
            SetCurve(clip, "Model_Body", typeof(Transform), "localEulerAnglesRaw.z", new AnimationCurve(bodyRotZ));

            SaveClip(clip, "Hunter_Point_Right.anim");
            return clip;
        }

        private static AnimationClip CreateHunterPointDown()
        {
            var clip = new AnimationClip { name = "Hunter_Point_Down" };
            Keyframe[] bodyPosY = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.18f, -0.15f),
                new Keyframe(1.80f, -0.15f),
                new Keyframe(2.20f, 0f)
            };
            Keyframe[] bodyRotX = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.18f, 26f),
                new Keyframe(1.80f, 24f),
                new Keyframe(2.20f, 0f)
            };
            Keyframe[] handRotX = new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.18f, 55f),
                new Keyframe(1.80f, 55f),
                new Keyframe(2.20f, 0f)
            };
            SetCurve(clip, "Socket_Hand_R", typeof(Transform), "localEulerAnglesRaw.x", new AnimationCurve(handRotX));
            SetCurve(clip, "Model_Body", typeof(Transform), "m_LocalPosition.y", new AnimationCurve(bodyPosY));
            SetCurve(clip, "Model_Body", typeof(Transform), "localEulerAnglesRaw.x", new AnimationCurve(bodyRotX));

            SaveClip(clip, "Hunter_Point_Down.anim");
            return clip;
        }

        private static void SaveClip(AnimationClip clip, string filename)
        {
            string path = $"{AnimDir}/{filename}";
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(clip, existing);
            }
            else
            {
                AssetDatabase.CreateAsset(clip, path);
            }
            Debug.Log($"[AnimationGenerator] Created Clip: {path}");
        }

        // ==============================================================================
        // ANIMATOR CONTROLLERS
        // ==============================================================================
        private static void BuildMotherController(AnimationClip idle, AnimationClip run, AnimationClip guard, AnimationClip victory)
        {
            string path = $"{AnimDir}/Ctrl_Mother.controller";
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);

            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("IsGuarding", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("IsVictory", AnimatorControllerParameterType.Bool);

            var rootSm = ctrl.layers[0].stateMachine;

            var sIdle = rootSm.AddState("Idle");
            sIdle.motion = idle;

            var sRun = rootSm.AddState("Run");
            sRun.motion = run;

            var sGuard = rootSm.AddState("Guard");
            sGuard.motion = guard;

            var sVictory = rootSm.AddState("Victory");
            sVictory.motion = victory;

            // Idle <-> Run
            var tRun = sIdle.AddTransition(sRun);
            tRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            tRun.duration = 0.15f;

            var tIdle = sRun.AddTransition(sIdle);
            tIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            tIdle.duration = 0.15f;

            // Any -> Guard
            var tGuard = rootSm.AddAnyStateTransition(sGuard);
            tGuard.AddCondition(AnimatorConditionMode.If, 0, "IsGuarding");
            tGuard.duration = 0.12f;
            tGuard.canTransitionToSelf = false;

            var tGuardExit = sGuard.AddTransition(sIdle);
            tGuardExit.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGuarding");
            tGuardExit.duration = 0.15f;

            // Any -> Victory
            var tVic = rootSm.AddAnyStateTransition(sVictory);
            tVic.AddCondition(AnimatorConditionMode.If, 0, "IsVictory");
            tVic.duration = 0.20f;
            tVic.canTransitionToSelf = false;
        }

        private static void BuildChildController(AnimationClip idle, AnimationClip run, AnimationClip victory, AnimationClip dLeft, AnimationClip dRight, AnimationClip duck, AnimationClip lookUp)
        {
            string path = $"{AnimDir}/Ctrl_Child.controller";
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);

            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("IsVictory", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("DodgeLeft", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("DodgeRight", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Duck", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("LookUp", AnimatorControllerParameterType.Trigger);

            var rootSm = ctrl.layers[0].stateMachine;

            var sIdle = rootSm.AddState("Idle");
            sIdle.motion = idle;

            var sRun = rootSm.AddState("Run");
            sRun.motion = run;

            var sVic = rootSm.AddState("Victory");
            sVic.motion = victory;

            var sDLeft = rootSm.AddState("Dodge_Left");
            sDLeft.motion = dLeft;

            var sDRight = rootSm.AddState("Dodge_Right");
            sDRight.motion = dRight;

            var sDuck = rootSm.AddState("Duck");
            sDuck.motion = duck;

            var sLookUp = rootSm.AddState("Look_Up");
            sLookUp.motion = lookUp;

            // Idle <-> Run
            var tRun = sIdle.AddTransition(sRun);
            tRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            tRun.duration = 0.12f;

            var tIdle = sRun.AddTransition(sIdle);
            tIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            tIdle.duration = 0.12f;

            // Dodges
            var tDLeft = rootSm.AddAnyStateTransition(sDLeft);
            tDLeft.AddCondition(AnimatorConditionMode.If, 0, "DodgeLeft");
            tDLeft.duration = 0.08f;
            tDLeft.canTransitionToSelf = false;
            var tExitL = sDLeft.AddTransition(sIdle);
            tExitL.hasExitTime = true;
            tExitL.exitTime = 0.85f;

            var tDRight = rootSm.AddAnyStateTransition(sDRight);
            tDRight.AddCondition(AnimatorConditionMode.If, 0, "DodgeRight");
            tDRight.duration = 0.08f;
            tDRight.canTransitionToSelf = false;
            var tExitR = sDRight.AddTransition(sIdle);
            tExitR.hasExitTime = true;
            tExitR.exitTime = 0.85f;

            var tDuck = rootSm.AddAnyStateTransition(sDuck);
            tDuck.AddCondition(AnimatorConditionMode.If, 0, "Duck");
            tDuck.duration = 0.08f;
            tDuck.canTransitionToSelf = false;
            var tExitDuck = sDuck.AddTransition(sIdle);
            tExitDuck.hasExitTime = true;
            tExitDuck.exitTime = 0.85f;

            var tLookUp = rootSm.AddAnyStateTransition(sLookUp);
            tLookUp.AddCondition(AnimatorConditionMode.If, 0, "LookUp");
            tLookUp.duration = 0.08f;
            tLookUp.canTransitionToSelf = false;
            var tExitLookUp = sLookUp.AddTransition(sIdle);
            tExitLookUp.hasExitTime = true;
            tExitLookUp.exitTime = 0.85f;

            // Victory
            var tVic = rootSm.AddAnyStateTransition(sVic);
            tVic.AddCondition(AnimatorConditionMode.If, 0, "IsVictory");
            tVic.duration = 0.20f;
            tVic.canTransitionToSelf = false;
        }

        private static void BuildHunterController(AnimationClip stalk, AnimationClip run, AnimationClip lunge, AnimationClip angry, AnimationClip pointL, AnimationClip pointC, AnimationClip pointR, AnimationClip pointD)
        {
            string path = $"{AnimDir}/Ctrl_Hunter.controller";
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);

            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Lunge", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("IsAngry", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("IsVictory", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("PointLeft", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("PointCenter", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("PointRight", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("PointDown", AnimatorControllerParameterType.Trigger);

            var rootSm = ctrl.layers[0].stateMachine;

            var sStalk = rootSm.AddState("Stalk");
            sStalk.motion = stalk;

            var sRun = rootSm.AddState("Run");
            sRun.motion = run;

            var sLunge = rootSm.AddState("Lunge");
            sLunge.motion = lunge;

            var sAngry = rootSm.AddState("Angry");
            sAngry.motion = angry;

            var sVic = rootSm.AddState("Victory");
            sVic.motion = run; // Energetic celebration dance

            // Chăm Chăm Chăm Duel Pointing States (4 directions)
            var sPointL = rootSm.AddState("Point_Left");
            sPointL.motion = pointL;

            var sPointC = rootSm.AddState("Point_Center");
            sPointC.motion = pointC;

            var sPointR = rootSm.AddState("Point_Right");
            sPointR.motion = pointR;

            var sPointD = rootSm.AddState("Point_Down");
            sPointD.motion = pointD;

            // Stalk <-> Run
            var tRun = sStalk.AddTransition(sRun);
            tRun.AddCondition(AnimatorConditionMode.Greater, 2.5f, "Speed");
            tRun.duration = 0.15f;

            var tStalk = sRun.AddTransition(sStalk);
            tStalk.AddCondition(AnimatorConditionMode.Less, 2.5f, "Speed");
            tStalk.duration = 0.15f;

            // Lunge
            var tLunge = rootSm.AddAnyStateTransition(sLunge);
            tLunge.AddCondition(AnimatorConditionMode.If, 0, "Lunge");
            tLunge.duration = 0.08f;
            tLunge.canTransitionToSelf = false;
            var tLungeExit = sLunge.AddTransition(sStalk);
            tLungeExit.hasExitTime = true;
            tLungeExit.exitTime = 0.85f;

            // Pointing Transitions (4 directions)
            var tPL = rootSm.AddAnyStateTransition(sPointL);
            tPL.AddCondition(AnimatorConditionMode.If, 0, "PointLeft");
            tPL.duration = 0.08f;
            tPL.canTransitionToSelf = false;
            var tPLExit = sPointL.AddTransition(sStalk);
            tPLExit.hasExitTime = true;
            tPLExit.exitTime = 0.85f;

            var tPC = rootSm.AddAnyStateTransition(sPointC);
            tPC.AddCondition(AnimatorConditionMode.If, 0, "PointCenter");
            tPC.duration = 0.08f;
            tPC.canTransitionToSelf = false;
            var tPCExit = sPointC.AddTransition(sStalk);
            tPCExit.hasExitTime = true;
            tPCExit.exitTime = 0.85f;

            var tPR = rootSm.AddAnyStateTransition(sPointR);
            tPR.AddCondition(AnimatorConditionMode.If, 0, "PointRight");
            tPR.duration = 0.08f;
            tPR.canTransitionToSelf = false;
            var tPRExit = sPointR.AddTransition(sStalk);
            tPRExit.hasExitTime = true;
            tPRExit.exitTime = 0.85f;

            var tPD = rootSm.AddAnyStateTransition(sPointD);
            tPD.AddCondition(AnimatorConditionMode.If, 0, "PointDown");
            tPD.duration = 0.08f;
            tPD.canTransitionToSelf = false;
            var tPDExit = sPointD.AddTransition(sStalk);
            tPDExit.hasExitTime = true;
            tPDExit.exitTime = 0.85f;

            // Angry
            var tAngry = rootSm.AddAnyStateTransition(sAngry);
            tAngry.AddCondition(AnimatorConditionMode.If, 0, "IsAngry");
            tAngry.duration = 0.15f;
            tAngry.canTransitionToSelf = false;

            // Victory
            var tVicHunter = rootSm.AddAnyStateTransition(sVic);
            tVicHunter.AddCondition(AnimatorConditionMode.If, 0, "IsVictory");
            tVicHunter.duration = 0.20f;
            tVicHunter.canTransitionToSelf = false;
        }

        private static void AttachAnimatorToPrefab(string prefabPath, string controllerPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);

            if (prefab == null || controller == null) return;

            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            var anim = root.GetComponent<Animator>();
            if (anim == null) anim = root.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = false;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log($"[AnimationGenerator] Gắn Animator Controller vào {Path.GetFileName(prefabPath)} thành công!");
        }
    }
}
#endif
