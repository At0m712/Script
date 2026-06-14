using UnityEngine;
using UnityEngine.UI; // Indispensable pour parler avec les boutons

// Cette ligne force Unity à n'autoriser ce script QUE sur des objets qui ont un Bouton !
[RequireComponent(typeof(Button))] 
public class SonBouton : MonoBehaviour
{
    [Tooltip("Glisse ici le fichier audio de ton clic")]
    public AudioClip sonClic;

    void Start()
    {
        // La magie est ici : le script s'abonne tout seul à l'événement du clic. 
        // Tu n'auras même pas besoin de cliquer sur le petit "+" du bouton dans l'inspecteur !
        GetComponent<Button>().onClick.AddListener(JouerLeSon);
    }

    void JouerLeSon()
    {
        // Quand on clique, on appelle ton AudioManager pour que le son respecte
        // bien le volume des paramètres (si le joueur a baissé le son des effets).
        if (sonClic != null && AudioManager.instance != null)
        {
            AudioManager.instance.JouerSon(sonClic);
        }
    }
}