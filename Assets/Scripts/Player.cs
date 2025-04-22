using UnityEngine;
using TMPro;

public class Player : MonoBehaviour
{
    public string playerId; // Unique ID for the player
    public TMP_Text usernameText; // Reference to the TextMeshPro component for the username
    public TMP_Text healthText; // Reference to the TextMeshPro component for health
    public int maxHealth = 100; // Maximum health
    public int currentHealth; // Current health

    private void Start()
    {
        currentHealth = maxHealth; // Initialize health
    }

    public void Initialize(string id)
    {
        playerId = id;
        Debug.Log($"Player initialized with ID: {playerId}");
    }

    public void UpdateUsername(string newUsername)
    {
        if (usernameText != null)
        {
            usernameText.text = newUsername; // Update the displayed username
            Debug.Log($"Updated username for player {playerId} to {newUsername}");
        }
        else
        {
            Debug.LogWarning($"UsernameText is not assigned for player {playerId}");
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth); // Ensure health doesn't go below 0
        Debug.Log($"Player {playerId} took {damage} damage. Current health: {currentHealth}");

        if (healthText != null)
        {
            healthText.text = $"HP: {currentHealth}";
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"Player {playerId} has died.");
        // Add logic for player death (e.g., respawn or remove from the game)
    }
}