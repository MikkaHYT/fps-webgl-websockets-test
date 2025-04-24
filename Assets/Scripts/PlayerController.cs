using UnityEngine;
using System.Text;
using System.Collections;

public enum WeaponType
{
    Pistol,
    AssaultRifle,
    Sniper
}

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public float lookSpeed = 2f;
    public GameObject gunPrefab;
    public Transform gunSpawnPoint;

    public Transform grapplePoint;
    public float grappleSpeed = 20f;
    public LineRenderer grappleLine; // Reference to the LineRenderer
    private CharacterController characterController;
    private Vector3 velocity;
    private bool isGrounded;
    private float rotationX = 0f;
    private NetworkManager networkManager;

    public string gunType = "Pistol"; // Type of gun being used

    // Store the last known position and rotation
    private Vector3 lastPosition;
    private Vector3 lastRotation;

    private bool isGrappling = false;

    private float grappleCooldown = 2f;
    private float lastGrappleTime = -Mathf.Infinity;

    private WeaponType currentWeapon = WeaponType.Pistol;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        networkManager = FindFirstObjectByType<NetworkManager>();

        // Initialize last known position and rotation
        lastPosition = transform.position;
        lastRotation = transform.eulerAngles;
    }

    void Update()
    {
        HandleWeaponSwitching();

        // Handle shooting based on the current weapon
        if (Input.GetButtonDown("Fire1") && currentWeapon != WeaponType.AssaultRifle)
        {
            Shoot();
        }
        else if (Input.GetButton("Fire1") && currentWeapon == WeaponType.AssaultRifle)
        {
            Shoot();
        }

        // Handle ADS for sniper
        if (currentWeapon == WeaponType.Sniper)
        {
            HandleADS();
        }

        // Check if the player is grounded
        if (isGrappling)
        {
            Vector3 currentPositionn = transform.position;
            Vector3 currentRotationn = transform.eulerAngles;
            string message = $"update|{networkManager.playerId}|{currentPositionn.x}|{currentPositionn.y}|{currentPositionn.z}|{currentRotationn.x}|{currentRotationn.y}|{currentRotationn.z}";
            networkManager.SendMessage(message);
            // Handle mouse look
            float tmouseX = Input.GetAxis("Mouse X") * lookSpeed;
            float tmouseY = Input.GetAxis("Mouse Y") * lookSpeed;

            rotationX -= tmouseY;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);

            Camera.main.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
            transform.Rotate(Vector3.up * tmouseX);

            // Handle movement
            float grappleMoveX = Input.GetAxis("Horizontal");
            float grappleMoveZ = Input.GetAxis("Vertical");
            Vector3 grappleMove = transform.right * grappleMoveX + transform.forward * grappleMoveZ;
            characterController.Move(grappleMove * moveSpeed * Time.deltaTime);

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

        // Handle grappling
        Grapple();

        // Send player position and rotation to the server only if they have changed
        Vector3 currentPosition = transform.position;
        Vector3 currentRotation = transform.eulerAngles;

        if (currentPosition != lastPosition || currentRotation != lastRotation)
        {
            string message = $"update|{networkManager.playerId}|{currentPosition.x}|{currentPosition.y}|{currentPosition.z}|{currentRotation.x}|{currentRotation.y}|{currentRotation.z}";
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
        if (currentWeapon == WeaponType.Pistol)
        {
            ShootPistol();
        }
        else if (currentWeapon == WeaponType.AssaultRifle)
        {
            ShootAssaultRifle();
        }
        else if (currentWeapon == WeaponType.Sniper)
        {
            ShootSniper();
        }
    }

    public void ShootPistol()
    {
        // Get the direction where the mouse is looking
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f)) // Check if the ray hits something
        {
            targetPoint = hit.point; // Set the target point to the hit point
        }
        else
        {
            targetPoint = ray.GetPoint(100f); // Set the target point far away in the ray direction
        }

        // Calculate the direction to shoot
        Vector3 shootDirection = (targetPoint - gunSpawnPoint.position).normalized;

        // Instantiate the bullet locally for the shooting player
        GameObject bullet = Instantiate(gunPrefab, gunSpawnPoint.position, Quaternion.LookRotation(shootDirection));
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb.AddForce(shootDirection * 20f, ForceMode.Impulse);

        // Assign the player's ID to the bullet
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        bulletScript.ownerId = networkManager.playerId.ToString();

        // Destroy the bullet after 5 seconds
        Destroy(bullet, 5f);

        // Notify the server about the shooting event
        string message = $"shoot|{gunType}|{networkManager.playerId}|{gunSpawnPoint.position.x}|{gunSpawnPoint.position.y}|{gunSpawnPoint.position.z}|{shootDirection.x}|{shootDirection.y}|{shootDirection.z}";
        networkManager.SendMessage(message);
    }

    public void ShootAssaultRifle()
    {
        // Get the direction where the mouse is looking
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f)) // Check if the ray hits something
        {
            targetPoint = hit.point; // Set the target point to the hit point
        }
        else
        {
            targetPoint = ray.GetPoint(100f); // Set the target point far away in the ray direction
        }

        // Calculate the direction to shoot
        Vector3 shootDirection = (targetPoint - gunSpawnPoint.position).normalized;

        // Instantiate the bullet locally for the shooting player
        GameObject bullet = Instantiate(gunPrefab, gunSpawnPoint.position, Quaternion.LookRotation(shootDirection));
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb.AddForce(shootDirection * 20f, ForceMode.Impulse);

        // Assign the player's ID to the bullet
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        bulletScript.ownerId = networkManager.playerId.ToString();

        // Destroy the bullet after 5 seconds
        Destroy(bullet, 5f);

        // Notify the server about the shooting event
        string message = $"shoot|{gunType}|{networkManager.playerId}|{gunSpawnPoint.position.x}|{gunSpawnPoint.position.y}|{gunSpawnPoint.position.z}|{shootDirection.x}|{shootDirection.y}|{shootDirection.z}";
        networkManager.SendMessage(message);
    }

    public void ShootSniper()
    {
        // Perform a raycast to determine if the sniper shot hits a target
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f)) // Check if the ray hits something
        {
            // Check if the hit object has a PlayerController or similar component
            PlayerController hitPlayer = hit.collider.GetComponent<PlayerController>();
            if (hitPlayer != null)
            {
                
            // Send a kill message if the hit results in a kill
            string killMessage = $"death|{hitPlayer.networkManager.playerId}|{networkManager.playerId}";
            networkManager.SendMessage(killMessage);


            Debug.Log($"Sniper hit player: {hitPlayer.networkManager.playerId}");
            }
            else
            {
            Debug.Log("Sniper shot missed or hit a non-player object.");
            }
        }
        else
        {
            Debug.Log("Sniper shot missed.");
        }
    }

    private void HandleWeaponSwitching()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentWeapon = WeaponType.Pistol;
            gunType = "Pistol"; // Update the gun type
            networkManager.SendMessage($"switch|{networkManager.playerId}|{gunType}");
            Debug.Log("Switched to Pistol");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentWeapon = WeaponType.AssaultRifle;
            gunType = "AssaultRifle"; // Update the gun type
            Debug.Log("Switched to Assault Rifle");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            currentWeapon = WeaponType.Sniper;
            gunType = "Sniper"; // Update the gun type
            Debug.Log("Switched to Sniper");
        }
    }

    private void HandleADS()
    {
        if (Input.GetButton("Fire2"))
        {
            Camera.main.fieldOfView = 30f; // Zoom in
        }
        else
        {
            Camera.main.fieldOfView = 60f; // Reset zoom
        }
    }

    Vector3 GetShootDirection()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            return (hit.point - gunSpawnPoint.position).normalized;
        }
        else
        {
            return ray.direction;
        }
    }

    void SpawnBullet(Vector3 direction, float speed)
    {
        GameObject bullet = Instantiate(gunPrefab, gunSpawnPoint.position, Quaternion.LookRotation(direction));
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb.AddForce(direction * speed, ForceMode.Impulse);

        // Destroy the bullet after 5 seconds
        Destroy(bullet, 5f);
    }

    private void OnDestroy()
    {
        if (networkManager != null && networkManager.websocket != null)
        {
            string disconnectMessage = $"disconnect|{networkManager.playerId}";
            networkManager.SendMessage(disconnectMessage);
        }
    }
}