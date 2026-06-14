using UnityEngine;
using UnityEngine.UI;

public class VolumeSettingsUI : MonoBehaviour
{
    [Header("Glisse tes Sliders ici")]
    public Slider sliderMusique;
    public Slider sliderEffets;

    void Start()
    {
        // 1. On place les curseurs au bon endroit selon la sauvegarde
        if (sliderMusique != null && SaveManager.instance != null)
        {
            sliderMusique.value = SaveManager.instance.data.volumeMusique;
            // 2. On connecte le Slider à l'AudioManager par le code (plus besoin de le faire dans l'inspecteur !)
            sliderMusique.onValueChanged.AddListener(AudioManager.instance.ChangerVolumeMusique);
        }

        if (sliderEffets != null && SaveManager.instance != null)
        {
            sliderEffets.value = SaveManager.instance.data.volumeEffets;
            sliderEffets.onValueChanged.AddListener(AudioManager.instance.ChangerVolumeEffets);
        }
    }
}