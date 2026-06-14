using UnityEngine;
using System.Collections.Generic;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager instance;

    [Header("Ce que tu veux faire apparaître")]
    public GameObject[] prefabsEnnemis; 
    public GameObject prefabPiece;
    
    // C'est ce tableau (celui de ta photo) qu'on va enfin utiliser !
    public GameObject[] prefabsPowerUps; 
    
    [Tooltip("Chance de base dans l'inspecteur (Testable à 100%)")]
    [Range(0, 100)] public float chancePowerUp = 5f;

    [Header("Réglages de Base")]
    public int baseEnnemis = 5; 
    public int basePieces = 10;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    public void FaireApparaitreDans(List<GameObject> nouveauxModules, int niveauDifficulte, GateController porteAssociee)
    {
        List<Transform> pointsApparition = new List<Transform>();

        foreach (GameObject module in nouveauxModules)
        {
            Transform[] tousLesEnfants = module.GetComponentsInChildren<Transform>();
            foreach (Transform enfant in tousLesEnfants)
            {
                if (enfant.CompareTag("SpawnPoint"))
                {
                    pointsApparition.Add(enfant);
                }
            }
        }

        if (pointsApparition.Count == 0) return;

        // Mélange des points d'apparition
        for (int i = 0; i < pointsApparition.Count; i++)
        {
            Transform temp = pointsApparition[i];
            int randomIndex = Random.Range(i, pointsApparition.Count);
            pointsApparition[i] = pointsApparition[randomIndex];
            pointsApparition[randomIndex] = temp;
        }

        int niveauPlafonne = Mathf.Min(niveauDifficulte, 6);
        int nbEnnemis = baseEnnemis + (niveauPlafonne - 1);
        int nbPieces = basePieces + (niveauPlafonne * 2);

        int indexActuel = 0;    
        
        // --- 1. APPARITION DES ENNEMIS ---
        if (prefabsEnnemis != null && prefabsEnnemis.Length > 0)
        {
            for (int i = 0; i < nbEnnemis; i++)
            {
                if (indexActuel >= pointsApparition.Count) break;
                
                int indexAleatoire = Random.Range(0, prefabsEnnemis.Length);
                GameObject prefabChoisi = prefabsEnnemis[indexAleatoire];
                
                GameObject nouvelEnnemi = Instantiate(prefabChoisi, pointsApparition[indexActuel].position, pointsApparition[indexActuel].rotation, pointsApparition[indexActuel]);
                Enemy scriptEnnemi = nouvelEnnemi.GetComponentInChildren<Enemy>();

                if (porteAssociee != null && scriptEnnemi != null)
                {
                    scriptEnnemi.maPorte = porteAssociee;
                    porteAssociee.AjouterEnnemi();
                }
                
                indexActuel++;
            }
        }

        // --- CALCUL DE LA CHANCE DE POWER-UP ---
        float chanceFinale = chancePowerUp; 
        if (SaveManager.instance != null)
        {
            int niveauSpawn = SaveManager.instance.data.niveauSpawnPowerUp;
            chanceFinale += (niveauSpawn - 1) * 2f;
        }

        // --- 2. APPARITION DES PIÈCES ET POWER-UPS ---
        for (int i = 0; i < nbPieces; i++)
        {
            if (indexActuel >= pointsApparition.Count) break;
            
            // On vérifie si on gagne à la loterie ET si tu as bien mis des prefabs dans l'inspecteur
            if (Random.Range(0f, 100f) <= chanceFinale && prefabsPowerUps != null && prefabsPowerUps.Length > 0)
            {
                // On pioche un des 3 prefabs de ta capture d'écran !
                int indexAleatoire = Random.Range(0, prefabsPowerUps.Length);
                GameObject powerUpChoisi = prefabsPowerUps[indexAleatoire];
                
                // On l'instancie normalement car tes PowerUps se détruisent eux-mêmes avec Destroy()
                Instantiate(powerUpChoisi, pointsApparition[indexActuel].position, pointsApparition[indexActuel].rotation, pointsApparition[indexActuel]);
            }
            else
            {
                // S'il n'y a pas de Power-Up, on met une pièce normale (via l'ObjectPooler)
                if (ObjectPooler.instance != null)
                {
                    ObjectPooler.instance.SortirObjet("Piece", pointsApparition[indexActuel].position, pointsApparition[indexActuel].rotation, pointsApparition[indexActuel]);
                }
            }
            
            indexActuel++;
        }
    }
}