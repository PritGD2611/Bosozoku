using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIHealthController : MonoBehaviour
{
    [Header("UI References")]
    public Slider playerHealthSlider;
    public Slider enemyHealthSlider;
    public TextMeshProUGUI enemyNameText;

    [Header("Billboarding / Camera Facing")]
    public bool enemyBarFacesCamera = true;
    public Camera targetCamera; // if null, uses Camera.main

    private Health _playerHealth;
    private Health _enemyHealth;

    void Awake()
    {
        // Ensure sliders start hidden or zeroed if not bound
        if (playerHealthSlider != null)
        {
            playerHealthSlider.minValue = 0f;
            playerHealthSlider.value = 0f;
        }
        if (enemyHealthSlider != null)
        {
            enemyHealthSlider.minValue = 0f;
            enemyHealthSlider.value = 0f;
        }
        if (enemyNameText != null)
        {
            enemyNameText.text = string.Empty;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    public void BindPlayer(Health playerHealth)
    {
        // Unsubscribe previous
        if (_playerHealth != null)
        {
            _playerHealth.onHurt.RemoveListener(UpdatePlayerUI);
            _playerHealth.onDead.RemoveListener(UpdatePlayerUI);
        }
        _playerHealth = playerHealth;
        if (_playerHealth == null || playerHealthSlider == null) return;

        playerHealthSlider.maxValue = _playerHealth.maxHealth;
        playerHealthSlider.value = _playerHealth.currentHealth;

        _playerHealth.onHurt.AddListener(UpdatePlayerUI);
        _playerHealth.onDead.AddListener(UpdatePlayerUI);
    }

    public void BindEnemy(Health enemyHealth)
    {
        // Unsubscribe previous
        if (_enemyHealth != null)
        {
            _enemyHealth.onHurt.RemoveListener(UpdateEnemyUI);
            _enemyHealth.onDead.RemoveListener(UpdateEnemyUI);
        }
        _enemyHealth = enemyHealth;
        if (_enemyHealth == null || enemyHealthSlider == null) return;

        enemyHealthSlider.maxValue = _enemyHealth.maxHealth;
        enemyHealthSlider.value = _enemyHealth.currentHealth;

        if (enemyNameText != null)
        {
            enemyNameText.text = _enemyHealth.name;
        }

        _enemyHealth.onHurt.AddListener(UpdateEnemyUI);
        _enemyHealth.onDead.AddListener(UpdateEnemyUI);
    }

    private void UpdatePlayerUI()
    {
        if (_playerHealth == null || playerHealthSlider == null) return;
        playerHealthSlider.maxValue = _playerHealth.maxHealth;
        playerHealthSlider.value = _playerHealth.currentHealth;
    }

    private void UpdateEnemyUI()
    {
        if (_enemyHealth == null || enemyHealthSlider == null) return;
        enemyHealthSlider.maxValue = _enemyHealth.maxHealth;
        enemyHealthSlider.value = _enemyHealth.currentHealth;

        if (enemyNameText != null)
        {
            enemyNameText.text = _enemyHealth.name;
        }
    }

    void LateUpdate()
    {
        // Optional polling to keep UI fresh even if events weren’t wired
        if (_playerHealth != null && playerHealthSlider != null)
        {
            if (playerHealthSlider.maxValue != _playerHealth.maxHealth)
                playerHealthSlider.maxValue = _playerHealth.maxHealth;
            if (!Mathf.Approximately(playerHealthSlider.value, _playerHealth.currentHealth))
                playerHealthSlider.value = _playerHealth.currentHealth;
        }
        if (_enemyHealth != null && enemyHealthSlider != null)
        {
            if (enemyHealthSlider.maxValue != _enemyHealth.maxHealth)
                enemyHealthSlider.maxValue = _enemyHealth.maxHealth;
            if (!Mathf.Approximately(enemyHealthSlider.value, _enemyHealth.currentHealth))
                enemyHealthSlider.value = _enemyHealth.currentHealth;
        }

        // Billboard enemy health bar to face camera
        if (enemyBarFacesCamera && enemyHealthSlider != null)
        {
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam != null)
            {
                Transform t = enemyHealthSlider.transform;
                // Face camera by aligning forward opposite of camera's direction to the bar
                Vector3 toCam = cam.transform.position - t.position;
                toCam.y = 0f; // keep upright
                if (toCam.sqrMagnitude > 0.0001f)
                {
                    Quaternion look = Quaternion.LookRotation(toCam);
                    t.rotation = look;
                }
            }
        }
    }
}
