using UnityEngine;
using TMPro; // Add this for TextMeshPro support
using UnityEngine.UI; // Required for UI components
using NativeWebSocket;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour
{
    public WebSocket websocket;
    public GameObject playerPrefab;
    public Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();
    public string playerName = "Player"; // Default player name

    // UI References
    public TMP_InputField usernameInputField; // Drag the UsernameInput field here in the Inspector
    public GameObject pauseMenu; // Drag the Canvas or Pause Menu GameObject here

    async void Start()
    {
        Debug.Log("Initializing WebSocket connection...");
        websocket = new WebSocket("wss://ws.814850.xyz");

        websocket.OnOpen += () =>
        {
            Debug.Log("WebSocket connection opened.");
            string playerId = websocket.GetHashCode().ToString();
            string spawnMessage = $"spawn|{playerId}|{playerName}|{transform.position.x}|{transform.position.y}|{transform.position.z}|{transform.rotation.eulerAngles.x}|{transform.rotation.eulerAngles.y}|{transform.rotation.eulerAngles.z}";
            SendMessage(spawnMessage);

            // Assign the playerId to the local player
            GameObject localPlayer = Instantiate(playerPrefab, transform.position, transform.rotation);
            Player localPlayerScript = localPlayer.GetComponent<Player>();
            if (localPlayerScript != null)
            {
                localPlayerScript.Initialize(playerId);
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

    public void OnSubmitButtonClicked()
    {
        string username = usernameInputField.text.Trim();

        if (string.IsNullOrEmpty(username))
        {
            Debug.LogWarning("Username cannot be empty.");
            return;
        }

        playerName = username; // Update the player's name
        Debug.Log($"Username set to: {playerName}");

        // Send the updated username to the server
        string updateMessage = $"update_username|{websocket.GetHashCode()}|{playerName}";
        SendMessage(updateMessage);
    }

    public void OnResumeButtonClicked()
    {
        pauseMenu.SetActive(false); // Hide the pause menu
        Time.timeScale = 1; // Resume the game
        Debug.Log("Game resumed.");
    }

    public void OnQuitButtonClicked()
    {
        Debug.Log("Quitting the game...");
        Application.Quit();
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
        else if (messageType == "update")
        {
            Debug.Log($"Processing update message: {message}");

            if (parts.Length < 8)
            {
                Debug.LogError($"Invalid update message format: {message}");
                return;
            }

            string playerId = parts[1];
            Debug.Log($"Player ID: {playerId}");

            // Log each part to identify parsing issues
            Debug.Log($"Position X: {parts[2]}, Position Y: {parts[3]}, Position Z: {parts[4]}");
            Debug.Log($"Rotation X: {parts[5]}, Rotation Y: {parts[6]}, Rotation Z: {parts[7]}");

            bool posXParsed = float.TryParse(parts[2], out float posX);
            bool posYParsed = float.TryParse(parts[3], out float posY);
            bool posZParsed = float.TryParse(parts[4], out float posZ);
            bool rotXParsed = float.TryParse(parts[5], out float rotX);
            bool rotYParsed = float.TryParse(parts[6], out float rotY);
            bool rotZParsed = float.TryParse(parts[7], out float rotZ);

            if (posXParsed && posYParsed && posZParsed && rotXParsed && rotYParsed && rotZParsed)
            {
                Vector3 position = new Vector3(posX, posY, posZ);
                Quaternion rotation = Quaternion.Euler(rotX, rotY, rotZ);

                if (players.ContainsKey(playerId))
                {
                    GameObject player = players[playerId];
                    player.transform.position = position;
                    player.transform.rotation = rotation;
                    Debug.Log($"Updated player {playerId} to position {position} and rotation {rotation}");
                }
                else
                {
                    Debug.LogWarning($"Player ID {playerId} not found for update.");
                }
            }
            else
            {
                Debug.LogError($"Failed to parse numeric values in update message: {message}");
                if (!posXParsed) Debug.LogError($"Failed to parse Position X: {parts[2]}");
                if (!posYParsed) Debug.LogError($"Failed to parse Position Y: {parts[3]}");
                if (!posZParsed) Debug.LogError($"Failed to parse Position Z: {parts[4]}");
                if (!rotXParsed) Debug.LogError($"Failed to parse Rotation X: {parts[5]}");
                if (!rotYParsed) Debug.LogError($"Failed to parse Rotation Y: {parts[6]}");
                if (!rotZParsed) Debug.LogError($"Failed to parse Rotation Z: {parts[7]}");
            }
        }
        else if (messageType == "disconnect")
        {
            if (parts.Length < 2)
            {
                Debug.LogError($"Invalid disconnect message format: {message}");
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
        else if (messageType == "players")
        {
            if (parts.Length < 2)
            {
                Debug.LogError($"Invalid players message format: {message}");
                return;
            }

            if (int.TryParse(parts[1], out int playerCount) && parts.Length >= 2 + playerCount * 8)
            {
                for (int i = 0; i < playerCount; i++)
                {
                    string playerId = parts[2 + i * 8];
                    string playerName = parts[3 + i * 8];
                    if (float.TryParse(parts[4 + i * 8], out float posX) &&
                        float.TryParse(parts[5 + i * 8], out float posY) &&
                        float.TryParse(parts[6 + i * 8], out float posZ) &&
                        float.TryParse(parts[7 + i * 8], out float rotX) &&
                        float.TryParse(parts[8 + i * 8], out float rotY) &&
                        float.TryParse(parts[9 + i * 8], out float rotZ))
                    {
                        Vector3 position = new Vector3(posX, posY, posZ);
                        Quaternion rotation = Quaternion.Euler(rotX, rotY, rotZ);

                        if (!players.ContainsKey(playerId))
                        {
                            Debug.Log($"Spawning existing player: {playerName} (ID: {playerId})");
                            GameObject newPlayer = Instantiate(playerPrefab, position, rotation);
                            newPlayer.name = playerName;
                            players[playerId] = newPlayer;
                        }
                    }
                    else
                    {
                        Debug.LogError($"Invalid numeric values in players message: {message}");
                    }
                }
            }
            else
            {
                Debug.LogError($"Invalid players message format: {message}");
            }
        }
        else if (messageType == "shoot")
        {
            if (parts.Length < 9)
            {
                Debug.LogError($"Invalid shoot message format: {message}");
                return;
            }

            string playerId = parts[1];
            if (float.TryParse(parts[2], out float posX) &&
                float.TryParse(parts[3], out float posY) &&
                float.TryParse(parts[4], out float posZ) &&
                float.TryParse(parts[5], out float rotX) &&
                float.TryParse(parts[6], out float rotY) &&
                float.TryParse(parts[7], out float rotZ) &&
                float.TryParse(parts[8], out float rotW))
            {
                Vector3 position = new Vector3(posX, posY, posZ);
                Quaternion rotation = new Quaternion(rotX, rotY, rotZ, rotW);

                // Instantiate the bullet locally
                GameObject bullet = Instantiate(playerPrefab.GetComponent<PlayerController>().gunPrefab, position, rotation);
                Rigidbody rb = bullet.GetComponent<Rigidbody>();
                rb.AddForce(rotation * Vector3.forward * 20f, ForceMode.Impulse);

                // Assign the owner ID to the bullet
                Bullet bulletScript = bullet.GetComponent<Bullet>();
                bulletScript.ownerId = playerId;

                // Destroy the bullet after 5 seconds
                Destroy(bullet, 5f);

                Debug.Log($"Bullet spawned for player {playerId} at position {position} with rotation {rotation}");
            }
            else
            {
                Debug.LogError($"Failed to parse numeric values in shoot message: {message}");
            }
        }
        else if (messageType == "hit")
        {
            if (parts.Length < 3)
            {
                Debug.LogError($"Invalid hit message format: {message}");
                return;
            }

            string shooterId = parts[1];
            string targetId = parts[2];

            if (shooterId == websocket.GetHashCode().ToString())
            {
                // Local player shot someone
                Debug.Log($"[Local] Your bullet hit player {targetId}");
            }
            else if (targetId == websocket.GetHashCode().ToString())
            {
                // Local player got hit
                Debug.Log($"[Local] You were hit by player {shooterId}");
            }
            else
            {
                // Networked hit event
                Debug.Log($"[Network] Player {shooterId} hit player {targetId}");
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