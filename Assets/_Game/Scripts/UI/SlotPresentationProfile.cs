using UnityEngine;

[CreateAssetMenu(menuName = "Game/UI/Slot Presentation Profile")]
public sealed class SlotPresentationProfile : ScriptableObject
{
    [Header("Panel")]
    [SerializeField] private bool animatePanel = true;
    [SerializeField, Min(0.01f)] private float enterDuration = 0.25f;
    [SerializeField, Min(0.01f)] private float exitDuration = 0.15f;
    [SerializeField, Range(0.1f, 1f)] private float enterScale = 0.85f;
    [SerializeField, Min(0f)] private float enterOffset = 60f;
    [SerializeField] private float enterAngle = -3f;
    [Header("Buttons")]
    [SerializeField] private bool animateButtons = true;
    [SerializeField, Min(0.01f)] private float buttonDuration = 0.1f;
    [SerializeField, Range(1f, 1.2f)] private float hoverScale = 1.04f;
    [SerializeField, Range(0.5f, 1f)] private float pressedScale = 0.94f;
    [Header("Reward")]
    [SerializeField] private bool animateReward = true;
    [SerializeField, Min(0.01f)] private float rewardPopDuration = 0.2f;
    [Tooltip("Common, Uncommon, Rare, Epic, Jackpot; seconds after the pop.")]
    [SerializeField] private float[] rewardHold = { 0.35f, 0.35f, 0.5f, 0.65f, 0.8f };
    [SerializeField] private Color[] rarityColors = { Color.white, new Color(0.4f, 1f, 0.5f), new Color(0.3f, 0.7f, 1f), new Color(0.85f, 0.4f, 1f), new Color(1f, 0.8f, 0.2f) };
    public bool AnimatePanel => animatePanel;
    public float EnterDuration => Mathf.Max(0.01f, enterDuration);
    public float ExitDuration => Mathf.Max(0.01f, exitDuration);
    public float EnterScale => enterScale;
    public float EnterOffset => enterOffset;
    public float EnterAngle => enterAngle;
    public bool AnimateButtons => animateButtons;
    public float ButtonDuration => Mathf.Max(0.01f, buttonDuration);
    public float HoverScale => hoverScale;
    public float PressedScale => pressedScale;
    public bool AnimateReward => animateReward;
    public float RewardPopDuration => Mathf.Max(0.01f, rewardPopDuration);
    public float Hold(SlotTier tier) => rewardHold != null && (int)tier < rewardHold.Length && (int)tier >= 0 ? Mathf.Max(0f, rewardHold[(int)tier]) : 0.35f;
    public Color RarityColor(SlotTier tier) => rarityColors != null && (int)tier < rarityColors.Length && (int)tier >= 0 ? rarityColors[(int)tier] : UnityEngine.Color.white;
}
