using UnityEngine;

public class MenuModeManager : MonoBehaviour
{
    // Cette fonction doit être assignée UNIQUEMENT au gros bouton "JOUER"
    public void LancerLaPartie()
    {
        // 1. On regarde quel mode a été sélectionné dans le volet
        string modeChoisi = PlayerPrefs.GetString("ModeChoisi", "Normal");

        // 2. On lance la bonne fonction en fonction de la sauvegarde
        if (modeChoisi == "Normal")
        {
            if (ThemeManager.instance != null) ThemeManager.instance.BoutonJouer();
        }
        else if (modeChoisi == "Speedrun")
        {
            if (ThemeManager.instance != null) ThemeManager.instance.BoutonJouerSpeedrun();
        }
        else if (modeChoisi == "1v1")
        {
            if (MatchmakingManager.instance != null) MatchmakingManager.instance.ChercherUnePartie1v1();
        }
    }
}