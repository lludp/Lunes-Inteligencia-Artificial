using UnityEngine;

public class Document : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerInventory>().AddDocument();
            Destroy(gameObject); 
        }
    }
}