using UnityEngine;

public class TextureScroller : MonoBehaviour
{
    [Header("Réglages")]
    public Vector2 vitesse = new Vector2(0.05f, 0.05f);
    
    private Vector2 offsetActuel;
    
    // --- OPTIMISATION 1 : On garde le matériau en mémoire ---
    private Material monMateriauCible;

    // --- OPTIMISATION 2 : On transforme les textes en ID numériques (beaucoup plus rapide pour le processeur) ---
    private int idMainTex;
    private int idBaseMap;
    private int idBumpMap;

    private bool aMainTex;
    private bool aBaseMap;
    private bool aBumpMap;

    void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        
        if (rend != null)
        {
            // On clone le matériau UNE SEULE FOIS au démarrage, pas 60 fois par seconde !
            monMateriauCible = rend.material; 

            // On prépare les ID secrets de la carte graphique
            idMainTex = Shader.PropertyToID("_MainTex");
            idBaseMap = Shader.PropertyToID("_BaseMap");
            idBumpMap = Shader.PropertyToID("_BumpMap");

            // On vérifie une bonne fois pour toutes quelles textures existent
            aMainTex = monMateriauCible.HasProperty(idMainTex);
            aBaseMap = monMateriauCible.HasProperty(idBaseMap);
            aBumpMap = monMateriauCible.HasProperty(idBumpMap);
        }
    }

    void Update()
    {
        if (monMateriauCible == null) return;

        // 1. Calcul du mouvement
        offsetActuel += vitesse * Time.deltaTime;

        // 2. On applique le décalage (ultra-rapide grâce aux ID numériques et booléens)
        if (aMainTex) monMateriauCible.SetTextureOffset(idMainTex, offsetActuel);
        if (aBaseMap) monMateriauCible.SetTextureOffset(idBaseMap, offsetActuel);
        if (aBumpMap) monMateriauCible.SetTextureOffset(idBumpMap, offsetActuel);
    }
}