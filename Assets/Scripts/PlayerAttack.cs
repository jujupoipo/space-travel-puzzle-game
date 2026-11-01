
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public float attackRange = 4f;
    public int damage = 1;
    public KeyCode attackKey = KeyCode.F;

    void Update()
    {
        if (Input.GetKeyDown(attackKey))
        {
            Attack();
        }
    }

    void Attack()
    {
        Vector3 origin = transform.position + transform.up;
        Vector3 direction = transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, attackRange))
        {
            PlanetEnemy enemy = hit.collider.GetComponentInParent<PlanetEnemy>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Debug.Log("You attacked the enemy!");
            }
        }
    }
}
