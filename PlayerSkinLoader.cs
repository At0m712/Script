using UnityEngine;

public class PlayerSkinLoader : MonoBehaviour
{
    // On crée une instance pour que la boutique (ThemeManager) puisse lui parler facilement
    public static PlayerSkinLoader instance;

    [Header("Glisse ici les modèles enfants de ta capsule")]
    public GameObject[] modelesSkins;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        // Au lancement du jeu, on lit la sauvegarde et on affiche le bon skin
        AppliquerSkin(SaveManager.instance.data.skinEquipe);
    }

    // Cette fonction allume 1 skin précis et éteint tous les autres
    public void AppliquerSkin(int indexDuSkin)
    {
        // Sécurité : on vérifie que le numéro demandé existe bien dans le tableau
        if (modelesSkins == null || indexDuSkin < 0 || indexDuSkin >= modelesSkins.Length) return;

        for (int i = 0; i < modelesSkins.Length; i++)
        {
            if (modelesSkins[i] != null)
            {
                // Si 'i' correspond au skin demandé, on l'active (true), sinon on le désactive (false)
                modelesSkins[i].SetActive(i == indexDuSkin);
            }
        }
    }
}