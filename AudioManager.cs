using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // --- LE SINGLETON (Il survit partout) ---
    public static AudioManager instance;

    [Header("Les Haut-Parleurs")]
    public AudioSource sourceMusique;
    public AudioSource sourceEffets; // Pour les pièces, explosions, rebonds...

    [Header("Volumes en cours")]
    [Range(0f, 1f)] public float volumeMusique = 0.5f;
    [Range(0f, 1f)] public float volumeEffets = 1f;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Il ne sera jamais détruit !
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // On initialise les volumes au Start pour s'assurer que le SaveManager est bien prêt
        InitialiserVolumes();
    }

    private void InitialiserVolumes()
    {
        // On lit les valeurs sauvegardées
        if (SaveManager.instance != null)
        {
            volumeMusique = SaveManager.instance.data.volumeMusique;
            volumeEffets = SaveManager.instance.data.volumeEffets;
        }

        if (sourceMusique != null) sourceMusique.volume = volumeMusique;
        if (sourceEffets != null) sourceEffets.volume = volumeEffets;
    }

    // --- FONCTIONS POUR LES SLIDERS (NOUVEAU) ---
    public void ChangerVolumeMusique(float nouveauVolume)
    {
        volumeMusique = nouveauVolume;
        if (sourceMusique != null) sourceMusique.volume = volumeMusique;

        // On sauvegarde en direct
        if (SaveManager.instance != null)
        {
            SaveManager.instance.data.volumeMusique = volumeMusique;
            SaveManager.instance.SauvegarderPartie();
        }
    }

    public void ChangerVolumeEffets(float nouveauVolume)
    {
        volumeEffets = nouveauVolume;
        if (sourceEffets != null) sourceEffets.volume = volumeEffets;

        if (SaveManager.instance != null)
        {
            SaveManager.instance.data.volumeEffets = volumeEffets;
            SaveManager.instance.SauvegarderPartie();
        }
    }

    // --- FONCTIONS POUR LA MUSIQUE ---
    public void JouerMusique(AudioClip musiqueClip)
    {
        if (musiqueClip == null || sourceMusique == null) return;

        // Si c'est déjà la bonne musique qui tourne, on ne la recommence pas depuis le début
        if (sourceMusique.clip == musiqueClip && sourceMusique.isPlaying) return;

        sourceMusique.clip = musiqueClip;
        sourceMusique.loop = true; // La musique tourne en boucle
        sourceMusique.Play();
    }

    public void ArreterMusique()
    {
        if (sourceMusique != null) sourceMusique.Stop();
    }

    // --- FONCTIONS POUR LES BRUITAGES (SFX) ---
    public void JouerSon(AudioClip sonClip, float volume = 1f)
    {
        if (sonClip == null || sourceEffets == null) return;
        
        // PlayOneShot multiplie le volume du clip par notre réglage global
        sourceEffets.PlayOneShot(sonClip, volume * volumeEffets);
    }
}