using UnityEngine;
using System.Text;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public float lookSpeed = 2f;
    public GameObject gunPrefab;
    public Transform gunSpawnPoint;

    public Transform grapplePoint;
    public float grappleSpeed = 10f;
    public LineRenderer grappleLine; // Reference to the LineRenderer
    private CharacterController characterController;
    private Vector3 velocity;
    private bool isGrounded;
    private float rotationX = 0f;
    private NetworkManager networkManager;

    // Store the last known position and rotation
    private Vector3 lastPosition;
    private Vector3 lastRotation;

    private bool isGrappling = false;

    private float grappleCooldown = 2f;
    private float lastGrappleTime = -Mathf.Infinity;


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
        

        // Check if the player is grounded
        if (isGrappling)
        {
            Vector3 currentPositionn = transform.position;
            Vector3 currentRotationn = transform.eulerAngles;
            string message = $"update|{networkManager.websocket.GetHashCode()}|{currentPositionn.x}|{currentPositionn.y}|{currentPositionn.z}|{currentRotationn.x}|{currentRotationn.y}|{currentRotationn.z}";
            networkManager.SendMessage(message);
            // Handle mouse look
            float tmouseX = Input.GetAxis("Mouse X") * lookSpeed;
            float tmouseY = Input.GetAxis("Mouse Y") * lookSpeed;

            rotationX -= tmouseY;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);

            Camera.main.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
            transform.Rotate(Vector3.up * tmouseX);

            // Handle shooting
            if (Input.GetButtonDown("Fire1"))
            {
                Shoot();
            }
            
            return; // Disable grounded checks and movement while grappling
        }

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

        // Handle grappling
        Grapple();

        // Send player position and rotation to the server only if they have changed
        Vector3 currentPosition = transform.position;
        Vector3 currentRotation = transform.eulerAngles;

        if (currentPosition != lastPosition || currentRotation != lastRotation)
        {
            string message = $"update|{networkManager.websocket.GetHashCode()}|{currentPosition.x}|{currentPosition.y}|{currentPosition.z}|{currentRotation.x}|{currentRotation.y}|{currentRotation.z}";
            networkManager.SendMessage(message);

            // Update the last known position and rotation
            lastPosition = currentPosition;
            lastRotation = currentRotation;
        }
    }

    void Grapple()
    {
        if (Input.GetKeyDown(KeyCode.E) && !isGrappling && Time.time >= lastGrappleTime + grappleCooldown)
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, 50f))
            {
                grapplePoint.position = hit.point;
                StartCoroutine(GrappleToPoint(hit.point));
                lastGrappleTime = Time.time; // Update the last grapple time

                // Enable the LineRenderer and set its start and end points
                grappleLine.enabled = true;
                grappleLine.SetPosition(0, gunSpawnPoint.position); // Start point (player)
                grappleLine.SetPosition(1, hit.point); // End point (grapple point)
            }
        }
    }

    IEnumerator GrappleToPoint(Vector3 point)
    {
        isGrappling = true; // Start grappling

        float grappleProgress = 0f;
        while (grappleProgress < 1f)
        {
            grappleProgress += grappleSpeed * Time.deltaTime / Vector3.Distance(transform.position, point);
            Vector3 currentPoint = Vector3.Lerp(transform.position, point, grappleProgress);

            // Update the grapple line's start and end points
            grappleLine.SetPosition(0, gunSpawnPoint.position);
            grappleLine.SetPosition(1, currentPoint);

            yield return null;
        }

        // Move the player to the final point
        while (Vector3.Distance(transform.position, point) > 1f)
        {
            grappleLine.SetPosition(0, gunSpawnPoint.position);
            transform.position = Vector3.MoveTowards(transform.position, point, grappleSpeed * Time.deltaTime);
            yield return null;
        }

        isGrappling = false; // Stop grappling
        grappleLine.enabled = false; // Disable the line
    }

    public void Shoot()
    {
        // Instantiate the bullet locally for the shooting player
        GameObject bullet = Instantiate(gunPrefab, gunSpawnPoint.position, gunSpawnPoint.rotation);
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb.AddForce(gunSpawnPoint.forward * 20f, ForceMode.Impulse);

        // Assign the player's ID to the bullet
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        bulletScript.ownerId = networkManager.websocket.GetHashCode().ToString();

        // Destroy the bullet after 5 seconds
        Destroy(bullet, 5f);

        // Notify the server about the shooting event
        string message = $"shoot|{networkManager.websocket.GetHashCode()}|{gunSpawnPoint.position.x}|{gunSpawnPoint.position.y}|{gunSpawnPoint.position.z}|{gunSpawnPoint.rotation.x}|{gunSpawnPoint.rotation.y}|{gunSpawnPoint.rotation.z}|{gunSpawnPoint.rotation.w}";
        networkManager.SendMessage(message);
    }

    private void OnDestroy()
    {
        if (networkManager != null && networkManager.websocket != null)
        {
            string disconnectMessage = $"disconnect|{networkManager.websocket.GetHashCode()}";
            networkManager.SendMessage(disconnectMessage);
        }
    }
}