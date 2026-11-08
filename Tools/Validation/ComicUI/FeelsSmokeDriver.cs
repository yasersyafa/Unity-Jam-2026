using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Reflection;
public sealed class FeelsSmokeDriver : MonoBehaviour
{
    private int checks;
    private T Read<T>(object obj, string field) => (T)obj.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
    private void Check(bool value, string label) { if (!value) throw new Exception("FAIL: " + label); checks++; Debug.Log("FEELS PASS: " + label); }
    private void Start() { Application.runInBackground = true; StartCoroutine(Run()); }
    private IEnumerator Advance(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds) { yield return null; elapsed += Time.unscaledDeltaTime; }
        yield return null;
        yield return null;
    }
    private IEnumerator Run()
    {
        var flow = FindAnyObjectByType<SlotGameplayFlow>();
        var machine = FindAnyObjectByType<SlotMachineView>();
        var player = FindAnyObjectByType<PlayerHealth>();
        var health = FindAnyObjectByType<PlayerHealthUI>();
        var spawner = FindAnyObjectByType<EnemySpawner>(); spawner.enabled = false;
        yield return Advance(0.5f);
        Check(flow.IsOpen && Time.timeScale == 0f && !machine.IsTransitioning, $"slot enter completes while paused: open={flow.IsOpen}, time={Time.timeScale}, transition={machine.IsTransitioning}");
        var slider = Read<Slider>(health, "healthSlider");
        Check(Mathf.Approximately(slider.value, player.CurrentHealth), "initial HP synchronized");
        int rewards = 0; FindAnyObjectByType<SlotWeaponRewards>().WeaponAwarded += weapon => rewards++;
        machine.Spin(); machine.Spin(); machine.Skip();
        yield return Advance(2.5f);
        Check(rewards == 1 && !flow.IsOpen && Time.timeScale == 1f, "skip and duplicate click award once and resume after exit");
        var trail = Read<Image>(health, "damageTrail");
        player.TakeDamage(10f); yield return Advance(0.04f); player.TakeDamage(10f);
        yield return Advance(0.14f);
        Check(Mathf.Abs(slider.value - 80f) < 0.1f && trail.fillAmount > 0.8f, "rapid damage fills immediately and retains damage trail");
        yield return Advance(0.6f);
        Check(Mathf.Abs(trail.fillAmount - 0.8f) < 0.01f, "trail catches target");
        Check(trail.sprite != null && trail.type == Image.Type.Filled, "trail has renderable filled sprite");
        health.enabled = false; health.enabled = true;
        Check(Mathf.Approximately(slider.value, 80f), "HP re-enable synchronizes without stale tween");
        flow.ForceRoll(); flow.ForceRoll(); yield return Advance(0.5f);
        Check(machine.IsSpinning && flow.IsOpen, "auto-roll queued through enter");
        machine.Skip(); yield return Advance(2.5f);
        Check(rewards == 2 && !flow.IsOpen, "auto-roll grants exactly one reward");
        flow.Open(); yield return Advance(0.05f); flow.enabled = false;
        Check(Time.timeScale == 1f && !machine.IsTransitioning && !Read<GameObject>(machine, "slotCanvas").activeSelf, "disable during entry restores pause and hides immediately");
        flow.enabled = true; yield return Advance(0.4f);
        var button = Read<Button>(machine, "spinButton");
        var feedback = button.GetComponent<ComicButtonFeedback>();
        var visual = Read<RectTransform>(feedback, "visual");
        Vector3 rest = visual.localScale;
        feedback.OnPointerEnter(new PointerEventData(EventSystem.current)); yield return Advance(0.2f);
        Check(visual.localScale.x > rest.x, "button hover animates visual child");
        feedback.OnPointerExit(new PointerEventData(EventSystem.current)); yield return Advance(0.2f);
        Check(Vector3.Distance(visual.localScale, rest) < 0.001f, "button restores scale");
        var reward = Read<SlotRewardPresentation>(machine, "rewardPresentation");
        var weapons = Read<WeaponData[]>(FindAnyObjectByType<SlotWeaponRewards>(), "weapons");
        for (int tier = 0; tier < 5; tier++)
        {
            var weapon = Array.Find(weapons, w => (int)w.Tier == tier);
            reward.Present(weapon); yield return Advance(0.05f);
            Check(reward.IsPlaying, "rarity presentation " + tier);
            yield return Advance(1.1f);
            Check(!reward.IsPlaying, "rarity settles " + tier);
        }
        var durationFeedback = FindAnyObjectByType<WeaponDurationFeedback>();
        flow.GetType().GetField("weaponRemainingDuration", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(flow, 0.5f);
        yield return Advance(0.05f);
        Check(Read<bool>(durationFeedback, "critical"), "weapon timer critical accent");
        flow.GetType().GetField("weaponRemainingDuration", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(flow, 8f);
        yield return Advance(0.05f);
        Check(!Read<bool>(durationFeedback, "critical"), "new weapon timer clears accent");
        var banner = FindAnyObjectByType<WaveBannerPresentation>();
        banner.SendMessage("Wave", 3);
        yield return Advance(0.05f);
        Check(Read<CanvasGroup>(banner, "group").alpha == 0f, "wave banner defers while slot open");
        flow.enabled = false;
        yield return Advance(0.1f);
        Check(Read<CanvasGroup>(banner, "group").alpha > 0f, "wave banner appears after slot");
        yield return Advance(1f);
        Check(Read<CanvasGroup>(banner, "group").alpha == 0f, "wave banner clears");
        var weaponFeedback = FindAnyObjectByType<WeaponFeedback>();
        var equipRoot = Read<RectTransform>(weaponFeedback, "equipRoot");
        Check(Vector3.Distance(equipRoot.localScale, Vector3.one) < 0.001f, "equip scale returns to authored rest");
        weaponFeedback.PlayEquip(); yield return Advance(0.05f);
        weaponFeedback.enabled = false;
        Check(Vector3.Distance(equipRoot.localScale, Vector3.one) < 0.001f, "equip disable resets transform");
        weaponFeedback.enabled = true;
        player.TakeDamage(60f); yield return Advance(0.6f);
        Check(Read<bool>(health, "critical"), "critical HP accent active");
        player.TakeDamage(999f); yield return Advance(0.6f);
        Check(!Read<bool>(health, "critical") && slider.value == 0f && trail.fillAmount == 0f, "fatal damage settles and stops critical pulse");
        Debug.Log("FEELS SMOKE COMPLETE: " + checks + " checks; frame=" + Time.frameCount);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.Exit(0);
#endif
    }
}
