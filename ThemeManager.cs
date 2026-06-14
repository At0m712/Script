using UnityEngine;
using TMPro; 
using UnityEngine.UI; 
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;

public class ThemeManager : MonoBehaviour
{
    public static ThemeManager instance;

    [System.Serializable] 
    public struct Theme 
    { 
        public string nom; 
        public Material skybox; 
        public Material eau; 
        public int prix; 
        public Sprite imageCarrousel;
    }
    
    [System.Serializable]
    public struct SkinJoueur
    {
        public string nom;
        public GameObject modelePrefab; 
        public int prix;
        public Sprite imageCarrousel;
    }

    [Header("Listes Boutique")]
    public SkinJoueur[] mesSkins; 
    public Theme[] mesThemes;  

    [Header("Carrousel Images")]
    public Image imageCentraleSkin;   
    public Image imageCentraleTheme; // NOUVEAU : L'image dédiée aux décors/mondes

    [Header("Mise en Scène")]
    public Renderer eauRenderer; 
    public Transform spawnPointSkin; 

    [Header("Système Caméras")]
    public GameObject cameraMenu; 
    public GameObject cameraJeu;  

    [Header("Panels")]
    public GameObject panelPersonnalisation; 
    public GameObject panelHUDJeu;           
    public GameObject boutonsMenuPrincipal;  

    [Header("Onglets (Icônes)")]
    public Transform boutonOngletPerso; 
    public Transform boutonOngletMonde; 
    public Transform boutonOngletPowerUp; 

    [Header("Panneaux de la Boutique")]
    public GameObject panelAmeliorations; 

    [Header("Zone Achat (Carrousel)")]
    public TMP_Text texteNomItem;       
    public TMP_Text textePrixSkin;      
    public Button boutonActionSkin;     
    public TMP_Text texteBoutonAction;  
    
    [Header("Autres Infos")]
    public TMP_Text texteArgent;        
    public TMP_Text texteNiveau;

    public static bool jeuEstLance = false; 
    
    private int categorieActuelle = 0; 
    private int indexSkin = 0;   
    private int indexTheme = 0;  

    private Vector2 debutTouche;
    private Vector2 finTouche;
    private bool swipeValide = false;

    void Awake()
    {
        if (instance == null) instance = this;
    }

   void Start()
    {
        indexSkin = SaveManager.instance.data.skinEquipe;
        indexTheme = SaveManager.instance.data.themeEquipe;

        AppliquerThemeMonde(indexTheme);
        MettreAJourArgentUI();

        if (jeuEstLance) 
        {
            int niveauActuel = SaveManager.instance.data.niveau;
            
            if (texteNiveau != null) 
            {
                // Parfait ! On récupère le texte et on insère la variable
                string texteTraduit = LocalizationSettings.StringDatabase.GetLocalizedString("TexteUI", "JEU_NIVEAU");
                texteNiveau.text = string.Format(texteTraduit, niveauActuel);
            }
            
            LancerJeuDirect();
        }
        else 
        {
            SaveManager.instance.data.niveau = 1;
            SaveManager.instance.SauvegarderPartie();

            if (texteNiveau != null) 
            {
                // CORRECTION : On utilise la même logique robuste pour le niveau 1 !
                string texteTraduit = LocalizationSettings.StringDatabase.GetLocalizedString("TexteUI", "JEU_NIVEAU");
                texteNiveau.text = string.Format(texteTraduit, 1);
            }

            AfficherMenuAccueil();
            ChangerOnglet(0); 
        }
    }

    void Update()
    {
        if (jeuEstLance) return;
        if (boutonsMenuPrincipal != null && boutonsMenuPrincipal.activeSelf == true) return;
        if (panelPersonnalisation.activeSelf == false) return;
        
        if (categorieActuelle == 2) return; 

        if (Pointer.current == null) return;

        if (Pointer.current.press.wasPressedThisFrame)
        {
            Vector2 positionTouche = Pointer.current.position.ReadValue();

            // NOUVEAU : Le swipe marche sur l'image du skin OU l'image du thème selon ce qui est affiché
            bool toucheSurSkin = (imageCentraleSkin != null && imageCentraleSkin.gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(imageCentraleSkin.rectTransform, positionTouche, null));
            bool toucheSurTheme = (imageCentraleTheme != null && imageCentraleTheme.gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(imageCentraleTheme.rectTransform, positionTouche, null));

            if (toucheSurSkin || toucheSurTheme)
            {
                debutTouche = positionTouche;
                swipeValide = true; 
            }
            else
            {
                swipeValide = false; 
            }
        }

        if (Pointer.current.press.wasReleasedThisFrame && swipeValide)
        {
            finTouche = Pointer.current.position.ReadValue();
            CalculerSwipe();
            swipeValide = false; 
        }
    }

