using TMPro;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] float hitPoints = 100f;
    public TextMeshProUGUI healthText;
    public float currentHealth;


    public void TakeDamage(float damage)
    {

        currentHealth = (hitPoints -= damage);
        UpdateHealthUI();
        if (hitPoints <= 0)
        {
            GetComponent<DeathHandler>().HandleDeath();



        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = hitPoints;
        healthText.text = "HP:- " + currentHealth;



    }

    // Update is called once per frame
    void Update()
    {

    }

    void UpdateHealthUI()
    {
        if (healthText != null)
        {
            healthText.text = "HP:- " + (currentHealth);
        }
    }

    /*void UpdateHealthBeginningUI()
    {
        if (healthText != null)
        {
            healthText.text = "HP:- " + (currentHealth);
        }
    }*/



    public float GetCurrentHealth()
    {
        return currentHealth;
    }



}
