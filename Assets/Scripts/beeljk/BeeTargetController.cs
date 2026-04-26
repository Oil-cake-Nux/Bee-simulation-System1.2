using System;
using System.Collections.Generic;
using UnityEngine;

namespace ljk
{
    public class BeeTargetController : MonoBehaviour
    {
        [Header("基础引用")]
        public BeeSimulation beeSimulation;
        public List<Transform> targets = new List<Transform>();

        [Header("目标搜索")]
        public float targetDetectionRadius = 20f;
        public float stayDuration = 2f;
        public Transform currentTarget;

        [Header("到达设置")]
        public float stoppingDistance = 1.5f;
        public float hoverHeight = 2f;
        public float hoverRadius = 1.5f;
        public float hoverSpeed = 0.8f;
        public float hoverSmoothness = 2f;
        public bool enableHover = false;

        [Header("悬停行为")]
        public float minHoverDistance = 0.3f;
        public float maxHoverDistance = 3f;
        public float heightAdjustSpeed = 1.5f;

        [Header("玩家采集")]
        public bool enablePlayerCollect = true;
        public float playerCollectTriggerRadius = 1.8f;
        public float playerCollectMinHeight = 0.2f;
        public float playerCollectMaxHeight = 4f;
        public float playerCollectHeightOffset = 1.15f;
        public float playerCollectDuration = 2.4f;
        public float playerCollectForce = 4.8f;
        public float playerCollectDamping = 1.5f;
        public float playerCollectMicroHoverRadius = 0.08f;
        public float playerCollectBobAmplitude = 0.05f;
        public float playerCollectRearmDistance = 2.4f;

        [Header("路线循环")]
        public bool enableTargetCycleLoop = true;
        public bool enableStableRouteReplay = true;

        public bool IsHovering
        {
            get { return isHovering; }
        }

        public Vector3 HoverCenter
        {
            get { return hoverCenter; }
        }

        public bool HasActiveTarget
        {
            get { return currentTarget != null; }
        }

        public bool IsPlayerCollecting
        {
            get { return isPlayerCollecting; }
        }

        public string CurrentStateLabel
        {
            get
            {
                if (isPlayerCollecting)
                {
                    return "悬停采集";
                }

                if (CanUseAutopilot && isHovering)
                {
                    return "目标悬停";
                }

                if (currentTarget != null)
                {
                    return CanUseAutopilot ? "寻花中" : "接近采集点";
                }

                return CanUseAutopilot ? "自动巡航" : "手动巡航";
            }
        }

        public int CollectedTargetCount
        {
            get { return collectedTargetCount; }
        }

        public Vector3 PlayerCollectPoint
        {
            get { return playerCollectPoint; }
        }

        private readonly List<Transform> inactiveTargets = new List<Transform>();
        private readonly List<Transform> visitedTargetsThisCycle = new List<Transform>();
        private readonly List<Transform> learnedRoute = new List<Transform>();
        private float stayTimer = 0f;
        private bool isHovering = false;
        private Vector3 hoverCenter = Vector3.zero;
        private float hoverOrbitAngle = 0f;
        private Vector3 currentHoverTarget = Vector3.zero;
        private float currentHoverHeight = 0f;
        private bool isPlayerCollecting = false;
        private Vector3 playerCollectPoint = Vector3.zero;
        private float playerCollectTimer = 0f;
        private float playerCollectPhase = 0f;
        private Transform lastCollectedPlayerTarget;
        private int collectedTargetCount = 0;

        private bool CanUseAutopilot
        {
            get { return beeSimulation != null && beeSimulation.UsesAutopilot; }
        }

        private void Awake()
        {
            if (beeSimulation == null)
            {
                beeSimulation = GetComponent<BeeSimulation>();
            }
        }

        public void UpdateTargetState()
        {
            if (CanUseAutopilot)
            {
                UpdateAutopilotState();
                return;
            }

            UpdatePlayerCollectState();
        }

        private void UpdateAutopilotState()
        {
            if (isPlayerCollecting)
            {
                ResetPlayerCollectState();
            }

            if (currentTarget == null)
            {
                FindNewTarget();
                return;
            }

            float distance = Vector3.Distance(transform.position, currentTarget.position);

            if (enableHover && !isHovering && distance <= stoppingDistance)
            {
                StartHovering();
            }

            if (!isHovering)
            {
                if (distance > stoppingDistance * 1.2f)
                {
                    stayTimer = 0f;
                }

                return;
            }

            UpdateHoverBehavior();
            stayTimer += Time.deltaTime;

            if (stayTimer >= stayDuration)
            {
                EndHovering();
            }
        }

