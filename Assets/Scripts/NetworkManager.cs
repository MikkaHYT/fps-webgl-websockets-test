using UnityEngine;
using NativeWebSocket;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour
{
    public WebSocket websocket;
    public GameObject playerPrefab;
    public Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();
    public string playerName = "Player"; // Assign a unique name for this client

    async void Start()
    {
        Debug.Log("Initializing WebSocket connection...");
        websocket = new WebSocket("wss://ws.814850.xyz");

        websocket.OnOpen += () =>
        {
            Debug.Log("WebSocket connection opened.");
            // Send spawn command to the server
            string playerId = websocket.GetHashCode().ToString(); // Generate a unique ID for this client
            string spawnMessage = $"spawn|{playerId}|{playerName}|{transform.position.x}|{transform.position.y}|{transform.position.z}|{transform.rotation.eulerAngles.x}|{transform.rotation.eulerAngles.y}|{transform.rotation.eulerAngles.z}";
            SendMessage(spawnMessage);
        };

        websocket.OnError += (error) =>
        {
            Debug.LogError($"WebSocket error: {error}");
        };

        websocket.OnClose += (closeCode) =>
        {
            Debug.LogWarning($"WebSocket connection closed with code: {closeCode}");
        };

        websocket.OnMessage += (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log($"Received message: {message}");
            HandleMessage(message);
        };

        try
        {
            await websocket.Connect();
            Debug.Log("WebSocket connection established.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to connect WebSocket: {ex.Message}");
        }
    }

    void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
        websocket.DispatchMessageQueue();
        #endif
    }

    public async void SendMessage(string message)
    {
        if (websocket.State == WebSocketState.Open)
        {
            Debug.Log($"Sending message: {message}");
            await websocket.SendText(message);
        }
        else
        {
            Debug.LogWarning("WebSocket is not open. Cannot send message.");
        }
    }

    private void HandleMessage(string message)
    {
        Debug.Log($"Handling message: {message}");
        string[] parts = message.Split('|');
        if (parts.Length < 2)
        {
            Debug.LogError($"Invalid message format: {message}");
            return;
        }

        string messageType = parts[0];

        if (messageType == "spawn")
        {
            if (parts.Length < 9)
            {
                Debug.LogError($"Invalid spawn message format: {message}");
                return;
            }

            string playerId = parts[1];
            string playerName = parts[2];
            Vector3 position = new Vector3(
                float.Parse(parts[3]),
                float.Parse(parts[4]),
                float.Parse(parts[5])
            );
            Quaternion rotation = Quaternion.Euler(
                float.Parse(parts[6]),
                float.Parse(parts[7]),
                float.Parse(parts[8])
            );

            if (!players.ContainsKey(playerId))
            {
                Debug.Log($"Spawning new player: {playerName} (ID: {playerId})");
                GameObject newPlayer = Instantiate(playerPrefab, position, rotation);
                newPlayer.name = playerName; // Assign the player's name
                players[playerId] = newPlayer;
            }
            else
            {
                Debug.LogWarning($"Player with ID {playerId} already exists.");
            }
        }
        else if (messageType == "update")
        {
            if (parts.Length < 8)
            {
                Debug.LogError($"Invalid update message format: {message}");
                // Replace the z rotation value with 0
                message = $"update|{websocket.GetHashCode()}|{parts[2]}|{parts[3]}|{parts[4]}|{parts[5]}|{parts[6]}|0";
                Debug.Log($"Modified message: {message}");
                return;
            }

            string playerId = parts[1];
            Vector3 position = new Vector3(
                float.Parse(parts[2]),
                float.Parse(parts[3]),
                float.Parse(parts[4])
            );
            Quaternion rotation = Quaternion.Euler(
                float.Parse(parts[5]),
                float.Parse(parts[6]),
                float.Parse(parts[7])
            );

            if (players.ContainsKey(playerId))
            {
                GameObject player = players[playerId];
                player.transform.position = position;
                player.transform.rotation = rotation;
            }
            else
            {
                Debug.LogWarning($"Player ID {playerId} not found for update.");
            }
        }
        else if (messageType == "players")
        {
            if (parts.Length < 2)
            {
                Debug.LogError($"Invalid players message format: {message}");
                return;
            }

            int playerCount = int.Parse(parts[1]);
            if (parts.Length < 2 + playerCount * 8)
            {
                Debug.LogError($"Invalid players message format: {message}");
                return;
            }

            for (int i = 0; i < playerCount; i++)
            {
                string playerId = parts[2 + i * 8];
                string playerName = parts[3 + i * 8];
                Vector3 position = new Vector3(
                    float.Parse(parts[4 + i * 8]),
                    float.Parse(parts[5 + i * 8]),
                    float.Parse(parts[6 + i * 8])
                );
                Quaternion rotation = Quaternion.Euler(
                    float.Parse(parts[7 + i * 8]),
                    float.Parse(parts[8 + i * 8]),
                    float.Parse(parts[9 + i * 8])
                );

                if (!players.ContainsKey(playerId))
                {
                    Debug.Log($"Spawning existing player: {playerName} (ID: {playerId})");
                    GameObject newPlayer = Instantiate(playerPrefab, position, rotation);
                    newPlayer.name = playerName;
                    players[playerId] = newPlayer;
                }
            }
        }
        else
        {
            Debug.LogWarning($"Unknown message type: {messageType}");
        }
    }

    private async void OnApplicationQuit()
    {
        Debug.Log("Closing WebSocket connection...");
        await websocket.Close();
        Debug.Log("WebSocket connection closed.");
    }
}