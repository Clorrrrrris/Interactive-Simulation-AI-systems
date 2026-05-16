using UnityEngine;

public class PinballManager : MonoBehaviour
{
    public GameObject pinballPrefab;  
    public int maxBalls;
    public float spawnHeight; 
    private int currentBalls = 0;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && currentBalls < maxBalls)
        {
            SpawnPinball();
        }

        GameObject[] gutters = GameObject.FindGameObjectsWithTag("Gutter");
        foreach (GameObject gutter in gutters)
        {
            Pinball[] balls = Object.FindObjectsByType<Pinball>(FindObjectsSortMode.None);
            foreach (Pinball ball in balls)
            {
                if (CheckCircleAABB(ball.transform.position, 0.5f, gutter.transform))
                {
                    //Debug.Log("Pinball fell into the gutter and was destroyed.");
                    Destroy(ball.gameObject);
                    BallDestroyed();
                }
                else if (ball.velocity.magnitude < 0.1f)
                {
                    //Debug.Log("Pinball stopped moving and was destroyed.");
                    Destroy(ball.gameObject);
                    BallDestroyed();
                }
            }
        }
    }

    void SpawnPinball()
    {
        GameObject table = GameObject.Find("Table");
        float x = 0f, z = 0f;

        if (table != null)
        {
            Vector3 scale = table.transform.localScale;
            float width = 10f * scale.x;
            float depth = 10f * scale.z;

            x = Random.Range(-width / 2f + 3f, width / 2f - 4f);
            z = table.transform.position.z + depth / 2f - 4f;
        }

        Vector3 spawnPos = new Vector3(x, spawnHeight, z - 5f);
        GameObject ball = Instantiate(pinballPrefab, spawnPos, Quaternion.identity);

        Pinball pb = ball.GetComponent<Pinball>();
        if (pb != null)
        {
            pb.velocity = new Vector3(0f, 0f, -8f);
        }

        currentBalls++;
    }

    public void BallDestroyed()
    {
        currentBalls--;
    }

    bool CheckCircleAABB(Vector3 c3D, float r, Transform box)
    {
        Vector2 c = new Vector2(c3D.x, c3D.z);
        Vector2 bPos = new Vector2(box.position.x, box.position.z);
        Vector2 half = new Vector2(box.localScale.x * 0.5f, box.localScale.z * 0.5f);

        float closestX = Mathf.Clamp(c.x, bPos.x - half.x, bPos.x + half.x);
        float closestZ = Mathf.Clamp(c.y, bPos.y - half.y, bPos.y + half.y);

        float dx = c.x - closestX;
        float dz = c.y - closestZ;

        return (dx * dx + dz * dz) <= r * r;
    }
}