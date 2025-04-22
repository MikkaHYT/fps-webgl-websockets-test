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

    // Local player reference
    public GameObject localPlayer; // Drag the local player object here in the Inspector
    private Player localPlayerScript; // Reference to the Player script of the local player

    public int localPlayerHealth = 100; // Local player health

    public GameObject deathUI; // Drag the Death UI GameObject here in the Inspector
    public GameObject healthUI; // Drag the Death UI GameObject here in the Inspector

    public TextMeshProUGUI RespawnTxt; // Reference to the TextMeshProUGUI component for respawn countdown
    public TextMeshProUGUI healthTxt; 

    public string lastAttackerId; // ID of the last player who attacked the local player

    public GameObject scrollViewContent; // Reference to the ScrollView content panel
    public GameObject playerNameTextPrefab; // Prefab for displaying player names in the ScrollView
    public GameObject menuUI;
    public static bool isMenuOpen = false;
    public bool menuInstantiated = false;
    public string playersMessage;
    public bool playersHandled = false;



    

    async void Start()
    {
        DontDestroyOnLoad(gameObject);
        Debug.Log("Initializing WebSocket connection...");
        websocket = new WebSocket("wss://ws.814850.xyz");

        websocket.OnOpen += () =>
        {
            Debug.Log("WebSocket connection opened.");
            SendHeartbeat();
            
        
        };

        websocket.OnError += (error) =>
        {
            Debug.LogError($"WebSocket error: {error}");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        };

        websocket.OnClose += (closeCode) =>
        {
            Debug.LogWarning($"WebSocket connection closed with code: {closeCode}");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
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

    public void OnJoinGameButtonClicked()
    {
        string username = usernameInputField.text.Trim();

        if (string.IsNullOrEmpty(username))
        {
            Debug.LogWarning("Username cannot be empty.");
            return;
        }

        playerName = username; // Update the player's name
        Debug.Log($"Joining game with username: {playerName}");

        // Switch to the "Game" scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
        Debug.Log($"Scene loading");
        
        
    }


    void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
        websocket.DispatchMessageQueue();
        #endif

        // Handle menu toggle
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Game") {
            isMenuOpen = !isMenuOpen;
            if (!menuInstantiated) {
                menuUI = Instantiate(menuUI);
                menuInstantiated = true;
            }
            menuUI.SetActive(isMenuOpen);
            Cursor.lockState = isMenuOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isMenuOpen;
        }
        }

        if (isMenuOpen)
        {
            return; // Stop player movement and actions when menu is open
        }

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Game" )
            {
                if (healthUI == null)
                {
                    Debug.LogError("Health UI prefab is not assigned.");
                    return;
                }
                
                
                if (healthUI == null)
                {
                    Debug.LogError("Failed to instantiate Health UI.");
                    return;
                }

                
                if (healthTxt == null)
                {
                    Debug.LogError("TextMeshProUGUI component not found in Health UI.");
                }
            }



        if (!playersHandled)
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Game")
            {
                
                healthUI = Instantiate(healthUI);
                healthTxt = healthUI.GetComponentInChildren<TextMeshProUGUI>();
                playersHandled = true; // Mark players message as handled
                healthTxt = healthUI.GetComponentInChildren<TextMeshProUGUI>();
                string playerId = websocket.GetHashCode().ToString();
                healthTxt = healthUI.GetComponentInChildren<TextMeshProUGUI>();
                string spawnMessage = $"spawn|{playerId}|{playerName}|{transform.position.x}|{transform.position.y}|{transform.position.z}|{transform.rotation.eulerAngles.x}|{transform.rotation.eulerAngles.y}|{transform.rotation.eulerAngles.z}";
                healthTxt = healthUI.GetComponentInChildren<TextMeshProUGUI>();
                SendMessage(spawnMessage);
                healthTxt = healthUI.GetComponentInChildren<TextMeshProUGUI>();
                HandleMessage(playersMessage); // Handle the players message if it was received before the scene loaded
                healthTxt = healthUI.GetComponentInChildren<TextMeshProUGUI>();
                string updateUsernameMessage = $"update_username|{websocket.GetHashCode()}|{playerName}";
                SendMessage(updateUsernameMessage);
                
            }
        }
        
    }

    private async void SendHeartbeat()
    {
        while (websocket != null && websocket.State == WebSocketState.Open)
        {
            await websocket.SendText("heartbeat");
            Debug.Log("Heartbeat sent.");
            await System.Threading.Tasks.Task.Delay(15000); // Send heartbeat every 15 seconds
        }
    }

    private void OnEnable()
    {
        SendHeartbeat();
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

        Debug.Log($"Username set to: {username}");

        // Send the updated username to the server
        string updateMessage = $"update_username|{websocket.GetHashCode()}|{username}";
        SendMessage(updateMessage);
    }

    public void OnResumeButtonClicked()
    {
        
        menuUI.SetActive(false); // Hide the menu UI
        pauseMenu.SetActive(false); // Hide the pause menu
        Time.timeScale = 1; // Resume the game
        Debug.Log("Game resumed.");
        isMenuOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false; // Hide the cursor
    }

    public void OnQuitButtonClicked()
    {
        Debug.Log("Quitting the game...");
        Application.Quit();
    }

    public List<string> PlayerList = new List<string>();

    private void HandleMessage(string message)
    {
        string[] parts = message.Split('|');
        string messageType = parts[0];

        if (messageType == "heartbeat")
        {
            Debug.Log("Heartbeat received from server.");
            return;
        }

        Debug.Log($"Handling message: {message}");

        if (parts.Length < 2)
        {
            Debug.LogError($"Invalid message format: {message}");
            return;
        }

        if (messageType == "update_username")
        {
            if (parts.Length < 3)
            {
                Debug.LogError($"Invalid update_username message format: {message}");
                return;
            }

            string playerId = parts[1];
            string newUsername = parts[2];

            if (players.ContainsKey(playerId))
            {
                GameObject player = players[playerId];
                Player playerScript = player.GetComponent<Player>();
                if (playerScript != null)
                {
                    playerScript.UpdateUsername(newUsername);
                }
                else
                {
                    Debug.LogWarning($"Player script not found for player ID {playerId}");
                }
            }
            else
            {
                Debug.LogWarning($"Player with ID {playerId} not found for username update.");
            }
        }
        else if (messageType == "spawn")
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
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Game")
            {
                playersMessage = message;
                return;
            }
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
                            PlayerList.Add(playerName);
                            GameObject newPlayer = Instantiate(playerPrefab, position, rotation);
                            newPlayer.name = playerName;

                            // Assign the playerId to the Player script
                            Player playerScript = newPlayer.GetComponent<Player>();
                            if (playerScript != null)
                            {
                                playerScript.Initialize(playerId);
                            }

                            // Set the player's name above their head
                            TextMeshProUGUI nameTag = newPlayer.GetComponentInChildren<TextMeshProUGUI>();
                            if (nameTag != null)
                            {
                                nameTag.text = playerName;
                            }
                            else
                            {
                                Debug.LogWarning($"Name tag (TextMeshProUGUI) not found for player {playerId}");
                            }

                            players[playerId] = newPlayer;
                        }
                        else
                        {
                            // Update the position and rotation of existing players
                            GameObject existingPlayer = players[playerId];
                            existingPlayer.transform.position = position;
                            existingPlayer.transform.rotation = rotation;

                            // Ensure the Player script is initialized
                            Player playerScript = existingPlayer.GetComponent<Player>();
                            if (playerScript != null && playerScript.playerId != playerId)
                            {
                                playerScript.Initialize(playerId);
                            }
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

            Debug.Log("Current players in the game:");
            foreach (var kvp in players)
            {
                Debug.Log($"Player ID: {kvp.Key}, Player Name: {kvp.Value.name}");
                // Create a new TextMeshProUGUI object for each player
                GameObject playerNameText = Instantiate(playerNameTextPrefab, scrollViewContent.transform);
                TextMeshProUGUI textComponent = playerNameText.GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    // Set the player name, color, and font size
                    textComponent.text = kvp.Value.name;
                    textComponent.color = Color.white;
                    textComponent.fontSize = 24;
                }
                else
                {
                    Debug.LogWarning($"TextMeshProUGUI component not found in prefab for player {kvp.Key}");
                }
                
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
            int damage = 20; // Default damage value

            // Update local player health if they were hit
            if (targetId == websocket.GetHashCode().ToString())
            {
                Debug.Log($"[Local] You were hit by player {shooterId} for {damage} damage.");
                lastAttackerId = shooterId;
                localPlayerHealth -= damage;
                healthTxt.text = $"{localPlayerHealth}";
                    if (localPlayerHealth <= 0)
                    {
                        onPlayerDied();
                    }
                
            }

            // Update target player's health locally if the local player shot them
            if (shooterId == websocket.GetHashCode().ToString())
            {
                Debug.Log($"[Local] Your bullet hit player {targetId} for {damage} damage.");
                
            }
        }
        else
        {
            Debug.LogWarning($"Unknown message type: {messageType}");
        }
    }

    // Coroutine to handle countdown and respawn
    System.Collections.IEnumerator RespawnCountdown()
            {
                RespawnTxt = deathUI.GetComponentInChildren<TextMeshProUGUI>();
                localPlayer = GameObject.Find("LocalPlayer");
                if (RespawnTxt == null)
                {
                    Debug.LogError("RespawnTxt not found in the instantiated Death UI.");
                    yield break;
                }
                int countdown = 5;
                while (countdown > 0)
                {
                    Debug.Log($"Respawning in {countdown}...");
                    string respawnMessage = $"update|{websocket.GetHashCode()}|500|500|0|0|0|0";
                    SendMessage(respawnMessage);
                    RespawnTxt.text = $"Respawning in {countdown} seconds..";
                    yield return new WaitForSeconds(1);
                    countdown--;
                }

                // Disable the death UI screen
                deathUI.SetActive(false);

                // Teleport the local player to the respawn position
                localPlayer.transform.position = new Vector3(0, 0, 0);
                localPlayerHealth = 100; // Reset health
            }

    public void onPlayerDied()
    {
        Debug.Log("Local player has died.");
        // Notify the server that the local player has died
        if (!string.IsNullOrEmpty(lastAttackerId))
        {
            string deathMessage = $"death|{websocket.GetHashCode()}|{lastAttackerId}";
            SendMessage(deathMessage);
        }
        // Add logic for handling local player death (e.g., respawn, game over screen, etc.)
        Debug.Log("Respawning local player...");
        // Example respawn logic (you can customize this)
        if (localPlayer != null)
        {
            // Teleport the local player above the map
            localPlayer = GameObject.Find("LocalPlayer");
            localPlayer.transform.position = new Vector3(0, 500, 0);
            

            // Enable the death UI screen
            if (deathUI == null)
            {
                Debug.LogWarning("Death UI is not assigned. Instantiating a new one.");
                //deathUI = Instantiate(deathUI);
            }
            deathUI.SetActive(true);

            // Start a countdown coroutine
            StartCoroutine(RespawnCountdown());

            
        }
        else
        {
            Debug.LogError("Local player object is null.");
        }
    }

    private async void OnApplicationQuit()
    {
        Debug.Log("Closing WebSocket connection...");
        await websocket.Close();
        Debug.Log("WebSocket connection closed.");
    }
}