using UnityEngine;

public class Bullet : MonoBehaviour
{
    public string ownerId; // The ID of the player who owns this bullet
    private NetworkManager networkManager;

    private void Start()
    {
        // Find the NetworkManager in the scene
        networkManager = FindObjectOfType<NetworkManager>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if the bullet hit a player
        if (collision.gameObject.CompareTag("Player"))
        {
            // Get the Player component from the collided GameObject
            Player targetPlayer = collision.gameObject.GetComponent<Player>();
            if (targetPlayer != null)
            {
                string targetPlayerId = targetPlayer.playerId;

                if (ownerId == networkManager.websocket.GetHashCode().ToString())
                {
                    // Local player's bullet hit another player
                    Debug.Log($"[Local] Bullet from {ownerId} hit player {targetPlayerId}");
                }
                else if (targetPlayerId == networkManager.websocket.GetHashCode().ToString())
                {
                    // Another player's bullet hit the local player
                    Debug.Log($"[Local] Bullet from {ownerId} hit YOU (player {targetPlayerId})");

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
        }

        // Destroy the bullet on collision
        Destroy(gameObject);
    }
}