using UnityEngine;

public class StealKey : MonoBehaviour
{
    public LineOfSight enemyLOS; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!enemyLOS.CanSeePlayer(enemyLOS.transform, other.transform))
            {
                other.GetComponent<PlayerInventory>().GrabKey();
                Destroy(gameObject); 
            }
            else
            {
                Debug.Log("¡El cura te vio intentando robar!");
            }
        }
    }
}