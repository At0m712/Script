using UnityEngine;

public class LigneArriveeSpeedrun : MonoBehaviour
{
    private bool estFranchie = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !estFranchie)
        {
            estFranchie = true;
            
            // On appelle notre nouveau ChronoManager unique
            if (ChronoManager.instance != null)
            {
                ChronoManager.instance.ArreterChrono();
            }
        }
    }
}