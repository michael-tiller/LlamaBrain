using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using LlamaBrain.Runtime.Demo;
using LlamaBrain.Runtime.RedRoom.Interaction;

namespace LlamaBrain.Runtime.RedRoom.AI
{

  /// <summary>
  /// NPC follower component that uses NavMeshAgent for pathfinding and CharacterController for movement.
  /// Follows a target transform while maintaining a desired follow distance, with automatic recovery from stuck states,
  /// off-mesh link traversal, and embedded geometry detection. Includes comprehensive debug visualization and stuck state diagnostics.
  /// This is the main "NPC" component - it owns the brain (NpcAgentExample) and tracks the current dialogue trigger.
  /// </summary>
  [RequireComponent(typeof(NavMeshAgent))]
  [RequireComponent(typeof(CharacterController))]
  [RequireComponent(typeof(NpcAgentExample))]
  public sealed class NpcFollowerExample : MonoBehaviour
  {
    /// <summary>
    /// The NPC's brain/dialogue component. Cached on Awake.
    /// </summary>
    public NpcAgentExample Agent { get; private set; }

    /// <summary>
    /// The dialogue trigger this NPC is currently inside (set by NpcDialogueTrigger on enter/exit).
    /// </summary>
    public NpcDialogueTrigger CurrentDialogueTrigger { get; set; }

    /// <summary>
    /// Enumeration of possible causes for the NPC being stuck or unable to move.
    /// Used for diagnostic logging and recovery decision-making.
    /// </summary>
    public enum EStuckCause
    {
      /// <summary>No stuck condition detected.</summary>
      None = 0,
      /// <summary>NPC is off the NavMesh and needs recovery.</summary>
      OffNavMesh = 1,
      /// <summary>No valid path exists to the destination.</summary>
      NoPathToDestination = 2,
      /// <summary>NPC is moving too slowly while trying to catch up to target.</summary>
      LowMovementWhileCatchingUp = 3,
      /// <summary>No reachable destination could be found near the desired follow point.</summary>
      NoReachableDestinationFound = 4,
      /// <summary>Path calculation failed (internal NavMesh error).</summary>
      PathCalculationFailed = 5,
      /// <summary>NavMesh sampling failed (could not find valid NavMesh position).</summary>
      NavMeshSamplingFailed = 6,
      /// <summary>Target is beyond the teleport distance threshold.</summary>
      TargetTooFar = 7,
      /// <summary>NPC is embedded in geometry (intersecting with obstacles).</summary>
      EmbeddedInGeometry = 8
    }

    [Header("Target")]
    /// <summary>The target transform to follow. If null, the NPC will not move.</summary>
    [SerializeField] private Transform target;

    [Header("Follow")]
    /// <summary>Desired distance to maintain from the target (in meters).</summary>
    [SerializeField] private float followDistance = 2.0f;      // desired offset from player
    /// <summary>Distance behind the follow offset at which the NPC starts moving to catch up (in meters).</summary>
    [SerializeField] private float startCatchUp = 0.75f;       // start moving when this far behind offset
    /// <summary>Distance within the follow offset at which the NPC stops moving (in meters).</summary>
    [SerializeField] private float stopCatchUp = 0.25f;        // stop when within this of offset
    /// <summary>Distance window for slowing down as the NPC approaches the stop threshold (in meters).</summary>
    [SerializeField] private float slowDownWindow = 1.0f;      // ease down near stop
    /// <summary>Distance beyond which the NPC will teleport closer to the target (in meters).</summary>
    [SerializeField] private float teleportDistance = 25.0f;

    [Header("Movement")]
    /// <summary>Walking speed in meters per second.</summary>
    [SerializeField] private float walkSpeed = 3.5f;
    /// <summary>Running speed in meters per second.</summary>
    [SerializeField] private float runSpeed = 6.0f;
    /// <summary>Catch-up distance threshold at which the NPC switches from walking to running (in meters).</summary>
    [SerializeField] private float runWhenBeyond = 7.0f;       // catchUp distance to switch to run
    /// <summary>Interval between path recalculations while moving (in seconds).</summary>
    [SerializeField] private float repathInterval = 0.35f;
    /// <summary>Minimum distance the destination must change before updating the NavMeshAgent destination (in meters).</summary>
    [SerializeField] private float destUpdateThreshold = 1.0f; // meters
    /// <summary>Time window for stuck detection - NPC must move at least STUCK_MPS_THRESHOLD m/s within this time (in seconds).</summary>
    [SerializeField] private float stuckTimeout = 2.0f;        // seconds before considering stuck
    /// <summary>Maximum planar snap distance for movement destinations - strict to prevent snapping through walls (in meters).</summary>
    [SerializeField] private float maxNavSnapForMove = 1.0f;  // Strict: maximum snap distance for move destinations (prevents "other side of wall")
    /// <summary>Maximum planar snap distance for warps/teleports - more permissive than move snap (in meters).</summary>
    [SerializeField] private float maxNavSnapForWarp = 6.0f;  // Permissive: maximum snap distance for warps/teleports
    /// <summary>Maximum planar snap distance for off-navmesh recovery - very permissive for recovery scenarios (in meters).</summary>
    [SerializeField] private float maxNavSnapForRecover = 12.0f;  // Planar (XZ) cap for recovery (very permissive for "fell off ledge" scenarios)
    /// <summary>NavMesh sampling radius for moves/warps. Must be >= maxNavSnapForWarp (in meters).</summary>
    [SerializeField] private float navSampleRadiusMove = 6.0f;    // NavMesh sampling radius for moves/warps (must be >= maxNavSnapForWarp)
    /// <summary>NavMesh sampling radius for recovery. Must be >= maxNavVerticalForRecover and maxNavSnapForRecover (in meters).</summary>
    [SerializeField] private float navSampleRadiusRecover = 12.0f;  // NavMesh sampling radius for recovery (must be >= maxNavVerticalForRecover and maxNavSnapForRecover)
    /// <summary>Maximum vertical delta for move destinations - prevents wrong floor selection (in meters).</summary>
    [SerializeField] private float maxNavVerticalForMove = 0.75f;  // Maximum vertical delta for move destinations (prevents wrong floor selection)
    /// <summary>Maximum vertical delta for warps/teleports - more permissive than move vertical constraint (in meters).</summary>
    [SerializeField] private float maxNavVerticalForWarp = 2.0f;  // Maximum vertical delta for warps/teleports (more permissive)
    /// <summary>Maximum vertical delta for off-navmesh recovery - very permissive for recovery scenarios (in meters).</summary>
    [SerializeField] private float maxNavVerticalForRecover = 10.0f;  // Maximum vertical delta for off-navmesh recovery (very permissive)
    /// <summary>Distance for down-raycast to find target's floor surface (in meters).</summary>
    [SerializeField] private float targetGroundRayDistance = 25.0f;  // Distance for down-ray to find target's floor surface
    /// <summary>Distance for down-raycast in recovery scenarios - large for tall drops/airborne cases (in meters).</summary>
    [SerializeField] private float recoverGroundRayDistance = 200f;  // Distance for down-ray in recovery (large for tall drops/airborne cases)
    /// <summary>Minimum time between stuck cause log messages to prevent spam while allowing new diagnostics (in seconds).</summary>
    [SerializeField] private float stuckLogInterval = 1.0f;  // Minimum time between stuck cause log messages (prevents spam while allowing new diagnostics)
    /// <summary>Layer mask for walls/props ONLY (used for capsule cast blocking detection). MUST be set in inspector.</summary>
    [SerializeField] private LayerMask obstacleMask = 0;   // Layer mask for walls/props ONLY (blocks capsule cast) - MUST be set in inspector
    /// <summary>Layer mask for floors/terrain ONLY (used for down-raycast to find ground). MUST be set in inspector.</summary>
    [SerializeField] private LayerMask groundMask = 0;     // Layer mask for floors/terrain ONLY (for down-ray) - MUST be set in inspector

    [Header("Motor")]
    /// <summary>Rotation speed for turning toward movement direction (degrees per second).</summary>
    [SerializeField] private float rotationSpeed = 12f;
    /// <summary>Downward force applied when grounded to keep the character controller stuck to the ground (in m/s²).</summary>
    [SerializeField] private float stickToGround = 2f;

    [Header("Animation")]
    /// <summary>Rate at which animation speed blends toward actual movement speed.</summary>
    [SerializeField] private float speedChangeRate = 10f;
    /// <summary>Audio clip to play when the NPC lands (from animation event).</summary>
    public AudioClip LandingAudioClip;
    /// <summary>Array of audio clips to randomly select from for footstep sounds (from animation event).</summary>
    public AudioClip[] FootstepAudioClips;
    /// <summary>Volume for footstep and landing audio clips (0-1).</summary>
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Header("Debug")]
    /// <summary>Counter for the number of times the NPC has teleported (for debugging).</summary>
    [SerializeField]
    private int teleportCount = 0;

    /// <summary>GameObject used to display prompt indicators to the player.</summary>
    [SerializeField] private GameObject promptIndicator;
    /// <summary>Text component for displaying prompt indicator text.</summary>
    [SerializeField] private TextMeshProUGUI promptIndicatorText;

    /// <summary>Canvas used for world-space debug display.</summary>
    [SerializeField] private Canvas debugCanvas;
    /// <summary>Text component for displaying debug information.</summary>
    [SerializeField] private TextMeshProUGUI debugText;
    /// <summary>Whether to show debug information overlay.</summary>
    [SerializeField] private bool showDebugInfo = true;
    /// <summary>Local offset for the debug canvas relative to the NPC transform.</summary>
    [SerializeField] private Vector3 canvasOffset = new Vector3(0f, 2.5f, 0f);
    /// <summary>Scale factor for the debug canvas.</summary>
    [SerializeField] private float canvasScale = 0.01f;
    /// <summary>Whether to draw debug gizmos in the scene view.</summary>
    [SerializeField] private bool drawDebugGizmos = true;
    /// <summary>Whether to draw the NavMesh path as debug lines.</summary>
    [SerializeField] private bool drawPath = true;
    /// <summary>Whether to draw the current destination as a debug line.</summary>
    [SerializeField] private bool drawDestination = true;
    /// <summary>Whether to draw velocity vectors as debug lines.</summary>
    [SerializeField] private bool drawVelocity = true;
    /// <summary>Whether to draw the character controller capsule as a gizmo.</summary>
    [SerializeField] private bool drawCapsule = true;
    /// <summary>Whether to draw a line connecting the NPC to the target.</summary>
    [SerializeField] private bool drawTargetConnection = true;

    private NavMeshAgent _agent;
    private CharacterController _cc;

