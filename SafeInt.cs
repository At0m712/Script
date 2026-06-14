using UnityEngine;

// Cette structure remplace les "int" normaux pour bloquer les logiciels de triche (Memory Scanners)
public struct SafeInt
{
    private int cleSecrete;
    private int valeurCryptee;

    // --- CORRECTION : On utilise le générateur C# pur pour éviter l'erreur de constructeur Unity ---
    private static System.Random generateur = new System.Random();

    // Quand on crée la variable, on la masque avec un calcul aléatoire (XOR)
    public SafeInt(int valeurInitial)
    {
        cleSecrete = generateur.Next(10000, 99999);
        valeurCryptee = valeurInitial ^ cleSecrete;
    }

    // Le jeu peut lire et écrire la vraie valeur facilement grâce à ça
    public int Value
    {
        get { return valeurCryptee ^ cleSecrete; }
        set 
        { 
            cleSecrete = generateur.Next(10000, 99999);
            valeurCryptee = value ^ cleSecrete; 
        }
    }

    // --- MAGIE DU C# : Ces fonctions permettent de l'utiliser exactement comme un "int" normal ! ---
    public static implicit operator SafeInt(int valeur)
    {
        return new SafeInt(valeur);
    }

    public static implicit operator int(SafeInt safeInt)
    {
        return safeInt.Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}