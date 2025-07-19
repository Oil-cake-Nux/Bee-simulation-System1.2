using UnityEngine;
public class BeeSimulation : MonoBehaviour
{
    BeeTargetController beeTarget;
    [Header("蜜蜂基本参数")]
    public float beeRadius = 0.2f;         // 蜜蜂半径（胶囊体表示）
    public float moveSpeed = 2f;

    [Header("身体振荡参数")]
    public double l;
    public float oscillationForceWeight = 0.5f;  // 振荡力权重，对应论文中的α1
    [Range(0f, 90f)]
    public float pitchAngle = 20f;         // 俯仰角，一般约为20度
    public float thrustFactor = 16.429f;   // 推力系数 对应公式(3)
    public float swayFactor = 0.765f;        // 摇摆系数 对应公式(4)
    public float oscillationFrequency = 1.5f; // 振荡频率，控制振荡快慢
    //新加震荡参数
    public float targetHeight = 10f;
    public float heightDampingFactor = 0.5f;

    [Header("避障参数")]
    public float visualRange = 5f;         // 视觉感知范围
    public float obstacleDetectionAngle = 50f;  // 障碍物检测角度，约为±45度
    public float obstacleAvoidanceWeight = 5f; // 避障力权重
    public int rayCount = 20;               // 射线数量
    public float emergencyAvoidanceMultiplier = 1.3f; // 紧急避障倍率

    [Header("避障翻滚参数")]
    public float avoidanceRollMaxAngle = 60f;   // 避障时最大翻滚角度
    public float avoidanceRollSpeed = 3f;      // 翻滚速度系数
    public float rollRecoverySpeed = 2f;       // 翻滚恢复速度
    public bool enableAvoidanceRoll = true;    // 是否启用避障翻滚

    [Header("噪声参数")]
    public float curlNoiseWeight = 1.5f;   // 增加噪声权重，使效果更明显
    public bool enableCurlNoise = true;    // 是否启用卷曲噪声
    public float noiseInfluenceRadius = 3.0f; // 噪声影响半径

    [Header("调试")]
    public bool showDebugRays = true;      // 显示调试射线
    public bool showForces = true;         // 显示各种力的方向
    public bool showNoiseForce = true;     // 显示噪声力

    [Header("目标追逐参数")]
    //public GameObject BeeTarget;
    public Transform target;                  // 目标点Transform
    public float targetAttractionWeight = 1.5f; // 目标吸引力权重
    public float arrivalRadius = 3f;         // 到达半径
    public float slowingRadius = 2.5f;           // 减速半径
    public float bankingFactor = 1.5f;         // 减速开始半径
    public float maxForce = 7f;               // 最大转向力

    [Header("高级控制")]
    public float steeringPD_Kp = 2.5f;
    public float steeringPD_Kd = 0.8f;
    public float maxSpeed = 3.5f;                // 最大速度限制
    public float minObstacleAvoidanceDistance = 1.0f; // 最小避障距离
    // 蜜蜂状态
    public Vector3 velocity = Vector3.zero;
    private Vector3 acceleration = Vector3.zero;
    private float bodyRollAngle = 0f;
    private float bodyPitchAngle = 0f;
    private Vector3 lastAvoidanceForce = Vector3.zero; // 记录上一帧的避障力
    private bool isAvoidingObstacle = false;           // 是否正在避障
    private Vector3 lastCurlNoiseForce = Vector3.zero; // 上一帧的卷曲噪声力
    private float targetAvoidanceRollAngle = 0f;       // 目标避障翻滚角度
    private float currentAvoidanceRollAngle = 0f;      // 当前避障翻滚角度
    private float avoidanceRollDirection = 0f;         // 避障翻滚方向(正为右，负为左)
    private float obstacleAvoidanceStrength = 0f;      // 避障强度

    // 参照坐标系
    private Vector3 thrustAxis; // 推力轴
    private Vector3 liftAxis;   // 升力轴
    private Vector3 swayAxis;   // 摇摆轴

    // Unity组件引用
    private Rigidbody rb;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        beeTarget=GetComponent<BeeTargetController>();
        // 设置刚体属性
        rb.useGravity = true;  // 蜜蜂自己控制升力抵抗重力
        rb.drag = 0.5f;         // 添加一些阻力
        rb.angularDrag = 0.5f;  // 添加角阻力
        rb.interpolation = RigidbodyInterpolation.Interpolate; // 添加插值，使运动更平滑

