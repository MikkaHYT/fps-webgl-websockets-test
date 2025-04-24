using UnityEngine;

public class Bullet : MonoBehaviour
{
    public string ownerId; // The ID of the player who owns this bullet
    private NetworkManager networkManager;
    public int damage = 20; // Damage dealt by the bullet

    private void Start()
    {
        // Find the NetworkManager in the scene
        networkManager = FindFirstObjectByType<NetworkManager>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        try
        {
            // Get the Player component from the collided GameObject
            Player targetPlayer = collision.gameObject.GetComponent<Player>();
            // Check if the bullet hit a player
            if (collision.gameObject.CompareTag("Player"))
            {
                if (targetPlayer.playerId == ownerId)
                {
                    // Bullet hit the player who shot it, do nothing
                    return;
                }
                if (targetPlayer != null)
                {
                    string targetPlayerId = targetPlayer.playerId;
                    Debug.Log("Your ID: " + networkManager.playerId.ToString());
                    Debug.Log("Bullet hit: " + targetPlayerId);

                    if (ownerId == networkManager.playerId.ToString())
                    {
                        // Local player's bullet hit another player
                        Debug.Log($"[Local] Bullet from {ownerId} hit player {targetPlayerId}");
                        string hitMessage = $"hit|{ownerId}|{targetPlayerId}";
                        networkManager.SendMessage(hitMessage);
                    }
                    else if (targetPlayerId == networkManager.playerId.ToString())
                    {
                        // Another player's bullet hit the local player
                        Debug.Log($"[Local] Bullet from {ownerId} hit YOU (player {targetPlayerId})");
                        targetPlayer.TakeDamage(damage);

                        // Notify the original shooter
                        string hitMessage = $"hit|{ownerId}|{targetPlayerId}";
                        networkManager.SendMessage(hitMessage);
                    }
                    else
                    {
                        // Another player's bullet hit another player
                        Debug.Log($"[Network] Bullet from {ownerId} hit player {targetPlayerId}");
                    }
                }
                else
                {
                    // Assume the collided object is the local player with no player script, handle accordingly
                    Debug.LogWarning($"Bullet hit an object without a Player component: {collision.gameObject.name}, assuming it is the local player object.");
                }
            }
            Debug.Log("Bullet collided with: " + collision.gameObject.name);
            Debug.Log("Bullet owner ID: " + ownerId);
            Debug.Log("Target player ID: " + targetPlayer?.playerId);

            if (targetPlayer?.playerId != ownerId)
            {
                // Destroy the bullet on collision
                Destroy(gameObject);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error processing bullet collision: {ex.Message}");
            // Destroy the bullet even if an error occurs
            Destroy(gameObject);
        }
    }
}