    public void ChangerOnglet(int categorie)
    {
        categorieActuelle = categorie;

        if(boutonOngletPerso) boutonOngletPerso.localScale = Vector3.one;
        if(boutonOngletMonde) boutonOngletMonde.localScale = Vector3.one;
        if(boutonOngletPowerUp) boutonOngletPowerUp.localScale = Vector3.one;

        if (categorie == 0 && boutonOngletPerso) boutonOngletPerso.localScale = new Vector3(1.1f, 1.1f, 1.1f);
        if (categorie == 1 && boutonOngletMonde) boutonOngletMonde.localScale = new Vector3(1.1f, 1.1f, 1.1f);
        if (categorie == 2 && boutonOngletPowerUp) boutonOngletPowerUp.localScale = new Vector3(1.1f, 1.1f, 1.1f);

        if (categorie == 0) // SKINS
        {
            if (panelAmeliorations) panelAmeliorations.SetActive(false);
            ActiverElementsCarrousel(true);
            
            // NOUVEAU : On gère l'affichage des deux images séparées
            if (imageCentraleTheme) imageCentraleTheme.gameObject.SetActive(false);
            if (imageCentraleSkin) imageCentraleSkin.gameObject.SetActive(true);

            if(cameraMenu) cameraMenu.SetActive(true);
            if(cameraJeu) cameraJeu.SetActive(false);
            MontrerSkinJoueur(indexSkin); 
        }
        else if (categorie == 1) // MONDES
        {
            if (panelAmeliorations) panelAmeliorations.SetActive(false);
            ActiverElementsCarrousel(true);

            // NOUVEAU : On gère l'affichage des deux images séparées
            if (imageCentraleSkin) imageCentraleSkin.gameObject.SetActive(false);
            if (imageCentraleTheme) imageCentraleTheme.gameObject.SetActive(true);

            if(cameraMenu) cameraMenu.SetActive(false);
            if(cameraJeu) cameraJeu.SetActive(true);
            MontrerThemeMonde(indexTheme); 
        }
        else if (categorie == 2) // POWER-UPS
        {
            if (panelAmeliorations) panelAmeliorations.SetActive(true);
            ActiverElementsCarrousel(false); 

            if(cameraMenu) cameraMenu.SetActive(true);
            if(cameraJeu) cameraJeu.SetActive(false);
        }
        
        MettreAJourBoutonsBoutique();
    }

    void ActiverElementsCarrousel(bool actif)
    {
        // Géré dans ChangerOnglet pour les images
        if (texteNomItem) texteNomItem.gameObject.SetActive(actif);
        if (boutonActionSkin) boutonActionSkin.gameObject.SetActive(actif);
        if (textePrixSkin) textePrixSkin.transform.parent.gameObject.SetActive(actif); 
    }

    // --- NOUVEAU : Fonctions pour des flèches UI (si tu en utilises) ---
    public void BoutonCarrouselSuivant()
    {
        ChangerPrevisu(1);
    }

    public void BoutonCarrouselPrecedent()
    {
        ChangerPrevisu(-1);
    }
    // -------------------------------------------------------------------

    void CalculerSwipe()
    {
        float distanceX = finTouche.x - debutTouche.x;
        if (Mathf.Abs(distanceX) > 50)
        {
            ChangerPrevisu(distanceX > 0 ? -1 : 1);
        }
    }

    public void ChangerPrevisu(int direction)
    {
        if (categorieActuelle == 2) return; 

        if (categorieActuelle == 0)
        {
            indexSkin += direction;
            if (indexSkin >= mesSkins.Length) indexSkin = 0;
            if (indexSkin < 0) indexSkin = mesSkins.Length - 1;
            MontrerSkinJoueur(indexSkin);
        }
        else 
        {
            indexTheme += direction;
            if (indexTheme >= mesThemes.Length) indexTheme = 0;
            if (indexTheme < 0) indexTheme = mesThemes.Length - 1;
            MontrerThemeMonde(indexTheme);
        }
        MettreAJourBoutonsBoutique();
    }

    void MontrerSkinJoueur(int index)
    {
        if (PlayerSkinLoader.instance != null) PlayerSkinLoader.instance.AppliquerSkin(index);
        if (imageCentraleSkin != null)
        {
            if (mesSkins[index].imageCarrousel != null)
            {
                imageCentraleSkin.gameObject.SetActive(true);
                imageCentraleSkin.sprite = mesSkins[index].imageCarrousel;
                imageCentraleSkin.color = Color.white; 
                imageCentraleSkin.preserveAspect = true; 
            }
            else
            {
                imageCentraleSkin.gameObject.SetActive(true);
                imageCentraleSkin.color = new Color(1f, 1f, 1f, 0f); 
            }
        }
        if (texteNomItem) texteNomItem.text = mesSkins[index].nom;
    }

