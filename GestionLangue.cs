using UnityEngine;
using UnityEngine.Localization.Settings;
using System.Collections;

public class GestionLangue : MonoBehaviour
{
    private bool estEnChangement = false;

    // Cette fonction sera appelée par ton bouton unique
    public void BasculerLangue()
    {
        if (estEnChangement) return;
        StartCoroutine(BasculerLangueRoutine());
    }

    private IEnumerator BasculerLangueRoutine()
    {
        estEnChangement = true;

        // 1. On attend que le système de langue soit prêt (Très important, tu as bien fait !)
        yield return LocalizationSettings.InitializationOperation;

        // 2. On récupère la liste des langues et la langue actuellement affichée
        var toutesLesLangues = LocalizationSettings.AvailableLocales.Locales;
        var langueActuelle = LocalizationSettings.SelectedLocale;

        // 3. On trouve où on en est dans la liste (0, 1, 2...)
        int indexActuel = toutesLesLangues.IndexOf(langueActuelle);

        // 4. On calcule la suivante de façon dynamique !
        // Le modulo (%) permet de revenir à 0 quand on dépasse la fin de la liste.
        int indexSuivant = (indexActuel + 1) % toutesLesLangues.Count;

        // 5. On applique la nouvelle langue
        LocalizationSettings.SelectedLocale = toutesLesLangues[indexSuivant];

        estEnChangement = false;
    }
}