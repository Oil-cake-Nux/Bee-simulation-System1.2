using System.Collections.Generic;
using UnityEngine;

namespace ljk
{
    public class BeeSimulationManager : MonoBehaviour
    {
        [Header("模拟设置")]
        public GameObject beePrefab;

        [Header("轨迹显示")]
        public bool drawTrajectory = true;
        public float trajectoryDuration = 10f;
        public Color trajectoryColor = Color.yellow;

        [Header("蜜蜂对象")]
        public Vector3 BeePosition = Vector3.zero;
        public LineRenderer trajectoryRenderer;
        public GameObject beeInstance;

        private readonly List<Vector3> trajectoryPoints = new List<Vector3>();
        private const float TrajectoryUpdateInterval = 0.1f;
        private float lastTrajectoryUpdateTime;
        private bool demoPresentationEnabled = false;

        public int TrajectoryPointCount
        {
            get { return trajectoryPoints.Count; }
        }

        private void Start()
        {
            ResolveBeeInstance();
            InitializeSimulation();

            if (beeInstance != null)
            {
                beeInstance.tag = "Bee";
            }
        }

        private void Update()
        {
            if (beeInstance == null)
            {
                ResolveBeeInstance();
            }

            if (!drawTrajectory || beeInstance == null)
            {
                return;
            }

            if (trajectoryRenderer == null)
            {
                SetupTrajectoryRenderer();
            }

            UpdateTrajectory();
        }

        private void InitializeSimulation()
        {
            if (drawTrajectory)
            {
                SetupTrajectoryRenderer();
            }
        }

        private void ResolveBeeInstance()
        {
            if (beeInstance != null)
            {
                return;
            }

            beeInstance = GameObject.Find("Bee");
            if (beeInstance != null)
            {
                return;
            }

            try
            {
                beeInstance = GameObject.FindWithTag("Bee");
            }
            catch (UnityException)
            {
                beeInstance = null;
            }
        }

        private void SetupTrajectoryRenderer()
        {
            if (beeInstance == null)
            {
                return;
            }

            trajectoryRenderer = beeInstance.GetComponent<LineRenderer>();
            if (trajectoryRenderer == null)
            {
                trajectoryRenderer = beeInstance.AddComponent<LineRenderer>();
            }

            trajectoryRenderer.startWidth = 0.03f;
            trajectoryRenderer.endWidth = 0.01f;
            trajectoryRenderer.material = new Material(Shader.Find("Sprites/Default"));
            trajectoryRenderer.useWorldSpace = true;
            ApplyTrajectoryStyle();

            trajectoryPoints.Clear();
            trajectoryPoints.Add(beeInstance.transform.position);
            trajectoryRenderer.positionCount = 1;
            trajectoryRenderer.SetPosition(0, beeInstance.transform.position);

            lastTrajectoryUpdateTime = Time.time;
        }

        private void UpdateTrajectory()
        {
            if (Time.time - lastTrajectoryUpdateTime < TrajectoryUpdateInterval)
            {
                return;
            }

            lastTrajectoryUpdateTime = Time.time;
            trajectoryPoints.Add(beeInstance.transform.position);

            int maxPoints = Mathf.Max(1, Mathf.FloorToInt(trajectoryDuration / TrajectoryUpdateInterval));
            if (trajectoryPoints.Count > maxPoints)
            {
                trajectoryPoints.RemoveAt(0);
            }

            trajectoryRenderer.positionCount = trajectoryPoints.Count;
            for (int i = 0; i < trajectoryPoints.Count; i++)
            {
                trajectoryRenderer.SetPosition(i, trajectoryPoints[i]);
            }
        }

        public void ClearTrajectory()
        {
            trajectoryPoints.Clear();

            if (trajectoryRenderer != null && beeInstance != null)
            {
                trajectoryPoints.Add(beeInstance.transform.position);
                trajectoryRenderer.positionCount = 1;
                trajectoryRenderer.SetPosition(0, beeInstance.transform.position);
            }
        }

        public void ApplyPresentationPreset(bool enableDemoPresentation)
        {
            demoPresentationEnabled = enableDemoPresentation;
            trajectoryDuration = enableDemoPresentation ? 18f : 10f;
            trajectoryColor = enableDemoPresentation ? new Color(1f, 0.7f, 0.15f, 1f) : Color.yellow;
            ApplyTrajectoryStyle();
        }

        private void ApplyTrajectoryStyle()
        {
            if (trajectoryRenderer == null)
            {
                return;
            }

            float startWidth = demoPresentationEnabled ? 0.06f : 0.03f;
            float endWidth = demoPresentationEnabled ? 0.02f : 0.01f;
            trajectoryRenderer.startWidth = startWidth;
            trajectoryRenderer.endWidth = endWidth;
            trajectoryRenderer.startColor = trajectoryColor;
            trajectoryRenderer.endColor = new Color(trajectoryColor.r, trajectoryColor.g, trajectoryColor.b, 0.2f);
        }
    }
}
