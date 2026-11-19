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
        [SerializeField] private float runSpeed = 6.2f;
        [SerializeField] private float stunDurationOnExplode = 1.5f;
        [SerializeField] private float linkSpacing = 0.72f;

        private CharacterController characterController;
        private Vector3 moveDirection;
        private float lifeTimer = 0f;
        private bool isVanishing = false;

        private GameObject phantomMotherGo;
        private readonly List<GameObject> phantomChildren = new List<GameObject>();
        private readonly List<Vector3> chainHistory = new List<Vector3>();

        public bool IsActive => !isVanishing && lifeTimer < lifetime;

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
            decoy.InitializeSimple(templateVisual);
            ActiveClone = decoy;
            return decoy;
        }

        private void InitializeChain(MotherController realMother, SnakeChainIK realSnake)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.radius = 0.45f;
            characterController.height = 1.4f;
            characterController.center = new Vector3(0f, 0.7f, 0f);

            // 1. Nhân bản Rắn Mẹ Ảo (Giữ nguyên trang phục áo đỏ, nón lá, tóc búi chuẩn mực)
            phantomMotherGo = CloneVisual(realMother.gameObject, transform);
            phantomMotherGo.name = "Phantom_Mother";
            phantomMotherGo.transform.localPosition = Vector3.zero;
            phantomMotherGo.transform.localRotation = Quaternion.identity;

            // 2. Nhân bản toàn bộ Đàn Con Ảo (vị trí trùng khít đàn con thật lúc tách ra)
            if (realSnake != null && realSnake.Children != null)
            {
                for (int i = 0; i < realSnake.Children.Count; i++)
                {
                    var realChild = realSnake.Children[i];
                    if (realChild == null || !realChild.gameObject.activeInHierarchy) continue;

                    GameObject pChildGo = CloneVisual(realChild.gameObject, null);
                    pChildGo.name = $"Phantom_Child_{i}";
                    pChildGo.transform.SetPositionAndRotation(realChild.transform.position, realChild.transform.rotation);
                    phantomChildren.Add(pChildGo);
                }
            }

            // 3. Hướng rẽ tách đàn: Tách chéo góc 70 - 90 độ so với hướng đàn thật đang chạy
            float branchAngle = Random.value > 0.5f ? Random.Range(65f, 90f) : Random.Range(-90f, -65f);
            moveDirection = (Quaternion.Euler(0f, branchAngle, 0f) * realMother.transform.forward).normalized;

            // Khởi tạo lịch sử vị trí cho IK nối đuôi
            chainHistory.Clear();
            chainHistory.Add(transform.position);

            // Hiệu ứng khói tách đàn ma thuật
            CreatePoofFX(transform.position, new Color(1f, 0.85f, 0.2f, 0.8f));
            AudioManager.Instance?.PlaySFX(SoundType.Whoosh);
        }

        private void InitializeSimple(GameObject template)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.radius = 0.45f;
            characterController.height = 1.4f;
            characterController.center = new Vector3(0f, 0.7f, 0f);

            if (template != null)
            {
                phantomMotherGo = CloneVisual(template, transform);
            }
            Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
            moveDirection = (transform.forward + randomOffset).normalized;
        }

        private GameObject CloneVisual(GameObject source, Transform parent)
        {
            GameObject clone = Instantiate(source, parent);
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = Quaternion.identity;

            // Xóa bỏ các Script điều khiển thật và Collider để tránh xung đột vật lý
            var monos = clone.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var m in monos)
            {
                if (m != null && !(m is ChibiProceduralAnimator))
                {
                    Destroy(m);
                }
            }

            var colliders = clone.GetComponentsInChildren<Collider>(true);
            foreach (var c in colliders)
            {
                Destroy(c);
            }

            return clone;
        }

        private void Update()
        {
            if (isVanishing) return;

            float dt = Time.deltaTime;
            lifeTimer += dt;

            // 1. Rắn Mẹ Ảo điều hướng luồn lách thông minh trên sân đấu
            Vector3 move = moveDirection * runSpeed * dt;
            move.y = -9.81f * dt;
            if (characterController != null)
            {
                characterController.Move(move);
            }

            // Giữ trong ranh giới sân đấu tròn
            Vector3 curPos = transform.position;
            curPos.y = 0f;
            float distFromCenter = curPos.magnitude;
            if (distFromCenter > 8.5f)
            {
                // Bẻ lái mềm mại quay lại tâm sân khi chạm rìa
                Vector3 toCenter = (-curPos).normalized;
                moveDirection = Vector3.Slerp(moveDirection, toCenter, dt * 6f).normalized;
            }
            else if (Random.value < 0.025f)
            {
                // Uốn lượn đổi hướng tự nhiên như đàn rồng rắn
                float steerAngle = Random.Range(-45f, 45f);
                moveDirection = (Quaternion.Euler(0f, steerAngle, 0f) * moveDirection).normalized;
            }

            transform.position = ArenaBoundary.ClampToArena(transform.position, 0.5f);

            // Quay mặt Rắn Mẹ Ảo theo hướng di chuyển
            Vector3 flatDir = moveDirection; flatDir.y = 0f;
            if (flatDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flatDir), dt * 12f);
            }

            // 2. Đàn Con Ảo nối đuôi nhau luồn lách bám theo Mẹ Ảo
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

            // Cập nhật vị trí từng đốt con bám theo đốt phía trước
            Vector3 leaderPos = transform.position;
            Quaternion leaderRot = transform.rotation;

            for (int i = 0; i < phantomChildren.Count; i++)
            {
                var child = phantomChildren[i];
                if (child == null) continue;

                Vector3 targetNode = leaderPos - (leaderRot * Vector3.forward * linkSpacing);
                targetNode.y = 0f;

                // Di chuyển mượt mà tới vị trí mục tiêu
                Vector3 childPos = child.transform.position;
                Vector3 toTarget = targetNode - childPos;
                toTarget.y = 0f;

                float dist = toTarget.magnitude;
                if (dist > linkSpacing)
                {
                    childPos = Vector3.MoveTowards(childPos, targetNode, runSpeed * dt * 1.15f);
                }
                childPos = ArenaBoundary.ClampToArena(childPos, 0.45f);
                child.transform.position = childPos;

                // Quay mặt theo hướng người phía trước
                Vector3 faceDir = leaderPos - childPos;
                faceDir.y = 0f;
                if (faceDir.sqrMagnitude > 0.01f)
                {
                    child.transform.rotation = Quaternion.Slerp(child.transform.rotation, Quaternion.LookRotation(faceDir), dt * 14f);
                }

                leaderPos = childPos;
                leaderRot = child.transform.rotation;
            }
        }

        public void Vanish(bool caughtByHunter)
        {
            if (isVanishing) return;
            isVanishing = true;

            // Nổ tung khói ma thuật trên toàn bộ đàn rắn ảo
            CreatePoofFX(transform.position, Color.yellow);
            if (phantomChildren != null)
            {
                foreach (var c in phantomChildren)
                {
                    if (c != null) CreatePoofFX(c.transform.position, Color.magenta);
                }
            }

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

            Destroy(gameObject, 0.1f);
        }

        private void CreatePoofFX(Vector3 pos, Color color)
        {
            var poof = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            poof.name = "Poof_FX";
            poof.transform.position = pos + Vector3.up * 0.7f;
            poof.transform.localScale = Vector3.one * 1.5f;
            Destroy(poof.GetComponent<Collider>());

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent");
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", new Color(color.r, color.g, color.b, 0.75f));
            poof.GetComponent<Renderer>().sharedMaterial = mat;

            Destroy(poof, 0.45f);
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
