using System.Collections.Generic;
using UnityEngine;

namespace RongRanChamCham
{
    /// <summary>
    /// Thực thể Phân Thân Toàn Diện (Decoy Phantom Snake Chain).
    /// Khi Rồng Rắn ăn Bánh Gạo Cay, nguyên cả một đàn Rồng Rắn ảo ảnh (Mẹ + Đàn Con Út đội vương miện)
    /// sẽ tách ra ngay từ đàn rắn gốc và luồn lách chạy ngẫu nhiên trên sân, đánh lừa Thầy Thuốc AI.
    /// Nếu Thầy Thuốc vồ trúng đàn rắn ảo, ảo ảnh sẽ phát nổ gây Choáng (Stun) 1.5s cho Thầy Thuốc!
    /// </summary>
    public class DecoyCloneActor : MonoBehaviour
    {
        public static DecoyCloneActor ActiveClone { get; private set; }

        [Header("Thông số phân thân")]
        [SerializeField] private float lifetime = 6.0f;
        [SerializeField] private float runSpeed = 5.8f;
        [SerializeField] private float stunDurationOnExplode = 1.5f;
        [SerializeField] private float linkSpacing = 0.72f;

        private CharacterController characterController;
        private Vector3 moveDirection;
        private float lifeTimer = 0f;
        private bool isVanishing = false;

        private readonly List<GameObject> phantomChildren = new List<GameObject>();
        private readonly List<Vector3> chainHistory = new List<Vector3>();

        public bool IsActive => !isVanishing && lifeTimer < lifetime;
        public bool CanBeCaught => lifeTimer >= 1.2f;

        public Transform TailTransform
        {
            get
            {
                if (phantomChildren != null && phantomChildren.Count > 0 && phantomChildren[phantomChildren.Count - 1] != null)
                {
                    return phantomChildren[phantomChildren.Count - 1].transform;
                }
                return transform;
            }
        }

        public Vector3 TailPosition => TailTransform != null ? TailTransform.position : transform.position;

        public static DecoyCloneActor Spawn(MotherController realMother, SnakeChainIK realSnake)
        {
            if (ActiveClone != null)
            {
                ActiveClone.Vanish(false);
            }

            if (realMother == null) return null;

            Vector3 spawnPos = realMother.transform.position;
            Quaternion spawnRot = realMother.transform.rotation;

            GameObject rootGo = new GameObject("Decoy_Phantom_SnakeChain");
            rootGo.transform.SetPositionAndRotation(spawnPos, spawnRot);

            var decoy = rootGo.AddComponent<DecoyCloneActor>();
            decoy.InitializeChain(realMother, realSnake);
            ActiveClone = decoy;
            return decoy;
        }

        // Backward compatibility overload
        public static DecoyCloneActor Spawn(Vector3 pos, Quaternion rot, GameObject templateVisual)
        {
            var mother = GameManager.Instance != null ? GameManager.Instance.Mother : null;
            var snake = GameManager.Instance != null ? GameManager.Instance.SnakeChain : null;
            if (mother != null)
            {
                return Spawn(mother, snake);
            }

            GameObject cloneGo = new GameObject("Decoy_Phantom_SnakeChain");
            cloneGo.transform.SetPositionAndRotation(pos, rot);
            var decoy = cloneGo.AddComponent<DecoyCloneActor>();
            ActiveClone = decoy;
            return decoy;
        }

        private void InitializeChain(MotherController realMother, SnakeChainIK realSnake)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.radius = 0.45f;
            characterController.height = 1.4f;
            characterController.center = new Vector3(0f, 0.7f, 0f);

            // 1. Sao chép CHỈ MESH 3D của Rắn Mẹ (Tuyệt đối không nhân bản MotherController script để tránh xung đột input/null reference)
            CloneVisualMeshOnly(realMother.gameObject, transform);

            // 2. Hướng rẽ tách đàn độc lập: Tách chéo góc 70 - 85 độ so với hướng đàn thật đang chạy
            float branchAngle = Random.value > 0.5f ? Random.Range(65f, 85f) : Random.Range(-85f, -65f);
            moveDirection = (Quaternion.Euler(0f, branchAngle, 0f) * realMother.transform.forward).normalized;
            transform.rotation = Quaternion.LookRotation(moveDirection);

