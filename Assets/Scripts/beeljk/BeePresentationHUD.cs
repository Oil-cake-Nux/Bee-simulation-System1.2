using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
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
        public KeyCode toggleForcePanelKey = KeyCode.F3;
        public KeyCode clearTrajectoryKey = KeyCode.BackQuote;
        public bool showHud = true;
        public bool showForcePanel = true;

        private BeeSimulation beeSimulation;
        private BeeTargetController beeTargetController;
        private BeeSimulationManager beeSimulationManager;
        private VisualizationManager visualizationManager;
        private Canvas hudCanvas;
        private Text statusText;
        private Text hintText;
        private GameObject statusPanelObject;
        private GameObject forcePanelObject;
        private GameObject quickActionPanelObject;
        private GameObject helpPanelObject;
        private GameObject activeForcePanelBody;
        private PresentationFlightMode currentMode = PresentationFlightMode.Manual;
        private bool demoEnhancementEnabled = false;
        private bool isGameplayScene = false;
        private float timeScaleBeforeHelp = 1f;
        private const string MainMenuSceneName = "MainMenu";
        private const string GameplaySceneName = "Demo";
        private readonly StringBuilder builder = new StringBuilder(512);
        private readonly List<RuntimeSliderBinding> forceSliders = new List<RuntimeSliderBinding>();
        private readonly List<RuntimeToggleBinding> forceToggles = new List<RuntimeToggleBinding>();
        private readonly List<GameObject> forcePanelBodies = new List<GameObject>();

        private sealed class RuntimeSliderBinding
        {
            public Slider slider;
            public Text valueText;
            public Func<float> getter;
            public Action<float> setter;
            public string valueFormat;
            public bool isUpdating;
        }

        private sealed class RuntimeToggleBinding
        {
            public Toggle toggle;
            public Func<bool> getter;
            public Action<bool> setter;
            public bool isUpdating;
        }

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
            HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Update()
        {
            if (!isGameplayScene)
            {
                return;
            }

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

            if (Input.GetKeyDown(toggleForcePanelKey))
            {
                showForcePanel = !showForcePanel;
                RefreshForcePanelVisibility();
            }

            if (Input.GetKeyDown(clearTrajectoryKey) && beeSimulationManager != null)
            {
                beeSimulationManager.ClearTrajectory();
            }

            ResolveReferences();
            RefreshHud();
            RefreshForceControlValues();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            beeSimulation = null;
            beeTargetController = null;
            beeSimulationManager = null;
            visualizationManager = null;

            ResolveReferences();
            isGameplayScene = beeSimulation != null;
            SetHelpPanelVisible(false, false);
            RefreshHudVisibility();

            if (!isGameplayScene)
            {
                return;
            }

            SyncModeFromSimulation();
            ApplyPresentationMode(currentMode, false);
            SetActiveForcePanel(null);
            if (statusPanelObject != null)
            {
                statusPanelObject.SetActive(false);
            }
            RefreshHud();
            RefreshForceControlValues();
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

            Font font = LoadBuiltinFont();
            if (font == null)
            {
                Debug.LogError("BeePresentationHUD could not load a built-in UI font.");
                return;
            }

            GameObject canvasObject = new GameObject("PresentationCanvas");
            canvasObject.transform.SetParent(transform, false);
            hudCanvas = canvasObject.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            statusPanelObject = new GameObject("StatusPanel");
            statusPanelObject.transform.SetParent(canvasObject.transform, false);
            Image panelImage = statusPanelObject.AddComponent<Image>();
            panelImage.color = new Color(0.03f, 0.04f, 0.04f, 0.62f);

            RectTransform panelRect = statusPanelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0f, 0f);
            panelRect.sizeDelta = new Vector2(200f, 0f);

            statusText = CreateText("StatusText", statusPanelObject.transform, font, 14, TextAnchor.UpperLeft);
            RectTransform statusRect = statusText.rectTransform;
            statusRect.anchorMin = new Vector2(0f, 0f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.offsetMin = new Vector2(12f, 18f);
            statusRect.offsetMax = new Vector2(-12f, -18f);
            statusText.verticalOverflow = VerticalWrapMode.Truncate;

            statusPanelObject.SetActive(false);

            CreateQuickActionPanel(canvasObject.transform, font);
            CreateForceControlPanel(canvasObject.transform, font);
            CreateHelpPanel(canvasObject.transform, font);
            RefreshForcePanelVisibility();
        }

        private static Font LoadBuiltinFont()
        {
            string[] fontCandidates = { "LegacyRuntime.ttf", "Arial.ttf" };
            for (int i = 0; i < fontCandidates.Length; i++)
            {
                try
                {
                    Font font = Resources.GetBuiltinResource<Font>(fontCandidates[i]);
                    if (font != null)
                    {
                        return font;
                    }
                }
                catch (System.ArgumentException)
                {
                }
            }

            return null;
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

        private void CreateNavigationButtonRow(Transform parent, Font font)
        {
            GameObject rowObject = new GameObject("NavigationButtons");
            rowObject.transform.SetParent(parent, false);

            RectTransform rowRect = rowObject.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 0f);
            rowRect.anchorMax = new Vector2(1f, 0f);
            rowRect.pivot = new Vector2(0.5f, 0f);
            rowRect.offsetMin = new Vector2(8f, 8f);
            rowRect.offsetMax = new Vector2(-8f, 38f);

            HorizontalLayoutGroup layout = rowObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            CreateNavigationButton(rowObject.transform, font, "返回主界面", ReturnToMainMenu, 76f, 12);
            CreateNavigationButton(rowObject.transform, font, "重新开始", RestartExploration, 66f, 12);
            CreateNavigationButton(rowObject.transform, font, "?", ShowHelpPanel, 28f, 18);
        }

        private void CreateQuickActionPanel(Transform canvasTransform, Font font)
        {
            quickActionPanelObject = new GameObject("QuickActionPanel");
            quickActionPanelObject.transform.SetParent(canvasTransform, false);

            Image panelImage = quickActionPanelObject.AddComponent<Image>();
            panelImage.color = new Color(0.03f, 0.04f, 0.04f, 0.64f);

            RectTransform panelRect = quickActionPanelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(1f, 0f);
            panelRect.sizeDelta = new Vector2(250f, 86f);
            panelRect.anchoredPosition = new Vector2(-10f, 10f);

            hintText = CreateText("HintText", quickActionPanelObject.transform, font, 14, TextAnchor.LowerLeft);
            hintText.color = new Color(1f, 0.95f, 0.75f, 0.95f);
            RectTransform hintRect = hintText.rectTransform;
            hintRect.anchorMin = Vector2.zero;
            hintRect.anchorMax = Vector2.one;
            hintRect.offsetMin = new Vector2(8f, 42f);
            hintRect.offsetMax = new Vector2(-8f, -6f);
            hintText.verticalOverflow = VerticalWrapMode.Truncate;

            CreateNavigationButtonRow(quickActionPanelObject.transform, font);
        }

        private void CreateNavigationButton(Transform parent, Font font, string label, UnityEngine.Events.UnityAction action, float preferredWidth, int fontSize)
        {
            GameObject buttonObject = CreateButton(label + "Button", parent, font, label);
            LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
            layoutElement.minWidth = preferredWidth;
            layoutElement.preferredWidth = preferredWidth;
            layoutElement.minHeight = 28f;
            layoutElement.preferredHeight = 28f;

            Text labelText = buttonObject.GetComponentInChildren<Text>();
            labelText.fontSize = fontSize;
            labelText.alignment = TextAnchor.MiddleCenter;
            RectTransform labelRect = labelText.rectTransform;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            buttonObject.GetComponent<Button>().onClick.AddListener(action);
        }

        private void CreateHelpPanel(Transform canvasTransform, Font font)
        {
            helpPanelObject = new GameObject("OperationHelpPanel");
            helpPanelObject.transform.SetParent(canvasTransform, false);

            Image panelImage = helpPanelObject.AddComponent<Image>();
            panelImage.color = new Color(0.02f, 0.03f, 0.03f, 0.92f);

            RectTransform panelRect = helpPanelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(760f, 610f);
            panelRect.anchoredPosition = Vector2.zero;

            Text titleText = CreateText("HelpTitle", helpPanelObject.transform, font, 24, TextAnchor.MiddleCenter);
            titleText.text = "操作说明";
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(1f, 0.92f, 0.55f, 1f);
            RectTransform titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -18f);
            titleRect.sizeDelta = new Vector2(-40f, 42f);

            Text bodyText = CreateText("HelpBody", helpPanelObject.transform, font, 12, TextAnchor.UpperLeft);
            bodyText.text =
                "基础飞行：W / S 前进与减速，A / D 转向\n" +
                "高度控制：空格键上升，左 Ctrl 下降，左 Shift 加速\n" +
                "模式切换：Tab 切换手动飞行、自动飞行、演示模式\n" +
                "显示控制：F1 显示/隐藏状态栏，F2 切换可视化，F3 打开/收起力面板\n" +
                "轨迹控制：` 清空飞行轨迹\n" +
                "说明面板：点击右侧 ? 会暂停游戏并显示本说明\n\n" +
                "ForceControl 控制说明：\n" +
                "最大推力：限制合力上限，数值越大加速和修正越强\n" +
                "最大速度：限制自动/手动飞行的水平速度上限\n" +
                "移动速度：自动目标趋近时的期望飞行速度\n" +
                "振翅权重：控制振翅扰动力在合力中的占比\n" +
                "振翅频率：控制微扰动和姿态波动频率\n" +
                "目标高度：无目标时自动飞行保持的基础高度\n" +
                "高度阻尼：高度误差修正强度，过高会使上下修正更急\n" +
                "目标吸引：目标引导力权重，越高越积极飞向目标\n" +
                "减速半径：接近目标时开始减速的距离范围\n" +
                "障碍权重：避障力权重，越高越明显绕开树木等障碍\n" +
                "地面权重：低空时向上抬升的地面规避强度\n" +
                "扰动权重：Curl Noise 环境扰动对飞行轨迹的影响强度\n" +
                "感知范围：避障射线检测障碍的最远距离\n" +
                "绕行保持：选定左/右绕行方向后的保持时间，减少左右抖动\n" +
                "目标削弱：避障时临时削弱目标吸引，避免被目标拉回障碍\n" +
                "受力显示：勾选后允许显示对应力向量，未触发或向量为零时不会画线\n\n" +
                "受力线条颜色：\n" +
                "蓝色：速度向量  白色：玩家控制力  粉色：目标引导力\n" +
                "黄色：避障力  紫色：地面规避力  橙色：振翅扰动力\n" +
                "青色：噪声扰动力/噪声探针\n" +
                "绿色：合力";
            bodyText.lineSpacing = 1.0f;
            RectTransform bodyRect = bodyText.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(34f, 62f);
            bodyRect.offsetMax = new Vector2(-34f, -66f);

            GameObject closeButtonObject = CreateButton("CloseHelpButton", helpPanelObject.transform, font, "继续探索");
            RectTransform closeRect = closeButtonObject.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.sizeDelta = new Vector2(132f, 34f);
            closeRect.anchoredPosition = new Vector2(0f, 22f);

            Text closeText = closeButtonObject.GetComponentInChildren<Text>();
            closeText.alignment = TextAnchor.MiddleCenter;
            closeText.fontSize = 15;
            closeText.rectTransform.offsetMin = Vector2.zero;
            closeText.rectTransform.offsetMax = Vector2.zero;
            closeButtonObject.GetComponent<Button>().onClick.AddListener(HideHelpPanel);

            helpPanelObject.SetActive(false);
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private void CreateForceControlPanel(Transform canvasTransform, Font font)
        {
            forcePanelObject = new GameObject("ForceControlPanel");
            forcePanelObject.transform.SetParent(canvasTransform, false);

            RectTransform panelRect = forcePanelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0f, 0f);
            panelRect.sizeDelta = new Vector2(195f, 0f);

            GameObject tabRowObject = new GameObject("ForcePanelTabs");
            tabRowObject.transform.SetParent(forcePanelObject.transform, false);
            RectTransform tabRowRect = tabRowObject.AddComponent<RectTransform>();
            tabRowRect.anchorMin = new Vector2(0f, 1f);
            tabRowRect.anchorMax = new Vector2(1f, 1f);
            tabRowRect.pivot = new Vector2(0.5f, 1f);
            tabRowRect.offsetMin = new Vector2(8f, -109f);
            tabRowRect.offsetMax = new Vector2(-8f, -12f);

            GridLayoutGroup tabLayout = tabRowObject.AddComponent<GridLayoutGroup>();
            tabLayout.cellSize = new Vector2(84f, 28f);
            tabLayout.spacing = new Vector2(5f, 5f);
            tabLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            tabLayout.constraintCount = 2;
            tabLayout.childAlignment = TextAnchor.UpperCenter;

            GameObject flightPanel = CreatePanelSection(forcePanelObject.transform, font, "飞行动力", section =>
            {
                CreateForceSlider(section, font, "最大推力", 1f, 20f, () => beeSimulation.maxForce, value => beeSimulation.maxForce = value, "F1");
                CreateForceSlider(section, font, "最大速度", 0.5f, 8f, () => beeSimulation.maxSpeed, value => beeSimulation.maxSpeed = value, "F1");
                CreateForceSlider(section, font, "移动速度", 0.5f, 8f, () => beeSimulation.moveSpeed, value => beeSimulation.moveSpeed = value, "F1");
                CreateForceSlider(section, font, "振翅权重", 0f, 4f, () => beeSimulation.oscillationForceWeight, value => beeSimulation.oscillationForceWeight = value, "F2");
                CreateForceSlider(section, font, "振翅频率", 0.2f, 8f, () => beeSimulation.oscillationFrequency, value => beeSimulation.oscillationFrequency = value, "F2");
            });

            GameObject targetPanel = CreatePanelSection(forcePanelObject.transform, font, "高度与目标", section =>
            {
                CreateForceSlider(section, font, "目标高度", 1f, 20f, () => beeSimulation.targetHeight, value => beeSimulation.targetHeight = value, "F1");
                CreateForceSlider(section, font, "高度阻尼", 0.05f, 2f, () => beeSimulation.heightDampingFactor, value => beeSimulation.heightDampingFactor = value, "F2");
                CreateForceSlider(section, font, "目标吸引", 0f, 5f, () => beeSimulation.targetAttractionWeight, value => beeSimulation.targetAttractionWeight = value, "F2");
                CreateForceSlider(section, font, "减速半径", 0.5f, 8f, () => beeSimulation.slowingRadius, value => beeSimulation.slowingRadius = value, "F1");
            });

            GameObject avoidancePanel = CreatePanelSection(forcePanelObject.transform, font, "避障与扰动", section =>
            {
                CreateForceSlider(section, font, "障碍权重", 0f, 12f, () => beeSimulation.obstacleAvoidanceWeight, value => beeSimulation.obstacleAvoidanceWeight = value, "F1");
                CreateForceSlider(section, font, "地面权重", 0f, 16f, () => beeSimulation.groundAvoidanceWeight, value => beeSimulation.groundAvoidanceWeight = value, "F1");
                CreateForceSlider(section, font, "扰动权重", 0f, 5f, () => beeSimulation.curlNoiseWeight, value => beeSimulation.curlNoiseWeight = value, "F2");
                CreateForceSlider(section, font, "感知范围", 1f, 12f, () => beeSimulation.visualRange, value => beeSimulation.visualRange = value, "F1");
                CreateForceSlider(section, font, "绕行保持", 0.1f, 2f, () => beeSimulation.avoidanceSideHoldTime, value => beeSimulation.avoidanceSideHoldTime = value, "F2");
                CreateForceSlider(section, font, "目标削弱", 0f, 0.95f, () => beeSimulation.avoidanceGuidanceReduction, value => beeSimulation.avoidanceGuidanceReduction = value, "F2");
            });

            GameObject vectorPanel = CreatePanelSection(forcePanelObject.transform, font, "受力显示", section =>
            {
                CreateForceToggle(section, font, "速度向量", () => beeSimulation.showVelocityVector, value => beeSimulation.showVelocityVector = value);
                CreateForceToggle(section, font, "玩家控制力", () => beeSimulation.showPlayerControlVector, value => beeSimulation.showPlayerControlVector = value);
                CreateForceToggle(section, font, "目标引导力", () => beeSimulation.showGuidanceVector, value => beeSimulation.showGuidanceVector = value);
                CreateForceToggle(section, font, "避障力", () => beeSimulation.showObstacleAvoidanceVector, value => beeSimulation.showObstacleAvoidanceVector = value);
                CreateForceToggle(section, font, "地面规避力", () => beeSimulation.showGroundAvoidanceVector, value => beeSimulation.showGroundAvoidanceVector = value);
                CreateForceToggle(section, font, "振翅扰动力", () => beeSimulation.showOscillationVector, value => beeSimulation.showOscillationVector = value);
                CreateForceToggle(section, font, "噪声扰动力", () => beeSimulation.showNoiseVector, value => beeSimulation.showNoiseVector = value);
                CreateForceToggle(section, font, "合力", () => beeSimulation.showTotalForceVector, value => beeSimulation.showTotalForceVector = value);
                CreateForceToggle(section, font, "噪声探针", () => beeSimulation.showNoiseProbeVectors, value => beeSimulation.showNoiseProbeVectors = value);
            });

            CreateStatusTabButton(tabRowObject.transform, font);
            CreatePanelTabButton(tabRowObject.transform, font, "飞行动力", flightPanel);
            CreatePanelTabButton(tabRowObject.transform, font, "高度目标", targetPanel);
            CreatePanelTabButton(tabRowObject.transform, font, "避障扰动", avoidancePanel);
            CreatePanelTabButton(tabRowObject.transform, font, "受力显示", vectorPanel);
            SetActiveForcePanel(null);
        }

        private GameObject CreateButton(string objectName, Transform parent, Font font, string label)
        {
            GameObject buttonObject = new GameObject(objectName);
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.12f, 0.18f, 0.22f, 0.95f);

            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.18f, 0.29f, 0.35f, 1f);
            colors.pressedColor = new Color(0.09f, 0.14f, 0.17f, 1f);
            button.colors = colors;

            Text text = CreateText("Label", buttonObject.transform, font, 13, TextAnchor.MiddleLeft);
            text.text = label;
            text.color = new Color(0.96f, 0.98f, 1f, 1f);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 0f);
            textRect.offsetMax = new Vector2(-10f, 0f);
            text.raycastTarget = false;

            return buttonObject;
        }

        private void CreatePanelTabButton(Transform parent, Font font, string label, GameObject panelBody)
        {
            GameObject buttonObject = CreateButton(label + "Tab", parent, font, label);
            Text labelText = buttonObject.GetComponentInChildren<Text>();
            labelText.fontSize = 12;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.rectTransform.offsetMin = Vector2.zero;
            labelText.rectTransform.offsetMax = Vector2.zero;
            buttonObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                SetActiveForcePanel(activeForcePanelBody == panelBody ? null : panelBody);
            });
        }

        private void CreateStatusTabButton(Transform parent, Font font)
        {
            GameObject buttonObject = CreateButton("飞行状态Tab", parent, font, "飞行状态");
            Text labelText = buttonObject.GetComponentInChildren<Text>();
            labelText.fontSize = 12;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.rectTransform.offsetMin = Vector2.zero;
            labelText.rectTransform.offsetMax = Vector2.zero;
            buttonObject.GetComponent<Button>().onClick.AddListener(ToggleStatusPanel);
        }

        private GameObject CreatePanelSection(Transform parent, Font font, string title, Action<Transform> buildContent)
        {
            GameObject bodyObject = new GameObject(title + " Panel");
            bodyObject.transform.SetParent(parent, false);

            Image bodyImage = bodyObject.AddComponent<Image>();
            bodyImage.color = new Color(0.03f, 0.04f, 0.04f, 0.64f);

            RectTransform bodyRect = bodyObject.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(8f, 14f);
            bodyRect.offsetMax = new Vector2(-8f, -119f);

            VerticalLayoutGroup bodyLayout = bodyObject.AddComponent<VerticalLayoutGroup>();
            bodyLayout.spacing = 5f;
            bodyLayout.padding = new RectOffset(10, 10, 10, 10);
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = false;

            Text titleText = CreateText(title + " Title", bodyObject.transform, font, 15, TextAnchor.MiddleLeft);
            titleText.text = title;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(1f, 0.92f, 0.55f, 1f);
            LayoutElement titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
            titleLayout.minHeight = 26f;
            titleLayout.preferredHeight = 26f;

            buildContent(bodyObject.transform);
            bodyObject.SetActive(false);
            forcePanelBodies.Add(bodyObject);
            return bodyObject;
        }

        private void SetActiveForcePanel(GameObject panelBody)
        {
            activeForcePanelBody = panelBody;

            for (int i = 0; i < forcePanelBodies.Count; i++)
            {
                if (forcePanelBodies[i] != null)
                {
                    forcePanelBodies[i].SetActive(forcePanelBodies[i] == panelBody);
                }
            }

            if (panelBody != null && panelBody != statusPanelObject)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(panelBody.GetComponent<RectTransform>());
            }
        }

        private void ToggleStatusPanel()
        {
            if (statusPanelObject == null)
            {
                return;
            }

            statusPanelObject.SetActive(!statusPanelObject.activeSelf);
        }

        private void CreateFoldoutSection(Transform parent, Font font, string title, bool expanded, Action<Transform> buildContent)
        {
            GameObject sectionObject = new GameObject(title + " Section");
            sectionObject.transform.SetParent(parent, false);

            VerticalLayoutGroup sectionLayout = sectionObject.AddComponent<VerticalLayoutGroup>();
            sectionLayout.spacing = 4f;
            sectionLayout.childControlWidth = true;
            sectionLayout.childControlHeight = true;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.childForceExpandHeight = false;

            LayoutElement sectionLayoutElement = sectionObject.AddComponent<LayoutElement>();
            sectionLayoutElement.minHeight = 32f;

            GameObject headerObject = CreateButton(title + " Header", sectionObject.transform, font, (expanded ? "v  " : ">  ") + title);
            LayoutElement headerLayout = headerObject.AddComponent<LayoutElement>();
            headerLayout.minHeight = 26f;
            headerLayout.preferredHeight = 26f;

            GameObject bodyObject = new GameObject(title + " Body");
            bodyObject.transform.SetParent(sectionObject.transform, false);
            VerticalLayoutGroup bodyLayout = bodyObject.AddComponent<VerticalLayoutGroup>();
            bodyLayout.spacing = 3f;
            bodyLayout.padding = new RectOffset(4, 4, 0, 0);
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = false;

            ContentSizeFitter bodyFitter = bodyObject.AddComponent<ContentSizeFitter>();
            bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Text headerText = headerObject.GetComponentInChildren<Text>();
            headerObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                bool nextVisible = !bodyObject.activeSelf;
                bodyObject.SetActive(nextVisible);
                headerText.text = (nextVisible ? "v  " : ">  ") + title;
            });

            buildContent(bodyObject.transform);
            bodyObject.SetActive(expanded);
        }

        private void CreateForceSlider(Transform parent, Font font, string label, float min, float max, Func<float> getter, Action<float> setter, string valueFormat)
        {
            GameObject rowObject = new GameObject(label + " Row");
            rowObject.transform.SetParent(parent, false);

            VerticalLayoutGroup rowLayout = rowObject.AddComponent<VerticalLayoutGroup>();
            rowLayout.spacing = 2f;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;

            LayoutElement rowElement = rowObject.AddComponent<LayoutElement>();
            rowElement.minHeight = 42f;
            rowElement.preferredHeight = 42f;

            GameObject labelRow = new GameObject("LabelRow");
            labelRow.transform.SetParent(rowObject.transform, false);
            HorizontalLayoutGroup labelLayout = labelRow.AddComponent<HorizontalLayoutGroup>();
            labelLayout.childControlWidth = true;
            labelLayout.childForceExpandWidth = true;

            Text labelText = CreateText("Name", labelRow.transform, font, 12, TextAnchor.MiddleLeft);
            labelText.text = label;
            LayoutElement labelElement = labelText.gameObject.AddComponent<LayoutElement>();
            labelElement.preferredWidth = 122f;

            Text valueText = CreateText("Value", labelRow.transform, font, 12, TextAnchor.MiddleRight);
            valueText.color = new Color(0.85f, 1f, 0.9f, 1f);
            LayoutElement valueElement = valueText.gameObject.AddComponent<LayoutElement>();
            valueElement.preferredWidth = 48f;

            Slider slider = CreateSlider(rowObject.transform, min, max);

            RuntimeSliderBinding binding = new RuntimeSliderBinding
            {
                slider = slider,
                valueText = valueText,
                getter = getter,
                setter = setter,
                valueFormat = valueFormat
            };

            slider.onValueChanged.AddListener(value =>
            {
                if (binding.isUpdating || beeSimulation == null)
                {
                    return;
                }

                setter(value);
                valueText.text = value.ToString(valueFormat);
            });

            forceSliders.Add(binding);
        }

        private void CreateForceToggle(Transform parent, Font font, string label, Func<bool> getter, Action<bool> setter)
        {
            GameObject rowObject = new GameObject(label + " ToggleRow");
            rowObject.transform.SetParent(parent, false);

            HorizontalLayoutGroup rowLayout = rowObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.padding = new RectOffset(4, 4, 2, 2);
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;

            LayoutElement rowElement = rowObject.AddComponent<LayoutElement>();
            rowElement.minHeight = 28f;
            rowElement.preferredHeight = 28f;

            GameObject toggleObject = new GameObject("Toggle");
            toggleObject.transform.SetParent(rowObject.transform, false);
            Toggle toggle = toggleObject.AddComponent<Toggle>();
            LayoutElement toggleLayout = toggleObject.AddComponent<LayoutElement>();
            toggleLayout.minWidth = 22f;
            toggleLayout.preferredWidth = 22f;
            toggleLayout.minHeight = 22f;
            toggleLayout.preferredHeight = 22f;

            GameObject backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(toggleObject.transform, false);
            Image backgroundImage = backgroundObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.14f, 0.17f, 0.19f, 1f);
            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
            backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
            backgroundRect.pivot = new Vector2(0.5f, 0.5f);
            backgroundRect.sizeDelta = new Vector2(18f, 18f);

            GameObject checkmarkObject = new GameObject("Checkmark");
            checkmarkObject.transform.SetParent(backgroundObject.transform, false);
            Image checkmarkImage = checkmarkObject.AddComponent<Image>();
            checkmarkImage.color = new Color(0.95f, 0.68f, 0.22f, 1f);
            RectTransform checkmarkRect = checkmarkObject.GetComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0.5f, 0.5f);
            checkmarkRect.anchorMax = new Vector2(0.5f, 0.5f);
            checkmarkRect.pivot = new Vector2(0.5f, 0.5f);
            checkmarkRect.sizeDelta = new Vector2(10f, 10f);

            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkmarkImage;

            Text labelText = CreateText("Label", rowObject.transform, font, 12, TextAnchor.MiddleLeft);
            labelText.text = label;
            LayoutElement labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 128f;

            RuntimeToggleBinding binding = new RuntimeToggleBinding
            {
                toggle = toggle,
                getter = getter,
                setter = setter
            };

            toggle.onValueChanged.AddListener(value =>
            {
                if (binding.isUpdating || beeSimulation == null)
                {
                    return;
                }

                setter(value);
            });

            forceToggles.Add(binding);
        }

        private Slider CreateSlider(Transform parent, float min, float max)
        {
            GameObject sliderObject = new GameObject("Slider");
            sliderObject.transform.SetParent(parent, false);

            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;

            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(0f, 14f);
            LayoutElement sliderLayout = sliderObject.AddComponent<LayoutElement>();
            sliderLayout.minHeight = 14f;
            sliderLayout.preferredHeight = 14f;

            GameObject backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(sliderObject.transform, false);
            Image backgroundImage = backgroundObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.18f, 0.2f, 0.22f, 1f);
            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.35f);
            backgroundRect.anchorMax = new Vector2(1f, 0.65f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            GameObject fillAreaObject = new GameObject("Fill Area");
            fillAreaObject.transform.SetParent(sliderObject.transform, false);
            RectTransform fillAreaRect = fillAreaObject.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = new Vector2(6f, 0f);
            fillAreaRect.offsetMax = new Vector2(-6f, 0f);

            GameObject fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(fillAreaObject.transform, false);
            Image fillImage = fillObject.AddComponent<Image>();
            fillImage.color = new Color(0.95f, 0.68f, 0.22f, 1f);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            GameObject handleAreaObject = new GameObject("Handle Slide Area");
            handleAreaObject.transform.SetParent(sliderObject.transform, false);
            RectTransform handleAreaRect = handleAreaObject.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(7f, 0f);
            handleAreaRect.offsetMax = new Vector2(-7f, 0f);

            GameObject handleObject = new GameObject("Handle");
            handleObject.transform.SetParent(handleAreaObject.transform, false);
            Image handleImage = handleObject.AddComponent<Image>();
            handleImage.color = new Color(1f, 0.95f, 0.75f, 1f);
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(13f, 13f);

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;

            return slider;
        }

        private void ToggleForceControlContent()
        {
            showForcePanel = !showForcePanel;
            RefreshForcePanelVisibility();
        }

        private void RefreshForcePanelVisibility()
        {
            if (forcePanelObject != null)
            {
                forcePanelObject.SetActive(isGameplayScene && showHud && showForcePanel);
            }

            if (quickActionPanelObject != null)
            {
                quickActionPanelObject.SetActive(isGameplayScene && showHud);
            }
        }

        private void RefreshForceControlValues()
        {
            if (beeSimulation == null)
            {
                return;
            }

            for (int i = 0; i < forceSliders.Count; i++)
            {
                RuntimeSliderBinding binding = forceSliders[i];
                if (binding.slider == null || binding.getter == null)
                {
                    continue;
                }

                float value = binding.getter();
                binding.isUpdating = true;
                binding.slider.SetValueWithoutNotify(value);
                binding.isUpdating = false;

                if (binding.valueText != null)
                {
                    binding.valueText.text = value.ToString(binding.valueFormat);
                }
            }

            for (int i = 0; i < forceToggles.Count; i++)
            {
                RuntimeToggleBinding binding = forceToggles[i];
                if (binding.toggle == null || binding.getter == null)
                {
                    continue;
                }

                bool value = binding.getter();
                binding.isUpdating = true;
                binding.toggle.SetIsOnWithoutNotify(value);
                binding.isUpdating = false;
            }
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

        private void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SetHelpPanelVisible(false, false);
            SceneManager.LoadScene(MainMenuSceneName);
        }

        private void RestartExploration()
        {
            Time.timeScale = 1f;
            SetHelpPanelVisible(false, false);
            SceneManager.LoadScene(GameplaySceneName);
        }

        private void ShowHelpPanel()
        {
            SetHelpPanelVisible(true, true);
        }

        private void HideHelpPanel()
        {
            SetHelpPanelVisible(false, true);
        }

        private void SetHelpPanelVisible(bool visible, bool restoreTimeScale)
        {
            if (visible)
            {
                timeScaleBeforeHelp = Time.timeScale;
                Time.timeScale = 0f;

                if (helpPanelObject != null)
                {
                    helpPanelObject.SetActive(true);
                    helpPanelObject.transform.SetAsLastSibling();
                }

                return;
            }

            if (helpPanelObject != null)
            {
                helpPanelObject.SetActive(false);
            }

            if (restoreTimeScale)
            {
                Time.timeScale = timeScaleBeforeHelp;
            }
        }

        private void RefreshHudVisibility()
        {
            bool shouldShowHud = isGameplayScene && showHud;
            if (hudCanvas != null)
            {
                hudCanvas.enabled = shouldShowHud;
            }

            if (forcePanelObject != null)
            {
                forcePanelObject.SetActive(shouldShowHud && showForcePanel);
            }

            if (quickActionPanelObject != null)
            {
                quickActionPanelObject.SetActive(shouldShowHud);
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
            builder.AppendLine("速度: " + beeSimulation.CurrentSpeed.ToString("F2") + " 米/秒");
            builder.AppendLine("高度: " + beeSimulation.CurrentAltitude.ToString("F2") + " 米");

            if (beeSimulation.DistanceToTarget >= 0f)
            {
                builder.AppendLine("目标距离: " + beeSimulation.DistanceToTarget.ToString("F2") + " 米");
            }
            else
            {
                builder.AppendLine("目标距离: --");
            }

            builder.AppendLine("飞行模式: " + GetModeLabel(currentMode));
            builder.AppendLine("采集状态: " + GetCollectionStatus());
            builder.AppendLine("振翅频率: " + beeSimulation.oscillationFrequency.ToString("F2") + " 赫兹");
            builder.AppendLine("扰动强度: " + beeSimulation.curlNoiseWeight.ToString("F2"));
            builder.AppendLine("姿态 俯仰/横滚: " + beeSimulation.CurrentVisualPitch.ToString("F1") + "° / " + beeSimulation.CurrentVisualRoll.ToString("F1") + "°");

            if (beeSimulationManager != null)
            {
                builder.AppendLine("轨迹点数: " + beeSimulationManager.TrajectoryPointCount);
            }

            statusText.text = builder.ToString();
            hintText.text = "F1 状态栏  |  F2 可视化  |  F3 力面板\nTab 模式  |  ` 清空轨迹";
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
