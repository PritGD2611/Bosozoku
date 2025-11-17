using TMPro;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    //public TextMeshProUGUI healthText;
    public float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
        //UpdateHealthUI();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
       // UpdateHealthUI();

        if (currentHealth <= 0f)
        {
            // Optional: clamp to 0
            currentHealth = 0f;
            GetComponent<DeathHandler>()?.HandleDeath();
        }
    }

   /* void UpdateHealthUI()
    {
        if (healthText != null)
            healthText.text = "HP:- " + Mathf.Max(0, currentHealth).ToString("0");
    }*/

    public float GetCurrentHealth()
    {
        return currentHealth;
    }
}