            // 3. Khởi tạo toàn bộ Đàn Con Ảo xếp nối đuôi ngay sau Mẹ Ảo theo hàng thẳng ban đầu
            if (realSnake != null && realSnake.Children != null)
            {
                int childCount = realSnake.Children.Count;
                for (int i = 0; i < childCount; i++)
                {
                    var realChild = realSnake.Children[i];
                    if (realChild == null || !realChild.gameObject.activeInHierarchy) continue;

                    Vector3 initChildPos = transform.position - (moveDirection * ((i + 1) * linkSpacing));
                    initChildPos.y = 0f;

                    GameObject pChildGo = new GameObject($"Phantom_Child_{i}");
                    pChildGo.transform.SetPositionAndRotation(initChildPos, transform.rotation);

                    // Sao chép chỉ Mesh 3D của con
                    CloneVisualMeshOnly(realChild.gameObject, pChildGo.transform);

                    // Nếu là Bé Út (con cuối cùng): Sao chép cả Vương Miện vàng
                    if (i == childCount - 1)
                    {
                        var crown = realChild.transform.Find("Socket_Crown");
                        if (crown != null)
                        {
                            GameObject phantomCrown = Instantiate(crown.gameObject, pChildGo.transform);
                            phantomCrown.name = "Socket_Crown";
                            phantomCrown.transform.localPosition = crown.localPosition;
                            phantomCrown.transform.localRotation = crown.localRotation;
                            phantomCrown.transform.localScale = crown.localScale;
                            phantomCrown.SetActive(true);
                        }
                    }

                    phantomChildren.Add(pChildGo);
                }
            }

            // Điền trước lịch sử vị trí cho cả đàn nối đuôi nhau liền mạch
            chainHistory.Clear();
            for (int h = 0; h < 60; h++)
            {
                chainHistory.Add(transform.position - (moveDirection * (h * 0.12f)));
            }

            // Hiệu ứng pháo khói tách đàn vàng kim đẹp mắt
            CreatePoofFX(transform.position, new Color(1f, 0.85f, 0.2f, 0.6f));
            AudioManager.Instance?.PlaySFX(SoundType.Whoosh);
        }

        private void CloneVisualMeshOnly(GameObject sourceOwner, Transform parent)
        {
            Transform visualTransform = sourceOwner.transform.Find("Model_Body");
            if (visualTransform == null)
            {
                var rend = sourceOwner.GetComponentInChildren<Renderer>();
                if (rend != null) visualTransform = rend.transform;
            }

            if (visualTransform == null) return;

            GameObject meshGo = Instantiate(visualTransform.gameObject, parent);
            meshGo.name = visualTransform.name;
            meshGo.transform.localPosition = visualTransform.localPosition;
            meshGo.transform.localRotation = visualTransform.localRotation;
            meshGo.transform.localScale = visualTransform.localScale;

            // Xóa bỏ toàn bộ Script, Animator và Collider trên Mesh để chỉ giữ lại hình thể visual 3D
            var animators = meshGo.GetComponentsInChildren<Animator>(true);
            foreach (var a in animators) DestroyImmediate(a);

            var monos = meshGo.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var m in monos) DestroyImmediate(m);

