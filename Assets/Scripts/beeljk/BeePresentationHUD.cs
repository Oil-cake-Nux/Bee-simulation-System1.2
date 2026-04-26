using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ljk
{
    public class BeePresentationHUD : MonoBehaviour
    {
        private enum PresentationFlightMode
        {
            Manual = 0,
            Auto = 1,
            Demo = 2
        }

        public KeyCode toggleHudKey = KeyCode.F1;
        public KeyCode cycleModeKey = KeyCode.Tab;
        public KeyCode toggleVisualizationKey = KeyCode.F2;
        public KeyCode clearTrajectoryKey = KeyCode.BackQuote;
        public bool showHud = true;

        private BeeSimulation beeSimulation;
        private BeeTargetController beeTargetController;
        private BeeSimulationManager beeSimulationManager;
        private VisualizationManager visualizationManager;
        private Canvas hudCanvas;
        private Text statusText;
        private Text hintText;
        private PresentationFlightMode currentMode = PresentationFlightMode.Manual;
        private bool demoEnhancementEnabled = false;
        private readonly StringBuilder builder = new StringBuilder(512);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindObjectOfType<BeePresentationHUD>() != null)
            {
                return;
            }

            GameObject hudRoot = new GameObject("Bee Presentation HUD");
            hudRoot.AddComponent<BeePresentationHUD>();
        }

        private void Awake()
        {
            BeePresentationHUD[] existingHud = FindObjectsOfType<BeePresentationHUD>();
            if (existingHud.Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            CreateHud();
            ResolveReferences();
            SyncModeFromSimulation();
            ApplyPresentationMode(currentMode, false);
        }

        private void OnEnable()
        {
            ResolveReferences();
            RefreshHudVisibility();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleHudKey))
            {
                showHud = !showHud;
                RefreshHudVisibility();
            }

            if (Input.GetKeyDown(cycleModeKey))
            {
                CycleMode();
            }

            if (Input.GetKeyDown(toggleVisualizationKey))
            {
                ToggleVisualization();
            }

            if (Input.GetKeyDown(clearTrajectoryKey) && beeSimulationManager != null)
            {
                beeSimulationManager.ClearTrajectory();
            }

            ResolveReferences();
            RefreshHud();
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

            if (visualizationManager == null)
            {
                visualizationManager = FindObjectOfType<VisualizationManager>();
            }
        }

        private void CreateHud()
        {
            hudCanvas = GetComponentInChildren<Canvas>();
            if (hudCanvas != null)
            {
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            GameObject canvasObject = new GameObject("PresentationCanvas");
            canvasObject.transform.SetParent(transform, false);
            hudCanvas = canvasObject.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject panelObject = new GameObject("StatusPanel");
            panelObject.transform.SetParent(canvasObject.transform, false);
            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.05f, 0.08f, 0.12f, 0.72f);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(18f, -18f);
            panelRect.sizeDelta = new Vector2(360f, 220f);

            statusText = CreateText("StatusText", panelObject.transform, font, 19, TextAnchor.UpperLeft);
            RectTransform statusRect = statusText.rectTransform;
            statusRect.anchorMin = new Vector2(0f, 0f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.offsetMin = new Vector2(14f, 44f);
            statusRect.offsetMax = new Vector2(-14f, -14f);

            hintText = CreateText("HintText", panelObject.transform, font, 14, TextAnchor.LowerLeft);
            hintText.color = new Color(1f, 0.95f, 0.75f, 0.95f);
            RectTransform hintRect = hintText.rectTransform;
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.sizeDelta = new Vector2(0f, 34f);
            hintRect.anchoredPosition = new Vector2(0f, 8f);
        }

        private Text CreateText(string objectName, Transform parent, Font font, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);

            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = Color.white;

            RectTransform rectTransform = text.rectTransform;
            rectTransform.localScale = Vector3.one;
            return text;
        }

        private void CycleMode()
        {
            currentMode = (PresentationFlightMode)(((int)currentMode + 1) % 3);
            ApplyPresentationMode(currentMode, true);
        }

        private void SyncModeFromSimulation()
        {
            if (beeSimulation == null)
            {
                currentMode = PresentationFlightMode.Manual;
                return;
            }

            currentMode = beeSimulation.UsesAutopilot ? PresentationFlightMode.Auto : PresentationFlightMode.Manual;
        }

        private void ApplyPresentationMode(PresentationFlightMode mode, bool logChange)
        {
            currentMode = mode;

            if (beeSimulation != null)
            {
                beeSimulation.controlMode = mode == PresentationFlightMode.Manual
                    ? BeeSimulation.FlightControlMode.Player
                    : BeeSimulation.FlightControlMode.AutopilotTargets;
            }

            demoEnhancementEnabled = mode == PresentationFlightMode.Demo;

            if (beeSimulationManager != null)
            {
                beeSimulationManager.drawTrajectory = true;
                beeSimulationManager.ApplyPresentationPreset(demoEnhancementEnabled);
            }

            if (visualizationManager != null)
            {
                if (demoEnhancementEnabled)
                {
                    visualizationManager.EnableAllVisualization();
                }
            }
            else if (beeSimulation != null && demoEnhancementEnabled)
            {
                beeSimulation.showDebugRays = true;
                beeSimulation.showForces = true;
                beeSimulation.showNoiseForce = true;
            }

            if (logChange)
            {
                Debug.Log("飞行演示模式切换为: " + GetModeLabel(mode));
            }
        }

        private void ToggleVisualization()
        {
            if (visualizationManager != null)
            {
                visualizationManager.ToggleAllVisualization();
                return;
            }

            if (beeSimulation == null)
            {
                return;
            }

            bool enabled = !beeSimulation.showDebugRays;
            beeSimulation.showDebugRays = enabled;
            beeSimulation.showForces = enabled;
            beeSimulation.showNoiseForce = enabled;

            if (beeSimulationManager != null)
            {
                beeSimulationManager.drawTrajectory = enabled;
                if (!enabled)
                {
                    beeSimulationManager.ClearTrajectory();
                }
            }
        }

        private void RefreshHudVisibility()
        {
            if (hudCanvas != null)
            {
                hudCanvas.enabled = showHud;
            }
        }

        private void RefreshHud()
        {
            if (statusText == null || beeSimulation == null)
            {
                return;
            }

            builder.Length = 0;
            builder.AppendLine("蜜蜂飞行状态");
            builder.AppendLine("速度: " + beeSimulation.CurrentSpeed.ToString("F2") + " m/s");
            builder.AppendLine("高度: " + beeSimulation.CurrentAltitude.ToString("F2") + " m");

            if (beeSimulation.DistanceToTarget >= 0f)
            {
                builder.AppendLine("目标距离: " + beeSimulation.DistanceToTarget.ToString("F2") + " m");
            }
            else
            {
                builder.AppendLine("目标距离: --");
            }

            builder.AppendLine("飞行模式: " + GetModeLabel(currentMode));
            builder.AppendLine("采集状态: " + GetCollectionStatus());
            builder.AppendLine("振翅频率: " + beeSimulation.oscillationFrequency.ToString("F2") + " Hz");
            builder.AppendLine("扰动强度: " + beeSimulation.curlNoiseWeight.ToString("F2"));
            builder.AppendLine("姿态 Pitch/Roll: " + beeSimulation.CurrentVisualPitch.ToString("F1") + "° / " + beeSimulation.CurrentVisualRoll.ToString("F1") + "°");

            if (beeSimulationManager != null)
            {
                builder.AppendLine("轨迹点数: " + beeSimulationManager.TrajectoryPointCount);
            }

            statusText.text = builder.ToString();
            hintText.text = "F1 HUD  |  Tab 切换手动/自动/演示  |  F2 切换可视化  |  ` 清空轨迹";
        }

        private string GetCollectionStatus()
        {
            if (beeTargetController == null)
            {
                return "巡航";
            }

            string status = beeTargetController.CurrentStateLabel;
            if (beeTargetController.CollectedTargetCount > 0)
            {
                status += "  已采集: " + beeTargetController.CollectedTargetCount;
            }

            return status;
        }

        private string GetModeLabel(PresentationFlightMode mode)
        {
            switch (mode)
            {
                case PresentationFlightMode.Auto:
                    return "自动飞行";
                case PresentationFlightMode.Demo:
                    return "演示模式";
                default:
                    return "手动飞行";
            }
        }
    }
}
