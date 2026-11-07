using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BossEncounter : MonoBehaviour, IDamageable
{
    private BossConfig config;
    private Camera cameraReference;
    private Transform planeReference;
    private PlayerHealth player;
    private ShootingController shooting;
    private SlotGameplayFlow slots;
    private readonly List<BossAttackTarget> targets = new List<BossAttackTarget>();
    private readonly List<Vector2> offsets = new List<Vector2>();
    private float health;
    private bool running;
    private SpriteRenderer visual;
    private BoxCollider2D hitbox;
    private GameObject hud;
    private UnityEngine.UI.Image fill;
    private Sprite placeholder;

    public void BeginEncounter(BossConfig data, Camera camera, Transform plane, PlayerHealth target, ShootingController gun, SlotGameplayFlow slotFlow)
    {
        if (running || config != null || data == null || target.IsDead) return;
        config = data; cameraReference = camera; planeReference = plane; player = target;
        shooting = gun; slots = slotFlow; health = config.Health; running = true;
        placeholder = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), Vector2.one * 0.5f, Texture2D.whiteTexture.width);
        visual = gameObject.AddComponent<SpriteRenderer>();
        visual.sprite = config.Sprite != null ? config.Sprite : placeholder;
        visual.color = new Color(0.7f, 0.2f, 0.5f);
        transform.position = Point(new Vector2(0.5f, 0.78f)) + camera.transform.forward * 0.5f;
        transform.rotation = plane.rotation;
        transform.localScale = Vector3.one * 2f;
        hitbox = gameObject.AddComponent<BoxCollider2D>();
        hitbox.size = visual.sprite.bounds.size;
        CreateHud();
        player.Died += PlayerDied;
        StartCoroutine(AttackLoop());
    }
    private Vector3 Point(Vector2 viewport)
    {
        var plane = new Plane(planeReference.forward, planeReference.position);
        var ray = cameraReference.ViewportPointToRay(viewport);
        return plane.Raycast(ray, out float distance) ? ray.GetPoint(distance) : planeReference.position;
    }
    private IEnumerator AttackLoop()
    {
        yield return new WaitForSeconds(config.InitialDelay);
        int pattern = 0;
        while (running)
        {
            bool second = health <= config.Health * 0.5f;
            int count = pattern == 1 ? (second ? 9 : 6) : pattern == 2 ? (second ? 8 : 4) : (second ? 8 : 6);
            for (int i = 0; i < count; i++)
            {
                Vector2 offset;
                if (pattern == 1)
                {
                    int columns = second ? 3 : 2;
                    offset = new Vector2(Mathf.Lerp(-0.3f, 0.3f, (i % columns) / (float)(columns - 1)), (i / columns - 1) * 0.2f);
                }
                else
                {
                    float angle = i * Mathf.PI * 2f / count;
                    offset = new Vector2(Mathf.Cos(angle) * 0.3f, Mathf.Sin(angle) * 0.2f);
                    if (pattern == 2) offset /= Mathf.Abs(Mathf.Cos(angle)) + Mathf.Abs(Mathf.Sin(angle));
                }
                offsets.Add(offset);
                var obj = new GameObject("Boss Attack");
                obj.transform.rotation = planeReference.rotation;
                var renderer = obj.AddComponent<SpriteRenderer>();
                renderer.sprite = placeholder; renderer.color = new Color(1f, 0.55f, 0.1f); renderer.sortingOrder = 10;
                obj.AddComponent<BoxCollider2D>();
                targets.Add(obj.AddComponent<BossAttackTarget>());
            }
            float elapsed = 0f;
            while (running && elapsed < config.Duration(second) && targets.Exists(t => t != null && t.isActiveAndEnabled))
            {
                float progress = Mathf.Clamp01(elapsed / config.Duration(second));
                for (int i = 0; i < targets.Count; i++)
                    if (targets[i] != null && targets[i].isActiveAndEnabled)
                    {
                        targets[i].transform.position = Point(new Vector2(0.5f, 0.55f) + offsets[i] * Mathf.Lerp(0.3f, 1f, progress)) - cameraReference.transform.forward * 0.1f;
                        targets[i].transform.localScale = Vector3.one * Mathf.Lerp(0.16f, 0.48f, progress);
                    }
                yield return null;
                elapsed += Time.deltaTime;
            }
            bool failed = targets.Exists(t => t != null && t.isActiveAndEnabled);
            CleanupTargets();
            if (running && failed) player.TakeDamage(config.Damage(second));
            if (!running) yield break;
            yield return new WaitForSeconds(config.Delay(second));
            pattern = (pattern + 1) % 3;
        }
    }
    public void TakeDamage(float damage)
    {
        if (!running || !(damage > 0f) || Time.timeScale <= 0f) return;
        health = Mathf.Max(0f, health - damage);
        fill.rectTransform.anchorMax = new Vector2(health / config.Health, 1f);
        if (health > 0f) return;
        running = false; StopAllCoroutines(); CleanupTargets(); hitbox.enabled = false; visual.enabled = false;
        if (slots != null) slots.enabled = false;
        if (shooting != null) shooting.SetShootingAllowed(false);
        var panel = Panel("Victory", hud.transform, new Vector2(0.25f, 0.3f), new Vector2(0.75f, 0.7f), new Color(0.05f, 0.05f, 0.1f, 0.98f));
        Label("Menang!", panel.transform, new Vector2(0f, 0.5f), Vector2.one);
        var button = Panel("Restart", panel.transform, new Vector2(0.2f, 0.1f), new Vector2(0.8f, 0.4f), new Color(0.2f, 0.4f, 0.6f));
        button.AddComponent<UnityEngine.UI.Button>().onClick.AddListener(() => { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); });
        Label("Restart", button.transform, Vector2.zero, Vector2.one);
    }
    private void CreateHud()
    {
        hud = new GameObject("Boss HUD", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        var canvas = hud.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        var back = Panel("Boss HP", hud.transform, new Vector2(0.25f, 0.92f), new Vector2(0.75f, 0.97f), Color.black);
        fill = Panel("Health", back.transform, Vector2.zero, Vector2.one, Color.red).GetComponent<UnityEngine.UI.Image>();
        Label("BOSS", back.transform, Vector2.zero, Vector2.one);
    }
    private static GameObject Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)); obj.transform.SetParent(parent, false);
        var rect = (RectTransform)obj.transform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        obj.GetComponent<UnityEngine.UI.Image>().color = color; obj.GetComponent<UnityEngine.UI.Image>().raycastTarget = name == "Restart"; return obj;
    }
    private static void Label(string value, Transform parent, Vector2 min, Vector2 max)
    {
        var obj = new GameObject(value, typeof(RectTransform), typeof(TextMeshProUGUI)); obj.transform.SetParent(parent, false);
        var rect = (RectTransform)obj.transform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var text = obj.GetComponent<TextMeshProUGUI>(); text.text = value; text.fontSize = 28f; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
    }
    private void CleanupTargets() { foreach (var target in targets) if (target != null) { target.gameObject.SetActive(false); Destroy(target.gameObject); } targets.Clear(); offsets.Clear(); }
    private void PlayerDied() { running = false; StopAllCoroutines(); CleanupTargets(); if (hitbox != null) hitbox.enabled = false; }
    private void OnDisable() { PlayerDied(); if (player != null) player.Died -= PlayerDied; if (hud != null) Destroy(hud); }
    private void OnDestroy() { if (placeholder != null) Destroy(placeholder); }
}