    private Animator _anim;
    private bool _hasAnim;
    private int _idSpeed;
    private int _idMotionSpeed;
    private float _animBlend;

    private float _repathTimer;
    private bool _moving;
    private bool _wasMoving; // Track previous frame's movement state for edge detection
    private Vector3 _lastDest;

    // Stuck detection - tracks position over configured timeout window
    private Vector3 _stuckCheckPosition;
    private float _stuckCheckTimer = 0f;
    private int _stuckRecoveryAttempts = 0;
    private int _consecutivePathFailures = 0;
    private const float STUCK_MPS_THRESHOLD = 0.25f; // must move at least this many m/s to not be considered stuck
    private const int MAX_PATH_FAILURES_BEFORE_WARP = 3;
    private EStuckCause _currentStuckCause = EStuckCause.None;
    private float _lastStuckLogTime = -999f;

    // Gravity
    private float _gravity = 0f;

    // Off-mesh link cooldown to prevent spam teleports
    private float _offMeshCooldown = 0f;
    private const float OFFMESH_COOLDOWN = 0.2f;

    // Embedded test throttling
    private float _embeddedCheckTimer = 0f;
    private const float EMBEDDED_CHECK_INTERVAL = 0.15f;

    // Cache buffers for physics queries to avoid GC
    private readonly Collider[] _overlapBuf = new Collider[8];
    private readonly RaycastHit[] _castBuf = new RaycastHit[8];

    // Debug UI
    private bool _isStuck;
    private Camera _mainCamera;
    private StringBuilder _sb; // Cached StringBuilder for debug output
    private float _debugUpdateTimer = 0f;
    private const float DEBUG_UPDATE_INTERVAL = 0.1f; // Update debug display every 0.1s instead of every frame

    // Cached path for validation (reused to avoid allocations)
    private NavMeshPath _path;

    /// <summary>Whether the prompt indicator is currently showing.</summary>
    private bool isShowingPromptIndicator = false;
    public bool IsShowingPromptIndicator => isShowingPromptIndicator;

    /// <summary>
    /// Initializes the component, configures NavMeshAgent and CharacterController,
    /// validates required settings, and attempts to recover if spawned off the NavMesh.
    /// </summary>
    private void Awake()
    {
      _agent = GetComponent<NavMeshAgent>();
      _cc = GetComponent<CharacterController>();
      Agent = GetComponent<NpcAgentExample>();

      // Required: agent steers only, CC moves only
      _agent.updatePosition = false;
      _agent.updateRotation = false;

      // Followers should not auto-brake on frequent destination updates
      _agent.autoBraking = false;

      // Disable auto traversal - we handle off-mesh links manually
      _agent.autoTraverseOffMeshLink = false;

      // Sync NavMeshAgent dimensions to match CharacterController
      // Note: NavMesh bake must use matching agent type
      _agent.radius = _cc.radius;
      _agent.height = _cc.height;
      // baseOffset set to 0 - agent Y position handled by nextPosition syncing
      // Setting baseOffset from CC center can cause isOnNavMesh flaps near edges
      _agent.baseOffset = 0f;

      // Enforce CC center so transform.position represents the nav/feet anchor
      // This makes transform.position, NavMeshHit.position, and agent position all refer to the same point
      // REQUIRED AUTHORING: This overrides any custom Y center. If you need custom mesh alignment,
      // use a two-transform hierarchy (root with Agent, child with CC+visuals offset).
      // XZ center offset is preserved for horizontal alignment.
      // Force XZ center to zero to ensure transform.position is the capsule axis (nav anchor).
      // Any visual offset should be handled by a child transform, not CC center.
      _cc.center = new Vector3(0f, _cc.height * 0.5f, 0f);

      // Re-enable avoidance at Low quality to prevent corner pinning
      // while reducing jitter from constant recalculations
      _agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

      _lastDest = transform.position;
      _stuckCheckPosition = transform.position;
      teleportCount = 0;

      // Cache NavMeshPath to avoid per-call allocations
      _path = new NavMeshPath();

      // Cache StringBuilder for debug output
      _sb = new StringBuilder(512);

      // Hard error if masks are not configured - disable component to prevent silent failures
      if (obstacleMask == 0)
      {
        Debug.LogError("[NpcFollowerExample] obstacleMask is 0. Set it to walls/props layer mask in inspector. Component disabled.", this);
        enabled = false;
        return;
      }
      if (groundMask == 0)
      {
        Debug.LogError("[NpcFollowerExample] groundMask is 0. Set it to floors/terrain layer mask in inspector. Component disabled.", this);
        enabled = false;
        return;
      }

      // Validate navSampleRadiusMove covers max snap distance
      if (navSampleRadiusMove < maxNavSnapForWarp)
      {
        Debug.LogError("[NpcFollowerExample] navSampleRadiusMove < maxNavSnapForWarp; sampling will fail in vertical offsets.", this);
      }

      // Validate navSampleRadiusRecover covers recovery constraints
      if (navSampleRadiusRecover < maxNavVerticalForRecover)
      {
        Debug.LogError("[NpcFollowerExample] navSampleRadiusRecover < maxNavVerticalForRecover; recovery sampling will fail.", this);
      }
      if (navSampleRadiusRecover < maxNavSnapForRecover)
      {
        Debug.LogError("[NpcFollowerExample] navSampleRadiusRecover < maxNavSnapForRecover; recovery sampling will fail.", this);
      }

      // Authoring note: NavMesh agent type mismatch is the #1 cause of "isOnNavMesh flaps"
      // Ensure the baked NavMesh uses the same agent type as this agent's agentTypeID.
      // If they differ, you'll get intermittent isOnNavMesh false and inconsistent sampling.

      // Initialize agent state immediately to prevent one-frame garbage from stale position
      // If spawn is off-nav, try to recover to nearest valid nav point
      if (TrySampleNavForRecover(transform.position, out var spawnHit))
      {
        _agent.Warp(spawnHit.position);
        _agent.nextPosition = spawnHit.position;
        transform.position = spawnHit.position;
        _agent.ResetPath();
      }
      else
      {
        Debug.LogError($"[NpcFollowerExample] Spawn not recoverable near {transform.position}. Component disabled.", this);
        enabled = false;
        return;
      }
    }

    /// <summary>
    /// Helper to create a Vector3 with a specific Y coordinate.
    /// </summary>
    private static Vector3 WithY(Vector3 v, float y) => new Vector3(v.x, y, v.z);

    /// <summary>
    /// Computes planar (XZ) distance between two points.
    /// </summary>
    private static float PlanarDist(Vector3 a, Vector3 b)
    {
      float dx = a.x - b.x;
      float dz = a.z - b.z;
      return Mathf.Sqrt(dx * dx + dz * dz);
    }

    /// <summary>
    /// Checks if a point moved in the planar (XZ) direction beyond a tiny numerical tolerance.
    /// Used to gate physics checks - if moved at all, check for blocking.
    /// </summary>
    private static bool MovedPlanar(Vector3 a, Vector3 b)
    {
      var d = b - a;
      d.y = 0f;
      return d.sqrMagnitude > 1e-6f;
    }

    /// <summary>
    /// Projects a point onto the NPC's current navigation plane (Y = transform.position.y).
    /// Used to force navigation inputs onto a nav-probing plane before sampling.
    /// </summary>
    private Vector3 NavPlane(Vector3 p) => WithY(p, transform.position.y);

    /// <summary>
    /// Flattens a point to a specific Y coordinate.
    /// </summary>
    private static Vector3 FlattenToY(Vector3 p, float y) { p.y = y; return p; }

    /// <summary>
    /// Initializes animation system, hides prompt indicator, caches main camera, and initializes debug canvas.
    /// </summary>
    private void Start()
    {
      _hasAnim = TryGetComponent(out _anim);
      _idSpeed = Animator.StringToHash("Speed");
      _idMotionSpeed = Animator.StringToHash("MotionSpeed");

      HidePromptIndicator();

      // Cache main camera
      _mainCamera = Camera.main;

      InitializeDebugCanvas();
    }