            var colliders = meshGo.GetComponentsInChildren<Collider>(true);
            foreach (var c in colliders) DestroyImmediate(c);
        }

        private void Update()
        {
            if (isVanishing) return;

            float dt = Time.deltaTime;
            lifeTimer += dt;

            // 1. Rắn Mẹ Ảo tự động điều hướng hoàn toàn độc lập (Random AI Wanderer)
            Vector3 curPos = transform.position;
            curPos.y = 0f;
            float distFromCenter = curPos.magnitude;

            if (distFromCenter > 7.5f)
            {
                // Bẻ lái mềm mại quay lại tâm sân khi chạm rìa
                Vector3 toCenter = (-curPos).normalized;
                moveDirection = Vector3.Slerp(moveDirection, toCenter, dt * 5.0f).normalized;
            }
            else
            {
                // Tự động lượn sóng nhịp nhàng như đàn rắn đang trườn
                float wave = Mathf.Sin(Time.time * 2.5f) * 25f * dt;
                moveDirection = (Quaternion.Euler(0f, wave, 0f) * moveDirection).normalized;
            }

            Vector3 move = moveDirection * runSpeed * dt;
            move.y = -9.81f * dt;
            if (characterController != null && characterController.enabled)
            {
                characterController.Move(move);
            }
            else
            {
                transform.position += moveDirection * runSpeed * dt;
            }

            transform.position = ArenaBoundary.ClampToArena(transform.position, 0.5f);

            // Quay mặt Rắn Mẹ Ảo theo hướng di chuyển độc lập
            Vector3 flatDir = moveDirection; flatDir.y = 0f;
            if (flatDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flatDir), dt * 14f);
            }

            // Ghi nhận lịch sử vị trí cho đàn con ảo bám theo
            chainHistory.Insert(0, transform.position);
            int maxHistory = 120;
            if (chainHistory.Count > maxHistory)
            {
                chainHistory.RemoveAt(chainHistory.Count - 1);
            }

            // 2. Đàn Con Ảo nối đuôi nhau luồn lách bám theo Mẹ Ảo theo vết đường đi
            UpdatePhantomChildrenFollow(dt);

            // Hết thời gian hiệu lực (6s) -> Tự động tan biến
            if (lifeTimer >= lifetime)
            {
                Vanish(false);
            }
        }

        private void UpdatePhantomChildrenFollow(float dt)
        {
            if (phantomChildren == null || phantomChildren.Count == 0) return;

            for (int i = 0; i < phantomChildren.Count; i++)
            {
                var child = phantomChildren[i];
                if (child == null) continue;

                float desiredDist = (i + 1) * linkSpacing;
                Vector3 targetPos = SampleHistoryPosition(desiredDist);
                targetPos.y = 0f;

                // Di chuyển mượt mà tới vị trí mục tiêu
                child.transform.position = Vector3.Lerp(child.transform.position, targetPos, dt * 18f);

                // Quay mặt theo hướng người phía trước
                Vector3 leaderPos = (i == 0) ? transform.position : phantomChildren[i - 1].transform.position;
                Vector3 faceDir = leaderPos - child.transform.position;
                faceDir.y = 0f;
                if (faceDir.sqrMagnitude > 0.01f)
                {
                    child.transform.rotation = Quaternion.Slerp(child.transform.rotation, Quaternion.LookRotation(faceDir), dt * 16f);
                }
            }
        }

        private Vector3 SampleHistoryPosition(float targetDistance)
        {
            if (chainHistory.Count == 0) return transform.position;
            if (chainHistory.Count == 1) return chainHistory[0];

            float accumulated = 0f;
            for (int i = 0; i < chainHistory.Count - 1; i++)
            {
                float segment = Vector3.Distance(chainHistory[i], chainHistory[i + 1]);
                if (accumulated + segment >= targetDistance)
                {
                    float remainder = targetDistance - accumulated;
                    float t = segment > 0.001f ? (remainder / segment) : 0f;
                    return Vector3.Lerp(chainHistory[i], chainHistory[i + 1], t);
                }
                accumulated += segment;
            }
            return chainHistory[chainHistory.Count - 1];
        }

        public void Vanish(bool caughtByHunter)
        {
            if (isVanishing) return;
            isVanishing = true;

            // Hiệu ứng khói vàng tan biến nhẹ nhàng
            CreatePoofFX(transform.position, new Color(1f, 0.9f, 0.4f, 0.6f));

            AudioManager.Instance?.PlaySFX(SoundType.ShieldBlock);

            // Nếu Thầy Thuốc vồ trúng phân thân: Thầy Thuốc bị CHOÁNG 1.5s!
            if (caughtByHunter)
            {
                var hunter = FindAnyObjectByType<HunterAI>();
                if (hunter != null)
                {
                    hunter.ApplyExternalStun(stunDurationOnExplode);
                    Debug.Log("<color=yellow>[PHÂN THÂN]</color> Thầy Thuốc vồ nhầm ĐOÀN RẮN ẢO và bị CHOÁNG 1.5s!");
                }
            }

            if (ActiveClone == this) ActiveClone = null;

            // Xóa sạch các con ảo
            if (phantomChildren != null)
            {
                foreach (var c in phantomChildren)
                {
                    if (c != null) Destroy(c);
                }
                phantomChildren.Clear();
            }

            Destroy(gameObject, 0.05f);
        }

        private void CreatePoofFX(Vector3 pos, Color color)
        {
            var poof = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            poof.name = "Poof_FX";
            poof.transform.position = pos + Vector3.up * 0.4f;
            poof.transform.localScale = Vector3.one * 0.45f;
            Destroy(poof.GetComponent<Collider>());

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent");
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", new Color(color.r, color.g, color.b, 0.45f));
            poof.GetComponent<Renderer>().sharedMaterial = mat;

            Destroy(poof, 0.25f);
        }

        private void OnDestroy()
        {
            if (ActiveClone == this) ActiveClone = null;
            if (phantomChildren != null)
            {
                foreach (var c in phantomChildren)
                {
                    if (c != null) Destroy(c);
                }
                phantomChildren.Clear();
            }
        }
    }
}
