using UnityEngine;

[CreateAssetMenu(menuName = "Game/Boss Config")]
public sealed class BossConfig : ScriptableObject
{
    [SerializeField] private float health = 1500f;
    [SerializeField] private Sprite sprite;
    [SerializeField] private float initialDelay = 2f;
    [SerializeField] private Vector2 durations = new Vector2(4f, 3f);
    [SerializeField] private Vector2 delays = new Vector2(2f, 1.5f);
    [SerializeField] private Vector2 damages = new Vector2(10f, 15f);
    public float Health => Mathf.Max(1f, health);
    public Sprite Sprite => sprite;
    public float InitialDelay => Mathf.Max(0f, initialDelay);
    public float Duration(bool second) => Mathf.Max(0.1f, second ? durations.y : durations.x);
    public float Delay(bool second) => Mathf.Max(0f, second ? delays.y : delays.x);
    public float Damage(bool second) => Mathf.Max(0f, second ? damages.y : damages.x);
}
