using UnityEngine;

public class beeNest : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject beePrefab;
    [SerializeField] private float spawnInterval = 5f;   
    [SerializeField] private int maxBees = 10;            
    private float lastSpawnTime;

    void Update()
    {
       
        if (Time.time - lastSpawnTime >= spawnInterval && CountBees() < maxBees)
        {
            SpawnBee();
            lastSpawnTime = Time.time;
        }
    }

    void SpawnBee()
    {
        if (beePrefab == null) return;
        Instantiate(beePrefab, transform.position, Quaternion.identity);
    }

    int CountBees()
    {
   
        return FindObjectsOfType<beeControl>().Length;
    }
}