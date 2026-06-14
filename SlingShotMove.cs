using UnityEngine;
using UnityEngine.InputSystem; 
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(LineRenderer))]
public class SlingshotMove : MonoBehaviour
{
    [Header("Contrôles Tactiles (Pivot Joueur)")]
    public float rayonMaxUI = 250f; 
    public float forceMultiplier = 20f; 
    public float maxForce = 30f;         

    [Header("Effet de Tremblement (Puissance Max)")]
    public float forceTremblement = 0.05f; 
    public float forceTremblementUI = 5f;  

    [Header("UI (2D)")]
    public Image cercleDoigtUI; 

    [Header("Réglages Visuels (Taille 3D)")]
    public float maxVisualLength = 5f;           

    [Header("Esthétique Ligne de Base")]
    public Color couleurRepos = Color.green;
    public Color couleurTensionMax = Color.red;
    public float epaisseurBase = 0.3f; 
    public float vitesseRetour = 0.05f; 
    public SpriteRenderer pointeFlecheVisual; 

    [Header("Couche Supérieure (Chevrons)")]
    public LineRenderer lrChevronsAvant; 
    public LineRenderer lrChevronsArriere; 
    
    [Tooltip("Coche cette case si les chevrons pointent dans le mauvais sens")]
    public bool inverserSensChevrons = false; 
    
    public float epaisseurChevrons = 0.2f; 
    public float vitesseDefilement = 2f; 
    public float tailleRappelTexture = 2f; 
    public float decalageHauteur = 0.02f; 

    [Header("Élastique Arrière (Ligne 3D)")]
    public LineRenderer lrElastique;
    public float epaisseurElastique = 0.2f; 
    public float largeurCercle = 1.5f; 

    [Header("Réglages Sol")]
    public float hauteurDetection = 0.8f; 

    private Vector2 playerScreenPos; 
    private Rigidbody rb;
    private LineRenderer lrBase; 
    private bool isDragging = false;
    private Coroutine animationRetour;
    
    // --- OPTIMISATION : LA CAMÉRA EN MÉMOIRE ---
    private Camera mainCam;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        lrBase = GetComponent<LineRenderer>();
        
        // On mémorise la caméra ici pour ne plus jamais la chercher !
        mainCam = Camera.main;

        ConfigurerLigne(lrBase, epaisseurBase, epaisseurBase);
        if (lrChevronsAvant != null) ConfigurerLigne(lrChevronsAvant, epaisseurChevrons, epaisseurChevrons);
        if (lrChevronsArriere != null) ConfigurerLigne(lrChevronsArriere, epaisseurChevrons, epaisseurChevrons);
        
        if (lrElastique != null) ConfigurerLigne(lrElastique, largeurCercle, epaisseurElastique);

