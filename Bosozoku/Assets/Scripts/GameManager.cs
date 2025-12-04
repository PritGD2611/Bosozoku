using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public Health playerHealth;
    public UIHealthController uiController;

    void Awake()
    {
        // Bind player to UI on load
        if (uiController != null && playerHealth != null)
        {
            uiController.BindPlayer(playerHealth);
        }
    }

    void Start()
    {
        // Ensure binding if Awake order differed
        if (uiController != null && playerHealth != null)
        {
            uiController.BindPlayer(playerHealth);
        }
    }

    // Register/set the current target enemy on the UI
    public void RegisterEnemy(Health enemy)
    {
        if (uiController != null)
        {
            uiController.BindEnemy(enemy);
        }
    }

    // Clear the enemy UI binding
    public void UnregisterEnemy()
    {
        if (uiController != null)
        {
            uiController.BindEnemy(null);
        }
    }

    // Convenience per request
    public void SetTargetEnemy(Health enemy)
    {
        if (uiController != null)
        {
            uiController.BindEnemy(enemy);
        }
    }
}
