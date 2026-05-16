using UnityEngine;
using System.Collections.Generic;

public class SpawnPointTrigger : MonoBehaviour
{
    public OgreTreasureBehavior treasure; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[SpawnPoint] Player returned to spawn point.");
            
            OgreHTNManager[] ogres = FindObjectsByType<OgreHTNManager>(FindObjectsSortMode.None);

            bool treasureWasStolen = false;
            foreach (var ogre in ogres)
            {
                if (ogre.treasure.treasureStolen)
                {
                    treasureWasStolen = true;
                    break;
                }
            }

            if (treasureWasStolen)
            {
                PlayerController player = other.GetComponentInParent<PlayerController>();
                player.WinGame();
            }
            else
            {
                Debug.Log("[SpawnPoint] Player has no treasure yet.");
            }
        }
    }
}