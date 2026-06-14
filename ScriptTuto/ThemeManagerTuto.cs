using UnityEngine;

public class ThemeManagerTuto : MonoBehaviour
{
    public static ThemeManagerTuto instance;

    [System.Serializable] 
    public struct Theme 
    { 
        public string nom; 
        public Material skybox; 
        public Material eau; 
        
        // Conservés uniquement pour ne pas casser tes données existantes dans l'inspecteur
        public int prix; 
        public Sprite imageCarrousel;
    }
    
    [System.Serializable]
    public struct SkinJoueur
    {
        public string nom;
        public GameObject modelePrefab; 
        
        // Conservés uniquement pour ne pas casser tes données existantes dans l'inspecteur
        public int prix;
        public Sprite imageCarrousel;
    }

    [Header("Bases de données")]
    public SkinJoueur[] mesSkins; 
    public Theme[] mesThemes;  

    [Header("Mise en Scène")]
    public Renderer eauRenderer; 

    // Variable conservée car le GameManager l'utilise pour savoir si on peut faire pause
    public static bool jeuEstLance = false; 

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        // 1. On lit la sauvegarde pour savoir ce que le joueur a équipé
        int indexSkin = SaveManager.instance.data.skinEquipe;
        int indexTheme = SaveManager.instance.data.themeEquipe;

        // 2. On applique le décor du monde
        AppliquerThemeMonde(indexTheme);

        // 3. On applique le skin sur le joueur
        if (PlayerSkinLoader.instance != null)
        {
            PlayerSkinLoader.instance.AppliquerSkin(indexSkin);
        }
    }

    public void AppliquerThemeMonde(int index)
    {
        // Sécurité : si l'index n'existe pas, on annule
        if (index < 0 || index >= mesThemes.Length) return;

        // On change le ciel
        if (mesThemes[index].skybox != null) 
        {
            RenderSettings.skybox = mesThemes[index].skybox;
        }

        // On change la texture de l'eau
        if (eauRenderer != null && mesThemes[index].eau != null) 
        {
            eauRenderer.sharedMaterial = mesThemes[index].eau;
        }
    }
}