        if (pointeFlecheVisual != null) pointeFlecheVisual.enabled = false;
        if (cercleDoigtUI != null) cercleDoigtUI.gameObject.SetActive(false); 
    }

    void ConfigurerLigne(LineRenderer lr, float startWidth, float endWidth)
    {
        lr.positionCount = 2;
        lr.enabled = false;    
        lr.useWorldSpace = true; 
        lr.numCapVertices = 5; 
        lr.startWidth = startWidth;
        lr.endWidth = endWidth;
    }

    void Update()
    {
        if (Pointer.current == null) return;

        float vitesseActuelle = inverserSensChevrons ? -vitesseDefilement : vitesseDefilement;
        float offset = Time.time * vitesseActuelle;
        
        if (lrChevronsAvant != null && lrChevronsAvant.enabled && lrChevronsAvant.material != null)
        {
            lrChevronsAvant.material.SetTextureOffset("_MainTex", new Vector2(offset, 0));
        }
        if (lrChevronsArriere != null && lrChevronsArriere.enabled && lrChevronsArriere.material != null)
        {
            lrChevronsArriere.material.SetTextureOffset("_MainTex", new Vector2(offset, 0));
        }

        if (!isDragging)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
        }

        // --- CORRECTION DE L'OPTIMISATION PHYSIQUE ICI ---
        // On ne vérifie le sol QUE si on vient de toucher l'écran
        if (Pointer.current.press.wasPressedThisFrame)
        {
            bool estAuSol = Physics.Raycast(transform.position, Vector3.down, hauteurDetection);

            if (estAuSol)
            {
                isDragging = true;
                if (mainCam != null) playerScreenPos = mainCam.WorldToScreenPoint(transform.position + Vector3.up * 0.5f);
                
                if (animationRetour != null) StopCoroutine(animationRetour);
                
                lrBase.enabled = true;
                if (lrChevronsAvant != null) lrChevronsAvant.enabled = true;
                if (lrChevronsArriere != null) lrChevronsArriere.enabled = true; 
                if (pointeFlecheVisual != null) pointeFlecheVisual.enabled = true; 
                if (lrElastique != null) lrElastique.enabled = true;
                
                if (cercleDoigtUI != null) 
                {
                    cercleDoigtUI.gameObject.SetActive(true);
                    cercleDoigtUI.rectTransform.position = playerScreenPos;
                }
            }
        }

        if (isDragging)
        {
            if (Pointer.current.press.wasReleasedThisFrame) Tirer(); 
            else UpdateVisualLine(); 
        }
    }

    void UpdateVisualLine()
    {
        Camera camActuelle = Camera.main;
        if (camActuelle == null) return; 

        playerScreenPos = camActuelle.WorldToScreenPoint(transform.position + Vector3.up * 0.5f);
        Vector3 startPoint = transform.position + Vector3.up * 0.5f;
        Vector2 currentMousePos = Pointer.current.position.ReadValue();
        
        Vector2 dragPixels = playerScreenPos - currentMousePos;
        Vector2 clampedDragPixels = Vector2.ClampMagnitude(dragPixels, rayonMaxUI);
        Vector2 finalUICirclePos = playerScreenPos - clampedDragPixels;

        float tension = clampedDragPixels.magnitude / rayonMaxUI;
        Color couleurActuelle = Color.Lerp(couleurRepos, couleurTensionMax, tension);

        Vector3 drag3D = new Vector3(clampedDragPixels.x, 0f, clampedDragPixels.y).normalized;
        Vector3 rotatedDir = Quaternion.Euler(0, camActuelle.transform.eulerAngles.y, 0) * drag3D;
        Vector3 visualVector = rotatedDir * (tension * maxVisualLength);
        Vector3 endPoint = startPoint + visualVector;

        Ray ray = camActuelle.ScreenPointToRay(finalUICirclePos);
        Plane planAuSol = new Plane(Vector3.up, new Vector3(0, startPoint.y, 0));
        Vector3 positionDoigt3D = startPoint;

        if (planAuSol.Raycast(ray, out float distance))
        {
            positionDoigt3D = ray.GetPoint(distance);
        }

        Vector3 tremblement3D = Vector3.zero;
        Vector2 tremblementUI = Vector2.zero;
        
        if (tension >= 0.99f)
        {
            Vector2 vibration = Random.insideUnitCircle;
            tremblement3D = new Vector3(vibration.x, 0f, vibration.y) * forceTremblement;
            tremblementUI = vibration * forceTremblementUI;
        }

        if (cercleDoigtUI != null)
        {
            cercleDoigtUI.rectTransform.position = finalUICirclePos + tremblementUI;
            cercleDoigtUI.color = couleurActuelle;
        }

        Vector3 visualStartPoint = startPoint + tremblement3D;
        Vector3 visualEndPoint = endPoint + tremblement3D;
        Vector3 visualDoigt3D = positionDoigt3D + tremblement3D;

        lrBase.SetPosition(0, visualStartPoint);
        lrBase.SetPosition(1, visualEndPoint);
        lrBase.startColor = couleurActuelle;
        lrBase.endColor = couleurActuelle;

        if (lrChevronsAvant != null)
        {
            Vector3 decalage = Vector3.up * decalageHauteur;
            lrChevronsAvant.SetPosition(0, visualStartPoint + decalage);
            lrChevronsAvant.SetPosition(1, visualEndPoint + decalage);
            if (lrChevronsAvant.material != null)
            {
                lrChevronsAvant.material.SetTextureScale("_MainTex", new Vector2(visualVector.magnitude * tailleRappelTexture, 1));
            }
        }

        if (lrElastique != null)
        {
            lrElastique.startWidth = largeurCercle; 
            lrElastique.endWidth = epaisseurElastique; 
            lrElastique.SetPosition(0, visualDoigt3D);
            lrElastique.SetPosition(1, visualStartPoint);
            lrElastique.startColor = couleurActuelle;
            lrElastique.endColor = couleurActuelle;
        }

        if (lrChevronsArriere != null)
        {
            Vector3 decalage = Vector3.up * decalageHauteur;
            lrChevronsArriere.startWidth = epaisseurChevrons * (largeurCercle / epaisseurElastique); 
            lrChevronsArriere.endWidth = epaisseurChevrons;
            
            lrChevronsArriere.SetPosition(0, visualDoigt3D + decalage);
            lrChevronsArriere.SetPosition(1, visualStartPoint + decalage);
            
            if (lrChevronsArriere.material != null)
            {
                float longueurArriere = (visualDoigt3D - visualStartPoint).magnitude;
                lrChevronsArriere.material.SetTextureScale("_MainTex", new Vector2(longueurArriere * tailleRappelTexture * (largeurCercle / epaisseurElastique), 1));
            }
        }

        if (pointeFlecheVisual != null)
        {
            pointeFlecheVisual.transform.position = visualEndPoint;
            if (visualVector != Vector3.zero) pointeFlecheVisual.transform.rotation = Quaternion.LookRotation(visualVector);
            pointeFlecheVisual.color = couleurActuelle;
        }
    }

    void Tirer()
    {
        Camera camActuelle = Camera.main;
        if (camActuelle == null) return;

        isDragging = false;
        
        if (cercleDoigtUI != null) cercleDoigtUI.gameObject.SetActive(false);
        if (lrElastique != null) lrElastique.enabled = false;
        if (lrChevronsArriere != null) lrChevronsArriere.enabled = false; 

        Vector3 startPoint = transform.position + Vector3.up * 0.5f;
        Vector3 currentEndPoint = lrBase.GetPosition(1);
        animationRetour = StartCoroutine(AnimerRetour(currentEndPoint, startPoint));

        Vector2 currentMousePos = Pointer.current.position.ReadValue();
        Vector2 dragPixels = playerScreenPos - currentMousePos;
        Vector2 clampedDragPixels = Vector2.ClampMagnitude(dragPixels, rayonMaxUI);
        
        float tension = clampedDragPixels.magnitude / rayonMaxUI;
        Vector3 drag3D = new Vector3(clampedDragPixels.x, 0f, clampedDragPixels.y).normalized;
        Vector3 rotatedDir = Quaternion.Euler(0, camActuelle.transform.eulerAngles.y, 0) * drag3D;

        float forceAmount = Mathf.Clamp((tension * maxForce) * forceMultiplier, 0, maxForce);
        Vector3 forceToApply = rotatedDir * forceAmount;
        forceToApply.y = forceAmount * 0.15f; 
        
        rb.AddForce(forceToApply, ForceMode.Impulse);

        if (QuestManager.instance != null) QuestManager.instance.AjouterProgression(TypeActionQuete.FaireTirs, 1);
    }

    private IEnumerator AnimerRetour(Vector3 pointDeDepart, Vector3 pointCible)
    {
        float tempsEcoule = 0f;
        while (tempsEcoule < vitesseRetour)
        {
            tempsEcoule += Time.deltaTime;
            float progression = tempsEcoule / vitesseRetour;
            Vector3 positionActuelle = Vector3.Lerp(pointDeDepart, pointCible, progression);
            lrBase.SetPosition(1, positionActuelle);
            if (lrChevronsAvant != null) lrChevronsAvant.SetPosition(1, positionActuelle + (Vector3.up * decalageHauteur));
            if (pointeFlecheVisual != null) pointeFlecheVisual.transform.position = positionActuelle;
            yield return null;
        }
        lrBase.enabled = false;
        if (lrChevronsAvant != null) lrChevronsAvant.enabled = false;
        if (pointeFlecheVisual != null) pointeFlecheVisual.enabled = false; 
    }
}