    /// <summary>
    /// Main update loop that handles off-mesh links, recovery from stuck states, pathfinding,
    /// movement, rotation, stuck detection, animation, and debug visualization.
    /// </summary>
    private void Update()
    {
      if (!target) return;

      // Update off-mesh link cooldown (clamped to prevent negative drift)
      _offMeshCooldown = Mathf.Max(0f, _offMeshCooldown - Time.deltaTime);

      // Handle off-mesh links (jumps, drops, ladders)
      // With updatePosition=false, agent paths through links but never traverses them
      if (_agent.isOnOffMeshLink && _offMeshCooldown <= 0f)
      {
        _offMeshCooldown = OFFMESH_COOLDOWN;

        var data = _agent.currentOffMeshLinkData;

        // Use end position and snap to NavMesh for your agent mask
        Vector3 end = data.endPos;

        // Don't accept endPos if sampling fails or is too far - treat as failure to avoid warping off-mesh
        // Use recover sampling for drop/tall links where endPos.y may not be close to nav surface
        if (!TrySampleNavForRecover(end, out var hit))
        {
          SetStuckCause(EStuckCause.NavMeshSamplingFailed, $"OffMeshLink end not on NavMesh or too far: {end}");
          _agent.ResetPath();
          _repathTimer = 0f;
          return;
        }

        // Move CC/transform only (no agent warp yet - warping while on link can invalidate link state)
        WarpTransformOnly(hit);

        // Now complete link state
        _agent.CompleteOffMeshLink();

        // Now sync agent to transform
        SyncAgentToTransform();

        // Reset all path/timer state after warp (same as WarpTo)
        ResetNavStateAfterWarp();

        // Clear stuck cause after successful traversal
        ClearStuckCauseAny("OffMeshLink traversed");

        return;
      }

      // Recover if off-mesh
      if (!_agent.isOnNavMesh)
      {
        SetStuckCause(EStuckCause.OffNavMesh, $"NPC {gameObject.name} is off NavMesh at position {transform.position}");

        if (TryFindRecoverPointUnembedded(transform.position, out var hit))
        {
          WarpTo(hit);
          ClearStuckCauseAny($"Recovered from off-mesh, warped to {hit.position}");
        }
        else
        {
          SetStuckCause(EStuckCause.NavMeshSamplingFailed, $"NPC {gameObject.name} cannot find NavMesh near position {transform.position}");
          return;
        }
      }

      // Recover if embedded in geometry (even when on NavMesh - isOnNavMesh doesn't guarantee not intersecting obstacles)
      // Throttle check to reduce steady-state cost (only check when moving, stuck, or on off-mesh link, or periodically)
      bool shouldCheckEmbedded = _moving || _isStuck || _agent.isOnOffMeshLink;
      if (!shouldCheckEmbedded)
      {
        _embeddedCheckTimer += Time.deltaTime;
        if (_embeddedCheckTimer >= EMBEDDED_CHECK_INTERVAL)
        {
          shouldCheckEmbedded = true;
          _embeddedCheckTimer = 0f;
        }
      }
      else
      {
        _embeddedCheckTimer = 0f; // Reset timer when actively checking
      }

      if (shouldCheckEmbedded && IsEmbeddedAtPivot(transform.position))
      {
        SetStuckCause(EStuckCause.EmbeddedInGeometry, $"NPC {gameObject.name} is embedded in geometry at position {transform.position}");

        if (TryFindRecoverPointUnembedded(transform.position, out var hit))
        {
          WarpTo(hit);
          ClearStuckCauseAny($"Recovered from embedded state, warped to {hit.position}");
        }
        else
        {
          SetStuckCause(EStuckCause.NavMeshSamplingFailed, $"NPC {gameObject.name} cannot find NavMesh near embedded position {transform.position}");
          return;
        }
      }

      // Detect "moving but no path" deadlock (can happen if destination sampling fails persistently)
      if (_moving && !_agent.hasPath && _repathTimer <= 0f)
      {
        SetStuckCause(EStuckCause.NoPathToDestination, $"NPC {gameObject.name} is moving but has no path (deadlock)");

        // Force immediate repath attempt
        _repathTimer = repathInterval;

        // If we've been stuck like this for even 1 attempt, force recovery (don't wait)
        if (_stuckRecoveryAttempts > 0)
        {
          // Try to warp closer to target - use direct path to target position if follow point fails
          if (TryGetTargetNav(out var recoveryTargetHit))
          {
            // Try warping to a point between NPC and target
            Vector3 toTarget = recoveryTargetHit.position - transform.position;
            float dist = toTarget.magnitude;

            // Warp halfway or to follow distance, whichever is closer
            float warpDist = Mathf.Min(dist * 0.5f, dist - followDistance);
            warpDist = Mathf.Max(warpDist, 2.0f); // At least 2m forward

            Vector3 warpTarget = transform.position + toTarget.normalized * warpDist;

            if (TrySampleNavForWarp(warpTarget, out var hit))
            {
              if (WarpPointConnectedToTarget(hit.position))
              {
                WarpTo(hit);
                ClearStuckCauseAny("Recovered from no-path deadlock via warp");
              }
            }
          }
        }
      }

      // After any potential warp branches, keep the agent simulation anchored to the transform for this frame.
      // This ensures desiredVelocity, SetDestination, and remainingDistance are consistent with actual transform position.
      _agent.nextPosition = transform.position;

      Vector3 toPlayer = target.position - transform.position;
      toPlayer.y = 0f;
      float dPlayer = toPlayer.magnitude;

      if (dPlayer > teleportDistance)
      {
        SetStuckCause(EStuckCause.TargetTooFar, $"NPC {gameObject.name} target is too far: {dPlayer:F2}m (threshold: {teleportDistance}m)");

        // Use "behind target" position instead of target.position to avoid teleporting inside/forward of player
        // Sample target first to get nav point on target's layer, then build warp target relative to that
        // Fallback to NPC plane if target sampling fails
        Vector3 basis;
        if (TryGetTargetNav(out var warpTargetHit))
          basis = warpTargetHit.position;
        else
          basis = FlattenToY(target.position, transform.position.y); // last-resort basis on NPC plane

        Vector3 teleportBackDir = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : -transform.forward;
        Vector3 warpTarget = basis - teleportBackDir * (followDistance + 1f);

        if (!TrySampleNavForWarp(warpTarget, out var hit))
        {
          SetStuckCause(EStuckCause.NavMeshSamplingFailed, $"No NavMesh near warpTarget: {warpTarget}");
        }
        else if (!WarpPointConnectedToTarget(hit.position))
        {
          SetStuckCause(EStuckCause.NoPathToDestination, $"Warp point disconnected from target: {hit.position}");
        }
        else
        {
          WarpTo(hit);
          ClearStuckCauseAny($"Teleported closer to target");
        }
        return;
      }

      // "Behind desired offset"
      float catchUp = Mathf.Max(0f, dPlayer - followDistance);

      // Hysteresis removes start/stop chatter
      if (!_moving && catchUp > startCatchUp) _moving = true;
      else if (_moving && catchUp < stopCatchUp) _moving = false;

      // Clear path when stopping to avoid stale state and reduce internal steering work
      if (_wasMoving && !_moving)
      {
        _agent.ResetPath();
        _consecutivePathFailures = 0;

        _isStuck = false;
        _stuckRecoveryAttempts = 0;
        if (_currentStuckCause != EStuckCause.None)
          ClearStuckCause(_currentStuckCause, "Stopped moving, clearing stuck state");

        _repathTimer = 0f; // Force immediate repath on restart

        // Reset stuck window to avoid stale diagnostics
        _stuckCheckPosition = transform.position;
        _stuckCheckTimer = 0f;
      }
      _wasMoving = _moving;

      // Stable follow point: based on NPC->player direction (not target.forward)
      Vector3 backDir = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : -transform.forward;
      // Use sampled target nav surface when available to keep destination on same nav layer as target
      // (upper deck, bridge, etc.), otherwise fallback to NPC's nav plane
      Vector3 followPoint;
      if (TryGetTargetNav(out var targetHit))
      {
        followPoint = targetHit.position - backDir * followDistance; // correct vertical layer
      }
      else
      {
        followPoint = NavPlane(target.position - backDir * followDistance); // fallback
      }

      // Update destination sparsely (only when moving)
      if (_moving)
      {
        _repathTimer -= Time.deltaTime;
        if (_repathTimer <= 0f)
        {
          _repathTimer = repathInterval;

          if (TryFindReachableDestination(followPoint, out Vector3 validDestination))
          {
            _consecutivePathFailures = 0; // Reset on success
                                          // Always set destination if agent has no path (e.g., after ResetPath) or if destination changed significantly
            if (!_agent.hasPath || (_lastDest - validDestination).sqrMagnitude > destUpdateThreshold * destUpdateThreshold)
            {
              _lastDest = validDestination;
              _agent.SetDestination(_lastDest);
              ClearStuckCause(EStuckCause.NoReachableDestinationFound, "Found valid destination");
              ClearStuckCause(EStuckCause.NoPathToDestination, "Path set successfully");
            }
          }
          else
          {
            _consecutivePathFailures++;
            SetStuckCause(EStuckCause.NoReachableDestinationFound, $"NPC {gameObject.name} cannot find reachable destination for follow point {followPoint} (failure {_consecutivePathFailures}/{MAX_PATH_FAILURES_BEFORE_WARP})");

            // If path finding fails repeatedly, target is likely on disconnected NavMesh - warp immediately
            if (_consecutivePathFailures >= MAX_PATH_FAILURES_BEFORE_WARP && catchUp > startCatchUp)
            {
              // Sample target first to get nav point on target's layer, then build warp target relative to that
              // Fallback to NPC plane if target sampling fails
              Vector3 basis;
              if (TryGetTargetNav(out var pathFailureTargetHit))
                basis = pathFailureTargetHit.position;
              else
                basis = FlattenToY(target.position, transform.position.y); // last-resort basis on NPC plane

              Vector3 warpTarget = basis - backDir * (followDistance + 1f);
              if (!TrySampleNavForWarp(warpTarget, out NavMeshHit hit))
              {
                SetStuckCause(EStuckCause.NavMeshSamplingFailed, $"No NavMesh near warpTarget: {warpTarget}");
              }
              else if (!WarpPointConnectedToTarget(hit.position))
              {
                SetStuckCause(EStuckCause.NoPathToDestination, $"Warp point disconnected from target: {hit.position}");
              }
              else
              {
                WarpTo(hit);
                ClearStuckCauseAny($"Warped to target after {MAX_PATH_FAILURES_BEFORE_WARP} consecutive path failures");
              }
            }
          }
        }
      }
      else
      {
        // Reset timer so it repaths immediately when movement starts
        _repathTimer = 0f;
      }

      // Speed from catchUp
      float desiredSpeed = 0f;
      if (_moving)
      {
        desiredSpeed = (catchUp > runWhenBeyond) ? runSpeed : walkSpeed;

        if (catchUp < slowDownWindow)
          desiredSpeed *= Mathf.Clamp01(catchUp / slowDownWindow);
      }

      // agent already has speed set (only when moving to reduce noise)
      _agent.speed = _moving ? desiredSpeed : 0f;

      // Gravity integration: always apply gravity
      if (_cc.isGrounded)
      {
        _gravity = -stickToGround; // Small downward force when grounded
      }
      else
      {
        _gravity += Physics.gravity.y * Time.deltaTime;
      }

      // Hoist velocity vector to outer scope for stuck detection
      Vector3 v = Vector3.zero;

      // Only compute movement and rotation when moving
      if (_moving)
      {
        // use agent steering AS-IS (do not normalize)
        // Guard against stale values after ResetPath()
        v = _agent.hasPath ? _agent.desiredVelocity : Vector3.zero;
        v.y = 0f;

        // clamp to desiredSpeed (keeps steering shape but respects your speed choice)
        float vm = v.magnitude;
        if (vm > desiredSpeed && vm > 0.0001f)
          v *= (desiredSpeed / vm);

        // rotate toward actual velocity when moving, hold rotation when blocked
        // This prevents rotating into walls when CC collision prevents movement
        Vector3 actualVel = new Vector3(_cc.velocity.x, 0f, _cc.velocity.z);

        if (actualVel.sqrMagnitude > 0.01f) // Moving - rotate toward actual velocity
        {
          transform.rotation = Quaternion.Slerp(
              transform.rotation,
              Quaternion.LookRotation(actualVel),
              rotationSpeed * Time.deltaTime);
        }
        // When blocked (actualVel near zero), hold rotation instead of turning into blocker

        // motor: CC moves by steering vector + gravity
        _cc.Move(v * Time.deltaTime + Vector3.up * _gravity * Time.deltaTime);
      }
      else
      {
        // Idle: keep grounded stick only, no planar motion
        _cc.Move(Vector3.up * _gravity * Time.deltaTime);
      }

      // Immediately sync agent nextPosition after movement to prevent stale steering data
      _agent.nextPosition = transform.position;

      // Stuck detection: check at configured interval if we've moved enough while needing to catch up
      float planarSpeed = new Vector3(_cc.velocity.x, 0f, _cc.velocity.z).magnitude;
      _stuckCheckTimer += Time.deltaTime;

      if (_stuckCheckTimer >= stuckTimeout)
      {
        // Use planar distance (ignore Y) to avoid false positives from slopes/steps/grounding jitter
        float movedDistance = Vector2.Distance(
          new Vector2(transform.position.x, transform.position.z),
          new Vector2(_stuckCheckPosition.x, _stuckCheckPosition.z));
        bool needsToCatchUp = catchUp > startCatchUp; // out of acceptable range

        // If catchUp is at or below startCatchUp threshold, NPC is close enough - skip stuck detection entirely
        // The NPC should just stop moving and wait, not check for stuck
        if (!needsToCatchUp)
        {
          _isStuck = false;
          if (_currentStuckCause != EStuckCause.None)
          {
            ClearStuckCause(_currentStuckCause, $"Close enough (catchUp: {catchUp:F2}m <= {startCatchUp:F2}m), not stuck");
          }
          _stuckRecoveryAttempts = 0;
        }
        // Only consider stuck if we need to catch up (catchUp > startCatchUp)
        // Use speed threshold (m/s) instead of absolute distance for variable stuckTimeout
        else if ((movedDistance / stuckTimeout) < STUCK_MPS_THRESHOLD)
        {
          _isStuck = true;
          _stuckRecoveryAttempts++;

          // Determine specific stuck cause with detailed diagnostics
          EStuckCause detectedCause = EStuckCause.LowMovementWhileCatchingUp;

          // Gather diagnostic information
          float agentDesiredVelMag = _agent.hasPath ? _agent.desiredVelocity.magnitude : 0f;
          float appliedVelMag = v.magnitude;
          float actualVelMag = planarSpeed;
          float mps = movedDistance / stuckTimeout;

          string causeDetails = $"Moved only {movedDistance:F2}m in {stuckTimeout:F2}s ({mps:F2} m/s, threshold: {STUCK_MPS_THRESHOLD} m/s), catchUp: {catchUp:F2}m";
          causeDetails += $", desiredSpeed: {desiredSpeed:F2}, agentDesiredVel: {agentDesiredVelMag:F2}, appliedVel: {appliedVelMag:F2}, actualVel: {actualVelMag:F2}";
          causeDetails += $", moving: {_moving}, hasPath: {_agent.hasPath}";

          // Check if path is invalid
          if (!_agent.hasPath || _agent.pathStatus != NavMeshPathStatus.PathComplete)
          {
            detectedCause = EStuckCause.NoPathToDestination;
            causeDetails += $", PathStatus: {_agent.pathStatus}";
          }
          // Check if desired speed is too low
          else if (desiredSpeed < 0.1f)
          {
            causeDetails += $", LOW_DESIRED_SPEED";
          }
          // Check if agent desired velocity is too low
          else if (agentDesiredVelMag < 0.1f)
          {
            causeDetails += $", LOW_AGENT_VELOCITY";
          }
          // Check if applied velocity is too low
          else if (appliedVelMag < 0.1f)
          {
            causeDetails += $", LOW_APPLIED_VELOCITY";
          }

          SetStuckCause(detectedCause, $"NPC {gameObject.name} stuck - {causeDetails}, Recovery attempt: {_stuckRecoveryAttempts}");

          // Progressive recovery based on attempts
          if (_stuckRecoveryAttempts >= 3)
          {
            // After 3 failed attempts, warp closer to player
            // Sample target first to get nav point on target's layer, then build warp target relative to that
            // Fallback to NPC plane if target sampling fails
            Vector3 basis;
            if (TryGetTargetNav(out var stuckRecoveryTargetHit))
              basis = stuckRecoveryTargetHit.position;
            else
              basis = FlattenToY(target.position, transform.position.y); // last-resort basis on NPC plane

            Vector3 warpTarget = basis - backDir * (followDistance + 1f);
            if (!TrySampleNavForWarp(warpTarget, out NavMeshHit hit))
            {
              SetStuckCause(EStuckCause.NavMeshSamplingFailed, $"No NavMesh near warpTarget: {warpTarget}");
            }
            else if (!WarpPointConnectedToTarget(hit.position))
            {
              SetStuckCause(EStuckCause.NoPathToDestination, $"Warp point disconnected from target: {hit.position}");
            }
            else
            {
              WarpTo(hit);
              ClearStuckCauseAny($"Warped to recovery position after {_stuckRecoveryAttempts} attempts");
            }
          }
          else
          {
            // First attempts: reset path and try alternate destination
            _agent.ResetPath();
            _repathTimer = 0f;

            // Try to find path directly to player instead of follow point
            if (_stuckRecoveryAttempts >= 2)
            {
              // Sample target first to get nav point on target's layer, not arbitrary transform Y
              if (TryGetTargetNav(out var directPathTargetHit))
              {
                if (TryFindReachableDestination(directPathTargetHit.position, out Vector3 directPath))
                {
                  _lastDest = directPath;
                  _agent.SetDestination(_lastDest);
                  ClearStuckCause(EStuckCause.NoPathToDestination, "Trying direct path to target");
                }
                else
                {
                  SetStuckCause(EStuckCause.NoReachableDestinationFound, $"NPC {gameObject.name} cannot find reachable destination to target at {directPathTargetHit.position}");
                }
              }
              else
              {
                SetStuckCause(EStuckCause.NoReachableDestinationFound, $"NPC {gameObject.name} cannot sample NavMesh for target at {target.position}");
              }
            }
          }
        }
        else
        {
          // Making progress - reset recovery attempts
          _isStuck = false;
          float recoveryMps = movedDistance / stuckTimeout;
          if (recoveryMps >= STUCK_MPS_THRESHOLD)
          {
            if (_stuckRecoveryAttempts > 0)
            {
              ClearStuckCause(_currentStuckCause, $"Recovered - moved {movedDistance:F2}m ({recoveryMps:F2} m/s, threshold: {STUCK_MPS_THRESHOLD} m/s)");
            }
            _stuckRecoveryAttempts = 0;
          }
        }

        // Reset for next check window
        _stuckCheckPosition = transform.position;
        _stuckCheckTimer = 0f;
      }

      // Animation: drive from actual planar speed
      if (_hasAnim)
      {
        float planar = new Vector3(_cc.velocity.x, 0f, _cc.velocity.z).magnitude;

        _animBlend = Mathf.Lerp(_animBlend, planar, Time.deltaTime * speedChangeRate);
        if (_animBlend < 0.01f) _animBlend = 0f;

        _anim.SetFloat(_idSpeed, _animBlend);
        _anim.SetFloat(_idMotionSpeed, planar > 0.01f ? 1f : 0f);
      }

      // Update debug display (throttled to reduce GC)
      _debugUpdateTimer -= Time.deltaTime;
      if (_debugUpdateTimer <= 0f)
      {
        _debugUpdateTimer = DEBUG_UPDATE_INTERVAL;
        UpdateDebugDisplay(dPlayer, catchUp, desiredSpeed, planarSpeed);
      }

      // Draw debug lines every frame
      DrawDebugLines();
    }

