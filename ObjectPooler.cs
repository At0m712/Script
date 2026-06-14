using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    // Singleton pour y accéder partout facilement
    public static ObjectPooler instance;

    [System.Serializable]
    public class Pool
    {
        public string tag; // Ex: "Piece" ou "Explosion"
        public GameObject prefab;
        public int tailleReserve; // Combien on en crée au démarrage (ex: 50)
    }

    public List<Pool> mesReserves;
    public Dictionary<string, Queue<GameObject>> dictionnaireReserves;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        dictionnaireReserves = new Dictionary<string, Queue<GameObject>>();

        // Au lancement, on fabrique tous les objets et on les cache
        foreach (Pool reserve in mesReserves)
        {
            Queue<GameObject> fileDAttente = new Queue<GameObject>();

            for (int i = 0; i < reserve.tailleReserve; i++)
            {
                GameObject obj = Instantiate(reserve.prefab);
                obj.SetActive(false); // On le cache
                obj.transform.SetParent(this.transform); // On range ça proprement
                fileDAttente.Enqueue(obj);
            }

            dictionnaireReserves.Add(reserve.tag, fileDAttente);
        }
    }

    // Fonction pour sortir un objet de la réserve
    public GameObject SortirObjet(string tag, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!dictionnaireReserves.ContainsKey(tag)) return null;

        // On prend le premier objet de la file d'attente
        GameObject objetASortir = dictionnaireReserves[tag].Dequeue();

        // On le place et on l'allume
        objetASortir.SetActive(true);
        objetASortir.transform.position = position;
        objetASortir.transform.rotation = rotation;
        if (parent != null) objetASortir.transform.SetParent(parent);

        // On le remet à la fin de la file d'attente pour plus tard
        dictionnaireReserves[tag].Enqueue(objetASortir);

        return objetASortir;
    }
}