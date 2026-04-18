using UnityEngine;

namespace ljk
{
    public class BeeSimulation : MonoBehaviour
    {
        public enum FlightControlMode
        {
            Player = 0,
            AutopilotTargets = 1
        }

        [Header("控制模式")]
        public FlightControlMode controlMode = FlightControlMode.Player;
        public Transform visualRoot;
        public KeyCode ascendKey = KeyCode.Space;
        public KeyCode descendKey = KeyCode.LeftControl;
        public KeyCode boostKey = KeyCode.LeftShift;
        public float turnSpeed = 120f;
        public float maxClimbSpeed = 3f;
        public float brakeDamping = 4f;
        public float boostMultiplier = 1.25f;
        public float turnBankAngle = 28f;
        public float climbPitchAngle = 16f;
        public float divePitchAngle = 12f;
        public float modelTiltSmoothing = 8f;

        [Header("蜜蜂运动")]
        public float beeRadius = 0.2f;
        public float moveSpeed = 2f;

        [Header("振翅扰动")]
        public double l;
        public float oscillationForceWeight = 0.5f;
        [Range(0f, 90f)]
        public float pitchAngle = 20f;
        public float thrustFactor = 16.429f;
        public float swayFactor = 0.765f;
        public float oscillationFrequency = 1.5f;
        public float targetHeight = 10f;
        public float heightDampingFactor = 0.5f;

        [Header("障碍规避")]
        public float visualRange = 5f;
        public float obstacleDetectionAngle = 50f;
        public float obstacleAvoidanceWeight = 5f;
        public int rayCount = 20;
        public float emergencyAvoidanceMultiplier = 1.3f;

        [Header("地面规避")]
        public float groundAvoidanceHeight = 3f;
        public float groundAvoidanceWeight = 8f;
        public LayerMask groundLayerMask = 1;

        [Header("避障侧倾")]
        public float avoidanceRollMaxAngle = 60f;
        public float avoidanceRollSpeed = 3f;
        public float rollRecoverySpeed = 2f;
        public bool enableAvoidanceRoll = true;

        [Header("噪声扰动")]
        public float curlNoiseWeight = 1.5f;
        public bool enableCurlNoise = true;
        public float noiseInfluenceRadius = 3f;

        [Header("调试显示")]
        public bool showDebugRays = true;
        public bool showForces = true;
        public bool showNoiseForce = true;

        [Header("目标跟踪")]
        public Transform target;
        public float targetAttractionWeight = 1.5f;
        public float arrivalRadius = 3f;
        public float slowingRadius = 2.5f;
        public float bankingFactor = 1.5f;
        public float maxForce = 7f;

        [Header("高级参数")]
        public float steeringPD_Kp = 2.5f;
        public float steeringPD_Kd = 0.8f;
        public float maxSpeed = 3.5f;
        public float minObstacleAvoidanceDistance = 1f;
        public Vector3 velocity = Vector3.zero;

        public bool UsesAutopilot
        {
            get { return controlMode == FlightControlMode.AutopilotTargets; }
        }

        private bool IsPlayerCollecting
        {
            get { return !UsesAutopilot && beeTarget != null && beeTarget.IsPlayerCollecting; }
        }

        private BeeTargetController beeTarget;
        private Rigidbody rb;
        private Vector3 acceleration = Vector3.zero;
        private Vector3 lastAvoidanceForce = Vector3.zero;
        private Vector3 lastNoiseForce = Vector3.zero;
        private Vector3 lastControlForce = Vector3.zero;
        private Vector3 thrustAxis = Vector3.forward;
        private Vector3 liftAxis = Vector3.up;
        private Vector3 swayAxis = Vector3.right;
        private bool isAvoidingObstacle = false;
        private float obstacleAvoidanceStrength = 0f;
        private float targetAvoidanceRollAngle = 0f;
        private float currentAvoidanceRollAngle = 0f;
        private float bodyPitchAngle = 0f;
        private float bodyRollAngle = 0f;
        private float smoothedTurnInput = 0f;
        private float playerForwardInput = 0f;
        private float playerTurnInput = 0f;
        private float playerVerticalInput = 0f;
        private bool playerBoostInput = false;
        private float playerTargetHeight = 0f;
        private float visualPitch = 0f;
        private float visualRoll = 0f;
        private float originalMaxSpeed = 0f;
        private Transform cachedVisualRoot;
        private Quaternion visualRootBaseRotation = Quaternion.identity;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }

            beeTarget = GetComponent<BeeTargetController>();
            originalMaxSpeed = maxSpeed;

            ConfigureRigidbody();
            CacheVisualRoot();
        }

        private void Start()
        {
            UpdateBodyAxes();
            velocity = rb.velocity;
            playerTargetHeight = transform.position.y;
        }

        private void Update()
        {
            CacheVisualRoot();
            CapturePlayerInput();

            if (showDebugRays)
            {
                DrawDebugVisualization();
            }

            CurlNoiseField.UpdateTime();
        }

        private void FixedUpdate()
        {
            UpdateBodyAxes();
            UpdateDesiredFlightHeight();

            if (beeTarget != null)
            {
                beeTarget.UpdateTargetState();
            }

            if (!UsesAutopilot)
            {
                ApplyPlayerYawRotation();
                ResetAvoidanceState();
            }

            Vector3 oscillationForce = CalculateOscillationForce();
            Vector3 curlNoiseForce = enableCurlNoise ? GetCurlNoiseForce() : Vector3.zero;
            Vector3 obstacleAvoidanceForce = UsesAutopilot ? CalculateObstacleAvoidanceForce() : Vector3.zero;
            Vector3 groundAvoidanceForce = UsesAutopilot ? CalculateGroundAvoidanceForce() : Vector3.zero;
            Vector3 controlForce = UsesAutopilot ? Vector3.zero : CalculatePlayerControlForce();
            Vector3 guidanceForce = CalculateGuidanceForce();
            lastControlForce = controlForce + guidanceForce;

            float controlWeight = UsesAutopilot ? 0f : (IsPlayerCollecting ? 0.2f : 0.9f);
            float guidanceWeight = UsesAutopilot ? 1f : (IsPlayerCollecting ? 1.25f : 0f);
            float buzzWeight = UsesAutopilot ? oscillationForceWeight : Mathf.Max(1.2f, oscillationForceWeight * 2.4f);
            float noiseWeight = UsesAutopilot ? curlNoiseWeight : Mathf.Max(1.4f, curlNoiseWeight * 1.55f);

            Vector3 totalForce = controlForce * controlWeight;
            totalForce += guidanceForce * guidanceWeight;
            totalForce += groundAvoidanceForce * 1.5f;
            totalForce += obstacleAvoidanceForce * obstacleAvoidanceWeight;
            totalForce += oscillationForce * buzzWeight;
            totalForce += curlNoiseForce * noiseWeight;

            totalForce = ClampForce(totalForce);
            ApplyFlightForces(totalForce);

            if (UsesAutopilot)
            {
                ApplyAutopilotYawRotation();
            }

            ApplyBodyPosture();

            if (showNoiseForce && enableCurlNoise && curlNoiseForce.sqrMagnitude > 0.001f)
            {
                Debug.DrawRay(transform.position, curlNoiseForce.normalized, Color.cyan);
            }
        }

        private void ConfigureRigidbody()
        {
            rb.useGravity = false;
            rb.drag = 0f;
            rb.angularDrag = 0f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        private void CapturePlayerInput()
        {
            if (UsesAutopilot)
            {
                playerForwardInput = 0f;
                playerTurnInput = 0f;
                playerVerticalInput = 0f;
                playerBoostInput = false;
                return;
            }

            playerForwardInput = Input.GetAxisRaw("Vertical");
            playerTurnInput = Input.GetAxisRaw("Horizontal");
            playerVerticalInput = 0f;

            if (Input.GetKey(ascendKey))
            {
                playerVerticalInput += 1f;
            }

            if (Input.GetKey(descendKey))
            {
                playerVerticalInput -= 1f;
            }

            playerVerticalInput = Mathf.Clamp(playerVerticalInput, -1f, 1f);
            playerBoostInput = Input.GetKey(boostKey);
        }

        private void UpdateBodyAxes()
        {
            thrustAxis = transform.forward;
            liftAxis = transform.up;
            swayAxis = transform.right;
        }

        private void UpdateDesiredFlightHeight()
        {
            if (UsesAutopilot)
            {
                return;
            }

            if (IsPlayerCollecting && beeTarget != null)
            {
                playerTargetHeight = Mathf.Lerp(playerTargetHeight, beeTarget.PlayerCollectPoint.y, Time.fixedDeltaTime * 5f);
                return;
            }

            playerTargetHeight += playerVerticalInput * maxClimbSpeed * Time.fixedDeltaTime;

            if (playerTargetHeight < 0.5f)
            {
                playerTargetHeight = 0.5f;
            }
        }

        private void CacheVisualRoot()
        {
            Transform desiredVisualRoot = visualRoot;
            if (desiredVisualRoot == null && transform.childCount > 0)
            {
                desiredVisualRoot = transform.GetChild(0);
            }

            if (desiredVisualRoot == cachedVisualRoot)
            {
                return;
            }

            cachedVisualRoot = desiredVisualRoot;
            visualRoot = desiredVisualRoot;

            if (cachedVisualRoot != null)
            {
                visualRootBaseRotation = cachedVisualRoot.localRotation;
            }
        }

        private void ApplyPlayerYawRotation()
        {
            if (IsPlayerCollecting && beeTarget != null && beeTarget.currentTarget != null)
            {
                Vector3 toTarget = Vector3.ProjectOnPlane(beeTarget.currentTarget.position - transform.position, Vector3.up);
                if (toTarget.sqrMagnitude > 0.001f)
                {
                    Quaternion desiredRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.fixedDeltaTime * 5f);
                }

                smoothedTurnInput = Mathf.MoveTowards(smoothedTurnInput, 0f, Time.fixedDeltaTime * 8f);
                return;
            }

            smoothedTurnInput = Mathf.MoveTowards(smoothedTurnInput, playerTurnInput, Time.fixedDeltaTime * 6f);

            float speedFactor = Mathf.Clamp01(Vector3.ProjectOnPlane(velocity, Vector3.up).magnitude / Mathf.Max(0.1f, maxSpeed));
            float effectiveTurnSpeed = turnSpeed * Mathf.Lerp(0.5f, 1f, speedFactor);

            Quaternion yawRotation = Quaternion.Euler(0f, transform.eulerAngles.y + smoothedTurnInput * effectiveTurnSpeed * Time.fixedDeltaTime, 0f);
            transform.rotation = yawRotation;
        }

        private void ApplyAutopilotYawRotation()
        {
            Vector3 flatVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
            if (flatVelocity.sqrMagnitude < 0.01f)
            {
                return;
            }

            Quaternion desiredRotation = Quaternion.LookRotation(flatVelocity.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.fixedDeltaTime * 4f);
        }

        private float GetDesiredFlightHeight()
        {
            if (!UsesAutopilot)
            {
                if (IsPlayerCollecting && beeTarget != null)
                {
                    return beeTarget.PlayerCollectPoint.y;
                }

                return playerTargetHeight;
            }

            if (beeTarget != null)
            {
                if (beeTarget.IsHovering)
                {
                    return beeTarget.HoverCenter.y + beeTarget.hoverHeight;
                }

                if (beeTarget.currentTarget != null)
                {
                    return beeTarget.currentTarget.position.y + Mathf.Max(1.5f, beeTarget.hoverHeight * 0.5f);
                }
            }

            return targetHeight;
        }

        private Vector3 CalculatePlayerControlForce()
        {
            float collectionInputScale = IsPlayerCollecting ? 0.12f : 1f;
            float speedLimit = maxSpeed * (playerBoostInput ? boostMultiplier : 1f);
            float desiredForwardSpeed = Mathf.Max(0f, playerForwardInput) * speedLimit * collectionInputScale;
            float desiredVerticalSpeed = playerVerticalInput * maxClimbSpeed * 0.35f * collectionInputScale;

            Vector3 desiredVelocity = transform.forward * desiredForwardSpeed + Vector3.up * desiredVerticalSpeed;
            Vector3 steeringForce = (desiredVelocity - velocity) * steeringPD_Kp;

            if (Mathf.Abs(playerForwardInput) < 0.05f)
            {
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
                steeringForce += -horizontalVelocity * brakeDamping;
            }
            else if (playerForwardInput < -0.05f)
            {
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
                steeringForce += -horizontalVelocity * brakeDamping * 1.8f;
            }

            if (Mathf.Abs(playerVerticalInput) < 0.05f)
            {
                Vector3 verticalVelocity = Vector3.Project(velocity, Vector3.up);
                steeringForce += -verticalVelocity * brakeDamping * 0.75f;
            }

            if (IsPlayerCollecting)
            {
                steeringForce += -Vector3.ProjectOnPlane(velocity, Vector3.up) * brakeDamping * 0.8f;
            }

            return ClampForce(steeringForce);
        }

        private Vector3 CalculateGuidanceForce()
        {
            if (beeTarget == null)
            {
                return Vector3.zero;
            }

            Vector3 guidanceForce = UsesAutopilot
                ? beeTarget.CalculateTargetSeekForce()
                : beeTarget.CalculatePlayerCollectForce();

            return ClampForce(guidanceForce);
        }

        private void ApplyFlightForces(Vector3 totalForce)
        {
            acceleration = totalForce / Mathf.Max(0.01f, rb.mass);
            velocity += acceleration * Time.fixedDeltaTime;

            float horizontalSpeedLimit = UsesAutopilot ? originalMaxSpeed : maxSpeed * (playerBoostInput ? boostMultiplier : 1f);
            float verticalSpeedLimit = UsesAutopilot ? horizontalSpeedLimit * 0.6f : maxClimbSpeed;

            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
            float verticalSpeed = Vector3.Dot(velocity, Vector3.up);

            if (horizontalVelocity.magnitude > horizontalSpeedLimit)
            {
                horizontalVelocity = horizontalVelocity.normalized * horizontalSpeedLimit;
            }

            verticalSpeed = Mathf.Clamp(verticalSpeed, -verticalSpeedLimit, verticalSpeedLimit);
            velocity = horizontalVelocity + Vector3.up * verticalSpeed;

            rb.velocity = velocity;
        }

        private Vector3 ClampForce(Vector3 force)
        {
            if (force.magnitude > maxForce)
            {
                return force.normalized * maxForce;
            }

            return force;
        }

        private void ResetAvoidanceState()
        {
            isAvoidingObstacle = false;
            obstacleAvoidanceStrength = 0f;
            targetAvoidanceRollAngle = 0f;
            currentAvoidanceRollAngle = Mathf.Lerp(currentAvoidanceRollAngle, 0f, Time.fixedDeltaTime * rollRecoverySpeed);
            lastAvoidanceForce = Vector3.Lerp(lastAvoidanceForce, Vector3.zero, Time.fixedDeltaTime * 6f);
        }

        private Vector3 CalculateGroundAvoidanceForce()
        {
            RaycastHit hit;
            if (!Physics.Raycast(transform.position, Vector3.down, out hit, groundAvoidanceHeight * 2f, groundLayerMask))
            {
                return Vector3.zero;
            }

            if (hit.distance >= groundAvoidanceHeight)
            {
                return Vector3.zero;
            }

            float threat = 1f - hit.distance / Mathf.Max(0.01f, groundAvoidanceHeight);
            Vector3 force = (Vector3.up + transform.forward * 0.35f) * threat * groundAvoidanceWeight;

            if (showForces)
            {
                Debug.DrawRay(transform.position, force, Color.magenta);
            }

            return force;
        }

        private Vector3 CalculateObstacleAvoidanceForce()
        {
            int sampleCount = Mathf.Max(3, rayCount);
            float halfAngle = Mathf.Max(5f, obstacleDetectionAngle);
            Vector3 accumulatedDirection = Vector3.zero;
            float strongestThreat = 0f;
            bool detected = false;

            for (int i = 0; i < sampleCount; i++)
            {
                float lerp = sampleCount == 1 ? 0.5f : (float)i / (sampleCount - 1);
                float angle = Mathf.Lerp(-halfAngle, halfAngle, lerp);
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * transform.forward;

                RaycastHit hit;
                if (!Physics.Raycast(transform.position, direction, out hit, visualRange))
                {
                    continue;
                }

                if (!hit.collider.CompareTag("Obstacle"))
                {
                    continue;
                }

                float distanceWeight = 1f - hit.distance / Mathf.Max(0.01f, visualRange);
                if (hit.distance < minObstacleAvoidanceDistance)
                {
                    distanceWeight *= emergencyAvoidanceMultiplier;
                }

                Vector3 planarAway = Vector3.ProjectOnPlane(transform.position - hit.point, Vector3.up).normalized;
                if (planarAway.sqrMagnitude < 0.001f)
                {
                    planarAway = angle >= 0f ? -transform.right : transform.right;
                }

                accumulatedDirection += (planarAway + Vector3.up * 0.2f) * distanceWeight;
                strongestThreat = Mathf.Max(strongestThreat, distanceWeight);
                detected = true;

                if (showDebugRays)
                {
                    Debug.DrawRay(transform.position, direction * hit.distance, Color.red);
                    Debug.DrawRay(hit.point, planarAway, Color.yellow);
                }
            }

            if (!detected)
            {
                isAvoidingObstacle = false;
                obstacleAvoidanceStrength = Mathf.Lerp(obstacleAvoidanceStrength, 0f, Time.fixedDeltaTime * 4f);
                targetAvoidanceRollAngle = 0f;
                currentAvoidanceRollAngle = Mathf.Lerp(currentAvoidanceRollAngle, 0f, Time.fixedDeltaTime * rollRecoverySpeed);
                lastAvoidanceForce = Vector3.Lerp(lastAvoidanceForce, Vector3.zero, Time.fixedDeltaTime * 4f);
                return lastAvoidanceForce;
            }

            Vector3 desiredAvoidance = accumulatedDirection.normalized * strongestThreat;
            lastAvoidanceForce = Vector3.Lerp(lastAvoidanceForce, desiredAvoidance, Time.fixedDeltaTime * 8f);
            isAvoidingObstacle = true;
            obstacleAvoidanceStrength = Mathf.Clamp01(strongestThreat);

            if (enableAvoidanceRoll)
            {
                float sideSign = Mathf.Sign(Vector3.Dot(lastAvoidanceForce, transform.right));
                targetAvoidanceRollAngle = -sideSign * avoidanceRollMaxAngle * obstacleAvoidanceStrength;
                currentAvoidanceRollAngle = Mathf.Lerp(currentAvoidanceRollAngle, targetAvoidanceRollAngle, Time.fixedDeltaTime * avoidanceRollSpeed);
            }

            return lastAvoidanceForce;
        }

        private Vector3 CalculateOscillationForce()
        {
            float time = Time.time * oscillationFrequency;
            Vector3 noiseInput = transform.position * 0.1f + Vector3.one * time;
            float noiseX = Mathf.PerlinNoise(noiseInput.x, noiseInput.y) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(noiseInput.y, noiseInput.z) * 2f - 1f;
            float noiseZ = Mathf.PerlinNoise(noiseInput.z, noiseInput.x) * 2f - 1f;

            float pitchWave = Mathf.Sin(time * (1.3f + noiseY * 0.25f)) * 5f;
            float rollWave = Mathf.Cos(time * (1.7f + noiseY * 0.2f)) * 6f;
            float playerPitchBias = UsesAutopilot ? 0f : Mathf.Max(0f, playerForwardInput) * 5f;
            float playerRollBias = UsesAutopilot ? 0f : -smoothedTurnInput * 10f;

            bodyPitchAngle = Mathf.Lerp(
                bodyPitchAngle,
                Mathf.Clamp(noiseX * 15f + pitchWave + playerPitchBias, -25f, 25f),
                Time.fixedDeltaTime * 8f
            );

            bodyRollAngle = Mathf.Lerp(
                bodyRollAngle,
                Mathf.Clamp(noiseZ * 20f + rollWave + playerRollBias, -30f, 30f),
                Time.fixedDeltaTime * 8f
            );

            float desiredHeight = GetDesiredFlightHeight();
            float heightError = desiredHeight - transform.position.y;
            float heightCorrection = Mathf.Clamp(heightError * heightDampingFactor, -3f, 3f);

            float baseLift = rb.mass * 1.2f;
            float liftMagnitude = baseLift + heightCorrection;

            float thrustAngle = Mathf.Clamp(bodyPitchAngle, -30f, 30f);
            float thrustMagnitude = liftMagnitude * Mathf.Tan(thrustAngle * Mathf.Deg2Rad) * 0.2f;
            if (!UsesAutopilot)
            {
                thrustMagnitude *= Mathf.Lerp(0.4f, 1.25f, Mathf.Max(0f, playerForwardInput));
            }

            float swayAngle = Mathf.Clamp(bodyRollAngle, -25f, 25f);
            float swayMagnitude = liftMagnitude * Mathf.Tan(swayAngle * Mathf.Deg2Rad) * 0.3f;
            if (!UsesAutopilot)
            {
                swayMagnitude *= Mathf.Lerp(0.8f, 1.35f, Mathf.Abs(smoothedTurnInput));
            }

            Vector3 liftForce = liftAxis * liftMagnitude;
            Vector3 thrustForce = thrustAxis * thrustMagnitude;
            Vector3 swayForce = swayAxis * swayMagnitude;

            float horizontalNoise = Mathf.PerlinNoise(time * 1.2f, noiseY * 13.37f) * 2f - 1f;
            Vector3 horizontalPerturbation = transform.right * horizontalNoise * 0.5f;

            Vector3 buzzForce = (liftForce + thrustForce + swayForce) * 0.6f + horizontalPerturbation;
            buzzForce -= liftAxis * (baseLift * 0.6f);

            if (!UsesAutopilot)
            {
                buzzForce += Vector3.up * playerVerticalInput * 0.35f;
            }

            if (showForces)
            {
                Debug.DrawRay(transform.position, liftAxis * heightCorrection, Color.green);
                Debug.DrawRay(transform.position, thrustForce, new Color(1f, 0.5f, 0f));
                Debug.DrawRay(transform.position, swayForce, new Color(1f, 1f, 0f));
            }

            return buzzForce;
        }

        private Vector3 GetCurlNoiseForce()
        {
            Vector3 noiseForce = CurlNoiseField.GetCurlNoiseForce(transform.position);
            float distanceFactor = 1f;

            if (UsesAutopilot && beeTarget != null && beeTarget.currentTarget != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, beeTarget.currentTarget.position);
                distanceFactor = Mathf.Clamp01(distanceToTarget / Mathf.Max(0.01f, noiseInfluenceRadius));
            }
            else
            {
                float speedFactor = Mathf.Clamp01(Vector3.ProjectOnPlane(velocity, Vector3.up).magnitude / Mathf.Max(0.01f, maxSpeed));
                float inputFactor = Mathf.Clamp01(Mathf.Abs(playerTurnInput) + Mathf.Abs(playerVerticalInput) + Mathf.Max(0f, playerForwardInput));
                distanceFactor = Mathf.Lerp(0.55f, 1.1f, Mathf.Max(speedFactor, inputFactor * 0.8f));
            }

            noiseForce *= distanceFactor * 1.5f;

            Vector3 forwardComponent = Vector3.Project(noiseForce, transform.forward);
            Vector3 lateralComponent = noiseForce - forwardComponent;
            lastNoiseForce = lateralComponent * 1.3f + forwardComponent * 0.7f;
            return lastNoiseForce;
        }

        private void ApplyBodyPosture()
        {
            if (cachedVisualRoot == null)
            {
                return;
            }

            Vector3 localVelocity = transform.InverseTransformDirection(velocity);
            float normalizedVerticalSpeed = Mathf.Clamp(localVelocity.y / Mathf.Max(0.01f, maxClimbSpeed), -1f, 1f);
            float normalizedForwardSpeed = Mathf.Clamp01(localVelocity.z / Mathf.Max(0.01f, maxSpeed));

            float targetPitch = 0f;
            float targetRoll = 0f;

            if (UsesAutopilot)
            {
                targetPitch = -normalizedVerticalSpeed * climbPitchAngle + normalizedForwardSpeed * 4f;
                float sideAcceleration = Mathf.Clamp(Vector3.Dot(acceleration, transform.right) / Mathf.Max(0.01f, maxForce), -1f, 1f);
                targetRoll = -sideAcceleration * turnBankAngle;
            }
            else
            {
                if (playerVerticalInput > 0.05f)
                {
                    targetPitch -= climbPitchAngle * playerVerticalInput;
                }
                else if (playerVerticalInput < -0.05f)
                {
                    targetPitch += divePitchAngle * -playerVerticalInput;
                }
                else
                {
                    targetPitch -= normalizedVerticalSpeed * climbPitchAngle * 0.75f;
                }

                targetPitch += normalizedForwardSpeed * 6f;
                targetRoll = -smoothedTurnInput * turnBankAngle * Mathf.Lerp(0.55f, 1f, normalizedForwardSpeed);
            }

            targetPitch += bodyPitchAngle * 0.45f;
            targetRoll += bodyRollAngle * 0.6f;

            if (IsPlayerCollecting)
            {
                targetPitch += 8f;
                targetRoll *= 0.45f;
            }

            if (isAvoidingObstacle && enableAvoidanceRoll)
            {
                targetRoll += currentAvoidanceRollAngle;
            }

            visualPitch = Mathf.Lerp(visualPitch, targetPitch, Time.fixedDeltaTime * modelTiltSmoothing);
            visualRoll = Mathf.Lerp(visualRoll, targetRoll, Time.fixedDeltaTime * modelTiltSmoothing);

            cachedVisualRoot.localRotation = visualRootBaseRotation * Quaternion.Euler(visualPitch, 0f, visualRoll);
        }

        private void DrawDebugVisualization()
        {
            if (!showDebugRays)
            {
                return;
            }

            if (beeTarget != null && beeTarget.currentTarget != null)
            {
                Debug.DrawLine(transform.position, beeTarget.currentTarget.position, Color.magenta);
            }

            int sampleCount = Mathf.Max(3, rayCount);
            float halfAngle = Mathf.Max(5f, obstacleDetectionAngle);
            for (int i = 0; i < sampleCount; i++)
            {
                float lerp = sampleCount == 1 ? 0.5f : (float)i / (sampleCount - 1);
                float angle = Mathf.Lerp(-halfAngle, halfAngle, lerp);
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * transform.forward;
                Debug.DrawRay(transform.position, direction * visualRange, Color.cyan);
            }

            if (showForces)
            {
                Debug.DrawRay(transform.position, velocity, Color.blue);
                Debug.DrawRay(transform.position, lastControlForce, Color.white);
                Debug.DrawRay(transform.position, lastAvoidanceForce, Color.yellow);
                Debug.DrawRay(transform.position, lastNoiseForce, Color.cyan);
            }
        }
    }
}
