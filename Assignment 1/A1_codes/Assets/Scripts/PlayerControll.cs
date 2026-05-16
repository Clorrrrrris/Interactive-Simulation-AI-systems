using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerController : MonoBehaviour
{
    private int count; // Variable to keep track of collected Collectible objects.
    public TextMeshProUGUI countText; // UI text component to display count of Collectible objects collected.
    public GameObject winTextObject; // UI object to display winning text.
    public GameObject gameOverTextObject;

    // variables for shooting related
    public GameObject projectilePrefab;
    public GameObject platformPrefab;
    public Transform shootPoint;
    public float shootForce = 30f;
    private bool canShoot = true;

    // variables for path trail
    public GameObject pathMarkerPrefab;
    private float lastMarkerTime;
    public float markerInterval = 0.3f;

    private GameObject currentPlatform;
    private bool isOnPlatform = false;

    void Start()
    {
        SetCountText(); // Update the count display.
        winTextObject.SetActive(false); // Initially set the win text to be inactive.
        gameOverTextObject.SetActive(false);
        lastMarkerTime = Time.time;
    }

    void Update()
    {
        // leave path marker
        if (Time.time - lastMarkerTime > markerInterval)
        {
            LeavePathMarker();
            lastMarkerTime = Time.time;
        }

        // check if shoot
        if (Input.GetMouseButtonDown(0) && canShoot && count > 0)
        {
            ShootProjectile();
        }

    }

    void OnTriggerEnter(Collider other)
    {
        // check if the object the player collided with has tag Collectible
        if (other.gameObject.CompareTag("Collectible"))
        {
            other.gameObject.SetActive(false); // make collided object disappear
            count = count + 1;
            SetCountText(); // Update the count display.
        }
        // check if reach goal point
        else if (other.gameObject.CompareTag("Goal"))
        {
            WinGame();
        }
        // check if fall into cavity
        else if (other.gameObject.CompareTag("Cavity"))
        {
            GameOver();
        }
        // check if stand on the platform
        else if (other.gameObject.CompareTag("Projectile Platform"))
        {
            currentPlatform = other.gameObject;
            isOnPlatform = true;
            //Debug.Log("Landed on platform: " + currentPlatform.name);
        }
    }

    void OnTriggerExit(Collider other)
    {
        //Debug.Log("Trigger Exit with: " + other.name + " (Tag: " + other.tag + ")");

        // check if player leave the platform
        if (other.gameObject.CompareTag("Projectile Platform"))
        {
            //Debug.Log("Left platform: " + other.name);

            if (other.gameObject == currentPlatform && isOnPlatform)
            {
                //Debug.Log("Destroying platform: " + other.name);
                Destroy(other.gameObject);
                currentPlatform = null;
                isOnPlatform = false;
            }
        }
    }


    void LeavePathMarker()
    {
        Vector3 markerPosition = new Vector3(
            // the same x,y-axis of player
            transform.position.x,
            0.01f,
            transform.position.z
        );

        Quaternion rotation = Quaternion.Euler(90f, 0f, 0f);

        Instantiate(pathMarkerPrefab, markerPosition, rotation);
    }

    void ShootProjectile()
    {
        //Debug.Log($"ShootProjectile called -- can shoot: {canShoot}, Count: {count}");

        if (projectilePrefab == null)
        {
            Debug.LogError("Projectile prefab is not assigned.");
            return;
        }

        if (shootPoint == null)
        {
            Debug.LogError("Shoot point is not assigned.");
            return;
        }

        GameObject projectile = Instantiate(
            projectilePrefab,
            shootPoint.position,
            shootPoint.rotation
        );

        Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();
        if (projectileRb == null)
        {
            Debug.LogError("Projectile has no Rigidbody component.");
            return;
        }

        projectileRb.linearVelocity = shootPoint.forward * shootForce;

        Projectile projScript = projectile.GetComponent<Projectile>();
        if (projScript == null)
        {
            projScript = projectile.AddComponent<Projectile>();
        }
    
        projScript.platformPrefab = platformPrefab;
        projScript.player = this.gameObject;

        // decrease collectible count
        count = count - 1;
        SetCountText();

        // disable shooting temporarily
        canShoot = false;
        StartCoroutine(EnableShooting(0.5f));
    }

    IEnumerator EnableShooting(float delay)
    {
        yield return new WaitForSeconds(delay);
        canShoot = true;
    }

    void WinGame()
    {
        winTextObject.SetActive(true);
        Time.timeScale = 0; // palse game
    }

    void GameOver()
    {
        gameOverTextObject.SetActive(true);
        Time.timeScale = 0;
    }

    // function to update the displayed count of "Collectible" objects collected.
    void SetCountText()
    {
        // Update the count text with the current count.
        countText.text = "Count: " + count.ToString();
    }

}
