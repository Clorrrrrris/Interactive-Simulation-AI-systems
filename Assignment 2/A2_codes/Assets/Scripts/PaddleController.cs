using UnityEngine;

public class PaddleController : MonoBehaviour
{
    public enum PaddleType { Left, Right }
    public PaddleType paddleType;

    public float maxAngle = 45f; 
    public float angularSpeed = 180f;

    private float currentAngle = 0f;
    private float lastAngle = 0f;

    void Update()
    {
        lastAngle = currentAngle;

        // left paddle move
        if (paddleType == PaddleType.Left && Input.GetKey(KeyCode.A))
        {
            currentAngle = Mathf.MoveTowards(currentAngle, maxAngle, angularSpeed * Time.deltaTime);
        }
        // right paddle move
        else if (paddleType == PaddleType.Right && Input.GetKey(KeyCode.D))
        {
            currentAngle = Mathf.MoveTowards(currentAngle, -maxAngle, angularSpeed * Time.deltaTime);
        }
        else
        {
            currentAngle = Mathf.MoveTowards(currentAngle, 0f, angularSpeed * Time.deltaTime);
        }

        // paddle rotate about y axis
        transform.localRotation = Quaternion.Euler(0f, -currentAngle, 0f);
    }


    public float GetSpeed()
    {
        return (currentAngle - lastAngle) / Time.deltaTime;
    }
}