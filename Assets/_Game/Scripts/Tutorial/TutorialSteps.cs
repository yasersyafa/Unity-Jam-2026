using System.Collections.Generic;
using UnityEngine;

/// <summary>Sweep the crosshair over floating markers. Shooting is allowed so the crosshair moves, but nothing to hit yet.</summary>
public sealed class AimStep : TutorialStep
{
    private readonly List<TutorialTarget> markers = new();

    public override string Title => "AIM!";
    public override string Instruction => "Move the mouse and touch every target with your crosshair.";
    public override string Hint => "Slide the crosshair right onto the bullseye!";
    public override bool TeasesMisses => false;

    public override void Enter()
    {
        markers.Clear();
        foreach (var point in Director.Layout.AimMarkers)
        {
            var marker = Director.SpawnTarget(point, shootable: false);
            if (marker != null) markers.Add(marker);
        }
    }

    public override bool Tick()
    {
        bool remaining = false;
        foreach (var marker in markers)
        {
            if (marker == null || marker.IsCleared) continue;
            if (Director.IsAimOver(marker))
            {
                marker.Touch();
                Director.Progress("PING!");
            }
            else remaining = true;
        }
        return !remaining;
    }
}

/// <summary>Shoot three still targets, then two swinging ones.</summary>
public sealed class ShootStep : TutorialStep
{
    private int round;

    public override string Title => "SHOOT!";
    public override string Instruction => "Left click to shoot. Smash every target!";
    public override string Hint => "Aim, then left click. Moving targets: wait for them to cross your crosshair.";

    public override void Enter()
    {
        round = 0;
        foreach (var point in Director.Layout.StillTargets) Director.SpawnTarget(point, shootable: true);
    }

    public override bool Tick()
    {
        // A limited starting weapon may run dry here; the slot lesson comes next anyway.
        if (Director.Shooting != null && !Director.Shooting.HasAmmo) return true;
        if (Director.ActiveTargetCount > 0) return false;
        var layout = Director.Layout;
        if (round > 0 || layout.MovingTargets.Length == 0) return true;
        round++;
        Director.Coach("Too easy? Now they MOVE!");
        foreach (var point in layout.MovingTargets)
            Director.SpawnTarget(point, shootable: true, layout.MovingSwing, layout.MovingSwingSpeed);
        return false;
    }
}

/// <summary>Open the slot machine, spin it and take the weapon it gives.</summary>
public sealed class SlotStep : TutorialStep
{
    private int equippedAtStart;

    public override string Title => "SPIN!";
    public override string Instruction => "Press E to open the SLOT, then spin for a new weapon!";
    public override string Hint => "Press E! The slot only opens once its cooldown is over.";
    public override bool AllowSlot => true;
    public override bool TeasesMisses => false;

    public override void Enter() => equippedAtStart = Director.WeaponsEquipped;

    public override bool Tick()
    {
        if (Director.Slots == null) return true;
        return Director.WeaponsEquipped > equippedAtStart && !Director.Slots.IsOpen;
    }

    public override void Exit()
    {
        if (Director.Shooting != null && Director.Shooting.CurrentWeapon != null)
            Director.Coach($"{Director.Shooting.CurrentWeapon.name}! Ammo is limited. Run dry? Spin again.");
    }
}

/// <summary>Chain hits without missing until the style meter reaches rank B.</summary>
public sealed class StyleStep : TutorialStep
{
    private const int TargetRank = 1; // B
    private const int FallbackHits = 8;
    private int lane;
    private int hitsAtStart;
    private int lastRank = -1;
    private bool warnedAmmo;

    public override string Title => "COMBO!";
    public override string Instruction => "Keep hitting without missing until rank B!";
    public override string Hint => "Misses drain the gauge, and so does waiting. Keep firing!";
    public override bool AllowSlot => true;

    public override void Enter()
    {
        lane = 0;
        lastRank = Director.Style != null ? Director.Style.RankIndex : -1;
        hitsAtStart = Director.HitCount;
        warnedAmmo = false;
    }

    public override bool Tick()
    {
        var layout = Director.Layout;
        // Bounded so a failed spawn or an empty lane list can never spin forever.
        for (int i = Director.ActiveTargetCount; i < layout.ComboActiveTargets && layout.ComboLanes.Length > 0; i++)
        {
            Director.SpawnTarget(layout.ComboLanes[lane % layout.ComboLanes.Length], shootable: true, layout.ComboSwing, layout.ComboSwingSpeed);
            lane = (lane + 1) % layout.ComboLanes.Length;
        }
        if (Director.Shooting != null && !Director.Shooting.HasAmmo && !warnedAmmo)
        {
            warnedAmmo = true;
            Director.Coach("Out of ammo! Press E and spin again!");
        }
        if (Director.Style == null) return Director.HitCount - hitsAtStart >= FallbackHits;
        int rank = Director.Style.RankIndex;
        if (rank > lastRank && Director.Style.HasRank) Director.Progress(Director.Style.CurrentRank.label + "!");
        lastRank = rank;
        return rank >= TargetRank;
    }
}

/// <summary>A real enemy flies in and fires back; take it down.</summary>
public sealed class SurviveStep : TutorialStep
{
    private Enemy enemy;
    private bool defeated;
    private bool coachedDamage;

    public override string Title => "SURVIVE!";
    public override string Instruction => "A real enemy! Shoot it before it attacks you!";
    public override string Hint => "Aim at the robot and hold fire. Watch your HP in the corner!";
    public override bool AllowSlot => true;

    public override void Enter()
    {
        defeated = coachedDamage = false;
        enemy = Director.SpawnEnemy();
        if (enemy != null) enemy.Died += OnDied;
        Director.PlayerDamaged += OnPlayerDamaged;
    }

    public override bool Tick()
    {
        if (defeated || enemy == null) return true;
        // An enemy that reaches the player returns to its pool without dying.
        // Retry the lesson instead of waiting for a death event from an inactive enemy.
        if (!enemy.gameObject.activeInHierarchy)
        {
            enemy.Died -= OnDied;
            enemy = Director.SpawnEnemy();
            if (enemy != null) enemy.Died += OnDied;
            Director.Coach("Try again! Shoot the enemy before it reaches you!");
        }
        return false;
    }

    public override void Exit()
    {
        if (enemy != null) enemy.Died -= OnDied;
        Director.PlayerDamaged -= OnPlayerDamaged;
    }

    private void OnDied()
    {
        defeated = true;
        Director.Progress("KO!");
    }

    private void OnPlayerDamaged()
    {
        if (coachedDamage) return;
        coachedDamage = true;
        Director.Coach("OUCH! You lost HP. Keep shooting and watch your HP!");
    }
}

/// <summary>Final stamp before the real waves begin.</summary>
public sealed class FinishStep : TutorialStep
{
    private const float Duration = 1.6f;
    private float startedAt;

    public override string Title => "READY!";
    public override string Instruction => "You're ready. Showtime!";
    public override bool AllowSlot => true;
    public override bool TeasesMisses => false;

    public override void Enter()
    {
        startedAt = Time.unscaledTime;
        Director.Progress("READY!");
    }

    public override bool Tick() => Time.unscaledTime - startedAt >= Duration;
}