    // NOUVEAU : Cette fonction utilise maintenant la nouvelle imageCentraleTheme
    void MontrerThemeMonde(int index)
    {
        AppliquerThemeMonde(index);
        if (imageCentraleTheme != null)
        {
            if (mesThemes[index].imageCarrousel != null)
            {
                imageCentraleTheme.gameObject.SetActive(true);
                imageCentraleTheme.sprite = mesThemes[index].imageCarrousel;
                imageCentraleTheme.color = Color.white; 
                imageCentraleTheme.preserveAspect = true; 
            }
            else
            {
                imageCentraleTheme.gameObject.SetActive(true);
                imageCentraleTheme.color = new Color(1f, 1f, 1f, 0f);
            }
        }
        if (texteNomItem) texteNomItem.text = mesThemes[index].nom;
    }

    void AppliquerThemeMonde(int index)
    {
        if (index < 0 || index >= mesThemes.Length) return;
        if (mesThemes[index].skybox != null) RenderSettings.skybox = mesThemes[index].skybox;
        if (eauRenderer != null && mesThemes[index].eau != null) eauRenderer.sharedMaterial = mesThemes[index].eau;
    }

    public void MettreAJourBoutonsBoutique()
    {
        if (categorieActuelle == 2) return; 

        bool estDebloque = false;
        bool estEquipe = false; 
        int prix = 0;
        int monArgent = SaveManager.instance.data.argentTotal;

        if (categorieActuelle == 0)
        {
            estDebloque = SaveManager.instance.data.skinsDebloques.Contains(indexSkin);
            estEquipe = (indexSkin == SaveManager.instance.data.skinEquipe); 
            prix = mesSkins[indexSkin].prix;
        }
        else
        {
            estDebloque = SaveManager.instance.data.themesDebloques.Contains(indexTheme);
            estEquipe = (indexTheme == SaveManager.instance.data.themeEquipe); 
            prix = mesThemes[indexTheme].prix;
        }

        boutonActionSkin.onClick.RemoveAllListeners();

        // ✅ NOUVEAU : On récupère toutes les traductions de la boutique !
        // ⚠️ Assure-toi que "TexteUI" est bien le nom de ta table de traduction
        string texteAcquis = LocalizationSettings.StringDatabase.GetLocalizedString("TexteUI", "BOUTIQUE_ACQUIS");
        string texteEquipe = LocalizationSettings.StringDatabase.GetLocalizedString("TexteUI", "BOUTIQUE_EQUIPE");
        string texteEquiper = LocalizationSettings.StringDatabase.GetLocalizedString("TexteUI", "BOUTIQUE_EQUIPER");
        string texteAcheter = LocalizationSettings.StringDatabase.GetLocalizedString("TexteUI", "BOUTIQUE_ACHETER");

        if (estDebloque)
        {
            // On affiche "OWNED", "OBTENIDO" ou "ACQUIS" à la place du prix
            if (textePrixSkin) textePrixSkin.text = texteAcquis; 

            if (estEquipe)
            {
                // On affiche "EQUIPPED" etc...
                if (texteBoutonAction) texteBoutonAction.text = texteEquipe;
                boutonActionSkin.interactable = false; // On grise le bouton car c'est déjà équipé
            }
            else
            {
                // On affiche "EQUIP" etc...
                if (texteBoutonAction) texteBoutonAction.text = texteEquiper;
                boutonActionSkin.interactable = true; // Cliquable pour l'équiper
                boutonActionSkin.onClick.AddListener(ValiderChoix);
            }
        }
        else // S'il n'est pas acheté
        {
            if (textePrixSkin) textePrixSkin.text = "" + prix;
            
            // On affiche "BUY" ou "COMPRAR"
            if (texteBoutonAction) texteBoutonAction.text = texteAcheter;

            if (monArgent >= prix)
            {
                boutonActionSkin.interactable = true;
                boutonActionSkin.onClick.AddListener(AcheterItem);
            }
            else
            {
                boutonActionSkin.interactable = false; // Pas assez d'argent
            }
        }
    }
    public void AcheterItem()
    {
        if (categorieActuelle == 2) return; 

        int prix = (categorieActuelle == 0) ? mesSkins[indexSkin].prix : mesThemes[indexTheme].prix;

        if(GameManager.instance != null && GameManager.instance.DepenserArgent(prix))
        {
            if (categorieActuelle == 0) SaveManager.instance.data.skinsDebloques.Add(indexSkin);
            else                        SaveManager.instance.data.themesDebloques.Add(indexTheme);

            string nomDeLobjet = (categorieActuelle == 0) ? mesSkins[indexSkin].nom : mesThemes[indexTheme].nom;
            if (FirebaseManager.instance != null) FirebaseManager.instance.AnalyserAchat(nomDeLobjet, prix);
            
            SaveManager.instance.SauvegarderPartie();
            MettreAJourBoutonsBoutique();
            ValiderChoix(); 
            MettreAJourArgentUI(); 
        }
    }

