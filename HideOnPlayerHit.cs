using UnityEngine;

public class HideOnPlayerHit : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("HideOnPlayerHit triggered by: " + other.name);
        if (other.CompareTag("Player"))
        {
            gameObject.SetActive(false);
        }
    }
}
