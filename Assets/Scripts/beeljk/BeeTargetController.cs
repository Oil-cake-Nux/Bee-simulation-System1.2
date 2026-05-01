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
        public bool enableHover = true;

        [Header("悬停行为")]
        public float minHoverDistance = 0.3f;
        public float maxHoverDistance = 3f;
        public float heightAdjustSpeed = 1.5f;
        public float hoverSettleRadius = 0.65f;
        public float hoverBrakeDamping = 2.5f;
        public float hoverBobAmplitude = 0.12f;
        public bool useTargetRendererBounds = true;
        public float hoverSurfaceClearance = 0.35f;

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

        public Vector3 AutopilotCollectPoint
        {
            get { return GetAutopilotCollectPoint(); }
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
        private int learnedRouteIndex = 0;
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

            Vector3 collectPoint = GetAutopilotCollectPoint();
            float distance = Vector3.Distance(transform.position, collectPoint);
            float horizontalDistance = Vector3.ProjectOnPlane(transform.position - currentTarget.position, Vector3.up).magnitude;

            if (enableHover && !isHovering && (distance <= stoppingDistance || horizontalDistance <= stoppingDistance))
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

            if (HasSettledAtHoverPoint())
            {
                stayTimer += Time.deltaTime;
            }
            else
            {
                stayTimer = Mathf.Max(0f, stayTimer - Time.deltaTime * 0.5f);
            }

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

            Vector3 seekPoint = enableHover ? GetAutopilotCollectPoint() : currentTarget.position;
            Vector3 toTarget = seekPoint - transform.position;
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

        private Vector3 GetAutopilotCollectPoint()
        {
            if (currentTarget == null)
            {
                return transform.position;
            }

            return GetTargetSurfaceCollectPoint(currentTarget, hoverHeight);
        }

        private Vector3 GetTargetSurfaceCollectPoint(Transform targetTransform, float fallbackHeight)
        {
            if (targetTransform == null)
            {
                return transform.position;
            }

            Bounds targetBounds;
            if (useTargetRendererBounds && TryGetTargetBounds(targetTransform, out targetBounds))
            {
                Vector3 collectPoint = targetBounds.center;
                collectPoint.y = targetBounds.max.y + hoverSurfaceClearance;
                return collectPoint;
            }

            return targetTransform.position + Vector3.up * fallbackHeight;
        }

        private bool TryGetTargetBounds(Transform targetTransform, out Bounds targetBounds)
        {
            Renderer[] renderers = targetTransform.GetComponentsInChildren<Renderer>();
            targetBounds = new Bounds(targetTransform.position, Vector3.zero);
            bool hasBounds = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer targetRenderer = renderers[i];
                if (targetRenderer == null || !targetRenderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    targetBounds = targetRenderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    targetBounds.Encapsulate(targetRenderer.bounds);
                }
            }

            return hasBounds;
        }

        private void StartHovering()
        {
            if (currentTarget == null)
            {
                return;
            }

            isHovering = true;
            stayTimer = 0f;
            hoverCenter = GetAutopilotCollectPoint();
            currentHoverHeight = GetAutopilotCollectPoint().y;
            hoverOrbitAngle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            UpdateHoverTarget(true);
        }

        private void UpdateHoverBehavior()
        {
            hoverCenter = currentTarget != null ? GetAutopilotCollectPoint() : hoverCenter;

            float targetHeight = hoverCenter.y;
            currentHoverHeight = Mathf.Lerp(currentHoverHeight, targetHeight, Time.deltaTime * heightAdjustSpeed);
            hoverOrbitAngle += hoverSpeed * Time.deltaTime;

            UpdateHoverTarget(false);

            float distanceToHoverTarget = Vector3.Distance(transform.position, currentHoverTarget);
            if (distanceToHoverTarget < minHoverDistance * 0.5f)
            {
                hoverOrbitAngle += Mathf.PI * 0.5f;
            }
        }

        private void UpdateHoverTarget(bool snap)
        {
            float visitRadius = Mathf.Min(hoverRadius, Mathf.Max(0.05f, stoppingDistance * 0.35f));
            float verticalBob = Mathf.Sin(hoverOrbitAngle * 2.1f) * hoverBobAmplitude;

            Vector3 desiredOffset = new Vector3(
                Mathf.Cos(hoverOrbitAngle) * visitRadius,
                currentHoverHeight - hoverCenter.y + verticalBob,
                Mathf.Sin(hoverOrbitAngle * 0.85f) * visitRadius
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
            if (distance < 0.001f && beeSimulation.velocity.sqrMagnitude < 0.001f)
            {
                return Vector3.zero;
            }

            float desiredSpeed = Mathf.Lerp(0f, hoverSpeed, Mathf.Clamp01(distance / Mathf.Max(0.01f, maxHoverDistance)));

            Vector3 desiredVelocity = distance > 0.001f ? toHoverTarget.normalized * desiredSpeed : Vector3.zero;
            Vector3 steeringForce = (desiredVelocity - beeSimulation.velocity) * beeSimulation.steeringPD_Kp;
            Vector3 positionCorrection = toHoverTarget * 1.15f;
            Vector3 dampingForce = -beeSimulation.velocity * hoverBrakeDamping;
            Vector3 totalForce = (steeringForce + positionCorrection + dampingForce) * beeSimulation.targetAttractionWeight;

            return Vector3.ClampMagnitude(totalForce, beeSimulation.maxForce * 0.8f);
        }

        private bool HasSettledAtHoverPoint()
        {
            Vector3 toHoverTarget = currentHoverTarget - transform.position;
            float allowedDistance = Mathf.Max(hoverSettleRadius, minHoverDistance);
            return toHoverTarget.magnitude <= allowedDistance;
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

            hoverCenter = GetTargetSurfaceCollectPoint(currentTarget, playerCollectHeightOffset);
            playerCollectPhase += Time.deltaTime * Mathf.Max(0.1f, hoverSpeed * 2.4f);

            Vector3 microOffset = new Vector3(
                Mathf.Sin(playerCollectPhase * 1.1f),
                0f,
                Mathf.Cos(playerCollectPhase * 0.9f)
            ) * playerCollectMicroHoverRadius;

            microOffset.y = Mathf.Sin(playerCollectPhase * 2.2f) * playerCollectBobAmplitude;

            Vector3 desiredPoint = hoverCenter + microOffset;

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
