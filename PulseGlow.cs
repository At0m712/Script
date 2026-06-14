using UnityEngine;

public class PulseGlow : MonoBehaviour
{
    [Header("Animation de Flottaison (Haut/Bas)")]
    public float vitesseFlottaison = 2f;      // La vitesse à laquelle le trophée monte et descend
    public float hauteurFlottaison = 0.2f;    // L'amplitude du mouvement (0.2 = mouvement subtil)

    [Header("Animation de Rotation")]
    public Vector3 axeDeRotation = new Vector3(0f, 50f, 0f); // Tourne sur l'axe Y (Effet pièce de monnaie)

    [Header("Effet de Lueur (Optionnel)")]
    public Renderer rendererTrophee;          // Glisse ton SpriteRenderer ou MeshRenderer ici
    public float vitesseGlow = 2f;
    public float minIntensity = 1f;
    public float maxIntensity = 5f;

    private Vector3 positionDeDepart;
    private Material materialTrophee;
    private Color couleurEmissionBase;
    private bool aUnGlow = false;

    void Start()
    {
        // 1. On mémorise la position de départ pour ne pas que le trophée s'envole
        positionDeDepart = transform.position;

        // 2. On vérifie si un Renderer a été assigné et s'il possède une option de Glow (Emission)
        if (rendererTrophee != null && rendererTrophee.material.HasProperty("_EmissionColor"))
        {
            materialTrophee = rendererTrophee.material;
            couleurEmissionBase = materialTrophee.GetColor("_EmissionColor");
            aUnGlow = true;
        }
    }

    void Update()
    {
        // --- ANIMATION 1 : FLOTTAISON (Haut / Bas) ---
        // On utilise Mathf.Sin pour créer une vague fluide de va-et-vient au fil du temps
        float nouveauY = positionDeDepart.y + (Mathf.Sin(Time.time * vitesseFlottaison) * hauteurFlottaison);
        transform.position = new Vector3(transform.position.x, nouveauY, transform.position.z);

        // --- ANIMATION 2 : ROTATION ---
        // On fait tourner l'objet en douceur indépendamment des chutes de FPS (Time.deltaTime)
        transform.Rotate(axeDeRotation * Time.deltaTime);

        // --- ANIMATION 3 : GLOW (Pulsation lumineuse) ---
        if (aUnGlow)
        {
            float sineWave = (Mathf.Sin(Time.time * vitesseGlow) + 1f) / 2f;
            float currentIntensity = Mathf.Lerp(minIntensity, maxIntensity, sineWave);
            Color finalColor = couleurEmissionBase * currentIntensity;
            materialTrophee.SetColor("_EmissionColor", finalColor);
        }
    }
}