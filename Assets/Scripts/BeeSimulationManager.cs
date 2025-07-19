using System.Collections.Generic;
using UnityEngine;

public class BeeSimulationManager : MonoBehaviour
{
    [Header("模拟设置")]
    public GameObject beePrefab;         // 蜜蜂预制体
    public GameObject obstaclePrefab;    // 障碍物预制体
    public int obstacleLow = 5;       // 障碍物行数
    public int LowCount = 4;  //每行障碍物数量
    public Vector3 simulationAreaSize = new Vector3(80f, 40f, 80f); // 模拟区域大小

    [Header("绘制设置")]
    public bool drawTrajectory = true;   // 是否绘制轨迹
    public float trajectoryDuration = 10f; // 轨迹持续时间
    public Color trajectoryColor = Color.yellow; // 轨迹颜色

    [Header("蜜蜂设置")]
    public Vector3 BeePosition = Vector3.zero;
    // 轨迹线渲染器
    private LineRenderer trajectoryRenderer;
    private List<Vector3> trajectoryPoints = new List<Vector3>();
    private float trajectoryUpdateInterval = 0.1f;
    private float lastTrajectoryUpdateTime;

    // 蜜蜂实例
    public GameObject beeInstance;
    //private BeeSimulation beeSimulation;


    void Start()
    {
        // 初始化场景
        InitializeSimulation();
        if (beeInstance == null)
        { beeInstance = GameObject.Find("Bee"); }
        beeInstance.tag = "Bee";
    }

    void Update()
    {
        // 更新轨迹
        if (drawTrajectory && beeInstance != null)
        {
            UpdateTrajectory();
        }

        // 按空格键重置模拟
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    ResetSimulation();
        //}
    }
    /// <summary>
    /// 初始化模拟场景
    /// </summary>
    private void InitializeSimulation()
    {
        // 创建边界
        //CreateBoundaries();

        // 创建障碍物
        //CreateObstacles();

        // 创建蜜蜂
        //CreateBee();

        // 设置轨迹渲染器
        if (drawTrajectory)
        {
            SetupTrajectoryRenderer();
        }
    }


    /// <summary>
    /// 创建边界
    /// </summary>
    //private void CreateBoundaries()
    //{
    //    // 创建一个玻璃材质
    //    Material glassMaterial = new Material(Shader.Find("Transparent/Diffuse"));
    //    glassMaterial.color = new Color(1, 1, 1, 0.5f);// 设置透明度为 0.5

    //    // 地面
    //    GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
    //    floor.transform.localScale = new Vector3(simulationAreaSize.x * 0.1f, 1f, simulationAreaSize.z * 0.1f);
    //    floor.transform.position = new Vector3(transform.position.x, transform.position.y - simulationAreaSize.y * 0.5f, transform.position.z);
    //    floor.GetComponent<Renderer>().material.color = new Color(0.8f, 0.8f, 0.8f);
    //    floor.transform.SetParent(this.transform);
    //    // 四面墙
    //    //float wallThickness = 0.1f;

    //    //顶部
    //    //GameObject upwall = GameObject.CreatePrimitive(PrimitiveType.Cube);
    //    //upwall.transform.localScale = new Vector3(simulationAreaSize.x, wallThickness, simulationAreaSize.z);
    //    //upwall.transform.position = new Vector3(transform.position.x, transform.position.y + simulationAreaSize.y * 0.5f, transform.position.z);
    //    //upwall.GetComponent<Renderer>().material = glassMaterial;// 设置为透明
    //    //upwall.transform.SetParent(this.transform);

    //    // 前墙
    //    //GameObject frontWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
    //    //frontWall.transform.localScale = new Vector3(simulationAreaSize.x, simulationAreaSize.y, wallThickness);
    //    //frontWall.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z+simulationAreaSize.z * 0.5f);
    //    //frontWall.GetComponent<Renderer>().material = glassMaterial;// 设置为透明
    //    //frontWall.transform.SetParent(this.transform);

    //    //// 后墙
    //    //GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
    //    //backWall.transform.localScale = new Vector3(simulationAreaSize.x, simulationAreaSize.y, wallThickness);
    //    //backWall.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z - simulationAreaSize.z * 0.5f);
    //    //backWall.GetComponent<Renderer>().material = glassMaterial;// 设置为透明
    //    //backWall.transform.SetParent(this.transform);

