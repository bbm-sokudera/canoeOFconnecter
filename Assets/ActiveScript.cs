using UnityEngine;

public class ActiveScript : MonoBehaviour
{
    [SerializeField] private Transform parentObject;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
            Transform target = parentObject.Find("Image");
            target.gameObject.SetActive(true);
    }
}
