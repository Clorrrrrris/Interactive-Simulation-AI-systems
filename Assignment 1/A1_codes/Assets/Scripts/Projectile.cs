using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class Projectile : MonoBehaviour
{
    public GameObject platformPrefab;
    public GameObject player;
    public float maxDistance = 50f;
    private Vector3 startPosition;

    public static List<GameObject> activePlatforms = new List<GameObject>();

    void Start()
    {
        startPosition = transform.position;
        //Destroy(gameObject, 5f);
    }

    void Update()
    {
        // destroy if projectile fly too far
        if (Vector3.Distance(startPosition, transform.position) > maxDistance)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // create platform only when hitting the cavity
        if (collision.gameObject.CompareTag("Cavity"))
        {
            CreatePlatform();
        }
        Destroy(gameObject); // destroy if hit anything


    }

    void CreatePlatform()
    {
        Vector3 platformPosition = new Vector3(
            transform.position.x,
            -0.1f, // height of platform
            transform.position.z
        );

        GameObject platform = Instantiate(platformPrefab, platformPosition, Quaternion.identity);
        platform.tag = "Projectile Platform";
        
        activePlatforms.Add(platform);
    }

}