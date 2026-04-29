using UnityEngine;
using UnityEngine.SceneManagement;

namespace ljk
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string gameSceneName = "Demo";

        public void StartExploration()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameSceneName);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
