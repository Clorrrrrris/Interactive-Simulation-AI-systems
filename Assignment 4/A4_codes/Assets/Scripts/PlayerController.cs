using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class PlayerController : MonoBehaviour
{
    public float moveSpeed; 
    public float mouseSensitivity;
    public CharacterController controller;
    public Transform cameraTransform;

    public float invisibilityMaxTime;
    public float invisibilityRemaining;
    public bool isInvisible = false;

    public int lives = 2;

    public TMP_Text invisTime;
    public TMP_Text invisIndicator;
    public TMP_Text livesCount;
    public GameObject winLosePanel;
    public TMP_Text resultText;

    public Transform spawnPoint;
    private float cameraPitch = 0f;


    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        invisibilityRemaining = invisibilityMaxTime;
        UpdateUI();
    }

    void Update()
    {
        LookAround();
        Move();
        HandleInvisibility();
        UpdateUI();
    }


    // PLAYER LOOK / MOUSE CAMERA
    void LookAround()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -80f, 80f);

        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }


    // MOVEMENT
    void Move()
    {
        float h = Input.GetAxis("Horizontal");   // A D
        float v = Input.GetAxis("Vertical");     // W S

        Vector3 move = (transform.right * h + transform.forward * v);
        controller.SimpleMove(move * moveSpeed);
    }


    // INVISIBILITY
    void HandleInvisibility()
    {
        // toggle invisibility
        if (Input.GetKeyDown(KeyCode.Space) && invisibilityRemaining > 0)
        {
            isInvisible = !isInvisible;
            UpdateIndicator();
        }

        // countdown time
        if (isInvisible)
        {
            invisibilityRemaining -= Time.deltaTime;

            if (invisibilityRemaining <= 0)
            {
                invisibilityRemaining = 0;
                isInvisible = false;
                UpdateIndicator();
            }
        }
    }


    // TAKING DAMAGE FROM OGRE
    public void TakeDamage()
    {
        lives = lives - 1;
        UpdateUI(); 

        if (lives == 0)
            LoseGame();
    }


    // UI UPDATE
    void UpdateUI()
    {
        if (invisTime != null)
            invisTime.text = $"Invisible time remaining: {invisibilityRemaining:F1}s";

        if (livesCount != null)
            livesCount.text = $"Lives: {lives}";
    }
    
    void UpdateIndicator()
    {
        if (invisIndicator == null) return;

        if (isInvisible)
            invisIndicator.text = "Invisible: YES";
        else
            invisIndicator.text = "Invisible: NO";
    }

    // GAME END
    public void WinGame()
    {
        ShowEndScreen("YOU WIN!\n Treasure collected.");
    }

    void LoseGame()
    {
        ShowEndScreen("YOU LOSE!\n Ogre killed you.");
    }

    void ShowEndScreen(string message)
    {
        Time.timeScale = 0f;
        resultText.text = message;
        winLosePanel.SetActive(true);
    }
}
