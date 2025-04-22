using UnityEngine;
using TMPro; // Add this for TextMeshPro support
using UnityEngine.UI; // Required for UI components
using NativeWebSocket;
using System.Collections.Generic;
using UnityEngine.SceneManagement; // For scene management

public class NetworkManager : MonoBehaviour
{
    public WebSocket websocket;
    public GameObject playerPrefab;
    public Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();
    public string playerName = "Player"; // Default player name

    // UI References for the "Joining" scene
    public TMP_InputField usernameInputField; // Drag the UsernameInput field here in the Inspector
    public Transform playerListContent; // Reference to the Scroll View content
    public GameObject playerEntryPrefab; // Reference to the player entry prefab

    // UI References for the "Game" scene
    public GameObject pauseMenu; // Drag the Canvas or Pause Menu GameObject here

    private bool isInGameScene = false;

    async void Start()
    {
        DontDestroyOnLoad(gameObject);
        Debug.Log("Initializing WebSocket connection...");
        websocket = new WebSocket("wss://ws.814850.xyz");

        websocket.OnOpen += () =>
        {
            Debug.Log("WebSocket connection opened.");
            if (isInGameScene)
            {
                JoinGame();
            }
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

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isInGameScene = scene.name == "Game";

        if (isInGameScene)
        {
            JoinGame();
        }
        else if (scene.name == "Joining")
        {
            PopulatePlayerList();
        }
    }

    private void JoinGame()
    {
        string playerId = websocket.GetHashCode().ToString();
        string spawnMessage = $"spawn|{playerId}|{playerName}|0|0|0|0|0|0";
        SendMessage(spawnMessage);

        // Assign the playerId to the local player
        GameObject localPlayer = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        Player localPlayerScript = localPlayer.GetComponent<Player>();
        if (localPlayerScript != null)
        {
            localPlayerScript.Initialize(playerId);
        }
    }

    private void PopulatePlayerList()
    {
        // Clear the existing player list UI
        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        // Request the list of players from the server
        SendMessage("request_players");
    }

    public void OnJoinButtonClicked()
    {
        string username = usernameInputField.text.Trim();

        if (string.IsNullOrEmpty(username))
        {
            Debug.LogWarning("Username cannot be empty.");
            return;
        }

        playerName = username; // Update the player's name
        Debug.Log($"Username set to: {playerName}");

        // Load the Game scene
        SceneManager.LoadScene("Game");
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

        if (messageType == "players")
        {
            UpdatePlayerList(parts);
        }
        else if (messageType == "spawn")
        {
            HandleSpawnMessage(parts);
        }
        else if (messageType == "update")
        {
            HandleUpdateMessage(parts);
        }
        else if (messageType == "disconnect")
        {
            HandleDisconnectMessage(parts);
        }
        else
        {
            Debug.LogWarning($"Unknown message type: {messageType}");
        }
    }

    private void UpdatePlayerList(string[] parts)
    {
        if (int.TryParse(parts[1], out int playerCount))
        {
            for (int i = 0; i < playerCount; i++)
            {
                string playerName = parts[2 + i * 2];
                string playerData = parts[3 + i * 2]; // Example: health or score

                // Create a new player entry in the UI
                GameObject playerEntry = Instantiate(playerEntryPrefab, playerListContent);
                TMP_Text playerText = playerEntry.GetComponentInChildren<TMP_Text>();
                if (playerText != null)
                {
                    playerText.text = $"{playerName} - {playerData}";
                }
            }
        }
        else
        {
            Debug.LogError("Failed to parse player count.");
        }
    }

    private void HandleSpawnMessage(string[] parts)
    {
        if (parts.Length < 9)
        {
            Debug.LogError($"Invalid spawn message format: {message}");
            return;
        }

        string playerId = parts[1];
        string playerName = parts[2];
        if (float.TryParse(parts[3], out float posX) &&
            float.TryParse(parts[4], out float posY) &&
            float.TryParse(parts[5], out float posZ) &&
            float.TryParse(parts[6], out float rotX) &&
            float.TryParse(parts[7], out float rotY) &&
            float.TryParse(parts[8], out float rotZ))
        {
            Vector3 position = new Vector3(posX, posY, posZ);
            Quaternion rotation = Quaternion.Euler(rotX, rotY, rotZ);

            if (!players.ContainsKey(playerId))
            {
                Debug.Log($"Spawning new player: {playerName} (ID: {playerId})");
                GameObject newPlayer = Instantiate(playerPrefab, position, rotation);
                newPlayer.name = playerName; // Assign the player's name

                // Assign the playerId to the Player script
                Player playerScript = newPlayer.GetComponent<Player>();
                if (playerScript != null)
                {
                    playerScript.Initialize(playerId);
                }

                players[playerId] = newPlayer;
            }
            else
            {
                Debug.LogWarning($"Player with ID {playerId} already exists.");
            }
        }
        else
        {
            Debug.LogError($"Invalid numeric values in spawn message: {message}");
        }
    }

    private void HandleUpdateMessage(string[] parts)
    {
        if (parts.Length < 8)
        {
            Debug.LogError($"Invalid update message format: {message}");
            return;
        }

        string playerId = parts[1];
        if (float.TryParse(parts[2], out float posX) &&
            float.TryParse(parts[3], out float posY) &&
            float.TryParse(parts[4], out float posZ) &&
            float.TryParse(parts[5], out float rotX) &&
            float.TryParse(parts[6], out float rotY) &&
            float.TryParse(parts[7], out float rotZ))
        {
            Vector3 position = new Vector3(posX, posY, posZ);
            Quaternion rotation = Quaternion.Euler(rotX, rotY, rotZ);

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
        else
        {
            Debug.LogError($"Failed to parse numeric values in update message.");
        }
    }

    private void HandleDisconnectMessage(string[] parts)
    {
        if (parts.Length < 2)
        {
            Debug.LogError($"Invalid disconnect message format.");
            return;
        }

        string playerId = parts[1];
        if (players.ContainsKey(playerId))
        {
            Debug.Log($"Removing player with ID: {playerId}");
            Destroy(players[playerId]);
            players.Remove(playerId);
        }
        else
        {
            Debug.LogWarning($"Player ID {playerId} not found for disconnect.");
        }
    }

    private async void OnApplicationQuit()
    {
        Debug.Log("Closing WebSocket connection...");
        await websocket.Close();
        Debug.Log("WebSocket connection closed.");
    }
}