        private void UpdatePlayerCollectState()
        {
            if (!enablePlayerCollect)
            {
                ResetPlayerCollectState();
                currentTarget = null;
                return;
            }

            ResetAutopilotHoverState();
            TryRearmPlayerCollect();

            if (isPlayerCollecting)
            {
                if (currentTarget == null)
                {
                    EndPlayerCollect(false);
                    return;
                }

                UpdatePlayerCollectPoint(false);
                playerCollectTimer += Time.deltaTime;

                if (playerCollectTimer >= playerCollectDuration)
                {
                    EndPlayerCollect(true);
                }

                return;
            }

            currentTarget = FindBestPlayerCollectTarget();
            if (currentTarget == null)
            {
                return;
            }

            if (CanStartPlayerCollect(currentTarget))
            {
                StartPlayerCollect(currentTarget);
            }
        }

        public Vector3 CalculateTargetSeekForce()
        {
            if (!CanUseAutopilot || currentTarget == null || beeSimulation == null)
            {
                return Vector3.zero;
            }

            if (isHovering && enableHover)
            {
                return CalculateHoverForce();
            }

            Vector3 toTarget = currentTarget.position - transform.position;
            float distance = toTarget.magnitude;
            if (distance < 0.001f)
            {
                return Vector3.zero;
            }

            float desiredSpeed = beeSimulation.moveSpeed;
            if (distance < stoppingDistance)
            {
                desiredSpeed = hoverSpeed;
            }
            else if (distance < beeSimulation.slowingRadius)
            {
                float slowdownFactor = Mathf.InverseLerp(stoppingDistance, beeSimulation.slowingRadius, distance);
                desiredSpeed = Mathf.Lerp(hoverSpeed, beeSimulation.moveSpeed, slowdownFactor * slowdownFactor);
            }

            Vector3 desiredVelocity = toTarget.normalized * desiredSpeed;
            Vector3 steeringForce = (desiredVelocity - beeSimulation.velocity) * beeSimulation.steeringPD_Kp;
            return Vector3.ClampMagnitude(steeringForce * beeSimulation.targetAttractionWeight, beeSimulation.maxForce);
        }

        public void FindNewTarget()
        {
            if (!CanUseAutopilot)
            {
                currentTarget = null;
                return;
            }

            currentTarget = FindAutopilotTarget();
        }

        private Transform FindBestPlayerCollectTarget()
        {
            return FindClosestTarget(IsPlayerCollectTargetCandidate);
        }

        private Transform FindClosestTarget(Predicate<Transform> candidateFilter)
        {
            Transform bestTarget = null;
            float maxSqrDistance = targetDetectionRadius * targetDetectionRadius;
            float closestSqrDistance = float.MaxValue;
            Vector3 currentPosition = transform.position;

            foreach (Transform candidate in targets)
            {
                if (candidate == null || (candidateFilter != null && !candidateFilter(candidate)))
                {
                    continue;
                }

                float sqrDistance = (candidate.position - currentPosition).sqrMagnitude;
                if (sqrDistance > maxSqrDistance || sqrDistance >= closestSqrDistance)
                {
                    continue;
                }

                closestSqrDistance = sqrDistance;
                bestTarget = candidate;
            }

            return bestTarget;
        }

        private Transform FindAutopilotTarget()
        {
            Transform nextTarget = FindRouteReplayTarget();
            if (nextTarget != null)
            {
                return nextTarget;
            }

            nextTarget = FindClosestTarget(IsAutopilotTargetCandidate);
            if (nextTarget != null)
            {
                return nextTarget;
            }

            if (!enableTargetCycleLoop || inactiveTargets.Count == 0)
            {
                return null;
            }

            CompleteAutopilotCycle();

            nextTarget = FindRouteReplayTarget();
            if (nextTarget != null)
            {
                return nextTarget;
            }

            return FindClosestTarget(IsAutopilotTargetCandidate);
        }