    public void ValiderChoix()
    {
        if (categorieActuelle == 0) SaveManager.instance.data.skinEquipe = indexSkin;
        else                        SaveManager.instance.data.themeEquipe = indexTheme;
        
        SaveManager.instance.SauvegarderPartie();

        // NOUVEAU : On relance l'affichage du bouton pour qu'il passe sur "ÉQUIPÉ" tout de suite !
        MettreAJourBoutonsBoutique(); 
    }

    public void MettreAJourArgentUI()
    {
        if (texteArgent != null) 
            texteArgent.text = "" + SaveManager.instance.data.argentTotal;
    }

    public void RafraichirAffichageArgent()
    {
        MettreAJourArgentUI();
    }

    void AfficherMenuAccueil()
    {
        Time.timeScale = 0f; 
        if (panelHUDJeu) panelHUDJeu.SetActive(false); 
        if (panelPersonnalisation) panelPersonnalisation.SetActive(true);
        if (boutonsMenuPrincipal) boutonsMenuPrincipal.SetActive(true);
        if (texteNiveau) texteNiveau.gameObject.SetActive(false);
    }

    void LancerJeuDirect()
    {
        Time.timeScale = 1f; 
        if (panelPersonnalisation) panelPersonnalisation.SetActive(false);
        if (boutonsMenuPrincipal) boutonsMenuPrincipal.SetActive(false);
        if (panelHUDJeu) panelHUDJeu.SetActive(true);
        if (texteNiveau) texteNiveau.gameObject.SetActive(true);
        
        if(cameraMenu) cameraMenu.SetActive(false);
        if(cameraJeu) cameraJeu.SetActive(true);
    }

   public void BoutonJouer() 
    {
        string mode = PlayerPrefs.GetString("ModeChoisi", "Normal");

        if (mode == "1v1")
        {
            // 📝 LE POST-IT : On dit à Unity de lancer la recherche au prochain chargement
            PlayerPrefs.SetInt("AutoStartMatchmaking", 1);
            PlayerPrefs.Save(); 

            
            // On signale que le jeu n'a pas encore démarré (on reste sur l'UI du menu)
            jeuEstLance = false; 
        }
        else
        {
            // Mode Classique ou Speedrun
            jeuEstLance = true; 
        }

        // 🧹 LE MÉNAGE : On recharge la scène pour détruire tous les anciens objets/niveaux !
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    // --- NOUVELLE FONCTION : Utilisable par le Solo ET le 1v1 ---
    public void LancerTransitionVersJeu()
    {
         if (panelPersonnalisation) panelPersonnalisation.SetActive(false);
         if (boutonsMenuPrincipal) boutonsMenuPrincipal.SetActive(false);
         if (panelHUDJeu) panelHUDJeu.SetActive(true);
         
         // On cache le texte "Niveau 1" si on est en mode 1v1
         string modeChoisi = PlayerPrefs.GetString("ModeChoisi", "Normal");
         if (texteNiveau) texteNiveau.gameObject.SetActive(modeChoisi == "Normal");
         
         Time.timeScale = 1f; 

         // Le changement de caméra !
         if(cameraMenu != null && cameraJeu != null)
         {
             cameraJeu.SetActive(true); 
             CameraFollow scriptCam = cameraJeu.GetComponent<CameraFollow>();
             if (scriptCam != null) 
             {
                 scriptCam.DemarrerTransitionDePuis(cameraMenu.transform);
             }
             cameraMenu.SetActive(false); 
         }
    }
    public void BoutonJouerSpeedrun()
    {

        jeuEstLance = true;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    
    public void AfficherMasquerPanel() {
       bool active = !panelPersonnalisation.activeSelf;
       panelPersonnalisation.SetActive(active); 
       boutonsMenuPrincipal.SetActive(!active);
       
       if(active) {
           indexSkin = SaveManager.instance.data.skinEquipe;
           indexTheme = SaveManager.instance.data.themeEquipe;
           ChangerOnglet(categorieActuelle); 
       } else {
           indexSkin = SaveManager.instance.data.skinEquipe;
           indexTheme = SaveManager.instance.data.themeEquipe;
           AppliquerThemeMonde(indexTheme); 
           
           if (PlayerSkinLoader.instance != null)
           {
               PlayerSkinLoader.instance.AppliquerSkin(indexSkin);
           }
       }
    }
}