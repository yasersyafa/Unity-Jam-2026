using UnityEngine;

/// <summary>Waits until the scene transition has fully revealed the scene, then a short beat.</summary>
public sealed class IntroState : GameState
{
    private float elapsed;

    public override void Enter()
    {
        elapsed = 0f;
        Manager.SetPlayerControl(shooting: false, manualSlot: false);
    }

    public override void Tick()
    {
        if (SceneTransition.IsTransitioning) return;
        elapsed += Time.unscaledDeltaTime;
        if (elapsed < Manager.IntroDelay) return;
        if (Manager.Tutorial != null && !TutorialDirector.Completed) Manager.ChangeState<TutorialState>();
        else Manager.ChangeState<WeaponSelectState>();
    }
}

/// <summary>First-play tutorial. Its slot lesson hands out the first weapon, so it skips weapon select when finished.</summary>
public sealed class TutorialState : GameState
{
    public override void Enter() => Manager.Tutorial.Begin(Manager);

    public override void Tick()
    {
        var tutorial = Manager.Tutorial;
        tutorial.Tick();
        if (!tutorial.IsComplete) return;
        if (tutorial.WeaponGranted) Manager.ChangeState<CombatState>();
        else Manager.ChangeState<WeaponSelectState>();
    }

    public override void Exit() => Manager.Tutorial.Stop();
}

/// <summary>Opens the slot machine for the first weapon and waits for it to close.</summary>
public sealed class WeaponSelectState : GameState
{
    public override void Enter()
    {
        if (Manager.Slots != null) Manager.Slots.Open();
    }

    public override void Tick()
    {
        if (Manager.Slots == null || !Manager.Slots.IsOpen) Manager.ChangeState<CombatState>();
    }
}

/// <summary>Regular waves. The slot can be reopened by the player once its cooldown ends.</summary>
public sealed class CombatState : GameState
{
    public override void Enter()
    {
        Manager.SetPlayerControl(shooting: true, manualSlot: true);
        if (Manager.Spawner != null) Manager.Spawner.Begin();
    }
}

/// <summary>Boss fight; the manager moves to victory when the boss is defeated.</summary>
public sealed class BossState : GameState
{
    public override void Enter() => Manager.SetPlayerControl(true, true);
}

public sealed class BossIntroState : GameState
{
    private bool started;
    public override void Enter() { started = false; Manager.SetPlayerControl(false, false); }
    public override void Tick()
    {
        Manager.SetPlayerControl(false, false);
        if (started || (Manager.Slots != null && Manager.Slots.IsOpen)) return;
        started = true;
        Manager.BossIntro.Play(() =>
        {
            if (Manager.isActiveAndEnabled && Manager.Current == this)
                Manager.Spawner.SpawnBoss();
        });
    }
    public override void Exit() => Manager.BossIntro.CancelAndReset();
}

public sealed class VictoryState : GameState
{
    public override bool IsTerminal => true;
    public override void Enter() => Manager.SetPlayerControl(shooting: false, manualSlot: false);
}

public sealed class GameOverState : GameState
{
    public override bool IsTerminal => true;
    public override void Enter() => Manager.SetPlayerControl(shooting: false, manualSlot: false);
}
