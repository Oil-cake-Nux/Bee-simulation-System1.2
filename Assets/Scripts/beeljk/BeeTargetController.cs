
using System.Collections.Generic;
using UnityEngine;

namespace ljk {
    public class BeeTargetController : MonoBehaviour
    {
        public BeeSimulation beeSimulation;
        // 目标相关参数
        public List<Transform> targets = new List<Transform>();
        public float targetDetectionRadius = 20f;    // 目标检测半径
        public float stayDuration = 2f;            // 目标点停留时间
        public Transform currentTarget;           // 当前追踪目标
        private float stayTimer;                   // 停留计时器
        private List<Transform> inactiveTargets = new List<Transform>(); // 失活目标列表

        [Header("目标到达参数")]
        public float stoppingDistance = 1.5f;           // 停止距离
        public float hoverHeight = 2f;                  // 悬停高度
        public float hoverRadius = 1.5f;                // 悬停半径
        public float hoverSpeed = 0.8f;                 // 悬停速度
        public float hoverSmoothness = 2f;              // 悬停平滑度
        public bool enableHover = false;                 // 是否启用悬停

        [Header("悬停行为参数")]
        public float minHoverDistance = 0.3f;           // 最小悬停距离
        public float maxHoverDistance = 3f;             // 最大悬停距离
        public float heightAdjustSpeed = 1.5f;          // 高度调整速度

        private bool isHovering = false;
        private Vector3 hoverCenter;
        private float hoverAngle = 0f;
        private Vector3 currentHoverTarget;
        private float currentHoverHeight;

        // 添加公共属性供外部访问
        public bool IsHovering { get { return isHovering; } }
        public Vector3 HoverCenter { get { return hoverCenter; } }

        GameObject target2;
        Transform t2t;
        private void Start()
        {
            beeSimulation = GetComponent<BeeSimulation>();
            ///以下为调试代码
        }
        //private void Update()
        //{
        //    // 更新目标状态
        //    UpdateTargetState();

        //    // 计算并应用力
        //    Vector3 seekForce = CalculateTargetSeekForce();
        //    // 这里添加实际应用力的逻辑（示例）：
        //    // velocity += seekForce * Time.deltaTime;
        //    // transform.position += velocity * Time.deltaTime;
        //}

        public void UpdateTargetState()
        {
            if (currentTarget != null)
            {
                float distance = Vector3.Distance(transform.position, currentTarget.position);

                // 改进的悬停触发条件
                if (distance <= stoppingDistance && enableHover && !isHovering)
                {
                    StartHovering();
                }

                if (isHovering)
                {
                    UpdateHoverBehavior();

                    // 在悬停状态下累积停留时间
                    stayTimer += Time.deltaTime;

                    if (stayTimer >= stayDuration)
                    {
                        EndHovering();
                    }
                }
                else
                {
                    // 离开到达范围时重置计时器
                    if (distance > stoppingDistance * 1.2f)
                    {
                        stayTimer = 0f;
                    }
                }
            }
            else
            {
                // 没有当前目标时寻找新目标
                FindNewTarget();
            }
        }

        /// <summary>
        /// 开始悬停行为
        /// </summary>
        private void StartHovering()
        {
            isHovering = true;
            hoverCenter = currentTarget.position;
            currentHoverHeight = hoverCenter.y + hoverHeight;
            hoverAngle = Random.Range(0f, 360f);

            // 初始化悬停目标点
            UpdateHoverTarget();

            Debug.Log("开始悬停行为");
        }

        /// <summary>
        /// 更新悬停行为
        /// </summary>
        private void UpdateHoverBehavior()
        {
            // 平滑调整悬停高度
            float targetHeight = hoverCenter.y + hoverHeight;
            currentHoverHeight = Mathf.Lerp(currentHoverHeight, targetHeight, Time.deltaTime * heightAdjustSpeed);

            // 更新悬停角度
            hoverAngle += hoverSpeed * Time.deltaTime;

            // 更新悬停目标点
            UpdateHoverTarget();

            // 检查是否需要重新调整位置
            float distanceToHoverTarget = Vector3.Distance(transform.position, currentHoverTarget);
            if (distanceToHoverTarget < minHoverDistance)
            {
                // 到达悬停点，选择新的悬停点
                hoverAngle += 90f; // 跳转到下一个象限
                UpdateHoverTarget();
            }
        }

        private void UpdateHoverTarget()
        {
            // 计算圆周运动位置
            float x = Mathf.Cos(hoverAngle) * hoverRadius;
            float z = Mathf.Sin(hoverAngle) * hoverRadius;

            currentHoverTarget = hoverCenter + new Vector3(x, currentHoverHeight - hoverCenter.y, z);
        }

