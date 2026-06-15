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
    private byte[] cleAES; 

    void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        saveFilePath = Application.persistentDataPath + "/joueurData.json";
        InitialiserCleSecurite();
        ChargerPartie();
    }

    private void InitialiserCleSecurite()
    {
        string secretJoueur = PlayerPrefs.GetString("CleSecreteJoueur", "");
        if (string.IsNullOrEmpty(secretJoueur))
        {
            secretJoueur = Guid.NewGuid().ToString();
            PlayerPrefs.SetString("CleSecreteJoueur", secretJoueur);
            PlayerPrefs.Save();
        }

        using (SHA256 sha256 = SHA256.Create())
        {
            cleAES = sha256.ComputeHash(Encoding.UTF8.GetBytes(secretJoueur + "MonJeuSecret2026"));
        }
    }

    // Fonction classique de sauvegarde
    public void SauvegarderPartie()
    {
        string json = JsonUtility.ToJson(data);
        File.WriteAllBytes(saveFilePath, Crypter(json));

        // NOUVEAU : On envoie instantanément la copie au Cloud !
        if (ProfileManager.instance != null)
        {
            ProfileManager.instance.PousserSauvegardeVersCloud(json);
        }
    }

    // NOUVEAU : Le Cloud utilise cette fonction pour écraser les données locales
    public void EcraserAvecDonneesCloud(string jsonCloud)
    {
        try
        {
            data = JsonUtility.FromJson<PlayerData>(jsonCloud);
            // On sauvegarde localement la version du cloud pour pouvoir y jouer hors-ligne
            File.WriteAllBytes(saveFilePath, Crypter(jsonCloud));
            Debug.Log("☁️ [SaveManager] Données locales écrasées par le Serveur avec succès !");
        }
        catch(Exception e)
        {
            Debug.LogError("🚨 Erreur lors de l'écrasement des données Cloud : " + e.Message);
        }
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
            catch 
            { 
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
            aesAlg.GenerateIV(); 
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
            using (MemoryStream msEncrypt = new MemoryStream())
            {
                msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(texteEnClair);
                    }
                }
                return msEncrypt.ToArray(); 
            }
        }
    }

    private string Decrypter(byte[] fichierComplet)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = cleAES;
            byte[] iv = new byte[16];
            Array.Copy(fichierComplet, 0, iv, 0, iv.Length);
            aesAlg.IV = iv;
            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
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