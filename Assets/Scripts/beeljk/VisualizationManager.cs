
using System.Collections.Generic;
using UnityEngine;

namespace ljk
{
    public class VisualizationManager : MonoBehaviour
    {
        [Header("可视化控制")]
        public bool enableAllVisualization = true;

        [Header("管理的脚本")]
        public BeeSimulation beeSimulation;
        public BeeTargetController beeTargetController;
        public BeeSimulationManager beeSimulationManager;

        [Header("UI引用")]
        public UnityEngine.UI.Button toggleButton;
        public UnityEngine.UI.Text buttonText;

        private void Start()
        {
            // 自动查找相关组件（如果未手动赋值）
            if (beeSimulation == null)
                beeSimulation = FindObjectOfType<BeeSimulation>();
            if (beeTargetController == null)
                beeTargetController = FindObjectOfType<BeeTargetController>();
            if (beeSimulationManager == null)
                beeSimulationManager = FindObjectOfType<BeeSimulationManager>();

            // 设置按钮点击事件
            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(ToggleAllVisualization);
            }

            // 初始化可视化状态
            UpdateAllVisualization();
            UpdateButtonText();
        }

        /// <summary>
        /// 切换所有可视化状态
        /// </summary>
        public void ToggleAllVisualization()
        {
            enableAllVisualization = !enableAllVisualization;
            UpdateAllVisualization();
            UpdateButtonText();

            Debug.Log($"所有可视化: {(enableAllVisualization ? "Turn on" : "Turn off")}");
        }

        /// <summary>
        /// 更新所有可视化组件状态
        /// </summary>
        private void UpdateAllVisualization()
        {
            // 更新 BeeSimulation 的可视化
            if (beeSimulation != null)
            {
                beeSimulation.showDebugRays = enableAllVisualization;
                beeSimulation.showForces = enableAllVisualization;
                beeSimulation.showNoiseForce = enableAllVisualization;
            }

            // 更新 BeeSimulationManager 的轨迹绘制
            if (beeSimulationManager != null)
            {
                beeSimulationManager.drawTrajectory = enableAllVisualization;

                // 如果关闭可视化，立即清除轨迹
                if (!enableAllVisualization && beeSimulationManager.trajectoryRenderer != null)
                {
                    beeSimulationManager.trajectoryRenderer.positionCount = 0;
                }
            }

            // 注意：BeeTargetController 的可视化主要在 OnDrawGizmosSelected 中
            // 这在编辑器中有效，但在运行时需要其他处理方式
        }

        /// <summary>
        /// 更新按钮文本
        /// </summary>
        private void UpdateButtonText()
        {
            if (buttonText != null)
            {
                buttonText.text = enableAllVisualization ? "Turn off visualization" : "Turn on visualization";
            }
        }

        /// <summary>
        /// 强制开启所有可视化
        /// </summary>
        public void EnableAllVisualization()
        {
            enableAllVisualization = true;
            UpdateAllVisualization();
            UpdateButtonText();
        }

        /// <summary>
        /// 强制关闭所有可视化
        /// </summary>
        public void DisableAllVisualization()
        {
            enableAllVisualization = false;
            UpdateAllVisualization();
            UpdateButtonText();
        }

        // 在编辑器中也可调用
        [ContextMenu("切换可视化")]
        private void ToggleInEditor()
        {
            ToggleAllVisualization();
        }
    }
}
