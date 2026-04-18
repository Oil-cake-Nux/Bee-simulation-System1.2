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
            trajectoryRenderer.startColor = trajectoryColor;
            trajectoryRenderer.endColor = new Color(trajectoryColor.r, trajectoryColor.g, trajectoryColor.b, 0.2f);
            trajectoryRenderer.useWorldSpace = true;

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
    }
}
