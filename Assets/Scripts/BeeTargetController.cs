using UnityEngine;
using System.Collections.Generic;

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
    GameObject target2;
    Transform t2t;
    private void Start()
    {
        beeSimulation=GetComponent<BeeSimulation>();
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
            // 计算与当前目标的距离
            float distance = Vector3.Distance(transform.position, currentTarget.position);
            //Debug.Log(distance);
            if (distance <= beeSimulation.arrivalRadius)
            {
                // 在到达范围内时累积停留时间
                stayTimer += Time.deltaTime;
                //Debug.Log(stayTimer);
                if (stayTimer >= stayDuration)
                {
                    // 标记目标为失活状态
                    inactiveTargets.Add(currentTarget);
                    currentTarget = null;
                    stayTimer = 0f;
                }
            }
            else
            {
                // 离开到达范围时重置计时器
                stayTimer = 0f;
                //Debug.Log(stayTimer);
            }
        }
        else
        {
            // 没有当前目标时寻找新目标
            FindNewTarget();
        }
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

        Vector3 toTargetDir = currentTarget.position - transform.position;
        float targetDistance = toTargetDir.magnitude;
        Vector3 desiredDirection = toTargetDir.normalized;

        // 计算期望速度
        float desiredSpeed = beeSimulation.moveSpeed;
        if (targetDistance < beeSimulation.arrivalRadius)
        {
            desiredSpeed = 1;
        }
        else if (targetDistance < beeSimulation.slowingRadius)
        {
            desiredSpeed = beeSimulation.moveSpeed * (targetDistance / beeSimulation.slowingRadius);
        }

        // 计算转向力
        Vector3 desiredVelocity = desiredDirection * desiredSpeed;
        Vector3 steeringForce = (desiredVelocity - beeSimulation.velocity) * beeSimulation.steeringPD_Kp;

        // 加入距离因子
        float distanceFactor = Mathf.Clamp01(targetDistance / (beeSimulation.slowingRadius * 2));

        return steeringForce * beeSimulation.targetAttractionWeight * (1 + distanceFactor);
    }

    // 可视化辅助工具
    public void OnDrawGizmosSelected()
    {
        // 绘制检测范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, targetDetectionRadius);

        // 绘制当前目标
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
}