using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Enemy))]
public sealed class EnemyHealthBar : MonoBehaviour
{
    [Header("Dimensions")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.75f, -0.05f);
    [SerializeField] private float barWidth = 1.1f;
    [SerializeField] private float barHeight = 0.16f;
    [SerializeField] private float borderPadding = 0.03f;

    [Header("Colors")]
    [SerializeField] private Color fullColor = new Color(0.15f, 0.95f, 0.35f, 1f); // Vibrant Green
    [SerializeField] private Color midColor = new Color(1f, 0.78f, 0.1f, 1f);      // Amber
    [SerializeField] private Color lowColor = new Color(0.95f, 0.15f, 0.15f, 1f);   // Red
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.88f); // Dark background

    private Enemy enemy;
    private Transform barRoot;
    private SpriteRenderer bgRenderer;
    private SpriteRenderer fillRenderer;
    private Transform fillTransform;
    private TextMeshPro tmpText;
    private float maxHealth = 1f;
    private Camera cam;
    private float displayedHealth = float.NaN, displayedMaximum = float.NaN;
    private int displayedHealthNumber = -1, displayedMaximumNumber = -1;

    private static Sprite centerPivotSprite;
    private static Sprite leftPivotSprite;

    private static void EnsureSprites()
    {
        if (centerPivotSprite == null)
            centerPivotSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        if (leftPivotSprite == null)
            leftPivotSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0f, 0.5f), 1f);
    }

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void OnEnable()
    {
        if (barRoot == null)
        {
            BuildVisuals();
        }
        if (barRoot != null) barRoot.gameObject.SetActive(enemy != null && !enemy.IsDead);
        displayedHealth = float.NaN;
        UpdateDisplay();
    }

    private void BuildVisuals()
    {
        EnsureSprites();

        var enemySr = GetComponentInChildren<SpriteRenderer>();
        int sortingLayer = enemySr != null ? enemySr.sortingLayerID : 0;
        int baseOrder = enemySr != null ? enemySr.sortingOrder : 0;
        Material mat = enemySr != null ? enemySr.sharedMaterial : null;

        var col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            offset.y = Mathf.Max(offset.y, col.offset.y + (col.size.y * 0.5f) + 0.22f);
        }

        var rootGo = new GameObject("HealthBarVisuals");
        rootGo.transform.SetParent(transform, false);
        rootGo.transform.localPosition = offset;
        barRoot = rootGo.transform;

        // Background
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(barRoot, false);
        bgGo.transform.localPosition = Vector3.zero;
        bgGo.transform.localScale = new Vector3(barWidth, barHeight, 1f);
        bgRenderer = bgGo.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = centerPivotSprite;
        bgRenderer.color = backgroundColor;
        bgRenderer.sortingLayerID = sortingLayer;
        bgRenderer.sortingOrder = baseOrder + 50;
        if (mat != null) bgRenderer.sharedMaterial = mat;

        // Fill
        float innerWidth = barWidth - (borderPadding * 2f);
        float innerHeight = barHeight - (borderPadding * 2f);
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(barRoot, false);
        fillGo.transform.localPosition = new Vector3(-innerWidth * 0.5f, 0f, -0.01f);
        fillTransform = fillGo.transform;
        fillTransform.localScale = new Vector3(innerWidth, innerHeight, 1f);
        fillRenderer = fillGo.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = leftPivotSprite;
        fillRenderer.color = fullColor;
        fillRenderer.sortingLayerID = sortingLayer;
        fillRenderer.sortingOrder = baseOrder + 51;
        if (mat != null) fillRenderer.sharedMaterial = mat;

        // HP Text
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(barRoot, false);
        textGo.transform.localPosition = new Vector3(0f, 0.16f, -0.02f);
        tmpText = textGo.AddComponent<TextMeshPro>();
        tmpText.fontSize = 2.2f;
        tmpText.fontStyle = FontStyles.Bold;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;
        tmpText.sortingLayerID = sortingLayer;
        tmpText.sortingOrder = baseOrder + 52;
        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null) tmpText.font = font;
    }

    private void LateUpdate()
    {
        if (enemy == null || enemy.IsDead)
        {
            if (barRoot != null && barRoot.gameObject.activeSelf)
                barRoot.gameObject.SetActive(false);
            return;
        }

        if (cam == null) cam = Camera.main;
        if (cam != null && barRoot != null)
        {
            barRoot.rotation = cam.transform.rotation;
        }

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (enemy == null || fillRenderer == null || enemy.Data == null) return;

        maxHealth = Mathf.Max(1f, enemy.Data.HealthPoint);
        float current = Mathf.Max(0f, enemy.CurrentHealth);
        if (displayedHealth == current && displayedMaximum == maxHealth) return;
        displayedHealth = current;
        displayedMaximum = maxHealth;
        float ratio = Mathf.Clamp01(current / maxHealth);

        float innerWidth = barWidth - (borderPadding * 2f);
        float innerHeight = barHeight - (borderPadding * 2f);
        if (fillTransform != null)
            fillTransform.localScale = new Vector3(innerWidth * ratio, innerHeight, 1f);

        fillRenderer.color = ratio > 0.5f
            ? Color.Lerp(midColor, fullColor, (ratio - 0.5f) * 2f)
            : Color.Lerp(lowColor, midColor, ratio * 2f);

        if (tmpText != null)
        {
            int healthNumber = Mathf.CeilToInt(current), maximumNumber = Mathf.CeilToInt(maxHealth);
            if (healthNumber != displayedHealthNumber || maximumNumber != displayedMaximumNumber)
            {
                tmpText.text = $"{healthNumber}/{maximumNumber}";
                displayedHealthNumber = healthNumber;
                displayedMaximumNumber = maximumNumber;
            }
        }
    }

}
