using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class WeaponAudioSyncTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    private GameObject root;
    private AudioService service;
    private ShootingController shooting;
    private WeaponData weapon;
    private AudioCue cue;
    private AudioClip oneShotClip;
    private InputActionAsset actions;
    private InputActionReference reference;
    private float previousTimeScale;
    private bool previousRunInBackground;

    private static void Set(object target, string field, object value)
    {
        for (var type = target.GetType(); type != null; type = type.BaseType)
        {
            var info = type.GetField(field, Private);
            if (info != null) { info.SetValue(target, value); return; }
        }
        Assert.Fail("Field not found: " + field + " on " + target.GetType().Name);
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        previousTimeScale = Time.timeScale;
        previousRunInBackground = Application.runInBackground;
        Time.timeScale = 1f;
        Application.runInBackground = true;

        if (AudioService.Instance != null)
            Object.Destroy(AudioService.Instance.gameObject);
        yield return null;

        service = AudioService.Ensure();
        oneShotClip = AudioClip.Create("UziOneShotTest", 44100, 1, 44100, false);

        cue = ScriptableObject.CreateInstance<AudioCue>();
        cue.clips = new[] { oneShotClip };
        cue.volume = 0.85f;
        cue.pitch = new Vector2(0.96f, 1.04f);
        cue.cooldown = 0f;
        cue.maximumVoices = 20;
        cue.ui = false;

        weapon = ScriptableObject.CreateInstance<WeaponData>();
        Set(weapon, "firingMode", WeaponData.FireMode.Automatic);
        Set(weapon, "fireRate", 15f);
        Set(weapon, "unlimitedAmmo", true);
        Set(weapon, "enableRecoil", false);
        Set(weapon, "shotSound", cue);

        root = new GameObject("Uzi audio sync integration");
        root.SetActive(false);

        var cameraObject = new GameObject("Camera", typeof(Camera));
        cameraObject.transform.SetParent(root.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;

        var hud = new GameObject("HUD", typeof(RectTransform), typeof(Canvas));
        hud.transform.SetParent(root.transform, false);
        var canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var crosshair = new GameObject("Crosshair", typeof(RectTransform), typeof(Image));
        crosshair.transform.SetParent(hud.transform, false);
        var weaponImage = new GameObject("Weapon", typeof(RectTransform), typeof(Image));
        weaponImage.transform.SetParent(hud.transform, false);

        var feedback = root.AddComponent<WeaponFeedback>();
        feedback.Configure(canvas, (RectTransform)crosshair.transform, weaponImage.GetComponent<Image>(), camera.transform);

        actions = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = new InputActionMap("Player");
        actions.AddActionMap(map);
        InputAction action = map.AddAction("Attack", InputActionType.Button, "<Mouse>/leftButton");
        reference = InputActionReference.Create(action);

        shooting = root.AddComponent<ShootingController>();
        shooting.Configure(camera, weapon, feedback, reference);

        var combatAudio = root.AddComponent<CombatAudio>();
        Set(combatAudio, "shooting", shooting);

        root.SetActive(true);
        yield return null;
        shooting.enabled = false;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = previousTimeScale;
        Application.runInBackground = previousRunInBackground;
        if (root != null) Object.Destroy(root);
        if (service != null) Object.Destroy(service.gameObject);
        Object.Destroy(weapon);
        Object.Destroy(cue);
        Object.Destroy(oneShotClip);
        Object.Destroy(reference);
        Object.Destroy(actions);
        yield return null;
    }

    private AudioSource[] ActiveUziVoices()
        => service.GetComponents<AudioSource>().Where(s => !s.loop && s.isPlaying && s.clip == oneShotClip).ToArray();

    [UnityTest]
    public IEnumerator AutomaticShots_PlayOneShotSfxPerFireInterval_WithoutDroppingVoices()
    {
        MethodInfo fireShot = typeof(ShootingController).GetMethod("FireShot", Private);
        var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        int shotsFired = 0;
        shooting.ShotFired += () => shotsFired++;

        const int totalShots = 8;
        for (int i = 0; i < totalShots; i++)
        {
            double firedAt = Time.timeAsDouble;
            fireShot.Invoke(shooting, new object[] { center, true });
            Assert.That(shooting.NextShotTime - firedAt, Is.EqualTo(weapon.ShotInterval).Within(0.001));
            yield return new WaitForSecondsRealtime(weapon.ShotInterval);
        }

        Assert.That(shotsFired, Is.EqualTo(totalShots));
        Assert.That(ActiveUziVoices().Length, Is.EqualTo(totalShots));
    }

    [UnityTest]
    public IEnumerator HighRateAutomatic_25Hz_DoesNotExceedVoicePool()
    {
        var machineGunCue = ScriptableObject.CreateInstance<AudioCue>();
        machineGunCue.clips = new[] { oneShotClip };
        machineGunCue.volume = 0.45f;
        machineGunCue.pitch = new Vector2(0.96f, 1.04f);
        machineGunCue.cooldown = 0f;
        machineGunCue.maximumVoices = 30;
        machineGunCue.ui = false;

        var machineGun = ScriptableObject.CreateInstance<WeaponData>();
        Set(machineGun, "firingMode", WeaponData.FireMode.Automatic);
        Set(machineGun, "fireRate", 25f);
        Set(machineGun, "unlimitedAmmo", true);
        Set(machineGun, "enableRecoil", false);
        Set(machineGun, "shotSound", machineGunCue);

        try
        {
            shooting.EquipWeapon(machineGun);
            MethodInfo fireShot = typeof(ShootingController).GetMethod("FireShot", Private);
            var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            const int totalShots = 27;
            for (int i = 0; i < totalShots; i++)
            {
                fireShot.Invoke(shooting, new object[] { center, true });
                yield return new WaitForSecondsRealtime(0.01f);
            }

            Assert.That(ActiveUziVoices().Length, Is.EqualTo(totalShots));
        }
        finally
        {
            Object.Destroy(machineGun);
            Object.Destroy(machineGunCue);
        }
    }

    [UnityTest]
    public IEnumerator SemiAutomaticWeapon_PlaysSingleOneShotSfxPerTap()
    {
        var awpCue = ScriptableObject.CreateInstance<AudioCue>();
        awpCue.clips = new[] { oneShotClip };
        awpCue.volume = 0.95f;
        awpCue.pitch = Vector2.one;
        awpCue.cooldown = 0f;
        awpCue.maximumVoices = 4;
        awpCue.ui = false;

        var awp = ScriptableObject.CreateInstance<WeaponData>();
        Set(awp, "firingMode", WeaponData.FireMode.SemiAutomatic);
        Set(awp, "fireRate", 1.5f);
        Set(awp, "unlimitedAmmo", true);
        Set(awp, "enableRecoil", false);
        Set(awp, "shotSound", awpCue);

        try
        {
            shooting.EquipWeapon(awp);
            Assert.That(shooting.IsSemiAutomatic, Is.True);
            MethodInfo fireShot = typeof(ShootingController).GetMethod("FireShot", Private);
            var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            fireShot.Invoke(shooting, new object[] { center, false });
            yield return null;
            Assert.That(ActiveUziVoices().Length, Is.EqualTo(1));

            fireShot.Invoke(shooting, new object[] { center, false });
            yield return null;
            Assert.That(ActiveUziVoices().Length, Is.EqualTo(2));
        }
        finally
        {
            Object.Destroy(awp);
            Object.Destroy(awpCue);
        }
    }

    [UnityTest]
    public IEnumerator LoopCue_PlaysBuildupFromZero_ThenLoopsFromLoopStartOffset()
    {
        var loopClip = AudioClip.Create("FlamethrowerLoopTest", 22050, 1, 44100, false); // 0.5s
        var loopCue = ScriptableObject.CreateInstance<AudioCue>();
        loopCue.clips = new[] { loopClip };
        loopCue.volume = 1f;
        loopCue.pitch = Vector2.one;
        loopCue.loop = true;
        loopCue.loopStartSeconds = 0.2f;

        try
        {
            service.StartLoop(loopCue);
            var initialSources = service.GetComponents<AudioSource>().Where(s => !s.loop && s.isPlaying && s.clip == loopClip).ToArray();
            Assert.That(initialSources.Length, Is.EqualTo(1));
            var firstSource = initialSources[0];
            Assert.That(firstSource.time, Is.LessThan(0.15f));

            // Wait past the first 0.5s pass so AudioService completes crossfade onto the second source at 0.2s.
            yield return new WaitForSecondsRealtime(0.55f);

            var audibleSources = service.GetComponents<AudioSource>()
                .Where(s => !s.loop && s.isPlaying && s.clip == loopClip && s.volume > 0.5f)
                .ToArray();
            Assert.That(audibleSources.Length, Is.EqualTo(1));
            Assert.That(audibleSources[0], Is.Not.SameAs(firstSource));
            Assert.That(audibleSources[0].time, Is.GreaterThanOrEqualTo(0.19f));

            service.StopLoop(loopCue);
            yield return new WaitForSecondsRealtime(0.1f);
            var afterStop = service.GetComponents<AudioSource>().Where(s => !s.loop && s.isPlaying && s.clip == loopClip).ToArray();
            Assert.That(afterStop, Is.Empty);
        }
        finally
        {
            service.StopLoop(loopCue);
            Object.Destroy(loopCue);
            Object.Destroy(loopClip);
        }
    }

    [UnityTest]
    public IEnumerator FlamethrowerHold_PlaysSingleLoopInsteadOfStackingPerShot()
    {
        var flameCue = ScriptableObject.CreateInstance<AudioCue>();
        flameCue.clips = new[] { oneShotClip };
        flameCue.volume = 1f;
        flameCue.pitch = Vector2.one;
        flameCue.loop = true;
        flameCue.loopStartSeconds = 0.2f;

        var flame = ScriptableObject.CreateInstance<WeaponData>();
        Set(flame, "firingMode", WeaponData.FireMode.Automatic);
        Set(flame, "fireRate", 20f);
        Set(flame, "unlimitedAmmo", true);
        Set(flame, "enableRecoil", false);
        Set(flame, "shotSound", flameCue);

        try
        {
            shooting.EquipWeapon(flame);
            MethodInfo fireShot = typeof(ShootingController).GetMethod("FireShot", Private);
            var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            for (int i = 0; i < 6; i++)
            {
                fireShot.Invoke(shooting, new object[] { center, true });
                yield return new WaitForSecondsRealtime(0.05f);
            }

            Assert.That(ActiveUziVoices().Length, Is.EqualTo(1));

            shooting.SetShootingAllowed(false);
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(ActiveUziVoices(), Is.Empty);
        }
        finally
        {
            Object.Destroy(flame);
            Object.Destroy(flameCue);
        }
    }

    private (GameObject enemyGo, Enemy enemy) SpawnTestEnemy(
        Vector3 position,
        float health,
        AudioCue hitCue,
        EnemyData sharedData,
        AudioCue deathCue = null)
    {
        Set(sharedData, "healthPoint", health);
        var go = new GameObject("TestEnemy");
        go.SetActive(false);
        go.transform.position = position;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(2f, 2f);
        var enemy = go.AddComponent<Enemy>();
        Set(enemy, "data", sharedData);
        var audio = go.AddComponent<CombatAudio>();
        Set(audio, "enemy", enemy);
        Set(audio, "damaged", hitCue);
        Set(audio, "died", deathCue);
        go.SetActive(true);
        return (go, enemy);
    }

    [UnityTest]
    public IEnumerator NormalHit_PlaysGeneralEnemyHit_WhileFlamethrowerAndWaterGunSuppressIt()
    {
        var enemyHitClip = AudioClip.Create("EnemyHitTest", 44100, 1, 44100, false);
        var enemyHitCue = ScriptableObject.CreateInstance<AudioCue>();
        enemyHitCue.clips = new[] { enemyHitClip };
        enemyHitCue.volume = 0.7f;
        enemyHitCue.pitch = Vector2.one;
        enemyHitCue.cooldown = 0f;
        enemyHitCue.maximumVoices = 8;

        var enemyData = ScriptableObject.CreateInstance<EnemyData>();
        var (enemyGo, _) = SpawnTestEnemy(Vector3.zero, 500f, enemyHitCue, enemyData);
        var camera = root.GetComponentInChildren<Camera>();
        var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        var suppressedSingle = ScriptableObject.CreateInstance<SingleTargetDamage>();
        Set(suppressedSingle, "suppressEnemyHitSound", true);
        var flameCone = ScriptableObject.CreateInstance<ConeDamage>();
        Set(flameCone, "suppressEnemyHitSound", true);

        try
        {
            yield return null;
            var shot = new ShotContext(camera, center, 10f, ~0);

            Assert.That(suppressedSingle.Apply(shot), Is.True);
            Assert.That(flameCone.Apply(shot), Is.True);
            var suppressedVoices = service.GetComponents<AudioSource>().Where(s => !s.loop && s.isPlaying && s.clip == enemyHitClip).ToArray();
            Assert.That(suppressedVoices, Is.Empty);

            Assert.That(DamageType.Default.Apply(shot), Is.True);
            var generalVoices = service.GetComponents<AudioSource>().Where(s => !s.loop && s.isPlaying && s.clip == enemyHitClip).ToArray();
            Assert.That(generalVoices.Length, Is.EqualTo(1));
        }
        finally
        {
            Object.Destroy(enemyGo);
            Object.Destroy(enemyData);
            Object.Destroy(suppressedSingle);
            Object.Destroy(flameCone);
            Object.Destroy(enemyHitCue);
            Object.Destroy(enemyHitClip);
        }
    }

    [UnityTest]
    public IEnumerator ExplosionHit_PlaysWeaponExplosionOncePerImpact_AndSuppressesGeneralEnemyHit()
    {
        var enemyHitClip = AudioClip.Create("EnemyHitSuppressed", 44100, 1, 44100, false);
        var explosionClip = AudioClip.Create("ExplosionImpactTest", 44100, 1, 44100, false);

        var enemyHitCue = ScriptableObject.CreateInstance<AudioCue>();
        enemyHitCue.clips = new[] { enemyHitClip };
        enemyHitCue.cooldown = 0f;
        enemyHitCue.maximumVoices = 8;

        var explosionCue = ScriptableObject.CreateInstance<AudioCue>();
        explosionCue.clips = new[] { explosionClip };
        explosionCue.cooldown = 0f;
        explosionCue.maximumVoices = 6;

        var enemyData = ScriptableObject.CreateInstance<EnemyData>();
        var (enemy1, _) = SpawnTestEnemy(Vector3.zero, 500f, enemyHitCue, enemyData);
        var (enemy2, _) = SpawnTestEnemy(new Vector3(0.5f, 0f, 0f), 500f, enemyHitCue, enemyData);
        var camera = root.GetComponentInChildren<Camera>();
        var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        var explosion = ScriptableObject.CreateInstance<ExplosionDamage>();
        Set(explosion, "radius", 3f);
        Set(explosion, "impactSound", explosionCue);

        try
        {
            yield return null;
            var shot = new ShotContext(camera, center, 100f, ~0);
            Assert.That(explosion.ApplyImpact(shot, enemy1.GetComponent<Collider2D>(), Vector2.zero), Is.True);

            var hitVoices = service.GetComponents<AudioSource>().Where(s => !s.loop && s.isPlaying && s.clip == enemyHitClip).ToArray();
            var boomVoices = service.GetComponents<AudioSource>().Where(s => !s.loop && s.isPlaying && s.clip == explosionClip).ToArray();
            Assert.That(hitVoices, Is.Empty);
            Assert.That(boomVoices.Length, Is.EqualTo(1));
        }
        finally
        {
            Object.Destroy(enemy1);
            Object.Destroy(enemy2);
            Object.Destroy(enemyData);
            Object.Destroy(explosion);
            Object.Destroy(explosionCue);
            Object.Destroy(enemyHitCue);
            Object.Destroy(explosionClip);
            Object.Destroy(enemyHitClip);
        }
    }

    [UnityTest]
    public IEnumerator EnemyDeath_PlaysConfiguredDeathSfxCueOnSfxGroup()
    {
        var defaultDeathClip = AudioClip.Create("EnemyDeathDefaultTest", 44100, 1, 44100, false);
        var tankDeathClip = AudioClip.Create("EnemyDeathTankTest", 44100, 1, 44100, false);
        var defaultCue = ScriptableObject.CreateInstance<AudioCue>();
        defaultCue.clips = new[] { defaultDeathClip };
        defaultCue.cooldown = 0f;
        defaultCue.maximumVoices = 8;

        var tankCue = ScriptableObject.CreateInstance<AudioCue>();
        tankCue.clips = new[] { tankDeathClip };
        tankCue.cooldown = 0f;
        tankCue.maximumVoices = 8;

        var enemyData = ScriptableObject.CreateInstance<EnemyData>();
        var tankData = ScriptableObject.CreateInstance<EnemyData>();
        var (normalGo, normalEnemy) = SpawnTestEnemy(Vector3.zero, 50f, null, enemyData, defaultCue);
        var (tankGo, tankEnemy) = SpawnTestEnemy(new Vector3(2f, 0f, 0f), 100f, null, tankData, tankCue);

        try
        {
            yield return null;
            normalEnemy.TakeDamage(50f);
            tankEnemy.TakeDamage(100f);

            var active = service.GetComponents<AudioSource>().Where(s => !s.loop && s.isPlaying).ToArray();
            Assert.That(active.Any(s => s.clip == defaultDeathClip && s.outputAudioMixerGroup == service.sfxGroup), Is.True);
            Assert.That(active.Any(s => s.clip == tankDeathClip && s.outputAudioMixerGroup == service.sfxGroup), Is.True);
        }
        finally
        {
            Object.Destroy(normalGo);
            Object.Destroy(tankGo);
            Object.Destroy(enemyData);
            Object.Destroy(tankData);
            Object.Destroy(defaultCue);
            Object.Destroy(tankCue);
            Object.Destroy(defaultDeathClip);
            Object.Destroy(tankDeathClip);
        }
    }
}
