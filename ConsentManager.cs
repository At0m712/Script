using UnityEngine;
using GoogleMobileAds.Ump.Api;
using GoogleMobileAds.Api;
using System.Collections.Generic;

public class ConsentManager : MonoBehaviour
{
    public static ConsentManager instance;

    void Awake()
    {
        // 🛡️ CORRECTION : Le script devient immortel et ne se lance qu'une seule fois !
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        ConsentRequestParameters request = new ConsentRequestParameters
        {
            TagForUnderAgeOfConsent = false 
        };

        ConsentInformation.Update(request, (FormError error) =>
        {
            if (error != null) 
            { 
                Debug.LogError($"[ConsentManager] Erreur Update UMP : {error.ErrorCode} - {error.Message}"); 
                return; 
            }

            ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
            {
                if (formError != null) 
                { 
                    Debug.LogError($"[ConsentManager] Erreur LoadAndShow UMP : {formError.ErrorCode} - {formError.Message}"); 
                    return; 
                }

                if (ConsentInformation.CanRequestAds())
                {
                    InitialiserAdMob();
                }
            });
        });

        if (ConsentInformation.CanRequestAds())
        {
            InitialiserAdMob();
        }
    }

    private void InitialiserAdMob()
    {
        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            if (AdMobManager.instance != null)
            {
                AdMobManager.instance.LoadRewardedAd();
            }
        });
    }

    public void BoutonModifierConsentement()
    {
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
            Debug.LogWarning($"[ConsentManager] Formulaire non disponible ou non requis pour ce joueur. Statut actuel : {status}");
        }
    }
}