        private Transform FindRouteReplayTarget()
        {
            if (!enableStableRouteReplay || learnedRoute.Count == 0)
            {
                return null;
            }

            int validCount = learnedRoute.Count;
            for (int i = 0; i < validCount; i++)
            {
                int index = (learnedRouteIndex + i) % validCount;
                Transform candidate = learnedRoute[index];
                if (candidate == null || inactiveTargets.Contains(candidate))
                {
                    continue;
                }

                learnedRouteIndex = (index + 1) % validCount;
                return candidate;
            }

            return null;
        }

        private void CompleteAutopilotCycle()
        {
            if (enableStableRouteReplay && visitedTargetsThisCycle.Count > 1)
            {
                learnedRoute.Clear();
                learnedRoute.AddRange(visitedTargetsThisCycle);
                learnedRouteIndex = 0;
            }

            inactiveTargets.Clear();
            visitedTargetsThisCycle.Clear();
        }

        private bool IsAutopilotTargetCandidate(Transform candidate)
        {
            return !inactiveTargets.Contains(candidate);
        }

        private bool IsPlayerCollectTargetCandidate(Transform candidate)
        {
            return candidate != lastCollectedPlayerTarget;
        }

        private bool CanStartPlayerCollect(Transform candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            Vector3 toTarget = transform.position - candidate.position;
            float horizontalDistance = Vector3.ProjectOnPlane(toTarget, Vector3.up).magnitude;
            float verticalOffset = toTarget.y;

            return horizontalDistance <= playerCollectTriggerRadius &&
                   verticalOffset >= playerCollectMinHeight &&
                   verticalOffset <= playerCollectMaxHeight;
        }

        private void StartHovering()
        {
            if (currentTarget == null)
            {
                return;
            }

            isHovering = true;
            stayTimer = 0f;
            hoverCenter = currentTarget.position;
            currentHoverHeight = hoverCenter.y + hoverHeight;
            hoverOrbitAngle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            UpdateHoverTarget(true);
        }

        private void UpdateHoverBehavior()
        {
            float targetHeight = hoverCenter.y + hoverHeight;
            currentHoverHeight = Mathf.Lerp(currentHoverHeight, targetHeight, Time.deltaTime * heightAdjustSpeed);
            hoverOrbitAngle += hoverSpeed * Time.deltaTime;

            UpdateHoverTarget(false);

            float distanceToHoverTarget = Vector3.Distance(transform.position, currentHoverTarget);
            if (distanceToHoverTarget < minHoverDistance)
            {
                hoverOrbitAngle += Mathf.PI * 0.5f;
                UpdateHoverTarget(true);
            }
        }

        private void UpdateHoverTarget(bool snap)
        {
            Vector3 desiredOffset = new Vector3(
                Mathf.Cos(hoverOrbitAngle) * hoverRadius,
                currentHoverHeight - hoverCenter.y,
                Mathf.Sin(hoverOrbitAngle) * hoverRadius
            );

            Vector3 desiredTarget = hoverCenter + desiredOffset;

            if (snap)
            {
                currentHoverTarget = desiredTarget;
                return;
            }

            currentHoverTarget = Vector3.Lerp(currentHoverTarget, desiredTarget, Time.deltaTime * hoverSmoothness);
        }

        private Vector3 CalculateHoverForce()
        {
            Vector3 toHoverTarget = currentHoverTarget - transform.position;
            float distance = toHoverTarget.magnitude;
            if (distance < 0.001f)
            {
                return Vector3.zero;
            }

            float desiredSpeed = hoverSpeed;
            if (distance < maxHoverDistance)
            {
                desiredSpeed = Mathf.Lerp(0.1f, hoverSpeed, Mathf.Clamp01(distance / Mathf.Max(0.01f, maxHoverDistance)));
            }

            Vector3 desiredVelocity = toHoverTarget.normalized * desiredSpeed;
            Vector3 steeringForce = (desiredVelocity - beeSimulation.velocity) * beeSimulation.steeringPD_Kp;
            Vector3 positionCorrection = toHoverTarget * 0.5f;
            Vector3 totalForce = (steeringForce + positionCorrection) * beeSimulation.targetAttractionWeight;

            return Vector3.ClampMagnitude(totalForce, beeSimulation.maxForce * 0.5f);
        }

