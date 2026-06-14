using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    private TextMeshProUGUI textMesh;
    private int dernierCompte = -1; 

    void Start()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (LevelGenerator.instance != null && LevelGenerator.instance.porteActuelle != null)
        {
            int count = Mathf.Max(0, LevelGenerator.instance.porteActuelle.ennemisRestants);
            
            if (count != dernierCompte)
            {
                // OPTIMISATION : SetText évite de créer des "String Garbage"
                textMesh.SetText("Ennemis : {0}", count);
                dernierCompte = count; 
            }
        }
    }
}