    /// <summary>
    /// Sets the target transform that the NPC should follow.
    /// </summary>
    /// <param name="newTarget">The transform to follow. Can be null to stop following.</param>
    public void SetTarget(Transform newTarget) => target = newTarget;

    /// <summary>
    /// Samples a NavMesh position with fallback to closest edge, ensuring the sampled point
    /// is within maxDist (planar) and maxVerticalDelta (vertical) to prevent snapping through walls,
    /// to wrong islands, or to wrong vertical layers on stacked NavMeshes.
    /// </summary>
    /// <param name="desired">Desired position (XZ used for distance check, Y ignored)</param>
    /// <param name="maxDistPlanar">Maximum planar (XZ) distance allowed</param>
    /// <param name="probeY">Y coordinate to use for NavMesh probing (e.g., transform.position.y for current layer, desired.y for target layer)</param>
    /// <param name="maxVerticalDelta">Maximum vertical delta from probeY allowed (prevents wrong floor selection)</param>
    /// <param name="sampleRadius">NavMesh sampling radius to use</param>
    /// <param name="hit">Sampled NavMesh position</param>
    private bool TrySampleNav(Vector3 desired, float maxDistPlanar, float probeY, float maxVerticalDelta, float sampleRadius, out NavMeshHit hit)
    {
      int mask = _agent.areaMask;
      Vector3 probe = WithY(desired, probeY);

      if (NavMesh.SamplePosition(probe, out hit, sampleRadius, mask) &&
          PlanarDist(desired, hit.position) <= maxDistPlanar &&
          Mathf.Abs(hit.position.y - probeY) <= maxVerticalDelta)
        return true;

      if (NavMesh.FindClosestEdge(probe, out hit, mask) &&
          PlanarDist(desired, hit.position) <= maxDistPlanar &&
          Mathf.Abs(hit.position.y - probeY) <= maxVerticalDelta)
        return true;

      return false;
    }

    /// <summary>
    /// Overload that defaults to probing at NPC's current Y level with move vertical constraint and move sample radius.
    /// </summary>
    /// <param name="desired">Desired position to sample.</param>
    /// <param name="maxDistPlanar">Maximum planar distance allowed for snapping.</param>
    /// <param name="hit">Output: NavMesh hit result if sampling succeeded.</param>
    /// <returns>True if a valid NavMesh position was found within constraints, false otherwise.</returns>
    private bool TrySampleNav(Vector3 desired, float maxDistPlanar, out NavMeshHit hit)
      => TrySampleNav(desired, maxDistPlanar, transform.position.y, maxNavVerticalForMove, navSampleRadiusMove, out hit);

    /// <summary>
    /// Convenience overload that takes probeY and defaults to warp vertical constraint and move sample radius.
    /// </summary>
    /// <param name="desired">Desired position to sample.</param>
    /// <param name="maxDistPlanar">Maximum planar distance allowed for snapping.</param>
    /// <param name="probeY">Y coordinate to use for NavMesh probing.</param>
    /// <param name="hit">Output: NavMesh hit result if sampling succeeded.</param>
    /// <returns>True if a valid NavMesh position was found within constraints, false otherwise.</returns>
    private bool TrySampleNav(Vector3 desired, float maxDistPlanar, float probeY, out NavMeshHit hit)
      => TrySampleNav(desired, maxDistPlanar, probeY, maxNavVerticalForWarp, navSampleRadiusMove, out hit);

    /// <summary>
    /// Gets the capsule radius (slightly reduced for safety margin).
    /// NOTE: If you see "can't find warp point" near door frames, this may be too strict.
    /// Consider tightening to 0.90f or special-casing hits that are extremely close.
    /// </summary>
    private float CapsuleRadius() => Mathf.Max(0.01f, _cc.radius * 0.95f);

