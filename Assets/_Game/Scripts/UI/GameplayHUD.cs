using UnityEngine;

namespace RongRanChamCham
{
    public class GameplayHUD : MonoBehaviour
    {
        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle heartStyle;
        private GUIStyle bannerStyle;
        private GUIStyle buttonStyle;
        private bool stylesInitialized = false;

        private void InitStyles()
        {
            if (stylesInitialized) return;

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter
            };
            headerStyle.normal.textColor = Color.white;

            subHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            };
            subHeaderStyle.normal.textColor = Color.yellow;

            heartStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                alignment = TextAnchor.UpperLeft
            };
            heartStyle.normal.textColor = new Color(1.0f, 0.3f, 0.4f);

            bannerStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            bannerStyle.normal.textColor = Color.white;

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            // 1. Top Center: Match Timer
            if (GameManager.Instance != null)
            {
                float time = GameManager.Instance.RemainingTime;
                int mins = Mathf.FloorToInt(time / 60f);
                int secs = Mathf.FloorToInt(time % 60f);
                string timeText = $"⏱️ Thời gian: {mins:00}:{secs:00}";

                GUI.color = Color.black;
                GUI.Label(new Rect((Screen.width / 2f) - 148, 17, 300, 40), timeText, headerStyle);
                GUI.color = Color.white;
                GUI.Label(new Rect((Screen.width / 2f) - 150, 15, 300, 40), timeText, headerStyle);
            }

            // 2. Top Left: Con Út Health (Hearts)
            int health = GameManager.Instance != null ? GameManager.Instance.ConUtHealth : 3;
            string hearts = "";
            for (int i = 0; i < 3; i++)
            {
                hearts += (i < health) ? "❤️ " : "🖤 ";
            }
            GUI.Label(new Rect(25, 20, 250, 40), $"Con Út: {hearts}", heartStyle);

            // 3. Top Right: Active Food Buff Bar
            if (BuffSystem.Instance != null && BuffSystem.Instance.CurrentBuffType.HasValue)
            {
                var buff = BuffSystem.Instance.CurrentBuffType.Value;
                float remTime = BuffSystem.Instance.RemainingBuffTime;

                string buffName = "";
                Color buffColor = Color.white;
                switch (buff)
                {
                    case FoodType.BanhMi: buffName = "🥖 BÁNH MÌ (Giáp Vàng)"; buffColor = new Color(1.0f, 0.85f, 0.2f); break;
                    case FoodType.Pho: buffName = "🍜 PHỞ (Hồi Máu + Giáp)"; buffColor = new Color(0.3f, 1.0f, 0.5f); break;
                    case FoodType.Kimbap: buffName = "🍙 KIMBAP (Tăng Tốc +35%)"; buffColor = new Color(0.3f, 0.8f, 1.0f); break;
                    case FoodType.Kimchi: buffName = "🥬 KIMCHI (Khổng Lồ +40%)"; buffColor = new Color(1.0f, 0.4f, 0.2f); break;
                }

                subHeaderStyle.normal.textColor = buffColor;
                GUI.Label(new Rect(Screen.width - 320, 20, 300, 30), buffName, subHeaderStyle);

                // Progress bar
                float progress = remTime / GameConstants.BuffDuration;
                GUI.Box(new Rect(Screen.width - 320, 50, 300 * progress, 14), "");
            }

            // 4. Bottom Instructions: Controls
            string controls = "⌨️ [WASD / Mũi Tên]: Di chuyển 360° | [SPACE]: Giữ THỦ (Khóa mặt đối đầu Thợ Săn)";
            GUI.color = Color.black;
            GUI.Label(new Rect(21, Screen.height - 39, Screen.width, 30), controls);
            GUI.color = Color.white;
            GUI.Label(new Rect(20, Screen.height - 40, Screen.width, 30), controls);

            // 5. Game State Overlays
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.CurrentState == GameState.Victory)
                {
                    GUI.backgroundColor = new Color(0.1f, 0.7f, 0.2f, 0.9f);
                    GUI.Box(new Rect((Screen.width / 2f) - 250, (Screen.height / 2f) - 100, 500, 200), "\n🎉 CHIẾN THẮNG!\nMẸ ĐÃ BẢO VỆ CON ÚT THÀNH CÔNG!", bannerStyle);
                    if (GUI.Button(new Rect((Screen.width / 2f) - 100, (Screen.height / 2f) + 30, 200, 50), "Chơi Lại", buttonStyle))
                    {
                        GameManager.Instance.StartMatch();
                    }
                }
                else if (GameManager.Instance.CurrentState == GameState.GameOver)
                {
                    GUI.backgroundColor = new Color(0.8f, 0.1f, 0.1f, 0.9f);
                    GUI.Box(new Rect((Screen.width / 2f) - 250, (Screen.height / 2f) - 100, 500, 200), "\n💥 THUA CUỘC!\nTHỢ SĂN ĐÃ BẮT ĐƯỢC CON ÚT!", bannerStyle);
                    if (GUI.Button(new Rect((Screen.width / 2f) - 100, (Screen.height / 2f) + 30, 200, 50), "Thử Lại", buttonStyle))
                    {
                        GameManager.Instance.StartMatch();
                    }
                }
            }
        }
    }
}