        /// <summary>
        /// 结束悬停行为
        /// </summary>
        private void EndHovering()
        {
            inactiveTargets.Add(currentTarget);
            currentTarget = null;
            stayTimer = 0f;
            isHovering = false;
            Debug.Log("结束悬停行为");
        }

        public void FindNewTarget()
        {
            Transform bestTarget = null;
            float closestDistance = Mathf.Infinity;

            foreach (Transform t in targets)
            {
                if (t == null || inactiveTargets.Contains(t)) continue;

                Vector3 toTarget = t.position - transform.position;
                float distance = toTarget.magnitude;

                // 选择检测范围内最近的有效目标
                if (distance <= targetDetectionRadius &&
                    distance < closestDistance)
                {
                    closestDistance = distance;
                    bestTarget = t;
                }
            }

            currentTarget = bestTarget;
        }

        public Vector3 CalculateTargetSeekForce()
        {
            if (currentTarget == null) return Vector3.zero;

            // 悬停状态使用悬停力计算
            if (isHovering && enableHover)
            {
                return CalculateHoverForce();
            }

            Vector3 toTargetDir = currentTarget.position - transform.position;
            float targetDistance = toTargetDir.magnitude;
            Vector3 desiredDirection = toTargetDir.normalized;

            // 改进的减速曲线 - 更平滑的减速
            float desiredSpeed = beeSimulation.moveSpeed;

            if (targetDistance < stoppingDistance)
            {
                // 在停止距离内，使用悬停速度
                desiredSpeed = hoverSpeed;
            }
            else if (targetDistance < beeSimulation.slowingRadius)
            {
                // 平滑减速过渡
                float slowdownFactor = (targetDistance - stoppingDistance) /
                                     (beeSimulation.slowingRadius - stoppingDistance);
                slowdownFactor = Mathf.Clamp01(slowdownFactor);
                // 使用缓动函数使减速更自然
                desiredSpeed = Mathf.Lerp(hoverSpeed, beeSimulation.moveSpeed, slowdownFactor * slowdownFactor);
            }

            // 计算转向力
            Vector3 desiredVelocity = desiredDirection * desiredSpeed;
            Vector3 steeringForce = (desiredVelocity - beeSimulation.velocity) * beeSimulation.steeringPD_Kp;

            // 距离因子 - 接近目标时减小力
            float distanceFactor = Mathf.Clamp01(targetDistance / beeSimulation.slowingRadius);

            return steeringForce * beeSimulation.targetAttractionWeight * (0.5f + distanceFactor * 0.5f);
        }

        /// <summary>
        /// 计算悬停行为力
        /// </summary>
        private Vector3 CalculateHoverForce()
        {
            Vector3 toHoverTarget = currentHoverTarget - transform.position;
            float distanceToTarget = toHoverTarget.magnitude;

            // 计算期望速度 - 基于距离的动态速度
            float desiredSpeed = hoverSpeed;
            if (distanceToTarget < 1f)
            {
                desiredSpeed = Mathf.Lerp(0.1f, hoverSpeed, distanceToTarget);
            }

            Vector3 desiredDirection = toHoverTarget.normalized;
            Vector3 desiredVelocity = desiredDirection * desiredSpeed;

            // 改进的转向力计算 - 添加阻尼项
            Vector3 velocityError = desiredVelocity - beeSimulation.velocity;
            Vector3 steeringForce = velocityError * beeSimulation.steeringPD_Kp;

            // 添加位置修正力 - 帮助蜜蜂保持在正确位置
            Vector3 positionCorrection = toHoverTarget * 0.5f;

            // 限制力的大小
            float maxHoverForce = beeSimulation.maxForce * 0.3f;
            Vector3 totalForce = (steeringForce + positionCorrection) * beeSimulation.targetAttractionWeight * 0.8f;

            if (totalForce.magnitude > maxHoverForce)
            {
                totalForce = totalForce.normalized * maxHoverForce;
            }

            return totalForce;
        }

        // 改进可视化方法
        public void OnDrawGizmosSelected()
        {
            //// 绘制检测范围
            //Gizmos.color = Color.yellow;
            //Gizmos.DrawWireSphere(transform.position, targetDetectionRadius);

            // 绘制当前目标
            if (currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, currentTarget.position);

                // 绘制停止距离
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(currentTarget.position, stoppingDistance);

                //// 绘制悬停区域
                //if (enableHover)
                //{
                //    Gizmos.color = Color.blue;
                //    Gizmos.DrawWireSphere(currentTarget.position + Vector3.up * hoverHeight, hoverRadius);

                //    // 绘制当前悬停目标点
                //    if (isHovering)
                //    {
                //        Gizmos.color = Color.cyan;
                //        Gizmos.DrawSphere(currentHoverTarget, 0.2f);
                //        Gizmos.DrawLine(transform.position, currentHoverTarget);
                //    }
                //}
            }
        }
    }
}
