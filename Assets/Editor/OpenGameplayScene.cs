using UnityEditor;
using UnityEditor.SceneManagement;

namespace RongRanChamCham.Editor
{
    public static class OpenGameplayScene
    {
        [MenuItem("Rồng Rắn/▶ Mở Scene Chơi Game (Gameplay) %&g")]
        public static void Open()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene("Assets/_Game/Scenes/Gameplay.unity");
            }
        }
    }
}