        // 初始化坐标轴
        UpdateBodyAxes();

        // 初始速度
        velocity = transform.forward * Random.Range(0.5f, 0.8f);
    }

    void Update()
    {
        // 更新噪声场时间

        // 绘制调试可视化
        if (showDebugRays)
        {
            DrawDebugVisualization();
        }
        CurlNoiseField.UpdateTime();
    }

    void FixedUpdate()
    {
        // 更新蜜蜂身体坐标轴
        UpdateBodyAxes();
        beeTarget.UpdateTargetState();
        // 计算各种力
        Vector3 curlNoiseForce = Vector3.zero;
        if (enableCurlNoise)
        {
            // 获取卷曲噪声力
            curlNoiseForce = GetCurlNoiseForce();

            // 周期性地强化噪声影响
            float noisePulse = 1.0f + 0.5f * Mathf.Sin(Time.time * 0.5f);
            curlNoiseForce *= noisePulse;
        }

        Vector3 oscillationForce = CalculateOscillationForce();
        Vector3 obstacleAvoidanceForce = CalculateObstacleAvoidanceForce();
        Vector3 targetSeekForce = beeTarget.CalculateTargetSeekForce();
        //Debug.Log(targetSeekForce);
        //oscillationForce=new Vector3(0,0,0);
        // 力优先级处理
        Vector3 totalForce = Vector3.zero;

        // 如果检测到障碍物，优先考虑避障，但保持一定的速度
        if (obstacleAvoidanceForce.magnitude > 0.1f)
        {
            isAvoidingObstacle = true;

            // 避障强度调控避障
            float avoidanceWeight = obstacleAvoidanceWeight * (1.0f + obstacleAvoidanceStrength * 0.5f);

            totalForce = avoidanceWeight * obstacleAvoidanceForce +
                         oscillationForceWeight * oscillationForce * 0.1f; // 避障时减弱但不要减弱太多振荡

            // 避障时仍然保持一定的目标追踪力，保持整体方向一致性
            totalForce += targetSeekForce * 0.23f + curlNoiseWeight * curlNoiseForce * 0.5f;

        }
        else
        {
            isAvoidingObstacle = false;
            obstacleAvoidanceStrength = Mathf.Lerp(obstacleAvoidanceStrength, 0f, Time.fixedDeltaTime * 2f);

            // 正常追踪目标
            totalForce = targetSeekForce +
                         oscillationForceWeight * oscillationForce +
                         curlNoiseWeight * curlNoiseForce;
        }

        // 限制最大力
        if (totalForce.magnitude > maxForce)
        {
            totalForce = totalForce.normalized * maxForce;
        }

        // 应用力
        acceleration = totalForce / rb.mass;

        // 在避障时确保速度不会减慢太多
        float currentSpeed = velocity.magnitude;
        velocity += acceleration * Time.fixedDeltaTime;

        // 限制最大速度，但避障时确保不会低于最低速度
        float speedLimit = isAvoidingObstacle ?
                        Mathf.Max(maxSpeed * 0.5f, velocity.magnitude) : // 避障时保持至少70%的最大速度
                        maxSpeed;
        //限制速度大小
        if (velocity.magnitude > speedLimit)
        {
            velocity = velocity.normalized * speedLimit;
        }

        // 更新位置和旋转
        rb.velocity = velocity;

        // 更新方向
        if (velocity.magnitude > 0.1f)
        {
            // 避障时使用更快的转向响应
            float turnSpeed = isAvoidingObstacle ? 4.5f : 3f;
            transform.forward = Vector3.Lerp(transform.forward, velocity.normalized, Time.fixedDeltaTime * turnSpeed);
        }

        // 计算翻滚角度
        CalculateBodyRollAngle();

        // 应用身体姿态
        ApplyBodyPosture();

        // 可视化力
        if (showNoiseForce && enableCurlNoise)
        {
            Debug.DrawRay(transform.position, curlNoiseForce.normalized * 1.0f, new Color(0.0f, 1.0f, 1.0f));
        }
    }

    #region 获取卷曲噪声力
    /// <summary>
    /// 获取卷曲噪声力
    /// </summary>
    private Vector3 GetCurlNoiseForce()
    {
        // 检查是否需要更新噪声方向
        //if (Time.time > noiseChangeTime)
        //{
        //    // 更新下一次噪声变化的时间
        //    noiseChangeTime = Time.time + noiseSwitchInterval * Random.Range(0.8f, 1.2f);
        //}

        // 获取当前位置的卷曲噪声力
        Vector3 noiseForce = CurlNoiseField.GetCurlNoiseForce(transform.position);

        // 应用距离因子，与目标距离越远，噪声影响越大
        if (beeTarget.currentTarget != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, beeTarget.currentTarget.position);
            float distanceFactor = Mathf.Clamp01(distanceToTarget / noiseInfluenceRadius);
            noiseForce *= distanceFactor * 1.5f; // 在远离目标时增强噪声影响
        }

        // 确保噪声力不会对主要的飞行方向有太大影响
        Vector3 forwardComponent = Vector3.Project(noiseForce, transform.forward);
        Vector3 lateralComponent = noiseForce - forwardComponent;

        // 增强横向分量，减弱前向分量，使蜜蜂更有随机性但仍保持前进
        return lateralComponent * 1.3f + forwardComponent * 0.7f;
    }
    #endregion

    /// <summary>
    /// 更新蜜蜂身体坐标轴
    /// </summary>
    private void UpdateBodyAxes()
    {
        // 使用蜜蜂的局部坐标系
        thrustAxis = transform.forward;  // z轴为推力轴
        liftAxis = transform.up;         // y轴为升力轴
        swayAxis = transform.right;      // x轴为摇摆轴
    }


    private Vector3 CalculateOscillationForce()
    {
        // 使用噪声生成更自然的振荡模式
        float time = Time.time * oscillationFrequency;

        // 使用三维Perlin噪声生成基础振荡参数
        Vector3 noiseInput = transform.position * 0.1f + Vector3.one * time;
        float noiseX = Mathf.PerlinNoise(noiseInput.x, noiseInput.y) * 2f - 1f;
        float noiseY = Mathf.PerlinNoise(noiseInput.y, noiseInput.z) * 2f - 1f;
        float noiseZ = Mathf.PerlinNoise(noiseInput.z, noiseInput.x) * 2f - 1f;

        // 动态频率调节（增加随机性）
        float dynamicFrequency = oscillationFrequency * (1f + noiseY * 0.3f);

        // 生成更随机的身体角度（幅度限制在±15度内）
        //bodyPitchAngle = thrustFactor + 3.5f * Mathf.Sin(((2.986f + time) / 21.072f) * Mathf.PI);
        //bodyRollAngle = -swayFactor + 18.914f * Mathf.Sin(((38.298f + time) / 27.654f) * Mathf.PI);
        bodyPitchAngle = Mathf.Lerp(
            bodyPitchAngle,
            Mathf.Clamp(
                noiseX * 15f + Mathf.Sin(time * 1.3f) * 5f, // 组合噪声和弱周期信号
                -25f,
                25f
            ),
            Time.deltaTime * 8f
        );

        bodyRollAngle = Mathf.Lerp(
            bodyRollAngle,
            Mathf.Clamp(
                noiseZ * 20f + Mathf.Cos(time * 1.7f) * 6f,
                -30f,
                30f
            ),
            Time.deltaTime * 8f
        );

        // 升力计算（加入高度稳定机制）
        float baseLift = rb.mass;
        float heightDelta = transform.position.y - 10f; // 初始高度基准
        float liftDamping = Mathf.Clamp(-heightDelta * 1.5f, -3f, 3f);

        // 随机升力扰动（幅度更小）
        float liftNoise = Mathf.PerlinNoise(time * 0.7f, 0) * 0.4f - 0.2f;
        float liftMagnitude = baseLift + liftDamping + liftNoise;

        // 推力计算（限制最大倾角影响）
        float thrustAngle = Mathf.Clamp(bodyPitchAngle, -40f, 40f);
        float thrustMagnitude = liftMagnitude * Mathf.Tan(thrustAngle * Mathf.Deg2Rad) * 0.3f;

        // 摇摆力计算（增加随机方向性）
        float swayAngle = bodyRollAngle * (Random.value > 0.5f ? 1f : -1f);
        float swayMagnitude = liftMagnitude * Mathf.Tan(swayAngle * Mathf.Deg2Rad) * 0.4f;

        // 合成振荡力（增加水平扰动）
        Vector3 liftForce = liftAxis * liftMagnitude;
        Vector3 thrustForce = thrustAxis * thrustMagnitude;
        Vector3 swayForce = swayAxis * swayMagnitude;

        // 添加水平随机扰动（增强自然感）
        float horizontalNoise = Mathf.PerlinNoise(time * 1.2f, 0) * 2f - 1f;
        Vector3 horizontalPerturbation = transform.right * horizontalNoise * 0.8f;

        // 可视化
        if (showForces)
        {
            Debug.DrawRay(transform.position, thrustForce.normalized, new Color(1f, 0.5f, 0f));
            Debug.DrawRay(transform.position, swayForce.normalized, new Color(1f, 1f, 0f));
            Debug.DrawRay(transform.position, horizontalPerturbation, new Color(0.5f, 1f, 0.5f));
        }

        return (liftForce + thrustForce + swayForce) * 0.7f + horizontalPerturbation;
    }
    //#region 身体振荡力
    /// <summary>
    /// 计算身体振荡力（对应论文IV.A部分）- 增强版
    /// </summary>
    //private Vector3 CalculateOscillationForce()
    //{
    //    // 计算当前时间下的俯仰角和摇摆角（基于正弦函数）
    //    float time = Time.time * oscillationFrequency; // 添加频率控制

    //    // 使用论文中的公式(3)和(4)计算俯仰角和摇摆角
    //    bodyPitchAngle = thrustFactor + 3.5f * Mathf.Sin(((2.986f + time) / 21.072f) * Mathf.PI);
    //    bodyRollAngle = -swayFactor + 18.914f * Mathf.Sin(((38.298f + time) / 27.654f) * Mathf.PI);
    //    l = -0.0009595f * bodyPitchAngle * bodyPitchAngle + 0.090635 * bodyPitchAngle - 0.34182f;
    //    // 添加额外振荡分量
    //    float extraPitch = 3.5f * Mathf.Sin(time * 1.5f);
    //    float extraRoll = 5f * Mathf.Sin(time * 2.3f);

    //    bodyPitchAngle += extraPitch;
    //    bodyRollAngle += extraRoll;

    //    // 计算升力、推力和摇摆力
    //    float baseLift = rb.mass * Random.Range(0.7f, 1.1f);// 升力添加随机值，以发生上下震荡的效果
    //                                                        // 添加高度阻尼（当前高度与初始高度的差值）
    //    float heightDelta = transform.position.y - 10f;  //10f为初始蜜蜂高度
    //    float liftDamping = Mathf.Clamp(-heightDelta * 1f, -2f, 2f);

    //    // 最终升力 = 基础升力 + 阻尼调整 + 随机振荡
    //    float liftMagnitude = baseLift + liftDamping + Random.Range(-1f, 1f);
    //    float thrustMagnitude = liftMagnitude * Mathf.Tan(bodyPitchAngle * Mathf.Deg2Rad);
    //    float swayMagnitude = liftMagnitude * Mathf.Tan(bodyRollAngle * Mathf.Deg2Rad);
    //    //改变升力

    //    //float liftMagnitude = (float)l* 1.225f* velocity.magnitude;
    //    //float thrustMagnitude = rb.mass * 9.81f * bodyPitchAngle;
    //    //float swayMagnitude = rb.mass * 9.81f * bodyRollAngle;

    //    int randomNumber = Random.Range(1, 101);
    //    // 合成振荡力
    //    Vector3 liftForce = liftAxis * liftMagnitude;
    //    Vector3 thrustForce = thrustAxis * thrustMagnitude;
    //    Vector3 swayForce;
    //    if (randomNumber % 2 == 0)
    //    {
    //         swayForce = -1*swayAxis * swayMagnitude;
    //    }
    //    else
    //    {
    //         swayForce = swayAxis * swayMagnitude;
    //    }

    //    // 可视化振荡的各个分量
    //    if (showForces)
    //    {
    //        Debug.DrawRay(transform.position, thrustForce.normalized , new Color(1.0f, 0.5f, 0.0f)); // 橙色表示推力
    //        Debug.DrawRay(transform.position, swayForce.normalized , new Color(1.0f, 1.0f, 0.0f)); // 黄色表示摇摆力
    //    }

    //    return liftForce + thrustForce + swayForce;
    //}
    //#endregion

    #region 避障力
    /// <summary>
    /// 计算障碍物避免力 - 改进侧向绕行策略
    /// </summary>
    private Vector3 CalculateObstacleAvoidanceForce()
    {
        // 避障累积力
        Vector3 avoidanceForce = Vector3.zero;
        bool obstacleDetected = false;
        float closestObstacleDistance = float.MaxValue;

        // 用于绕行方向决策
        Vector3 preferredAvoidanceDirection = Vector3.zero;
        bool hasPreferredDirection = false;
        float minDistanceWeight = 0f;
        int hitCount = 0;

        // 扇形区域检测
        float halfAngle = obstacleDetectionAngle;

        // 检查正前方，优先级最高
        //RaycastHit forwardHit;
        //if (Physics.Raycast(transform.position, transform.forward, out forwardHit, visualRange * 1.5f))
        //{
        //    if (forwardHit.collider.CompareTag("Obstacle"))
        //    {
        //        float distance = forwardHit.distance;
        //        closestObstacleDistance = distance;

        //        // 计算前方障碍物相对位置
        //        Vector3 obstacleLocalPos = transform.InverseTransformPoint(forwardHit.point);

        //        // 决定绕行方向 - 基于障碍物相对于蜜蜂的位置和当前速度
        //        // 总是选择更开阔的一侧绕行
        //        Vector3 avoidDir;

        //        // 确定障碍物在蜜蜂左侧还是右侧
        //        if (Mathf.Abs(obstacleLocalPos.x) < 0.2f) // 正前方
        //        {
        //            // 随机选择一个方向，但保持一致性
        //            if (!hasPreferredDirection)
        //            {
        //                // 使用当前帧时间创建一个伪随机但在短时间内保持一致的选择
        //                float randomValue = Mathf.PerlinNoise(transform.position.x * 0.5f, transform.position.z * 0.5f);
        //                avoidDir = randomValue > 0.5f ? transform.right : -transform.right;
        //                preferredAvoidanceDirection = avoidDir;
        //                hasPreferredDirection = true;
        //            }
        //            else
        //            {
        //                avoidDir = preferredAvoidanceDirection;
        //            }
        //        }
        //        else
        //        {
        //            // 向障碍物的反方向避开
        //            avoidDir = -Mathf.Sign(obstacleLocalPos.x) * transform.right;

        //            // 保存这个方向作为首选方向
        //            if (!hasPreferredDirection || distance < minDistanceWeight)
        //            {
        //                preferredAvoidanceDirection = avoidDir;
        //                hasPreferredDirection = true;
        //                minDistanceWeight = distance;
        //            }
        //        }

        //        // 距离权重 - 距离越近权重越大
        //        float distanceWeight = Mathf.Clamp01(1.0f - distance / visualRange) * 1.5f;

        //        // 紧急情况下增强避障力
        //        if (distance < minObstacleAvoidanceDistance)
        //        {
        //            distanceWeight *= emergencyAvoidanceMultiplier;
        //        }

        //        // 合成避障力 - 侧向分量 + 少量向上分量以帮助越过障碍物
        //        Vector3 sideAvoidance = avoidDir * distanceWeight * 2f;
        //        //Vector3 upAvoidance = transform.up * distanceWeight * 0.2f;

        //        // 为防止减速过多，增加前向分量
        //        Vector3 forwardComponent = transform.forward * distanceWeight * 1f;

        //        // 合成最终避障力
        //        avoidanceForce += sideAvoidance  + forwardComponent;

        //        // 更新翻滚方向
        //        avoidanceRollDirection = Mathf.Sign(Vector3.Dot(avoidDir, transform.right)) * distanceWeight * 2.0f;
        //        targetAvoidanceRollAngle = avoidanceRollDirection * avoidanceRollMaxAngle;

        //        obstacleDetected = true;
        //        hitCount++;

        //        // 调试显示
        //        if (showDebugRays)
        //        {
        //            Debug.DrawRay(transform.position, transform.forward * distance, Color.red);
        //            Debug.DrawRay(forwardHit.point, avoidDir * 2f, Color.yellow);
        //        }
        //    }
        //}

        // 扇形区域多方向检测 - 检测周围障碍物
        for (int i = 0; i < rayCount; i++)
        {
            // 计算射线角度，均匀分布在检测扇形内
            float angle = (i - (rayCount - 1) / 2f) * (halfAngle * 2) / (rayCount - 1);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;

            RaycastHit hit;
            if (Physics.Raycast(transform.position, direction, out hit, visualRange))
            {
                if (hit.collider.CompareTag("Obstacle"))
                {
                    float distance = hit.distance;

                    // 更新最近障碍物距离
                    if (distance < closestObstacleDistance)
                    {
                        closestObstacleDistance = distance;
                    }

                    // 根据距离和角度计算权重
                    float distanceWeight = Mathf.Clamp01(1.0f - distance / visualRange) * 1f;
                    float angleWeight = 1f - Mathf.Abs(angle) / halfAngle;
                    float totalWeight = distanceWeight * angleWeight;

                    // 计算障碍物相对位置
                    Vector3 obstacleLocalPos = transform.InverseTransformPoint(hit.point);

                    // 确定避障方向
                    Vector3 avoidDir = -Mathf.Sign(obstacleLocalPos.x) * transform.right*2;

                    // 如果这是最近的障碍物，更新首选避障方向
                    if (!hasPreferredDirection || totalWeight > minDistanceWeight)
                    {
                        preferredAvoidanceDirection = avoidDir;
                        hasPreferredDirection = true;
                        minDistanceWeight = totalWeight;
                    }

                    // 如果是紧急情况，增强避障力
                    if (distance < minObstacleAvoidanceDistance)
                    {
                        totalWeight *= emergencyAvoidanceMultiplier;
                    }

                    // 合成这个方向的避障力
                    Vector3 sideAvoidance = avoidDir * totalWeight ;
                    Vector3 forwardComponent = transform.forward * totalWeight * 1f;

                    avoidanceForce += sideAvoidance  + forwardComponent;

                    obstacleDetected = true;
                    hitCount++;

                    // 调试显示
                    if (showDebugRays)
                    {
                        Debug.DrawRay(transform.position, direction * distance, Color.red);
                        Debug.DrawRay(hit.point, avoidDir * totalWeight, Color.green);
                    }
                }
            }
        }

        // 如果检测到障碍物
        if (obstacleDetected)
        {
            // 平滑过渡避障力
            if (lastAvoidanceForce.magnitude > 0.1f)
            {
                // 在短时间内保持首选避障方向一致性
                avoidanceForce = Vector3.Lerp(lastAvoidanceForce, avoidanceForce, Time.fixedDeltaTime * 5f);
            }

            // 更新避障状态
            isAvoidingObstacle = true;
            obstacleAvoidanceStrength = Mathf.Clamp01(1.0f - closestObstacleDistance / visualRange) * 2.0f;

            // 更新翻滚角度 - 使翻滚更加明显
            if (enableAvoidanceRoll && hasPreferredDirection)
            {
                // 基于首选避障方向设置翻滚
                avoidanceRollDirection = Mathf.Sign(Vector3.Dot(preferredAvoidanceDirection, transform.right)) *
                                        obstacleAvoidanceStrength * 30.0f;
                //Debug.Log(avoidanceRollDirection);
                // 将翻滚角度限制在最大值范围内
                targetAvoidanceRollAngle = Mathf.Clamp(avoidanceRollDirection * avoidanceRollMaxAngle,
                                                     -avoidanceRollMaxAngle, avoidanceRollMaxAngle);
            }
        }
        else
        {
            // 没有障碍物，平滑恢复正常
            if (lastAvoidanceForce.magnitude > 0.1f)
            {
                // 平滑过渡到零避障力
                avoidanceForce = Vector3.Lerp(lastAvoidanceForce, Vector3.zero, Time.fixedDeltaTime * 2f);
            }

            isAvoidingObstacle = false;
            obstacleAvoidanceStrength = Mathf.Lerp(obstacleAvoidanceStrength, 0f, Time.fixedDeltaTime * 2f);

            // 恢复正常翻滚
            //if (enableAvoidanceRoll)
            //{
            //    targetAvoidanceRollAngle = Mathf.Lerp(targetAvoidanceRollAngle, 0f, Time.fixedDeltaTime * rollRecoverySpeed);
            //}
        }

        // 平滑过渡翻滚角度
        if (enableAvoidanceRoll)
        {
            // 障碍物越近，翻滚响应越快
            float responseSpeed = obstacleDetected ?
                                avoidanceRollSpeed * (1.0f + obstacleAvoidanceStrength) :
                                rollRecoverySpeed;

            currentAvoidanceRollAngle = Mathf.Lerp(currentAvoidanceRollAngle, targetAvoidanceRollAngle,
                                                 Time.fixedDeltaTime * responseSpeed);
        }

        // 保存此帧避障力用于下一帧平滑过渡
        lastAvoidanceForce = avoidanceForce;

        return avoidanceForce;
    }
    #endregion

    #region 计算身体翻滚角度
    /// <summary>
    /// 计算身体翻滚角度（对应论文公式6）
    /// </summary>
    private void CalculateBodyRollAngle()
    {
        // 避障过程中已经计算了翻滚角度，这里添加避障翻滚的贡献
        float normalRollAngle = bodyRollAngle;

        // 在避障时添加额外的翻滚
        if (isAvoidingObstacle && enableAvoidanceRoll)
        {
            // 根据避障强度和方向计算额外翻滚
            bodyRollAngle = normalRollAngle + currentAvoidanceRollAngle;
        }
        else if (!isAvoidingObstacle)
        {
            // 追踪目标时的翻滚角度计算
            if (target != null)
            {
                Vector3 toTarget = target.position - transform.position;
                Vector3 lateralVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);

                if (lateralVelocity.magnitude > 0.1f)
                {
                    // 计算转弯时的倾斜角度
                    Vector3 desiredDir = toTarget.normalized;
                    Vector3 currentDir = lateralVelocity.normalized;
                    float angle = Vector3.SignedAngle(currentDir, desiredDir, Vector3.up);

                    // 根据转弯角度计算翻滚角度
                    float turnRoll = Mathf.Clamp(angle * 0.5f, -40f, 40f);

                    // 如果启用了避障翻滚，则合并正常翻滚和避障翻滚
                    if (enableAvoidanceRoll)
                    {
                        bodyRollAngle = Mathf.Lerp(bodyRollAngle, normalRollAngle + turnRoll + currentAvoidanceRollAngle,
                                                 Time.fixedDeltaTime * 2f);
                    }
                    else
                    {
                        bodyRollAngle = Mathf.Lerp(bodyRollAngle, turnRoll, Time.fixedDeltaTime * 2f);
                    }
                }
            }
        }
    }
    #endregion

    #region 应用身体姿态
    /// <summary>
    /// 应用身体姿态 - 增强翻滚效果与避障协调性
    /// </summary>
    private void ApplyBodyPosture()
    {
        // 只有在有速度的情况下才应用姿态
        if (velocity.magnitude > 0.1f)
        {
            // 创建向前的方向 - 基于速度方向
            Quaternion forwardRotation = Quaternion.LookRotation(velocity.normalized);

            // 应用基本的身体俯仰角和横滚角
            float finalPitchAngle = bodyPitchAngle;
            float finalRollAngle = bodyRollAngle;
            //限制仰俯角
            finalPitchAngle = Mathf.Clamp(bodyPitchAngle, -60f, 60f);
            // 启用了翻滚，在避障状态下增强翻滚效果
            if (isAvoidingObstacle && enableAvoidanceRoll)
            {
                // 为避障增强翻滚效果
                float avoidanceRollContribution = currentAvoidanceRollAngle;

                // 避障时稍微抬头
                //finalPitchAngle = bodyPitchAngle - obstacleAvoidanceStrength * 8f;

                // 合并基础翻滚和避障翻滚
                finalRollAngle = bodyRollAngle * 0.5f + avoidanceRollContribution * 1.5f;

                // 保证最大翻滚幅度
                finalRollAngle = Mathf.Clamp(finalRollAngle, -avoidanceRollMaxAngle * 1.2f, avoidanceRollMaxAngle * 1.2f);
            }
            Debug.Log(finalRollAngle);
            // 创建姿态旋转
            //Quaternion pitchRotation = Quaternion.Euler(finalPitchAngle, 0, 0);
            Quaternion rollRotation = Quaternion.Euler(0, 0, -finalRollAngle);
            Debug.Log(rollRotation);
            // 组合所有旋转
            Quaternion targetRotation = forwardRotation  * rollRotation;

            // 根据避障状态调整旋转响应速度
            float rotationSpeed = isAvoidingObstacle ?
                             obstacleAvoidanceStrength*0.7f  : // 避障时更快的响应
                             2f;

            // 柔和地应用旋转
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);

            // 绘制调试线
            if (showForces && isAvoidingObstacle)
            {
                // 显示翻滚角度
                Debug.DrawRay(transform.position + Vector3.up * 0.5f,
                             transform.right * (finalRollAngle / avoidanceRollMaxAngle) * 0.7f,
                             new Color(1f, 0f, 1f)); // 紫色表示实际翻滚角度

                Debug.DrawRay(transform.position + Vector3.up * 0.5f,
                             transform.right * (currentAvoidanceRollAngle / avoidanceRollMaxAngle) * 0.5f,
                             Color.black); // 青色表示避障翻滚贡献
            }
        }
    }
    #endregion

    #region 绘制调试可视化
    /// <summary>
    /// 绘制调试可视化
    /// </summary>
    private void DrawDebugVisualization()
    {
        if (showForces)
        {
            // 绘制速度方向
            //Debug.DrawRay(transform.position, velocity.normalized * 2f, Color.blue);

            //// 绘制蜜蜂身体坐标轴
            //Debug.DrawRay(transform.position, thrustAxis, Color.red);
            //Debug.DrawRay(transform.position, liftAxis, Color.green);
            //Debug.DrawRay(transform.position, swayAxis, Color.yellow);

            // 绘制到目标的方向
            if (beeTarget.currentTarget != null)
            {
                Debug.DrawLine(transform.position, beeTarget.currentTarget.position, Color.magenta);
            }

            // 绘制避障翻滚方向
            //if (enableAvoidanceRoll && isAvoidingObstacle)
            //{
            //    Debug.DrawRay(transform.position, transform.right * avoidanceRollDirection * 2f, Color.cyan);

            //    // 显示目标翻滚角度和当前翻滚角度
            //    Vector3 rollIndicatorPos = transform.position + Vector3.up * 0.5f;
            //    Debug.DrawLine(rollIndicatorPos,
            //                  rollIndicatorPos + transform.right * (targetAvoidanceRollAngle / avoidanceRollMaxAngle) * 0.5f,
            //                  Color.yellow);
            //    Debug.DrawLine(rollIndicatorPos,
            //                  rollIndicatorPos + transform.right * (currentAvoidanceRollAngle / avoidanceRollMaxAngle) * 0.5f,
            //                  Color.green);
            //}
        }

        // 绘制视觉感知范围
        float detectionAngle = obstacleDetectionAngle;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = (i - (rayCount - 1) / 2f) * (detectionAngle * 2) / (rayCount - 1);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
            Debug.DrawRay(transform.position, direction * visualRange, Color.cyan);
        }

        //// 额外绘制紧急避障范围
        //Debug.DrawRay(transform.position, transform.forward * minObstacleAvoidanceDistance, Color.red);

        //绘制噪声场影响范围
        if (enableCurlNoise && showNoiseForce)
        {
            // 在蜜蜂周围采样几个点绘制噪声场方向
            int sampleCount = 8;
            float sampleRadius = 2.0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float angle = i * (360f / sampleCount);
                Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * sampleRadius;
                Vector3 samplePos = transform.position + offset;
                Vector3 noiseForce = CurlNoiseField.GetCurlNoiseForce(samplePos);

                if (noiseForce.magnitude > 0.01f)
                {
                    Debug.DrawRay(samplePos, noiseForce.normalized * 0.5f, new Color(0f, 1f, 1f, 0.5f));
                }
            }
        }
    }
    #endregion

}