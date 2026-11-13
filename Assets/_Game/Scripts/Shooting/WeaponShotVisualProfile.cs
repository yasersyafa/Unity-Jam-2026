using UnityEngine;

[CreateAssetMenu(menuName = "Game/Weapon Shot Visual Profile")]
public sealed class WeaponShotVisualProfile : ScriptableObject
{
    public enum VisualMode { Hitscan, Projectile, Stream }
    public enum Shape { Rock, Spear, Slipper, Dart, Durian, Cylinder, Rocket, Streak, Burst, Spike, Gun }

    [Header("Behaviour")]
    [SerializeField] private VisualMode mode;
    [Header("Projectile / Stream")]
    [Tooltip("Optional art replacement. Empty uses the selected silhouette.")]
    [SerializeField] private Sprite sprite;
    [SerializeField] private Shape shape = Shape.Rock;
    [SerializeField] private Color color = Color.white;
    [Tooltip("Size in reference Canvas pixels; silhouettes point along local +X.")]
    [SerializeField] private Vector2 size = new Vector2(50f, 30f);
    [Tooltip("Travel speed in reference Canvas pixels per second.")]
    [SerializeField, Min(1f)] private float speed = 1800f;
    [Tooltip("Degrees per second. Zero keeps the launch orientation.")]
    [SerializeField] private float spin;
    [Tooltip("Face the destination at launch. Disable for upright objects such as LPG and durian.")]
    [SerializeField] private bool orientToTravel = true;
    [Tooltip("Maximum stream length in reference Canvas pixels.")]
    [SerializeField, Min(1f)] private float streamLength = 800f;
    [SerializeField, Min(1f)] private float streamWidth = 36f;

    [Header("Trail")]
    [SerializeField] private Color trailColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField, Min(0f)] private float trailLength = 65f;
    [SerializeField, Min(1f)] private float trailWidth = 8f;

    [Header("Impact")]
    [SerializeField] private Sprite impactSprite;
    [SerializeField] private Shape impactShape = Shape.Burst;
    [SerializeField] private Color impactColor = new Color(1f, 0.8f, 0.2f, 1f);
    [SerializeField, Min(1f)] private float impactSize = 70f;
    [SerializeField, Min(0.01f)] private float impactDuration = 0.2f;
    [Tooltip("Radial fragments on arrival, e.g. durian spikes. Zero disables fragments.")]
    [SerializeField, Range(0, 16)] private int fragmentCount;
    [SerializeField, Min(0f)] private float fragmentDistance = 85f;

    public VisualMode Mode => mode;
    public Sprite Sprite => sprite;
    public Shape Silhouette => shape;
    public Color Color => color;
    public Vector2 Size => new Vector2(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y));
    public float Speed => Mathf.Max(1f, speed);
    public float Spin => spin;
    public bool OrientToTravel => orientToTravel;
    public float StreamLength => Mathf.Max(1f, streamLength);
    public float StreamWidth => Mathf.Max(1f, streamWidth);
    public Color TrailColor => trailColor;
    public float TrailLength => Mathf.Max(0f, trailLength);
    public float TrailWidth => Mathf.Max(1f, trailWidth);
    public Sprite ImpactSprite => impactSprite;
    public Shape ImpactShape => impactShape;
    public Color ImpactColor => impactColor;
    public float ImpactSize => Mathf.Max(1f, impactSize);
    public float ImpactDuration => Mathf.Max(0.01f, impactDuration);
    public int FragmentCount => Mathf.Clamp(fragmentCount, 0, 16);
    public float FragmentDistance => Mathf.Max(0f, fragmentDistance);
}
