using UnityEngine;
using GoogleMobileAds.Api;
using System;
using TMPro;

public enum TypeRecompense { Rien, Resurrection, Argent }

public class AdMobManager : MonoBehaviour
{
    public static AdMobManager instance;

    [Header("Interface (UI)")]
    public TMP_Text textePubsRestantes; 

    [Header("Identifiants AdMob")]
    // REMPLACE CES ID DE TEST PAR TES VRAIS ID AVANT LA PUBLICATION !
    private string adUnitIdPieces = "ca-app-pub-5127315609635046/5924930114"; 
    private string adUnitIdResurrection = "ca-app-pub-5127315609635046/6880411762"; 

    // Les deux "lecteurs" de vidéo indépendants
    private RewardedAd rewardedAdPieces;
    private RewardedAd rewardedAdResurrection;

    private bool recompenseMeritee = false; 
    private bool feuVertRecompense = false; 

    private TypeRecompense recompenseEnAttente = TypeRecompense.Rien;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        // On affiche le bon chiffre dès que le jeu se lance
        MettreAJourTextePubs(); 
    }

    // Fonction appelée par le ConsentManager
    public void LoadRewardedAd()
    {
        // On demande à charger les DEUX pubs en arrière-plan
        LoadAdPieces();
        LoadAdResurrection();
    }

    private void LoadAdPieces()
    {
        if (rewardedAdPieces != null)
        {
            rewardedAdPieces.Destroy();
            rewardedAdPieces = null;
        }

        var adRequest = new AdRequest(); 
        RewardedAd.Load(adUnitIdPieces, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null) return;
            rewardedAdPieces = ad;
            // On lie les événements et on lui dit de recharger cette pub-là à la fin
            RegisterEventHandlers(rewardedAdPieces, LoadAdPieces);
        });
    }

    private void LoadAdResurrection()
    {
        if (rewardedAdResurrection != null)
        {
            rewardedAdResurrection.Destroy();
            rewardedAdResurrection = null;
        }

        var adRequest = new AdRequest(); 
        RewardedAd.Load(adUnitIdResurrection, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null) return;
            rewardedAdResurrection = ad;
            // On lie les événements et on lui dit de recharger cette pub-là à la fin
            RegisterEventHandlers(rewardedAdResurrection, LoadAdResurrection);
        });
    }

    private void VerifierDatePubs()
    {
        if (SaveManager.instance == null) return;

        System.DateTime dateReelle = System.DateTime.Now;
        
        if (QuestManager.instance != null)
        {
            dateReelle += QuestManager.instance.differenceHeureInternet;
        }

        string dateAujourdhui = dateReelle.ToString("yyyy-MM-dd");
        
        if (SaveManager.instance.data.datePubPieces != dateAujourdhui)
        {
            SaveManager.instance.data.datePubPieces = dateAujourdhui;
            SaveManager.instance.data.pubsPiecesVuesAujourdhui = 0; 
            SaveManager.instance.SauvegarderPartie();
        }
    }

    public void MettreAJourTextePubs()
    {
        VerifierDatePubs(); 

        if (textePubsRestantes != null && SaveManager.instance != null)
        {
            int reste = 3 - SaveManager.instance.data.pubsPiecesVuesAujourdhui;
            if (reste < 0) reste = 0; 
            textePubsRestantes.text = reste.ToString(); 
        }
    }

    public void ShowAdPourPieces()
    {
        VerifierDatePubs(); 

        if (SaveManager.instance.data.pubsPiecesVuesAujourdhui >= 3)
        {
            Debug.Log("Limite de 3 pubs atteinte pour aujourd'hui !");
            return; 
        }

        recompenseEnAttente = TypeRecompense.Argent;
        LancerLaVideo(rewardedAdPieces);
    }

    public void ShowAdPourResurrection()
    {
        recompenseEnAttente = TypeRecompense.Resurrection;
        LancerLaVideo(rewardedAdResurrection);
    }

    // Cette fonction prend maintenant en paramètre la bonne pub à afficher
    private void LancerLaVideo(RewardedAd adAVisualiser)
    {
        if (adAVisualiser != null && adAVisualiser.CanShowAd())
        {
            recompenseMeritee = false; 
            adAVisualiser.Show((Reward reward) =>
            {
                recompenseMeritee = true; 
            });
        }
        else
        {
            Debug.Log("La vidéo n'est pas encore prête !");
            recompenseEnAttente = TypeRecompense.Rien; 
        }
    }

    void Update()
    {
        if (feuVertRecompense)
        {
            feuVertRecompense = false; 
            
            if (GameManager.instance != null)
            {
                if (recompenseEnAttente == TypeRecompense.Argent)
                {
                    GameManager.instance.AjouterArgent(25); 

                    SaveManager.instance.data.pubsPiecesVuesAujourdhui++;
                    SaveManager.instance.SauvegarderPartie();
                    
                    MettreAJourTextePubs();
                    
                    if (ThemeManager.instance != null)
                    {
                        ThemeManager.instance.RafraichirAffichageArgent();
                    }

                    if (ThemeManager.jeuEstLance == false)
                    {
                        Time.timeScale = 0f;
                        if (GameManager.instance.joueurRb != null)
                        {
                            GameManager.instance.joueurRb.linearVelocity = Vector3.zero;
                        }
                    }
                }
                else if (recompenseEnAttente == TypeRecompense.Resurrection)
                {
                    GameManager.instance.ExecuterResurrection(); 
                }
            }
            
            recompenseEnAttente = TypeRecompense.Rien;
        }
    }

    // Gestionnaire dynamique qui recharge la bonne pub à la fin
    private void RegisterEventHandlers(RewardedAd ad, Action fonctionDeRechargement)
    {
        ad.OnAdFullScreenContentClosed += () => 
        { 
            if (recompenseMeritee)
            {
                feuVertRecompense = true; 
                recompenseMeritee = false; 
            }
            else 
            {
                recompenseEnAttente = TypeRecompense.Rien;
            }
            
            // On recharge la pub spécifique qui vient d'être regardée
            fonctionDeRechargement?.Invoke(); 
        };
        
        ad.OnAdFullScreenContentFailed += (AdError error) => { fonctionDeRechargement?.Invoke(); };
    }
}