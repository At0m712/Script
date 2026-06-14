using UnityEngine;

[RequireComponent(typeof(ModuleInfo))]
public class RampeDynamique : MonoBehaviour
{
    [Header("Visuel à déformer")]
    [Tooltip("Glisse ici le Cube qui sert de route pour ta rampe")]
    public Transform visuelRampe;

    [Header("Réglages")]
    [Tooltip("Laisse 1 si tu utilises un Cube Unity basique.")]
    public float longueurDeBaseDuModele = 1f;

    [Header("Mode Emboîtement")]
    [Tooltip("Coche cette case car tes modules normaux ont leur pivot au centre. La rampe reculera automatiquement pour combler le vide !")]
    public bool modePivotCentral = true;

    void Start()
    {
        ConstruireRampe();
    }

    void ConstruireRampe()
    {
        ModuleInfo info = GetComponent<ModuleInfo>();
        if (info == null || visuelRampe == null) return;

        Vector3 pointDepart;
        Vector3 pointArrivee;

        if (modePivotCentral)
        {
            // --- LA MAGIE EST ICI ---
            // On recule le départ de la moitié de la taille (ex: -5m) pour coller au module précédent.
            // On avance l'arrivée de la moitié (ex: +5m) pour coller au module suivant.
            float moitieZ = info.tailleZ / 2f;
            pointDepart = transform.position + new Vector3(0, 0, -moitieZ);
            pointArrivee = transform.position + new Vector3(0, info.hauteurY, moitieZ);
        }
        else
        {
            // Mode normal (si un jour tu mets les pivots au début)
            pointDepart = transform.position;
            pointArrivee = transform.position + new Vector3(0, info.hauteurY, info.tailleZ);
        }

        // 1. On place le centre de la rampe exactement au milieu de cette nouvelle diagonale
        visuelRampe.position = (pointDepart + pointArrivee) / 2f;

        // 2. On oriente la rampe pour qu'elle regarde vers le haut
        visuelRampe.LookAt(pointArrivee);

        // 3. On calcule la distance exacte et on l'étire
        float distance = Vector3.Distance(pointDepart, pointArrivee);
        Vector3 nouvelleEchelle = visuelRampe.localScale;
        nouvelleEchelle.z = distance / longueurDeBaseDuModele;
        visuelRampe.localScale = nouvelleEchelle;
    }
}