        public Vector3 CalculatePlayerCollectForce()
        {
            if (beeSimulation == null || !isPlayerCollecting || currentTarget == null)
            {
                return Vector3.zero;
            }

            Vector3 toCollectPoint = playerCollectPoint - transform.position;
            float distance = toCollectPoint.magnitude;

            Vector3 desiredVelocity = Vector3.zero;
            if (distance > 0.001f)
            {
                float approachSpeed = Mathf.Lerp(0.15f, hoverSpeed, Mathf.Clamp01(distance / Mathf.Max(0.01f, maxHoverDistance)));
                desiredVelocity = toCollectPoint.normalized * approachSpeed;
            }

            Vector3 steeringForce = (desiredVelocity - beeSimulation.velocity) * playerCollectForce;
            Vector3 positionCorrection = toCollectPoint * playerCollectForce * 0.35f;
            Vector3 dampingForce = -beeSimulation.velocity * playerCollectDamping;
            Vector3 totalForce = steeringForce + positionCorrection + dampingForce;

            return Vector3.ClampMagnitude(totalForce, beeSimulation.maxForce * 0.85f);
        }

        private void StartPlayerCollect(Transform targetToCollect)
        {
            currentTarget = targetToCollect;
            isPlayerCollecting = true;
            playerCollectTimer = 0f;
            playerCollectPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            hoverCenter = currentTarget.position;
            UpdatePlayerCollectPoint(true);
        }

        private void UpdatePlayerCollectPoint(bool snap)
        {
            if (currentTarget == null)
            {
                return;
            }

            hoverCenter = currentTarget.position;
            playerCollectPhase += Time.deltaTime * Mathf.Max(0.1f, hoverSpeed * 2.4f);

            Vector3 microOffset = new Vector3(
                Mathf.Sin(playerCollectPhase * 1.1f),
                0f,
                Mathf.Cos(playerCollectPhase * 0.9f)
            ) * playerCollectMicroHoverRadius;

            microOffset.y = Mathf.Sin(playerCollectPhase * 2.2f) * playerCollectBobAmplitude;

            Vector3 desiredPoint = hoverCenter + Vector3.up * playerCollectHeightOffset + microOffset;

            if (snap)
            {
                playerCollectPoint = desiredPoint;
            }
            else
            {
                playerCollectPoint = Vector3.Lerp(playerCollectPoint, desiredPoint, Time.deltaTime * hoverSmoothness * 2f);
            }

            currentHoverTarget = playerCollectPoint;
            currentHoverHeight = playerCollectPoint.y;
        }

        private void EndPlayerCollect(bool markCollectedTarget)
        {
            if (markCollectedTarget)
            {
                lastCollectedPlayerTarget = currentTarget;
                collectedTargetCount++;
            }

            isPlayerCollecting = false;
            playerCollectTimer = 0f;
            playerCollectPoint = Vector3.zero;
            currentTarget = null;
        }

        private void TryRearmPlayerCollect()
        {
            if (lastCollectedPlayerTarget == null)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, lastCollectedPlayerTarget.position);
            if (distance > playerCollectRearmDistance)
            {
                lastCollectedPlayerTarget = null;
            }
        }

        private void EndHovering()
        {
            if (currentTarget != null)
            {
                inactiveTargets.Add(currentTarget);

                if (!visitedTargetsThisCycle.Contains(currentTarget))
                {
                    visitedTargetsThisCycle.Add(currentTarget);
                }
            }

            currentTarget = null;
            stayTimer = 0f;
            isHovering = false;
        }

        private void ResetAutopilotHoverState()
        {
            stayTimer = 0f;
            isHovering = false;
            hoverCenter = Vector3.zero;
            currentHoverTarget = Vector3.zero;
            currentHoverHeight = 0f;
        }

        private void ResetPlayerCollectState()
        {
            isPlayerCollecting = false;
            playerCollectTimer = 0f;
            playerCollectPoint = Vector3.zero;
            lastCollectedPlayerTarget = null;
            currentTarget = null;
            inactiveTargets.Clear();
            visitedTargetsThisCycle.Clear();
            learnedRoute.Clear();
            learnedRouteIndex = 0;
            ResetAutopilotHoverState();
        }

        public void OnDrawGizmosSelected()
        {
            if (currentTarget == null)
            {
                return;
            }

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentTarget.position, stoppingDistance);

            if (isPlayerCollecting)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(playerCollectPoint, 0.15f);
                Gizmos.DrawLine(transform.position, playerCollectPoint);
                return;
            }

            if (!isHovering)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(currentHoverTarget, 0.15f);
            Gizmos.DrawLine(transform.position, currentHoverTarget);
        }
    }
}
