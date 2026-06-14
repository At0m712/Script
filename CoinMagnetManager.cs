using UnityEngine;

public class CoinMagnetManager : MonoBehaviour
{
    public static CoinMagnetManager instance;

    [Header("La cible UI")]
    [Tooltip("Glisse ici l'icône de pièce de ton interface (HUD)")]
    public RectTransform cibleArgentUI; 

    void Awake()
    {
        if (instance == null) instance = this;
    }
}