    //    // 左墙
    //    //GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
    //    //leftWall.transform.localScale = new Vector3(wallThickness, simulationAreaSize.y, simulationAreaSize.z);
    //    //leftWall.transform.position = new Vector3(transform.position.x - simulationAreaSize.x * 0.5f, transform.position.y, transform.position.z);
    //    //leftWall.GetComponent<Renderer>().material = glassMaterial;// 设置为透明
    //    //leftWall.transform.SetParent(this.transform);

    //    //// 右墙
    //    //GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
    //    //rightWall.transform.localScale = new Vector3(wallThickness, simulationAreaSize.y, simulationAreaSize.z);
    //    //rightWall.transform.position = new Vector3(transform.position.x + simulationAreaSize.x * 0.5f, transform.position.y, transform.position.z);
    //    //rightWall.GetComponent<Renderer>().material = glassMaterial;// 设置为透明
    //    //rightWall.transform.SetParent(this.transform);

    //    // 给所有墙添加标签
    //    floor.tag = "Obstacle";
    //    //upwall.tag = "Obstacle";
    //    //frontWall.tag = "Obstacle";
    //    //backWall.tag = "Obstacle";
    //    //leftWall.tag = "Obstacle";
    //    //rightWall.tag = "Obstacle";
    //}

    ///// <summary>
    ///// 创建障碍物
    ///// </summary>
    //private void CreateObstacles()
    //{
    //    float x, z;
    //    z = 8;
    //    for(int j=0; j < obstacleLow; j++) {
    //        x = 6;
    //        if (j % 2 == 1)
    //            x = 10;
    //        for (int i = 0; i < LowCount; i++)
    //        {
    //        // (Y轴方向上的位置与活动场正中心相同)
    //        Vector3 position = new Vector3(
    //            x,
    //            transform.position.y,
    //            z
    //        );
    //            x += 8;
    //        // 如果没有提供预制体，则创建简单的障碍物
    //        GameObject obstacle;
    //        if (obstaclePrefab != null)
    //        {
    //            obstacle = Instantiate(obstaclePrefab, position, Quaternion.identity);
    //        }
    //        else
    //        {
    //            obstacle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
    //            obstacle.transform.position = position;
    //            obstacle.transform.localScale = new Vector3(
    //                Random.Range(0.5f, 1.5f),
    //                Random.Range(1f, 3f),
    //                Random.Range(0.5f, 1.5f)
    //            );
    //            obstacle.GetComponent<Renderer>().material.color = new Color(
    //                Random.Range(0.4f, 0.6f),
    //                Random.Range(0.4f, 0.6f),
    //                Random.Range(0.4f, 0.6f)
    //            );
    //        }

    //        //作为挂载物的孩子
    //        obstacle.transform.SetParent(this.transform);
    //        // 设置标签
    //        obstacle.tag = "Obstacle";
    //        }
    //        z += 8;
    //    }
    //}

    /// <summary>
    /// 设置运动轨迹线属性
    /// </summary>
    //注：运动轨迹的实现可有可无！
    private void SetupTrajectoryRenderer()
    {
        if (beeInstance != null)
        {
            trajectoryRenderer = beeInstance.AddComponent<LineRenderer>();
            trajectoryRenderer.startWidth = 0.03f;
            trajectoryRenderer.endWidth = 0.01f;
            trajectoryRenderer.material = new Material(Shader.Find("Sprites/Default"));
            trajectoryRenderer.startColor = trajectoryColor;
            trajectoryRenderer.endColor = new Color(trajectoryColor.r, trajectoryColor.g, trajectoryColor.b, 0.2f);
            trajectoryRenderer.useWorldSpace = true;

            // 初始化轨迹点
            trajectoryPoints.Clear();
            trajectoryPoints.Add(beeInstance.transform.position);
            trajectoryRenderer.positionCount = 1;
            trajectoryRenderer.SetPosition(0, beeInstance.transform.position);

            lastTrajectoryUpdateTime = Time.time;
        }
    }

    //轨迹可视化
    private void UpdateTrajectory()
    {
        if (Time.time - lastTrajectoryUpdateTime >= trajectoryUpdateInterval)
        {
            lastTrajectoryUpdateTime = Time.time;

            // 添加新点
            trajectoryPoints.Add(beeInstance.transform.position);

            // 如果点太多，移除最早的点
            if (trajectoryPoints.Count > Mathf.FloorToInt(trajectoryDuration / trajectoryUpdateInterval))
            {
                trajectoryPoints.RemoveAt(0);
            }

            // 更新线渲染器
            trajectoryRenderer.positionCount = trajectoryPoints.Count;
            for (int i = 0; i < trajectoryPoints.Count; i++)
            {
                trajectoryRenderer.SetPosition(i, trajectoryPoints[i]);
            }
        }
    }
}