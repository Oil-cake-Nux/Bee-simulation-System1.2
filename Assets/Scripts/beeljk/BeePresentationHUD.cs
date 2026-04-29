using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private GameObject forcePanelObject;
        private GameObject forceContentObject;
        private Text forcePanelHeaderText;
        private PresentationFlightMode currentMode = PresentationFlightMode.Manual;
        private bool demoEnhancementEnabled = false;
        private readonly StringBuilder builder = new StringBuilder(512);
        private readonly List<RuntimeSliderBinding> forceSliders = new List<RuntimeSliderBinding>();

        private sealed class RuntimeSliderBinding
        {
            public Slider slider;
            public Text valueText;
            public Func<float> getter;
            public Action<float> setter;
            public string valueFormat;
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

            GameObject panelObject = new GameObject("StatusPanel");
            panelObject.transform.SetParent(canvasObject.transform, false);
            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.03f, 0.04f, 0.04f, 0.62f);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0f, 0f);
            panelRect.sizeDelta = new Vector2(230f, 0f);

            statusText = CreateText("StatusText", panelObject.transform, font, 14, TextAnchor.UpperLeft);
            RectTransform statusRect = statusText.rectTransform;
            statusRect.anchorMin = new Vector2(0f, 0f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.offsetMin = new Vector2(12f, 52f);
            statusRect.offsetMax = new Vector2(-12f, -18f);

            hintText = CreateText("HintText", panelObject.transform, font, 14, TextAnchor.LowerLeft);
            hintText.color = new Color(1f, 0.95f, 0.75f, 0.95f);
            RectTransform hintRect = hintText.rectTransform;
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.sizeDelta = new Vector2(0f, 44f);
            hintRect.anchoredPosition = new Vector2(0f, 10f);

            CreateForceControlPanel(canvasObject.transform, font);
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

            Image panelImage = forcePanelObject.AddComponent<Image>();
            panelImage.color = new Color(0.03f, 0.04f, 0.04f, 0.64f);

            RectTransform panelRect = forcePanelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0f, 0f);
            panelRect.sizeDelta = new Vector2(195f, 0f);

            GameObject headerButtonObject = CreateButton("ForceHeaderButton", forcePanelObject.transform, font, "Force Controls  [F3]");
            RectTransform headerRect = headerButtonObject.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = new Vector2(0f, -14f);
            headerRect.sizeDelta = new Vector2(-18f, 30f);
            forcePanelHeaderText = headerButtonObject.GetComponentInChildren<Text>();
            headerButtonObject.GetComponent<Button>().onClick.AddListener(ToggleForceControlContent);

            GameObject viewportObject = new GameObject("ForceControlViewport");
            viewportObject.transform.SetParent(forcePanelObject.transform, false);
            Image viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.08f);
            Mask viewportMask = viewportObject.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0f, 0f);
            viewportRect.anchorMax = new Vector2(1f, 1f);
            viewportRect.offsetMin = new Vector2(8f, 14f);
            viewportRect.offsetMax = new Vector2(-8f, -58f);

            GameObject contentObject = new GameObject("ForceControlContent");
            contentObject.transform.SetParent(viewportObject.transform, false);
            forceContentObject = contentObject;

            RectTransform contentRect = contentObject.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup contentLayout = contentObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(2, 2, 6, 6);
            contentLayout.spacing = 6f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter contentFitter = contentObject.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = forcePanelObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            CreateFoldoutSection(contentObject.transform, font, "Flight Force", true, section =>
            {
                CreateForceSlider(section, font, "Max Force", 1f, 20f, () => beeSimulation.maxForce, value => beeSimulation.maxForce = value, "F1");
                CreateForceSlider(section, font, "Max Speed", 0.5f, 8f, () => beeSimulation.maxSpeed, value => beeSimulation.maxSpeed = value, "F1");
                CreateForceSlider(section, font, "Move Speed", 0.5f, 8f, () => beeSimulation.moveSpeed, value => beeSimulation.moveSpeed = value, "F1");
                CreateForceSlider(section, font, "Oscillation Weight", 0f, 4f, () => beeSimulation.oscillationForceWeight, value => beeSimulation.oscillationForceWeight = value, "F2");
                CreateForceSlider(section, font, "Oscillation Hz", 0.2f, 8f, () => beeSimulation.oscillationFrequency, value => beeSimulation.oscillationFrequency = value, "F2");
            });

            CreateFoldoutSection(contentObject.transform, font, "Height & Target", true, section =>
            {
                CreateForceSlider(section, font, "Target Height", 1f, 20f, () => beeSimulation.targetHeight, value => beeSimulation.targetHeight = value, "F1");
                CreateForceSlider(section, font, "Height Damping", 0.05f, 2f, () => beeSimulation.heightDampingFactor, value => beeSimulation.heightDampingFactor = value, "F2");
                CreateForceSlider(section, font, "Target Attraction", 0f, 5f, () => beeSimulation.targetAttractionWeight, value => beeSimulation.targetAttractionWeight = value, "F2");
                CreateForceSlider(section, font, "Slowing Radius", 0.5f, 8f, () => beeSimulation.slowingRadius, value => beeSimulation.slowingRadius = value, "F1");
            });

            CreateFoldoutSection(contentObject.transform, font, "Avoidance & Noise", false, section =>
            {
                CreateForceSlider(section, font, "Obstacle Weight", 0f, 12f, () => beeSimulation.obstacleAvoidanceWeight, value => beeSimulation.obstacleAvoidanceWeight = value, "F1");
                CreateForceSlider(section, font, "Ground Weight", 0f, 16f, () => beeSimulation.groundAvoidanceWeight, value => beeSimulation.groundAvoidanceWeight = value, "F1");
                CreateForceSlider(section, font, "Noise Weight", 0f, 5f, () => beeSimulation.curlNoiseWeight, value => beeSimulation.curlNoiseWeight = value, "F2");
                CreateForceSlider(section, font, "Visual Range", 1f, 12f, () => beeSimulation.visualRange, value => beeSimulation.visualRange = value, "F1");
            });
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
            if (forceContentObject != null)
            {
                forceContentObject.SetActive(showForcePanel);
            }

            if (forcePanelHeaderText != null)
            {
                forcePanelHeaderText.text = showForcePanel ? "Force Controls  [F3]" : "Force Controls collapsed  [F3]";
            }

            RectTransform panelRect = forcePanelObject != null ? forcePanelObject.GetComponent<RectTransform>() : null;
            if (panelRect != null)
            {
                panelRect.sizeDelta = showForcePanel ? new Vector2(195f, 0f) : new Vector2(195f, 0f);
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
            hintText.text = "F1 HUD  |  F2 View  |  F3 Force Panel  |  Tab Mode  |  ` Clear Trail";
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
