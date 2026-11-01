
using UnityEngine;

public class PlanetEnemy : MonoBehaviour
{
    public int health = 3;

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log("Enemy hit! Health remaining: " + health);

        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }
}