    /// <summary>
    /// Gets capsule top and bottom sphere centers from a transform pivot position.
    /// Input is transform.position (pivot), output is CC capsule endpoints.
    /// </summary>
    /// <param name="pivotWorld">World position of the transform pivot.</param>
    /// <param name="top">Output: World position of the top sphere center.</param>
    /// <param name="bottom">Output: World position of the bottom sphere center.</param>
    /// <param name="r">Output: Radius of the capsule spheres.</param>
    private void CapsulePointsFromPivot(Vector3 pivotWorld, out Vector3 top, out Vector3 bottom, out float r)
    {
      r = CapsuleRadius();
      float h = Mathf.Max(_cc.height, r * 2f);
      float centerToEnd = (h * 0.5f) - r;

      // center is local-space; rotate it into world-space relative to the pivot
      Vector3 centerWorld = pivotWorld + (transform.rotation * _cc.center);

      top = centerWorld + Vector3.up * centerToEnd;
      bottom = centerWorld - Vector3.up * centerToEnd;
    }

    /// <summary>
    /// Checks if a capsule (sized to controller) at a given pivot position is embedded in geometry.
    /// Used to catch thin wall cases where capsule cast might miss.
    /// </summary>
    /// <param name="pivotWorld">World position of the transform pivot to check.</param>
    /// <returns>True if the capsule is embedded in geometry, false otherwise.</returns>
    private bool IsEmbeddedAtPivot(Vector3 pivotWorld)
    {
      if (obstacleMask == 0) return false;
      CapsulePointsFromPivot(pivotWorld, out var top, out var bot, out var r);
      int n = Physics.OverlapCapsuleNonAlloc(top, bot, r, _overlapBuf, obstacleMask, QueryTriggerInteraction.Ignore);
      return n > 0;
    }

    /// <summary>
    /// Checks if a capsule (sized to controller) between two pivot positions is blocked by environment obstacles.
    /// Uses obstacleMask (walls/props only) to prevent false blocks from floors.
    /// If starting position is embedded, advances cast origin slightly to avoid immediate hit while still detecting blockers.
    /// </summary>
    /// <param name="aPivot">Starting pivot position.</param>
    /// <param name="bPivot">Ending pivot position.</param>
    /// <returns>True if the path is blocked, false otherwise.</returns>
    private bool IsBlockedBetweenPivots(Vector3 aPivot, Vector3 bPivot)
    {
      if (obstacleMask == 0) return false;

      Vector3 delta = bPivot - aPivot;
      float dist = delta.magnitude;
      if (dist <= 0.0001f) return false;

      Vector3 dir = delta / dist;

      // If start is overlapped, advance a bit so the cast isn't an immediate hit,
      // but keep the test active so we still reject "snap across wall" cases.
      if (IsEmbeddedAtPivot(aPivot))
      {
        float r = CapsuleRadius();
        float advance = Mathf.Min(dist * 0.5f, r * 0.25f);
        if (advance <= 0.0001f) return true;   // effectively blocked
        aPivot += dir * advance;
        dist -= advance;
      }

      CapsulePointsFromPivot(aPivot, out var top, out var bot, out var r2);
      int n = Physics.CapsuleCastNonAlloc(top, bot, r2, dir, _castBuf, dist, obstacleMask, QueryTriggerInteraction.Ignore);
      return n > 0;
    }

