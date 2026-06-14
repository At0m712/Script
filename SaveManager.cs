using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography; 
using System.Text;
using System; 

[System.Serializable]
public class PlayerData
{
    public int niveau = 1;
    public int argentTotal = 0;
    public int meilleurScore = 0;
    public int scoreSession = 0;
    
    public List<int> skinsDebloques = new List<int>() { 0 }; 
    public List<int> themesDebloques = new List<int>() { 0 };
    
    public int skinEquipe = 0;
    public int themeEquipe = 0;

    public string dateQuete = "";          
    public int indexQueteJour = -1;        
    public int progressionQuete = 0;       
    public bool recompenseRecuperee = false; 
    
    public int objectifQueteJour = 0;   
    public int recompenseQueteJour = 0; 
    
    public float volumeMusique = 0.5f;
    public float volumeEffets = 1f;
    public float meilleurTempsSpeedrun = 0f;

    public int niveauAimant = 1;
    public int niveauX2 = 1;
    public int niveauSpawnPowerUp = 1;

    public string datePubPieces = ""; 
    public int pubsPiecesVuesAujourdhui = 0;
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;
    
    public PlayerData data;
    private string saveFilePath;
    private byte[] cleAES; // Notre clé unique sécurisée en mémoire

    void Awake()
    {
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

        saveFilePath = Application.persistentDataPath + "/joueurData.json";
        InitialiserCleSecurite();
        ChargerPartie();
    }

    // --- 🔒 NOUVEAU SYSTÈME ANTI-PERTE DE DONNÉES 🔒 ---
    private void InitialiserCleSecurite()
    {
        // 1. On cherche la clé secrète du joueur. Si elle n'existe pas, on la crée.
        string secretJoueur = PlayerPrefs.GetString("CleSecreteJoueur", "");
        if (string.IsNullOrEmpty(secretJoueur))
        {
            secretJoueur = Guid.NewGuid().ToString(); // Génère un ID unique indestructible
            PlayerPrefs.SetString("CleSecreteJoueur", secretJoueur);
            PlayerPrefs.Save();
        }

        // 2. On transforme ce secret en une vraie clé de cryptage AES (32 octets)
        using (SHA256 sha256 = SHA256.Create())
        {
            cleAES = sha256.ComputeHash(Encoding.UTF8.GetBytes(secretJoueur + "MonJeuSecret2026"));
        }
    }

    public void SauvegarderPartie()
    {
        string json = JsonUtility.ToJson(data);
        byte[] donneesCryptees = Crypter(json);
        File.WriteAllBytes(saveFilePath, donneesCryptees);
    }

    public void ChargerPartie()
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                byte[] fichierComplet = File.ReadAllBytes(saveFilePath);
                string jsonClair = Decrypter(fichierComplet);
                data = JsonUtility.FromJson<PlayerData>(jsonClair);
            }
            catch (Exception e)
            {
                Debug.LogWarning("🚨 Erreur de lecture ou triche détectée. Remise à zéro. Détail : " + e.Message);
                data = new PlayerData();
                SauvegarderPartie();
            }
        }
        else
        {
            data = new PlayerData();
            SauvegarderPartie();
        }
    }

    private byte[] Crypter(string texteEnClair)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = cleAES;
            aesAlg.GenerateIV(); // Génère un nouveau cadenas aléatoire (IV) à chaque sauvegarde

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                // On écrit le cadenas (IV) au tout début du fichier pour pouvoir l'ouvrir plus tard (16 octets)
                msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);

                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(texteEnClair);
                    }
                }
                return msEncrypt.ToArray(); // Retourne le fichier complet
            }
        }
    }

    private string Decrypter(byte[] fichierComplet)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = cleAES;

            // On lit les 16 premiers octets pour retrouver le cadenas (IV)
            byte[] iv = new byte[16];
            Array.Copy(fichierComplet, 0, iv, 0, iv.Length);
            aesAlg.IV = iv;

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            // On décrypte le reste du fichier (en sautant les 16 premiers octets de l'IV)
            using (MemoryStream msDecrypt = new MemoryStream(fichierComplet, 16, fichierComplet.Length - 16))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
        }
    }
}