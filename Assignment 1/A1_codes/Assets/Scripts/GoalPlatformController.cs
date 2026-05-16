using UnityEngine;

public class GoalPlatformController : MonoBehaviour
{
    private BoxCollider groundCollider; 

    // variables to store original values
    private Vector3 originalCenter;
    private Vector3 originalSize;
    private Vector3 originalScale;
    private Vector3 originalPosition; 

    private Transform player;
    public float shrinkSpeed = 8.0f; 
    private bool isPlayerOnPlatform = false;

    // define shrink direction
    public bool shrinkFromPositiveEnd = true; // true: shrink from +z direction, else from -z

    void Start()
    {
        BoxCollider[] colliders = GetComponents<BoxCollider>();
        foreach (BoxCollider col in colliders)
        {
            if (!col.isTrigger)
            {
                groundCollider = col;
                break;
            }
        }

        if (groundCollider == null)
        {
            Debug.LogError("Could not find a non-trigger BoxCollider.");
            return;
        }

        // store original values
        originalCenter = groundCollider.center;
        originalSize = groundCollider.size;
        originalScale = transform.localScale;
        originalPosition = transform.position;
    }


    // only shrink when player is on the goal platform
    void Update()
    {
        if (!isPlayerOnPlatform || player == null) return;

        ShrinkFromOneEnd();
    }

    void ShrinkFromOneEnd()
    {
        // get player position
        Vector3 playerLocalPos = transform.InverseTransformPoint(player.position);
        float shrinkBoundary;
        
        if (shrinkFromPositiveEnd) // +z direction
        {
            float maxBoundary = originalCenter.z + originalSize.z / 2;
            shrinkBoundary = Mathf.Min(playerLocalPos.z, maxBoundary);
        }
        else // -z direction
        {
            float minBoundary = originalCenter.z - originalSize.z / 2;
            shrinkBoundary = Mathf.Max(playerLocalPos.z, minBoundary);
        }

        float newLengthZ;
        float newCenterZ;
        Vector3 newPosition = originalPosition;

        if (shrinkFromPositiveEnd)
        {
            // fix -z end
            float fixedEndZ = originalCenter.z - originalSize.z / 2;
            newLengthZ = Mathf.Max(0.1f, shrinkBoundary - fixedEndZ);
            newCenterZ = fixedEndZ + newLengthZ / 2;
            
            float scaleRatio = newLengthZ / originalSize.z;
            float positionOffset = (originalSize.z * originalScale.z - newLengthZ * originalScale.z) / 2;
            newPosition = originalPosition + transform.forward * positionOffset;
        }
        else
        {
            float fixedEndZ = originalCenter.z + originalSize.z / 2; 
            newLengthZ = Mathf.Max(0.1f, fixedEndZ - shrinkBoundary);
            newCenterZ = fixedEndZ - newLengthZ / 2;

            float scaleRatio = newLengthZ / originalSize.z;
            float positionOffset = (originalSize.z * originalScale.z - newLengthZ * originalScale.z) / 2;
            newPosition = originalPosition - transform.forward * positionOffset;
        }

        float delta = shrinkSpeed * Time.deltaTime;
        
        Vector3 targetColliderCenter = new Vector3(originalCenter.x, originalCenter.y, newCenterZ);
        Vector3 targetColliderSize = new Vector3(originalSize.x, originalSize.y, newLengthZ);
        
        groundCollider.center = Vector3.Lerp(groundCollider.center, targetColliderCenter, delta);
        groundCollider.size = Vector3.Lerp(groundCollider.size, targetColliderSize, delta);

        float targetScaleZ = (newLengthZ / originalSize.z) * originalScale.z;
        Vector3 targetScale = new Vector3(originalScale.x, originalScale.y, targetScaleZ);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, delta);
        
        transform.position = Vector3.Lerp(transform.position, newPosition, delta);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerOnPlatform = true;
            player = other.transform;
            //Debug.Log("Player on platform - shrinking from one end");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerOnPlatform = false;
            player = null;
        }
    }

    // make sure shrink can be seen
    void OnDrawGizmosSelected()
    {
        if (groundCollider != null && isPlayerOnPlatform && player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(groundCollider.center, groundCollider.size);
            
            Gizmos.color = Color.green;
            Vector3 playerLocalPos = transform.InverseTransformPoint(player.position);
            Gizmos.DrawSphere(playerLocalPos, 0.3f);
        }
    }

}