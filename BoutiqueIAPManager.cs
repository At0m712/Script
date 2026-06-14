using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension; 
using TMPro; // NOUVEAU : Pour gérer tes textes

public class BoutiqueIAPManager : MonoBehaviour, IStoreListener
{
    private static IStoreController storeController;
    private static IExtensionProvider storeExtensionProvider;

    [Header("--- PACK 1 ---")]
    public string idPack1 = "pack_pieces_180"; 
    public int recompensePack1 = 180;
    public TMP_Text textePrixPack1; // NOUVEAU : Le texte affiché sur ton bouton

    [Header("--- PACK 2 ---")]
    public string idPack2 = "pack_pieces_450"; 
    public int recompensePack2 = 450;
    public TMP_Text textePrixPack2; // NOUVEAU : Le texte affiché sur ton bouton

    [Header("--- PACK 3 ---")]
    public string idPack3 = "pack_pieces_500"; 
    public int recompensePack3 = 500;
    public TMP_Text textePrixPack3; // NOUVEAU : Le texte affiché sur ton bouton

    void Start()
    {
        if (storeController == null)
        {
            InitialiserAchats();
        }
        else
        {
            // Si on est déjà connecté en revenant sur le menu, on met à jour direct
            AfficherPrixLocalises(); 
        }
    }

    void InitialiserAchats()
    {
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        
        builder.AddProduct(idPack1, ProductType.Consumable); 
        builder.AddProduct(idPack2, ProductType.Consumable); 
        builder.AddProduct(idPack3, ProductType.Consumable); 
        
        UnityPurchasing.Initialize(this, builder);
    }

    // ==========================================
    // NOUVEAU : AFFICHAGE DES PRIX
    // ==========================================
    void AfficherPrixLocalises()
    {
        if (storeController == null) return;

        if (textePrixPack1 != null)
            textePrixPack1.text = storeController.products.WithID(idPack1).metadata.localizedPriceString;

        if (textePrixPack2 != null)
            textePrixPack2.text = storeController.products.WithID(idPack2).metadata.localizedPriceString;

        if (textePrixPack3 != null)
            textePrixPack3.text = storeController.products.WithID(idPack3).metadata.localizedPriceString;
    }

    // ==========================================
    // QUAND LE MAGASIN EST INITIALISÉ AVEC SUCCÈS
    // ==========================================
    // ==========================================
    // QUAND LE MAGASIN EST INITIALISÉ AVEC SUCCÈS
    // ==========================================
    // ==========================================
    // QUAND LE MAGASIN EST INITIALISÉ AVEC SUCCÈS
    // ==========================================
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        storeExtensionProvider = extensions;

        // Dès que Google Play est connecté, on affiche les vrais prix !
        AfficherPrixLocalises();
    }

    // ==========================================
    // FONCTIONS D'ACHAT (Boutons UI)
    // ==========================================
    public void AcheterPack1() { LancerPaiement(idPack1); }
    public void AcheterPack2() { LancerPaiement(idPack2); }
    public void AcheterPack3() { LancerPaiement(idPack3); }

    private void LancerPaiement(string idDuPack)
    {
        if (storeController != null)
        {
            storeController.InitiatePurchase(idDuPack);
        }
        else
        {
            Debug.LogError("Erreur : Impossible de se connecter au magasin d'achats.");
        }
    }

    // ==========================================
    // QUAND LE PAIEMENT EST RÉUSSI PAR GOOGLE
    // ==========================================
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        string produitAchete = args.purchasedProduct.definition.id;

        if (string.Equals(produitAchete, idPack1, System.StringComparison.Ordinal))
            DistribuerPieces(recompensePack1);
        else if (string.Equals(produitAchete, idPack2, System.StringComparison.Ordinal))
            DistribuerPieces(recompensePack2);
        else if (string.Equals(produitAchete, idPack3, System.StringComparison.Ordinal))
            DistribuerPieces(recompensePack3);

        return PurchaseProcessingResult.Complete;
    }

    private void DistribuerPieces(int montant)
    {
        Debug.Log("Paiement réussi ! Le joueur devrait recevoir " + montant + " pièces.");
        
        
        if (SaveManager.instance != null)
        {
            SaveManager.instance.data.argentTotal += montant;
            SaveManager.instance.SauvegarderPartie();
        }

        if (ThemeManager.instance != null)
        {
            ThemeManager.instance.RafraichirAffichageArgent();
        }
        
    }

    // ==========================================
    // FONCTIONS OBLIGATOIRES POUR UNITY
    // ==========================================
    public void OnInitializeFailed(InitializationFailureReason error) { }
    public void OnInitializeFailed(InitializationFailureReason error, string message) { }
    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason) 
    {
        Debug.Log("Achat annulé ou échoué : " + failureReason);
    }
}