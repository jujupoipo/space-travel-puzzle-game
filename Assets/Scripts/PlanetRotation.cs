using UnityEngine;

public class PlanetRotation : MonoBehaviour
{
    public float degreesPerSecond = 4f;

    private void Update()
    {
        // turn the planet around its own axis
        transform.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