    /// <summary>
    /// Centralized probeY computation with ray-or-fallback logic.
    /// Prevents probeY poisoning when down-ray misses by falling back to a sane alternative.
    /// Reports whether the ray was actually used via usedRay parameter.
    /// </summary>
    /// <param name="p">Position to probe from.</param>
    /// <param name="fallbackY">Y coordinate to use if raycast fails.</param>
    /// <param name="maxVerticalFallback">Maximum vertical difference allowed before using fallback.</param>
    /// <param name="rayDistance">Maximum distance for the down-raycast.</param>
    /// <param name="usedRay">Output: True if the raycast was used, false if fallback was used.</param>
    /// <returns>The Y coordinate to use for NavMesh probing.</returns>
    private float ProbeY(Vector3 p, float fallbackY, float maxVerticalFallback, float rayDistance, out bool usedRay)
    {
      usedRay = false;

      if (groundMask != 0)
      {
        var rayStart = p + Vector3.up * 1.0f;
        if (Physics.Raycast(rayStart, Vector3.down, out var rh, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
          usedRay = true;
          return rh.point.y;
        }
      }

      return Mathf.Abs(p.y - fallbackY) <= maxVerticalFallback ? p.y : fallbackY;
    }

    /// <summary>
    /// Raw NavMesh sampling for movement destinations (without anti-snap guard).
    /// Uses ground-ray driven probeY to match target sampling philosophy and prevent chest-height pivots from poisoning nav probing.
    /// </summary>
    /// <param name="desired">Desired position to sample.</param>
    /// <param name="hit">Output: NavMesh hit result if sampling succeeded.</param>
    /// <returns>True if a valid NavMesh position was found, false otherwise.</returns>
    private bool TrySampleNavForMoveRaw(Vector3 desired, out NavMeshHit hit)
    {
      float probeY = ProbeY(desired, transform.position.y, maxNavVerticalForMove, targetGroundRayDistance, out _);
      return TrySampleNav(desired, maxNavSnapForMove, probeY, maxNavVerticalForMove, navSampleRadiusMove, out hit);
    }

    /// <summary>
    /// Samples a NavMesh position for movement destinations with optional physics-based anti-snap guard.
    /// Note: Reachability is validated via NavMesh.CalculatePath in IsReachable, not via NavMesh.Raycast
    /// (which would reject valid cornering paths).
    /// </summary>
    /// <param name="desired">Desired position to sample.</param>
    /// <param name="sampled">Output: Sampled NavMesh position if successful.</param>
    /// <returns>True if a valid, unblocked NavMesh position was found, false otherwise.</returns>
    private bool TrySampleNavForMove(Vector3 desired, out Vector3 sampled)
    {
      sampled = default;
      if (!TrySampleNavForMoveRaw(desired, out var hit)) return false;

      // Only if we actually moved in planar direction (anti-snap through thin walls/doorways guard)
      if (MovedPlanar(desired, hit.position))
      {
        // desired and hit.position are both pivot positions (transform positions)
        // Flatten to navmesh Y for consistent check (protects against thin wall snaps)
        // NOTE: This will miss blockers that require vertical separation (stairs/ramps/lips).
        // If you see rare "teleport through railing" cases on sloped geometry, remove flattening
        // and cast in full 3D (keep obstacleMask floor-excluded).
        Vector3 aPivot = WithY(desired, hit.position.y);
        Vector3 bPivot = WithY(hit.position, hit.position.y);

        if (IsBlockedBetweenPivots(aPivot, bPivot))
          return false;
      }

      sampled = hit.position;
      return true;
    }

    /// <summary>
    /// Samples a NavMesh position for warps/recoveries with optional physics-based anti-snap guard.
    /// Prevents warping through walls when using permissive snap distance.
    /// </summary>
    /// <param name="desired">Desired position to sample.</param>
    /// <param name="hit">Output: NavMesh hit result if sampling succeeded.</param>
    /// <returns>True if a valid, unblocked NavMesh position was found, false otherwise.</returns>
    private bool TrySampleNavForWarp(Vector3 desired, out NavMeshHit hit)
    {
      hit = default;

      float probeY = ProbeY(desired, transform.position.y, maxNavVerticalForRecover, targetGroundRayDistance, out _);
      if (!TrySampleNav(desired, maxNavSnapForWarp, probeY, maxNavVerticalForWarp, navSampleRadiusMove, out hit))
        return false;

      if (IsEmbeddedAtPivot(hit.position))
        return false;

      if (MovedPlanar(desired, hit.position))
      {
        Vector3 aPivot = WithY(desired, hit.position.y);
        Vector3 bPivot = WithY(hit.position, hit.position.y);
        if (IsBlockedBetweenPivots(aPivot, bPivot))
          return false;
      }

      return true;
    }

    /// <summary>
    /// Samples a NavMesh position for off-navmesh recovery with very loose vertical and planar constraints.
    /// Used when CC is airborne, stepped onto non-nav geometry, or has large vertical separation from baked surface.
    /// Includes anti-snap guard to prevent jumping across thin walls/door seams.
    /// Uses down-ray to get sane probe plane when far above NavMesh, with fallback to desired.y if ray plane is misleading.
    /// </summary>
    /// <param name="desired">Desired position to sample.</param>
    /// <param name="hit">Output: NavMesh hit result if sampling succeeded.</param>
    /// <returns>True if a valid, unblocked NavMesh position was found, false otherwise.</returns>
    private bool TrySampleNavForRecover(Vector3 desired, out NavMeshHit hit)
    {
      hit = default;

      // Local helper to run guards consistently
      bool Accept(NavMeshHit h)
      {
        if (IsEmbeddedAtPivot(h.position)) return false;

        if (MovedPlanar(desired, h.position))
        {
          Vector3 aPivot = WithY(desired, h.position.y);
          Vector3 bPivot = WithY(h.position, h.position.y);
          if (IsBlockedBetweenPivots(aPivot, bPivot)) return false;
        }
        return true;
      }

      bool usedRay;
      float probeY = ProbeY(desired, transform.position.y, maxNavVerticalForRecover, recoverGroundRayDistance, out usedRay);

      // Pass 1: ray-derived probeY (or fallback)
      if (TrySampleNav(desired, maxNavSnapForRecover, probeY, maxNavVerticalForRecover, navSampleRadiusRecover, out hit) && Accept(hit))
        return true;

      // Pass 2: only if a ray was actually used
      if (usedRay)
      {
        if (TrySampleNav(desired, maxNavSnapForRecover, desired.y, maxNavVerticalForRecover, navSampleRadiusRecover, out hit) && Accept(hit))
          return true;
      }

      hit = default;
      return false;
    }

    /// <summary>
    /// Searches for an unembedded NavMesh recovery point near the origin.
    /// Tries center first, then ring search at increasing radii to find a valid nav point that isn't embedded.
    /// Expands search radius and sample count when origin is embedded (common case).
    /// </summary>
    /// <param name="origin">Origin position to search around.</param>
    /// <param name="hit">Output: NavMesh hit result if a recovery point was found.</param>
    /// <returns>True if a valid recovery point was found, false otherwise.</returns>
    private bool TryFindRecoverPointUnembedded(Vector3 origin, out NavMeshHit hit)
    {
      if (TrySampleNavForRecover(origin, out hit)) return true;

      bool embedded = IsEmbeddedAtPivot(origin);

      int samples = embedded ? 16 : 8;
      float max = embedded ? Mathf.Min(12f, maxNavSnapForRecover) : Mathf.Min(8f, maxNavSnapForRecover);

      float[] radii = embedded
        ? new[] { 0.5f, 1f, 2f, 3f, 4f, 6f, 8f, 10f, max }
        : new[] { 0.5f, 1f, 2f, 3f, 4f, 6f, max };

      for (int r = 0; r < radii.Length; r++)
      {
        float radius = radii[r];
        for (int i = 0; i < samples; i++)
        {
          float a = (i / (float)samples) * Mathf.PI * 2f;
          Vector3 p = origin + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
          p.y = origin.y;

          if (TrySampleNavForRecover(p, out hit)) return true;
        }
      }

      hit = default;
      return false;
    }

    /// <summary>
    /// Guards target nav sampling result against wall-crossing.
    /// Note: Embedding check is NOT performed here because we're not placing the NPC at the target position;
    /// we're using it as a basis to compute follow/warp points. Embedding checks belong on final destinations.
    /// </summary>
    /// <param name="targetPivot">Original target pivot position.</param>
    /// <param name="sampledPivot">Sampled NavMesh pivot position.</param>
    /// <returns>True if the sampled position is valid (not blocked by walls), false otherwise.</returns>
    private bool TargetNavGuard(Vector3 targetPivot, Vector3 sampledPivot)
    {
      if (MovedPlanar(targetPivot, sampledPivot))
      {
        Vector3 aPivot = WithY(targetPivot, sampledPivot.y);
        Vector3 bPivot = WithY(sampledPivot, sampledPivot.y);
        if (IsBlockedBetweenPivots(aPivot, bPivot)) return false;
      }

      return true;
    }

    /// <summary>
    /// Samples the target's NavMesh position. Used to get the target's actual nav surface
    /// (not arbitrary transform Y) for building warp targets on the correct vertical layer.
    /// Tries multiple probe planes (down-ray, pivot, NPC plane) and prefers candidates connected to the NPC's NavMesh island.
    /// This prevents wrong-floor selection on stacked meshes when down-ray fails or groundMask misses.
    /// </summary>
    /// <param name="targetHit">Output: NavMesh hit result for the target's position.</param>
    /// <returns>True if a valid NavMesh position was found for the target, false otherwise.</returns>
    private bool TryGetTargetNav(out NavMeshHit targetHit)
    {
      targetHit = default;
      if (!target) return false;

      bool hasAny = false;
      NavMeshHit any = default;

      bool TryProbe(float probeY, out NavMeshHit h)
      {
        h = default;
        return TrySampleNav(target.position, maxNavSnapForMove, probeY, maxNavVerticalForMove, navSampleRadiusMove, out h)
               && TargetNavGuard(target.position, h.position);
      }

      bool IsConnectedToNpc(Vector3 candidate)
      {
        if (!NavMesh.CalculatePath(candidate, transform.position, _agent.areaMask, _path))
          return false;
        return _path.status == NavMeshPathStatus.PathComplete;
      }

      // Candidate 1: down-ray feet plane
      if (groundMask != 0)
      {
        Vector3 rayStart = target.position + Vector3.up * 1.0f;
        if (Physics.Raycast(rayStart, Vector3.down, out var rh, targetGroundRayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
          if (TryProbe(rh.point.y, out var hRay))
          {
            if (IsConnectedToNpc(hRay.position))
            {
              targetHit = hRay;
              return true;
            }

            any = hRay;
            hasAny = true;
          }
        }
      }

      // Candidate 2: pivot plane
      if (TryProbe(target.position.y, out var hPivot))
      {
        if (IsConnectedToNpc(hPivot.position))
        {
          targetHit = hPivot;
          return true;
        }

        if (!hasAny)
        {
          any = hPivot;
          hasAny = true;
        }
      }

      // Candidate 3: NPC plane
      if (TryProbe(transform.position.y, out var hNpc))
      {
        if (IsConnectedToNpc(hNpc.position))
        {
          targetHit = hNpc;
          return true;
        }

        if (!hasAny)
        {
          any = hNpc;
          hasAny = true;
        }
      }

      if (hasAny)
      {
        targetHit = any;
        return true;
      }

      return false;
    }

    /// <summary>
    /// Validates that a warp point is connected to the target on the same NavMesh island.
    /// Assumes warpPos is already on-navmesh (from TrySampleNav result).
    /// Uses robust target sampling (with down-ray fallback) to handle non-feet transforms.
    /// </summary>
    /// <param name="warpPos">Warp position to validate.</param>
    /// <returns>True if the warp point is connected to the target via a valid path, false otherwise.</returns>
    private bool WarpPointConnectedToTarget(Vector3 warpPos)
    {
      if (!target) return false;

      if (!TryGetTargetNav(out var targetHit)) return false;

      if (!NavMesh.CalculatePath(warpPos, targetHit.position, _agent.areaMask, _path))
        return false;

      return _path.status == NavMeshPathStatus.PathComplete;
    }

    /// <summary>
    /// Warps only the transform/CC to a position (no agent sync).
    /// Used for off-mesh-link traversal where agent state must be completed first.
    /// </summary>
    /// <param name="hit">NavMesh hit containing the position to warp to.</param>
    private void WarpTransformOnly(NavMeshHit hit)
    {
      teleportCount++;

      _cc.enabled = false;
      transform.position = hit.position; // NavMesh anchor - transform.position is the NavMesh point
      _cc.enabled = true;

      // Stop residual CC velocity after teleport (prevents 1-frame shove)
      _cc.Move(Vector3.up * -stickToGround * Time.deltaTime);

      _gravity = -stickToGround;
      _stuckCheckPosition = transform.position;
      _stuckCheckTimer = 0f;
    }

    /// <summary>
    /// Syncs the agent to the current transform position.
    /// </summary>
    private void SyncAgentToTransform()
    {
      _agent.Warp(transform.position);
      _agent.nextPosition = transform.position;
    }

    /// <summary>
    /// Resets all path/timer state after a warp to leave the agent in a clean state.
    /// Shared by both WarpTo and off-mesh link traversal.
    /// </summary>
    private void ResetNavStateAfterWarp()
    {
      _agent.ResetPath();
      _repathTimer = 0f;
      _consecutivePathFailures = 0;
      _stuckRecoveryAttempts = 0;
      _stuckCheckPosition = transform.position;
      _stuckCheckTimer = 0f;
    }

    /// <summary>
    /// Warps the NPC to a NavMesh position with proper CC settling, agent sync, and state reset.
    /// Centralizes the warp sequence to avoid desync between branches.
    /// Resets all path/timer state so every warp leaves the agent in a clean state.
    /// </summary>
    /// <param name="hit">NavMesh hit containing the position to warp to.</param>
    private void WarpTo(NavMeshHit hit)
    {
      WarpTransformOnly(hit);
      SyncAgentToTransform();
      ResetNavStateAfterWarp();
    }


    /// <summary>
    /// Validates that a destination is reachable by computing a path.
    /// If unreachable, tries ring samples around the point, then falls back to nearest reachable edge.
    /// Returns true if a reachable destination was found, false otherwise.
    /// </summary>
    /// <param name="desiredPoint">Desired destination point.</param>
    /// <param name="result">Output: Reachable destination position if found.</param>
    /// <returns>True if a reachable destination was found, false otherwise.</returns>
    private bool TryFindReachableDestination(Vector3 desiredPoint, out Vector3 result)
    {
      result = Vector3.zero;

      EStuckCause searchFailureCause = EStuckCause.None;
      Vector3 lastFailedPoint = Vector3.zero;

      // Always try the primary point (cheap and keeps recovery logic alive)
      if (IsReachable(desiredPoint, out Vector3 reachablePoint, ref searchFailureCause, ref lastFailedPoint))
      {
        result = reachablePoint;
        return true;
      }

      // If not moving, skip the expensive fallback search
      if (!_moving)
      {
        return false;
      }

      // Run ring search immediately to fix common corner cases on first failure

      // Try ring samples at reduced radii and sample count (performance optimization)
      for (float radius = 1f; radius <= 2f; radius += 1f)
      {
        int samples = 4;
        for (int i = 0; i < samples; i++)
        {
          float angle = (i / (float)samples) * 360f * Mathf.Deg2Rad;
          Vector3 ringPoint = desiredPoint + new Vector3(
            Mathf.Cos(angle) * radius,
            0f,
            Mathf.Sin(angle) * radius
          );
          ringPoint.y = ProbeY(ringPoint, transform.position.y, maxNavVerticalForWarp, targetGroundRayDistance, out _);

          if (IsReachable(ringPoint, out reachablePoint, ref searchFailureCause, ref lastFailedPoint))
          {
            result = reachablePoint;
            return true;
          }
        }
      }

      // Fallback: find nearest reachable edge toward target
      int areaMask = _agent.areaMask;

      float edgeProbeY = ProbeY(desiredPoint, transform.position.y, maxNavVerticalForWarp, targetGroundRayDistance, out _);
      Vector3 edgeProbe = WithY(desiredPoint, edgeProbeY);

      if (NavMesh.FindClosestEdge(edgeProbe, out NavMeshHit edgeHit, areaMask))
      {
        if (IsReachable(edgeHit.position, out reachablePoint, ref searchFailureCause, ref lastFailedPoint))
        {
          result = reachablePoint;
          return true;
        }
      }

      // Last resort: log the failure (only once per search)
      string failureDetails = searchFailureCause != EStuckCause.None
        ? $" ({searchFailureCause} at {lastFailedPoint})"
        : "";
      SetStuckCause(EStuckCause.NoReachableDestinationFound,
        $"NPC {gameObject.name} TryFindReachableDestination failed for {desiredPoint} - tried all fallback methods{failureDetails}");
      return false;
    }

    /// <summary>
    /// Checks if a destination is reachable by computing a path.
    /// Returns true if path status is PathComplete.
    /// Does not log - logging is handled at the FindReachableDestination level.
    /// Uses deterministic path calculation from sampled start to sampled end to avoid agent-state coupling.
    /// </summary>
    /// <param name="destination">Destination position to check.</param>
    /// <param name="sampledPosition">Output: Sampled NavMesh position for the destination.</param>
    /// <param name="failureCause">Output: Stuck cause if the destination is unreachable.</param>
    /// <param name="lastFailedPoint">Output: Last point that failed validation.</param>
    /// <returns>True if the destination is reachable, false otherwise.</returns>
    private bool IsReachable(Vector3 destination, out Vector3 sampledPosition, ref EStuckCause failureCause, ref Vector3 lastFailedPoint)
    {
      sampledPosition = Vector3.zero;

      // Sample position on NavMesh using agent's area mask with snap distance and line-of-sight check
      if (!TrySampleNavForMove(destination, out sampledPosition))
      {
        failureCause = EStuckCause.NavMeshSamplingFailed;
        lastFailedPoint = destination;
        return false;
      }

      // Start sample should be strict-planar to avoid snapping across walls.
      // Use recover radius for contact offset/stairs/slopes, but keep planar snap strict (move-level).
      // Allow vertical permissive enough for slopes (warp-level).
      // Use down-ray to get sane probe plane (same protection as target sampling).
      float startProbeY = transform.position.y;
      bool gotStartRayY = false;

      if (groundMask != 0)
      {
        Vector3 rayStart = transform.position + Vector3.up * 1.0f;
        if (Physics.Raycast(rayStart, Vector3.down, out var rh, targetGroundRayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
          startProbeY = rh.point.y;
          gotStartRayY = true;
        }
      }

      bool SampleStart(float probeY, out NavMeshHit startHit)
      {
        return TrySampleNav(transform.position,
                            maxNavSnapForMove,
                            probeY,
                            maxNavVerticalForWarp,
                            navSampleRadiusRecover,
                            out startHit);
      }

      NavMeshHit startHit;
      if (!SampleStart(startProbeY, out startHit))
      {
        if (!gotStartRayY || !SampleStart(transform.position.y, out startHit))
        {
          failureCause = EStuckCause.NavMeshSamplingFailed;
          lastFailedPoint = transform.position;
          return false;
        }
      }

      // Reject start if embedded (rare but real near thin walls/props)
      if (IsEmbeddedAtPivot(startHit.position))
      {
        failureCause = EStuckCause.NavMeshSamplingFailed;
        lastFailedPoint = startHit.position;
        return false;
      }

      // Prevent start sample snapping across thin walls
      if (MovedPlanar(transform.position, startHit.position))
      {
        Vector3 aPivot = WithY(transform.position, startHit.position.y);
        Vector3 bPivot = WithY(startHit.position, startHit.position.y);

        if (IsBlockedBetweenPivots(aPivot, bPivot))
        {
          failureCause = EStuckCause.NavMeshSamplingFailed;
          lastFailedPoint = startHit.position;
          return false;
        }
      }

      // Compute path from sampled start to sampled end (deterministic, avoids agent-state coupling)
      if (!NavMesh.CalculatePath(startHit.position, sampledPosition, _agent.areaMask, _path))
      {
        failureCause = EStuckCause.PathCalculationFailed;
        lastFailedPoint = sampledPosition;
        return false;
      }

      // Only accept if path is complete
      if (_path.status != NavMeshPathStatus.PathComplete)
      {
        failureCause = EStuckCause.NoPathToDestination;
        lastFailedPoint = sampledPosition;
        return false;
      }

      return true;
    }

    /// <summary>
    /// Sets the stuck cause and logs it if it's different from the last logged cause.
    /// </summary>
    /// <param name="cause">The stuck cause to set.</param>
    /// <param name="message">The message to log when setting the stuck cause.</param>
    private void SetStuckCause(EStuckCause cause, string message)
    {
      if (cause == EStuckCause.None)
      {
        _currentStuckCause = EStuckCause.None;
        return;
      }

      _currentStuckCause = cause;

      // Throttle by time, not by enum, to allow new diagnostics and positions to be logged
      if (Time.time - _lastStuckLogTime >= stuckLogInterval)
      {
        Debug.LogWarning($"[NpcFollowerExample] {message}", this);
        _lastStuckLogTime = Time.time;
      }
    }

    /// <summary>
    /// Clears the stuck cause if it matches the provided cause, and logs recovery.
    /// </summary>
    /// <param name="cause">The stuck cause to clear.</param>
    /// <param name="message">The message to log when clearing the stuck cause.</param>
    private void ClearStuckCause(EStuckCause cause, string message)
    {
      if (_currentStuckCause == cause)
      {
        _currentStuckCause = EStuckCause.None;
        _lastStuckLogTime = -999f; // allow immediate next diagnostic
        if (!string.IsNullOrEmpty(message) && showDebugInfo)
        {
          Debug.Log($"[NpcFollowerExample] {message}", this);
        }
      }
    }

    /// <summary>
    /// Clears any stuck cause (regardless of current cause) and logs recovery.
    /// Use this after successful recovery warps where the cause may have been overwritten.
    /// </summary>
    /// <param name="message">The message to log when clearing the stuck cause.</param>
    private void ClearStuckCauseAny(string message)
    {
      if (_currentStuckCause == EStuckCause.None) return;

      _currentStuckCause = EStuckCause.None;
      _lastStuckLogTime = -999f;
      if (!string.IsNullOrEmpty(message) && showDebugInfo)
        Debug.Log($"[NpcFollowerExample] {message}", this);
    }

    /// <summary>
    /// Called by animation events to play footstep audio clips.
    /// </summary>
    /// <param name="animationEvent">The animation event that triggered this callback.</param>
    private void OnFootstep(AnimationEvent animationEvent)
    {
      if (animationEvent.animatorClipInfo.weight > 0.5f)
      {
        if (FootstepAudioClips != null && FootstepAudioClips.Length > 0)
        {
          var index = Random.Range(0, FootstepAudioClips.Length);
          AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_cc.center), FootstepAudioVolume);
        }
      }
    }

    /// <summary>
    /// Called by animation events to play landing audio clip.
    /// </summary>
    /// <param name="animationEvent">The animation event that triggered this callback.</param>
    private void OnLand(AnimationEvent animationEvent)
    {
      if (animationEvent.animatorClipInfo.weight > 0.5f)
      {
        if (LandingAudioClip != null)
        {
          AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_cc.center), FootstepAudioVolume);
        }
      }
    }

    /// <summary>
    /// Initializes the debug canvas if showDebugInfo is enabled.
    /// Creates a world space canvas if one is not assigned.
    /// </summary>
    private void InitializeDebugCanvas()
    {
      if (!showDebugInfo) return;

      // Refresh camera cache if null
      if (_mainCamera == null)
      {
        _mainCamera = Camera.main;
      }

      // Use existing canvas if assigned, otherwise create one
      if (debugCanvas == null)
      {
        GameObject canvasObj = new GameObject("DebugCanvas");
        canvasObj.transform.SetParent(transform, false); // worldPositionStays = false
        canvasObj.transform.localPosition = canvasOffset;
        canvasObj.transform.localRotation = Quaternion.identity;
        canvasObj.transform.localScale = Vector3.one * canvasScale;

        debugCanvas = canvasObj.AddComponent<Canvas>();
        debugCanvas.renderMode = RenderMode.WorldSpace;
        debugCanvas.worldCamera = _mainCamera;

        // Ensure world-space canvas has non-zero size (otherwise TMP may not render)
        var canvasRt = debugCanvas.GetComponent<RectTransform>();
        canvasRt.pivot = new Vector2(0.5f, 0.5f);
        canvasRt.sizeDelta = new Vector2(600f, 300f); // tune once, then leave it

        // Add CanvasScaler for better text rendering
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;

        // Add GraphicRaycaster but disable it to reduce interaction overhead
        var gr = canvasObj.AddComponent<GraphicRaycaster>();
        gr.enabled = false;
      }
      else
      {
        // Ensure canvas is properly configured
        debugCanvas.renderMode = RenderMode.WorldSpace;
        debugCanvas.worldCamera = _mainCamera;
        // Always enforce pose to handle prefab/scene modifications
        debugCanvas.transform.SetParent(transform, false); // worldPositionStays = false
        debugCanvas.transform.localPosition = canvasOffset;
        debugCanvas.transform.localRotation = Quaternion.identity;
        debugCanvas.transform.localScale = Vector3.one * canvasScale;

        // Ensure world-space canvas has non-zero size (otherwise TMP may not render)
        var canvasRt = debugCanvas.GetComponent<RectTransform>();
        if (canvasRt != null)
        {
          canvasRt.pivot = new Vector2(0.5f, 0.5f);
          canvasRt.sizeDelta = new Vector2(600f, 300f); // tune once, then leave it
        }

        // Disable GraphicRaycaster to reduce interaction overhead
        var gr = debugCanvas.GetComponent<GraphicRaycaster>();
        if (gr != null) gr.enabled = false;
      }

      // Find or create TMP text component
      if (debugText == null)
      {
        // Try to find existing TMP component in canvas children
        debugText = debugCanvas.GetComponentInChildren<TextMeshProUGUI>();

        // If not found, create one
        if (debugText == null)
        {
          GameObject textObj = new GameObject("DebugText");
          textObj.transform.SetParent(debugCanvas.transform, false);
          RectTransform rectTransform = textObj.AddComponent<RectTransform>();
          rectTransform.anchorMin = Vector2.zero;
          rectTransform.anchorMax = Vector2.one;
          rectTransform.sizeDelta = Vector2.zero;
          rectTransform.anchoredPosition = Vector2.zero;

          debugText = textObj.AddComponent<TextMeshProUGUI>();
          debugText.fontSize = 24;
          debugText.alignment = TextAlignmentOptions.TopLeft;
          debugText.color = Color.white;
          debugText.textWrappingMode = TextWrappingModes.NoWrap;
          debugText.overflowMode = TextOverflowModes.Overflow;
        }
      }

      // Disable raycast target to reduce interaction overhead
      debugText.raycastTarget = false;

      // Set active state after ensuring debugText exists
      debugText.gameObject.SetActive(showDebugInfo);
    }

    /// <summary>
    /// Updates canvas billboarding to face camera every frame in LateUpdate.
    /// This ensures smooth rotation even with Cinemachine cameras that update after Update().
    /// </summary>
    private void LateUpdate()
    {
      if (!showDebugInfo || debugCanvas == null) return;

      // Refresh camera cache if null
      if (_mainCamera == null)
      {
        _mainCamera = Camera.main;
      }

      if (_mainCamera == null) return;

      var t = debugCanvas.transform;
      t.LookAt(t.position + _mainCamera.transform.rotation * Vector3.forward,
               _mainCamera.transform.rotation * Vector3.up);
    }

    /// <summary>
    /// Updates the debug display with current follower state information.
    /// </summary>
    /// <param name="distanceToTarget">Current distance to the target in meters.</param>
    /// <param name="catchUp">Current catch-up distance (distance beyond follow distance) in meters.</param>
    /// <param name="desiredSpeed">Desired movement speed in meters per second.</param>
    /// <param name="planarSpeed">Actual planar movement speed in meters per second.</param>
    private void UpdateDebugDisplay(float distanceToTarget, float catchUp, float desiredSpeed, float planarSpeed)
    {
      if (!showDebugInfo || debugText == null || debugCanvas == null) return;

      // Build debug string (reuse cached StringBuilder)
      _sb.Clear();
      _sb.AppendLine($"<b>NPC Follower Debug</b>");
      _sb.AppendLine($"─────────────────────");
      _sb.AppendLine($"Target: {(target ? target.name : "None")}");
      _sb.AppendLine($"Distance: {distanceToTarget:F2}m");
      _sb.AppendLine($"Follow Distance: {followDistance:F2}m");
      _sb.AppendLine($"Catch Up: {catchUp:F2}m");
      _sb.AppendLine($"Moving: {(_moving ? "Yes" : "No")}");
      _sb.AppendLine($"Desired Speed: {desiredSpeed:F2} m/s");
      _sb.AppendLine($"Actual Speed: {planarSpeed:F2} m/s");
      // Guard against stale values after ResetPath()
      float remainingDist = _agent.hasPath ? _agent.remainingDistance : float.PositiveInfinity;
      _sb.AppendLine($"Remaining Distance: {(remainingDist == float.PositiveInfinity ? "N/A" : $"{remainingDist:F2}m")}");
      _sb.AppendLine($"Stuck: {(_isStuck ? "YES!" : "No")}");

      // Display stuck cause prominently if stuck
      if (_currentStuckCause != EStuckCause.None)
      {
        _sb.AppendLine($"<color=red><b>Stuck Cause: {_currentStuckCause}</b></color>");
      }
      else
      {
        _sb.AppendLine($"Stuck Cause: {_currentStuckCause}");
      }

      _sb.AppendLine($"Recovery Attempts: {_stuckRecoveryAttempts}/3");
      _sb.AppendLine($"Path Failures: {_consecutivePathFailures}/{MAX_PATH_FAILURES_BEFORE_WARP}");
      _sb.AppendLine($"On NavMesh: {_agent.isOnNavMesh}");
      _sb.AppendLine($"Grounded: {_cc.isGrounded}");
      _sb.AppendLine($"Gravity: {_gravity:F2}");
      _sb.AppendLine($"Repath Timer: {_repathTimer:F2}s");
      _sb.AppendLine($"Teleports: {teleportCount}");

      // Color code stuck state
      if (_isStuck)
      {
        debugText.color = Color.red;
      }
      else if (_moving)
      {
        debugText.color = Color.yellow;
      }
      else
      {
        debugText.color = Color.white;
      }

      debugText.text = _sb.ToString();
    }

    /// <summary>
    /// Draws debug visualization in the scene view (editor) and game view (runtime).
    /// </summary>
    private void OnDrawGizmos()
    {
      if (!drawDebugGizmos) return;

      // Draw capsule bounds
      if (drawCapsule && _cc != null)
      {
        Gizmos.color = _isStuck ? Color.red : (_moving ? Color.yellow : Color.green);
        Vector3 center = transform.position + _cc.center;
        float radius = _cc.radius;
        float height = _cc.height;

        // Draw capsule as wireframe
        Vector3 top = center + Vector3.up * (height * 0.5f - radius);
        Vector3 bottom = center - Vector3.up * (height * 0.5f - radius);

        // Draw cylinder part
        DrawWireCapsule(center, height, radius);
      }

      // Draw target connection
      if (drawTargetConnection && target != null)
      {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, target.position);

        // Draw target position
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(target.position, 0.3f);
      }
    }

    /// <summary>
    /// Draws debug lines every frame for runtime visualization.
    /// </summary>
    private void DrawDebugLines()
    {
      if (!drawDebugGizmos) return;

      // Draw NavMesh path
      if (drawPath && _agent != null && _agent.hasPath)
      {
        Color pathColor = _agent.path.status == NavMeshPathStatus.PathComplete ? Color.green : Color.yellow;
        Vector3[] corners = _agent.path.corners;

        for (int i = 0; i < corners.Length - 1; i++)
        {
          Debug.DrawLine(corners[i], corners[i + 1], pathColor);
        }

        // Draw path points
        for (int i = 0; i < corners.Length; i++)
        {
          Debug.DrawRay(corners[i], Vector3.up * 0.5f, pathColor);
        }
      }

      // Draw destination
      if (drawDestination && _agent != null && _agent.hasPath)
      {
        Debug.DrawRay(_agent.destination, Vector3.up * 1.0f, Color.blue, 0f, false);
        Debug.DrawLine(transform.position, _agent.destination, Color.blue);
      }

      // Draw velocity vectors
      if (drawVelocity)
      {
        // Agent desired velocity
        if (_agent != null && _agent.hasPath)
        {
          Vector3 desiredVel = _agent.desiredVelocity;
          if (desiredVel.magnitude > 0.01f)
          {
            Debug.DrawRay(transform.position, desiredVel, Color.green, 0f, false);
          }
        }

        // Character controller velocity
        if (_cc != null)
        {
          Vector3 ccVel = _cc.velocity;
          if (ccVel.magnitude > 0.01f)
          {
            Debug.DrawRay(transform.position, ccVel, Color.yellow, 0f, false);
          }
        }
      }

      // Draw follow point if we have a target
      if (target != null)
      {
        Vector3 toTarget = target.position - transform.position;
        float dist = toTarget.magnitude;
        if (dist > 0.01f)
        {
          Vector3 followPoint = target.position - toTarget.normalized * followDistance;
          Debug.DrawLine(transform.position, followPoint, Color.cyan, 0f, false);
          Debug.DrawRay(followPoint, Vector3.up * 0.5f, Color.cyan, 0f, false);
        }
      }

      // Draw stuck detection area
      if (_isStuck)
      {
        Debug.DrawRay(_stuckCheckPosition, Vector3.up * 2.0f, Color.red, 0f, false);
        Debug.DrawLine(transform.position, _stuckCheckPosition, Color.red, 0f, false);
      }
    }

    /// <summary>
    /// Helper to draw a wireframe capsule using Gizmos.
    /// </summary>
    /// <param name="center">Center position of the capsule.</param>
    /// <param name="height">Total height of the capsule.</param>
    /// <param name="radius">Radius of the capsule spheres.</param>
    private void DrawWireCapsule(Vector3 center, float height, float radius)
    {
      float halfHeight = height * 0.5f;
      float topY = center.y + halfHeight - radius;
      float bottomY = center.y - halfHeight + radius;

      // Draw vertical lines
      Vector3 topFront = new Vector3(center.x, topY, center.z + radius);
      Vector3 topBack = new Vector3(center.x, topY, center.z - radius);
      Vector3 topLeft = new Vector3(center.x - radius, topY, center.z);
      Vector3 topRight = new Vector3(center.x + radius, topY, center.z);

      Vector3 bottomFront = new Vector3(center.x, bottomY, center.z + radius);
      Vector3 bottomBack = new Vector3(center.x, bottomY, center.z - radius);
      Vector3 bottomLeft = new Vector3(center.x - radius, bottomY, center.z);
      Vector3 bottomRight = new Vector3(center.x + radius, bottomY, center.z);

      // Vertical lines
      Gizmos.DrawLine(topFront, bottomFront);
      Gizmos.DrawLine(topBack, bottomBack);
      Gizmos.DrawLine(topLeft, bottomLeft);
      Gizmos.DrawLine(topRight, bottomRight);

      // Top circle
      DrawWireCircle(new Vector3(center.x, topY, center.z), radius, Vector3.up);
      // Bottom circle
      DrawWireCircle(new Vector3(center.x, bottomY, center.z), radius, Vector3.up);

      // Top hemisphere arcs
      DrawWireArc(new Vector3(center.x, center.y + halfHeight, center.z), radius, 4);
      // Bottom hemisphere arcs
      DrawWireArc(new Vector3(center.x, center.y - halfHeight, center.z), radius, 4);
    }

    /// <summary>
    /// Draws a wireframe circle using Gizmos.
    /// </summary>
    /// <param name="center">Center position of the circle.</param>
    /// <param name="radius">Radius of the circle.</param>
    /// <param name="normal">Normal vector defining the plane of the circle.</param>
    private void DrawWireCircle(Vector3 center, float radius, Vector3 normal)
    {
      int segments = 16;
      Vector3 forward = (normal == Vector3.up) ? Vector3.forward : Vector3.up;
      Vector3 right = Vector3.Cross(normal, forward).normalized;
      forward = Vector3.Cross(right, normal).normalized;

      Vector3 prevPoint = center + right * radius;
      for (int i = 1; i <= segments; i++)
      {
        float angle = (i / (float)segments) * Mathf.PI * 2f;
        Vector3 point = center + (right * Mathf.Cos(angle) + forward * Mathf.Sin(angle)) * radius;
        Gizmos.DrawLine(prevPoint, point);
        prevPoint = point;
      }
    }

    /// <summary>
    /// Draws wireframe arcs for capsule hemispheres.
    /// </summary>
    /// <param name="center">Center position of the hemisphere.</param>
    /// <param name="radius">Radius of the hemisphere.</param>
    /// <param name="count">Number of arcs to draw around the hemisphere.</param>
    private void DrawWireArc(Vector3 center, float radius, int count)
    {
      for (int i = 0; i < count; i++)
      {
        float angle = (i / (float)count) * Mathf.PI * 2f;
        Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

        for (int j = 0; j < 8; j++)
        {
          float arcAngle = (j / 8f) * Mathf.PI;
          Vector3 arcDir = new Vector3(dir.x * Mathf.Sin(arcAngle), Mathf.Cos(arcAngle), dir.z * Mathf.Sin(arcAngle));
          Vector3 point = center + arcDir * radius;

          if (j > 0)
          {
            Vector3 prevArcDir = new Vector3(dir.x * Mathf.Sin((j - 1) / 8f * Mathf.PI), Mathf.Cos((j - 1) / 8f * Mathf.PI), dir.z * Mathf.Sin((j - 1) / 8f * Mathf.PI));
            Vector3 prevPoint = center + prevArcDir * radius;
            Gizmos.DrawLine(prevPoint, point);
          }
        }
      }
    }

    /// <summary>
    /// Shows the prompt indicator with the specified text.
    /// </summary>
    /// <param name="text">The text to display in the prompt indicator.</param>
    public void ShowPromptIndicator(string text)
    {
      Debug.LogFormat("{0}: {1} - {2}", nameof(NpcFollowerExample), nameof(ShowPromptIndicator), text);
      if (promptIndicator == null || promptIndicatorText == null) return;
      promptIndicator.SetActive(true);
      promptIndicatorText.text = text;
      isShowingPromptIndicator = true;
    }

    /// <summary>
    /// Hides the prompt indicator.
    /// </summary>
    public void HidePromptIndicator()
    {
      Debug.LogFormat("{0}: {1}", nameof(NpcFollowerExample), nameof(HidePromptIndicator));
      if (promptIndicator == null) return;
      promptIndicator.SetActive(false);
      isShowingPromptIndicator = false;
    }
  }
}