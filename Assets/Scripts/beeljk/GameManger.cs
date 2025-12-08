using UnityEngine;
using UnityEngine.UI;

namespace ljk
{
    public class SimplePauseManager : MonoBehaviour
    {
        public KeyCode pauseKey = KeyCode.Space;
        public Text pauseIndicator;  // 可选的暂停提示文本

        private bool isPaused = false;

        void Update()
        {
            if (Input.GetKeyDown(pauseKey))
            {
                TogglePause();
            }
        }

        void TogglePause()
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;

            // 更新暂停提示
            if (pauseIndicator != null)
            {
                pauseIndicator.text = isPaused ? "已暂停 (按空格继续)" : "";
                pauseIndicator.gameObject.SetActive(isPaused);
            }

            Debug.Log($"游戏 {(isPaused ? "已暂停" : "已恢复")}");
        }
    }
}

