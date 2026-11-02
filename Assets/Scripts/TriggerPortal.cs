using System.Collections.Generic;
using UnityEngine;

public class TriggerPortal : MonoBehaviour
{
    [Header("Destination")]
    public Transform targetSpawnPoint; // The spawn point on the other planet

    [Header("Settings")]
    public string playerTag = "Player"; // Make sure your player object has this tag

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            Teleport(other.transform);
        }
    }

    private void Teleport(Transform playerTransform)
    {
        if (targetSpawnPoint == null) return;

        Rigidbody rb = playerTransform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        playerTransform.position = targetSpawnPoint.position;

        Vector3 up = targetSpawnPoint.up;
        playerTransform.rotation = Quaternion.FromToRotation(playerTransform.up, up) * playerTransform.rotation;

        // Sécurisation pour éviter l'erreur NullReference dans la console
        if (targetSpawnPoint.name.Contains("Planet2"))
        {
            DreamGardenGenerator dreamGen = FindAnyObjectByType<DreamGardenGenerator>();
            if (dreamGen != null)
            {
                dreamGen.GenerateDreamGarden();

                DreamPuzzleController puzzleCtrl = FindAnyObjectByType<DreamPuzzleController>();
                if (puzzleCtrl != null)
                {
                    List<DreamLantern> lanterns = new List<DreamLantern>(dreamGen.GetComponentsInChildren<DreamLantern>());
                    List<DreamPhaseGate> gates = new List<DreamPhaseGate>(dreamGen.GetComponentsInChildren<DreamPhaseGate>());

                    if (lanterns != null && lanterns.Count > 0 && lanterns[lanterns.Count - 1] != null)
                    {
                        Vector3 exitPos = lanterns[lanterns.Count - 1].transform.position;
                        puzzleCtrl.Initialize(lanterns, gates, exitPos);
                        Debug.Log("SUCCESS: Puzzle initialized via Portal!");
                    }
                }
            }
        }

        Debug.Log("Teleported via portal!");
    }
}