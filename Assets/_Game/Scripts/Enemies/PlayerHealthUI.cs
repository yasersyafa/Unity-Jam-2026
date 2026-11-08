using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;

    public void Configure(PlayerHealth health, Slider slider, TextMeshProUGUI text = null)
    {
        playerHealth = health;
        healthSlider = slider;
        healthText = text;
        UpdateUI();
    }

    private void Awake()
    {
        if (playerHealth == null)
            playerHealth = GetComponentInParent<PlayerHealth>() ?? FindAnyObjectByType<PlayerHealth>();
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged += UpdateUI;
            playerHealth.Died += UpdateUI;
        }
        UpdateUI();
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= UpdateUI;
            playerHealth.Died -= UpdateUI;
        }
    }

    private void Update()
    {
        if (playerHealth != null && healthSlider != null && !Mathf.Approximately(healthSlider.value, playerHealth.CurrentHealth))
        {
            UpdateUI();
        }
    }

    public void UpdateUI()
    {
        if (playerHealth == null) return;

        float max = Mathf.Max(1f, playerHealth.MaximumHealth);
        float current = Mathf.Clamp(playerHealth.CurrentHealth, 0f, max);

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = max;
            healthSlider.value = current;
        }

        if (healthText != null)
        {
            healthText.text = $"HP {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }
    }
}
