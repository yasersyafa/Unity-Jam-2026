using PrimeTween;
using UnityEngine;

/// <summary>
/// Flight presentation for robot enemies: wings flap continuously while alive, and the body sprite faces the
/// direction it is flying. Reads only the transform's motion, so it works with every movement behaviour.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Enemy))]
public sealed class EnemyFlightVisual : MonoBehaviour
{
    [Header("Wings")]
    [Tooltip("Defaults to the child named LeftWing.")]
    [SerializeField] private Transform leftWing;
    [Tooltip("Defaults to the child named RightWing.")]
    [SerializeField] private Transform rightWing;
    [Tooltip("Flap swing either side of the rest angle, in degrees.")]
    [SerializeField, Range(0f, 90f)] private float flapAngle = 25f;
    [Tooltip("Seconds for one stroke (up or down).")]
    [SerializeField, Min(0.02f)] private float flapDuration = 0.16f;
    [SerializeField] private Ease flapEase = Ease.InOutSine;

    [Header("Facing")]
    [Tooltip("Defaults to the SpriteRenderer on the child named Visual.")]
    [SerializeField] private SpriteRenderer body;
    [Tooltip("Direction the body art faces without flipping.")]
    [SerializeField] private bool spriteFacesRight = true;
    [Tooltip("Horizontal speed (world units per second) needed before the body turns around.")]
    [SerializeField, Min(0f)] private float flipSpeedThreshold = 0.4f;
    [Tooltip("Minimum seconds between turns, so spiral and sway reversals do not flicker.")]
    [SerializeField, Min(0f)] private float minFlipInterval = 0.2f;

    private Enemy enemy;
    private EnemyApproach approach;
    private Camera view;
    private Quaternion leftRest, rightRest;
    private Tween leftFlap, rightFlap;
    private Vector3 lastPosition;
    private bool facingRight;
    private float sinceLastFlip;

    /// <summary>
    /// Decides the facing for this frame. Turns only when moving fast enough the other way and the last turn was
    /// long enough ago; otherwise keeps <paramref name="currentFacingRight"/>.
    /// </summary>
    public static bool ResolveFacingRight(bool currentFacingRight, float horizontalSpeed, float threshold, float sinceLastFlip, float minInterval)
    {
        if (Mathf.Abs(horizontalSpeed) < threshold || sinceLastFlip < minInterval) return currentFacingRight;
        return horizontalSpeed > 0f;
    }

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        approach = GetComponent<EnemyApproach>();
        if (leftWing == null) leftWing = transform.Find("LeftWing");
        if (rightWing == null) rightWing = transform.Find("RightWing");
        if (body == null && transform.Find("Visual") is Transform visual) body = visual.GetComponent<SpriteRenderer>();
        if (leftWing != null) leftRest = leftWing.localRotation;
        if (rightWing != null) rightRest = rightWing.localRotation;
        facingRight = spriteFacesRight;
    }

    private void OnEnable()
    {
        enemy.Died += OnDied;
        SetWingsVisible(true);
        // Random offset so a wave of enemies does not flap in lockstep. The right wing mirrors the left.
        float offset = Random.Range(0f, flapDuration);
        leftFlap = Flap(leftWing, leftRest, flapAngle, offset);
        rightFlap = Flap(rightWing, rightRest, -flapAngle, offset);
        lastPosition = transform.position;
        sinceLastFlip = minFlipInterval;
    }

    private Tween Flap(Transform wing, Quaternion rest, float angle, float offset)
    {
        if (wing == null) return default;
        Vector3 euler = rest.eulerAngles;
        return Tween.LocalRotation(wing, euler + new Vector3(0f, 0f, angle), euler - new Vector3(0f, 0f, angle),
            flapDuration, flapEase, cycles: -1, cycleMode: CycleMode.Yoyo, startDelay: offset);
    }

    private void Update()
    {
        // Wings slow down with the enemy (e.g. Water Gun slow).
        float speed = approach != null ? approach.SpeedMultiplier : 1f;
        if (leftFlap.isAlive) leftFlap.timeScale = speed;
        if (rightFlap.isAlive) rightFlap.timeScale = speed;
    }

    private void LateUpdate()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f || body == null) return; // paused (slot open)
        if (view == null) view = Camera.main;
        Vector3 right = view != null ? view.transform.right : Vector3.right;
        Vector3 position = transform.position;
        float horizontalSpeed = Vector3.Dot(position - lastPosition, right) / deltaTime;
        lastPosition = position;
        sinceLastFlip += deltaTime;

        bool next = ResolveFacingRight(facingRight, horizontalSpeed, flipSpeedThreshold, sinceLastFlip, minFlipInterval);
        if (next == facingRight) return;
        facingRight = next;
        sinceLastFlip = 0f;
        body.flipX = facingRight != spriteFacesRight;
    }

    // The explosion VFX takes over on death, so the wings stop and vanish instead of flapping through the pop-out.
    private void OnDied()
    {
        StopFlapping();
        SetWingsVisible(false);
    }

    private void StopFlapping()
    {
        leftFlap.Stop();
        rightFlap.Stop();
        if (leftWing != null) leftWing.localRotation = leftRest;
        if (rightWing != null) rightWing.localRotation = rightRest;
    }

    private void SetWingsVisible(bool visible)
    {
        SetVisible(leftWing, visible);
        SetVisible(rightWing, visible);
    }

    // Looked up on demand (only on spawn and death) so wing art with child sprites is covered too.
    private static readonly System.Collections.Generic.List<Renderer> wingRenderers = new();

    private static void SetVisible(Transform wing, bool visible)
    {
        if (wing == null) return;
        wing.GetComponentsInChildren(true, wingRenderers);
        foreach (var renderer in wingRenderers) renderer.enabled = visible;
        wingRenderers.Clear();
    }

    // Also runs on Destroy, so no tween ever outlives the enemy.
    private void OnDisable()
    {
        enemy.Died -= OnDied;
        StopFlapping();
    }
}
