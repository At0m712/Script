using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeShopUI : MonoBehaviour
{
    [Header("Éléments Aimant")]
    public Image[] carresAimant; 
    public TMP_Text textePrixAimant;
    public TMP_Text texteInfoAimant; // NOUVEAU : Texte "14 sec -> 16 sec"
    public Button boutonAmeliorerAimant;

    [Header("Éléments Multiplicateur X2")]
    public Image[] carresX2;
    public TMP_Text textePrixX2;
    public TMP_Text texteInfoX2; // NOUVEAU
    public Button boutonAmeliorerX2;

    [Header("Éléments Taux d'Apparition")]
    public Image[] carresSpawn;
    public TMP_Text textePrixSpawn;
    public TMP_Text texteInfoSpawn; // NOUVEAU
    public Button boutonAmeliorerSpawn;

    [Header("Réglages")]
    public int prixBaseUnitaire = 50; 
    public int niveauMax = 10; 

    [Header("Couleurs des Carrés")]
    public Color couleurAllume = Color.yellow; 
    public Color couleurEteint = new Color(0.2f, 0.2f, 0.2f, 0.5f); 

    void OnEnable()
    {
        ActualiserBoutiqueUpgrades();
    }

    public void ActualiserBoutiqueUpgrades()
    {
        if (SaveManager.instance == null) return;

        // 1. Ligne Aimant
        int nivAimant = SaveManager.instance.data.niveauAimant;
        ActualiserLigne(nivAimant, carresAimant, textePrixAimant, boutonAmeliorerAimant);
        ActualiserTexteInfo(texteInfoAimant, nivAimant, true); // true = c'est du temps (sec)

        // 2. Ligne X2
        int nivX2 = SaveManager.instance.data.niveauX2;
        ActualiserLigne(nivX2, carresX2, textePrixX2, boutonAmeliorerX2);
        ActualiserTexteInfo(texteInfoX2, nivX2, true);

        // 3. Ligne Taux de Spawn
        int nivSpawn = SaveManager.instance.data.niveauSpawnPowerUp;
        ActualiserLigne(nivSpawn, carresSpawn, textePrixSpawn, boutonAmeliorerSpawn);
        ActualiserTexteInfo(texteInfoSpawn, nivSpawn, false); // false = c'est un pourcentage (%)
    }

    // Fonction qui met à jour les carrés visuels et le bouton d'achat
    private void ActualiserLigne(int niveauActuel, Image[] tableauCarres, TMP_Text txtPrix, Button bouton)
    {
        for (int i = 0; i < tableauCarres.Length; i++)
        {
            if (tableauCarres[i] != null)
            {
                tableauCarres[i].color = (i < niveauActuel) ? couleurAllume : couleurEteint;
            }
        }

        if (niveauActuel >= niveauMax)
        {
            if (txtPrix != null) txtPrix.text = "MAX";
            bouton.interactable = false;
        }
        else
        {
            int prixSuivant = niveauActuel * prixBaseUnitaire;
            if (txtPrix != null) txtPrix.text = prixSuivant.ToString();
            bouton.interactable = (GameManager.argentTotal >= prixSuivant);
        }
    }

    // --- NOUVELLE FONCTION : Calcule et affiche le texte "Actuel -> Suivant" ---
    private void ActualiserTexteInfo(TMP_Text txtInfo, int niveauActuel, bool estTemps)
    {
        if (txtInfo == null) return;

        // Formules mathématiques exactes de tes autres scripts
        float valeurActuelle = estTemps ? (10f + (niveauActuel - 1) * 2f) : (5f + (niveauActuel - 1) * 2f);
        string unite = estTemps ? " sec" : " %";

        if (niveauActuel >= niveauMax)
        {
            // S'il est niveau Max, on n'affiche plus la flèche
            txtInfo.text = "Max : " + valeurActuelle + unite;
        }
        else
        {
            // On calcule la valeur de l'amélioration suivante
            float valeurSuivante = estTemps ? (10f + niveauActuel * 2f) : (5f + niveauActuel * 2f);
            txtInfo.text = valeurActuelle + unite + " -> " + valeurSuivante + unite;
        }
    }

    // --- FONCTIONS CLICS BOUTONS ---

    public void AcheterUpgradeAimant() { TenterAchatUpgrade(ref SaveManager.instance.data.niveauAimant); }
    public void AcheterUpgradeX2() { TenterAchatUpgrade(ref SaveManager.instance.data.niveauX2); }
    public void AcheterUpgradeSpawnPowerUp() { TenterAchatUpgrade(ref SaveManager.instance.data.niveauSpawnPowerUp); }

    private void TenterAchatUpgrade(ref int niveauAAmeliorer)
    {
        int prix = niveauAAmeliorer * prixBaseUnitaire;

        if (GameManager.instance != null && GameManager.instance.DepenserArgent(prix))
        {
            niveauAAmeliorer += 1;
            SaveManager.instance.SauvegarderPartie();
            
            ActualiserBoutiqueUpgrades();
            if (ThemeManager.instance != null) ThemeManager.instance.MettreAJourArgentUI();
        }
    }
}