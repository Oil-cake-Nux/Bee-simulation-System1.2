using UnityEngine;
using UnityEngine.UI;

namespace ljk
{
    public class VisualizationManager : MonoBehaviour
    {
        [Header("可视化控制")]
        public bool enableAllVisualization = true;

        [Header("管理脚本")]
        public BeeSimulation beeSimulation;
        public BeeTargetController beeTargetController;
        public BeeSimulationManager beeSimulationManager;

        [Header("UI引用")]
        public Button toggleButton;
        public Text buttonText;

        private void Start()
        {
            ResolveReferences();

            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(ToggleAllVisualization);
            }

            ApplyVisualizationState(enableAllVisualization, false);
        }

        public void ToggleAllVisualization()
        {
            ApplyVisualizationState(!enableAllVisualization, true);
        }

        public void EnableAllVisualization()
        {
            ApplyVisualizationState(true, false);
        }

        public void DisableAllVisualization()
        {
            ApplyVisualizationState(false, false);
        }

        [ContextMenu("切换可视化")]
        private void ToggleInEditor()
        {
            ToggleAllVisualization();
        }

        private void ResolveReferences()
        {
            if (beeSimulation == null)
            {
                beeSimulation = FindObjectOfType<BeeSimulation>();
            }

            if (beeTargetController == null)
            {
                beeTargetController = FindObjectOfType<BeeTargetController>();
            }

            if (beeSimulationManager == null)
            {
                beeSimulationManager = FindObjectOfType<BeeSimulationManager>();
            }
        }

        private void ApplyVisualizationState(bool enabled, bool logChange)
        {
            enableAllVisualization = enabled;
            UpdateAllVisualization();
            UpdateButtonText();

            if (logChange)
            {
                Debug.Log($"所有可视化: {(enableAllVisualization ? "开启" : "关闭")}");
            }
        }

        private void UpdateAllVisualization()
        {
            ResolveReferences();

            if (beeSimulation != null)
            {
                beeSimulation.showDebugRays = enableAllVisualization;
                beeSimulation.showForces = enableAllVisualization;
                beeSimulation.showNoiseForce = enableAllVisualization;
            }

            if (beeSimulationManager != null)
            {
                beeSimulationManager.drawTrajectory = enableAllVisualization;

                if (!enableAllVisualization && beeSimulationManager.trajectoryRenderer != null)
                {
                    beeSimulationManager.trajectoryRenderer.positionCount = 0;
                }
            }
        }

        private void UpdateButtonText()
        {
            if (buttonText != null)
            {
                buttonText.text = enableAllVisualization ? "关闭可视化" : "开启可视化";
            }
        }
    }
}
