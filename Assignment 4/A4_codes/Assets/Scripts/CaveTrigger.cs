using UnityEngine;
using System.Collections.Generic;

public class CaveTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[Cave] Player collected the treasure.");

            OgreHTNManager[] ogres = FindObjectsByType<OgreHTNManager>(FindObjectsSortMode.None);
            foreach (var ogre in ogres)
            {
                ogre.treasure.treasureStolen = true;
                ogre.OnTreasureStolen();
            }
        }
    }
}