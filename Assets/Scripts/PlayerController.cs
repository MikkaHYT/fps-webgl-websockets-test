using UnityEngine;
using System.Text;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public float lookSpeed = 2f;
    public GameObject gunPrefab;
    public Transform gunSpawnPoint;
    public GameObject menuUI;
    private CharacterController characterController;
    private Vector3 velocity;
    private bool isGrounded;
    private float rotationX = 0f;
    private bool isMenuOpen = false;
    private NetworkManager networkManager;

    // Store the last known position and rotation
    private Vector3 lastPosition;
    private Vector3 lastRotation;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        networkManager = FindObjectOfType<NetworkManager>();

        // Initialize last known position and rotation
        lastPosition = transform.position;
        lastRotation = transform.eulerAngles;
    }

    void Update()
    {
        // Handle menu toggle
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isMenuOpen = !isMenuOpen;
            menuUI.SetActive(isMenuOpen);
            Cursor.lockState = isMenuOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isMenuOpen;
        }

        if (isMenuOpen)
        {
            return; // Stop player movement and actions when menu is open
        }

        // Check if the player is grounded
        isGrounded = characterController.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to ensure grounded state
        }

        // Handle movement
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        characterController.Move(move * moveSpeed * Time.deltaTime);

        // Handle jumping
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y);
        }

        // Apply gravity
        velocity.y += Physics.gravity.y * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);

        // Handle mouse look
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        Camera.main.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);

        // Handle shooting
        if (Input.GetButtonDown("Fire1"))
        {
            Shoot();
        }

        // Send player position and rotation to the server only if they have changed
        Vector3 currentPosition = transform.position;
        Vector3 currentRotation = transform.eulerAngles;

        if (currentPosition != lastPosition || currentRotation != lastRotation)
        {
            string message = $"update|{networkManager.websocket.GetHashCode()}|{currentPosition.x}|{currentPosition.y}|{currentPosition.z}|{currentRotation.x}|{currentRotation.y}|{currentRotation.z}`";
            networkManager.SendMessage(message);

            // Update the last known position and rotation
            lastPosition = currentPosition;
            lastRotation = currentRotation;
        }
    }

    public void Shoot()
    {
        GameObject bullet = Instantiate(gunPrefab, gunSpawnPoint.position, gunSpawnPoint.rotation);
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb.AddForce(gunSpawnPoint.forward * 20f, ForceMode.Impulse);

        // Notify server about shooting
        string message = $"shoot|{networkManager.websocket.GetHashCode()}|{gunSpawnPoint.position.x}|{gunSpawnPoint.position.y}|{gunSpawnPoint.position.z}";
        networkManager.SendMessage(message);
    }
}