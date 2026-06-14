using UnityEngine;
using GoogleMobileAds.Ump.Api;
using GoogleMobileAds.Api;
using System.Collections.Generic;

public class ConsentManager : MonoBehaviour
{
    public static ConsentManager instance;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        // 1. Paramètres de requête de production (Zéro mode Debug/Test)
        ConsentRequestParameters request = new ConsentRequestParameters
        {
            TagForUnderAgeOfConsent = false // Le jeu cible un public ado/adulte
        };

        // 2. Demande de mise à jour des informations de consentement auprès de Google
        ConsentInformation.Update(request, (FormError error) =>
        {
            if (error != null) 
            { 
                Debug.LogError($"[ConsentManager] Erreur Update UMP : {error.ErrorCode} - {error.Message}"); 
                return; 
            }

            // 3. Charge et affiche le formulaire SEULEMENT si le joueur est en Europe et ne l'a jamais vu
            ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
            {
                if (formError != null) 
                { 
                    Debug.LogError($"[ConsentManager] Erreur LoadAndShow UMP : {formError.ErrorCode} - {formError.Message}"); 
                    return; 
                }

                // 4. Si l'utilisateur a donné son accord (ou est hors-UE), on initialise AdMob
                if (ConsentInformation.CanRequestAds())
                {
                    InitialiserAdMob();
                }
            });
        });

        // Sécurité : Si le joueur avait déjà consenti lors d'une session précédente, on lance directement
        if (ConsentInformation.CanRequestAds())
        {
            InitialiserAdMob();
        }
    }

    private void InitialiserAdMob()
    {
        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            // On prévient l'AdMobManager qu'il peut charger les publicités
            if (AdMobManager.instance != null)
            {
                AdMobManager.instance.LoadRewardedAd();
            }
        });
    }

    // --- LIEN DE RÉVOCATION (À lier à ton bouton dans les paramètres du jeu) ---
    public void BoutonModifierConsentement()
    {
        // On récupère le statut légal du formulaire auprès d'AdMob
        var status = ConsentInformation.PrivacyOptionsRequirementStatus;

        if (status == PrivacyOptionsRequirementStatus.Required)
        {
            ConsentForm.ShowPrivacyOptionsForm((FormError error) =>
            {
                if (error != null)
                {
                    Debug.LogError($"[ConsentManager] Impossible d'afficher le formulaire : {error.ErrorCode} - {error.Message}");
                    return;
                }
                
                Debug.Log("[ConsentManager] Le formulaire de modification du consentement s'est ouvert avec succès !");
            });
        }
        else
        {
            // Comportement normal en production : Si le joueur n'est pas en Europe, 
            // le statut ne sera pas "Required" et le bouton ne fera rien sans crasher.
            Debug.LogWarning($"[ConsentManager] Formulaire non disponible ou non requis pour ce joueur. Statut actuel : {status}");
        }
    }
}