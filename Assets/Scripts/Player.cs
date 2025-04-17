using UnityEngine;

public class Player : MonoBehaviour
{
    public string playerId; // Unique ID for the player

    public void Initialize(string id)
    {
        playerId = id;
        Debug.Log($"Player initialized with ID: {playerId}");
    }
}