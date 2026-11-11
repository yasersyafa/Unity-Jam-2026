using UnityEngine;

public sealed class WeaponCutInTrigger : MonoBehaviour
{
    [SerializeField] private ShootingController shooting;
    [SerializeField] private CharacterCutIn cutIn;
    [SerializeField] private Sprite portrait;
    [SerializeField] private string quote = "Coba yang ini!";
    [SerializeField] private Color accent = new Color(1f, 0.85f, 0.05f);
    private void OnEnable() { if (shooting != null) shooting.WeaponEquipped += OnEquipped; }
    private void OnDisable() { if (shooting != null) shooting.WeaponEquipped -= OnEquipped; }
    private void OnEquipped(WeaponData weapon) { if (cutIn != null) cutIn.Show(portrait, quote, accent); }
}
