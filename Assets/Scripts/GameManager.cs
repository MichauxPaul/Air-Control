using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Points")]
    // Nombre de points ajoutés quand un avion arrive correctement a une porte.
    public int GateArrivalPoints = 1000000;
    // Nombre de points ajoutés quand un avion termine son decollage.
    public int TakeoffPoints = 5000000;

    [Header("Score")]
    public long Score;
    // Texte qui affiche le Score pendant la partie.
    public TextMeshProUGUI ScoreText;

    [Header("Game Over")]
    // Panel a afficher quand la partie est perdue.
    public GameObject GameOverPanel;
    // Texte dans le panel de game over pour afficher le Score final.
    public TextMeshProUGUI GameOverScoreText;

    [Header("Audio")]
    // Ambience sonore du jeu pendant la partie
    public AudioSource BackgroundAudio;
    // AudioSource pour les SFX
    public AudioSource SFXSource;
    // Son joué lors d'une collision
    public AudioClip CollisionSound;

    // on empêche de déclencher plusieurs game over en même temps.
    private bool _isGameOver;


    private void Awake()
    {
        // Si un autre GameManager existe déjà
        if (Instance != null && Instance != this)
        {
            // On le détruit 
            Destroy(gameObject);
            // On quitte la fonction
            return;
        }

        // On garde ce GameManager comme instance globale.
        Instance = this;
        // On garde cet objet vivant entre les scènes afin de conserver la connexion avec la Plateforme
        DontDestroyOnLoad(gameObject);
        // A chaque chargement de scène, on réapplique l'état du mute sur les nouveaux AudioSource.
        SceneManager.sceneLoaded += OnSceneLoaded;

        // On s'inscrit a l'événement envoyé par la Plateforme quand elle a terminé de charger l'app.
        PlateformeClient.InitPlateformeApp += OnPlatformAppInitialized; 
        // On s'inscrit a l'événement de changement de son déclenché par le bouton de la Plateforme.
        PlateformeClient.SoundChange += OnPlatformSoundChanged; 
        // On remet le temps normal au cas ou on revient d'un game over.
        Time.timeScale = 1f;

        // Début d'une partie pour la Plateforme.
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            // On indique à la Plateforme qu'une partie commence.
            PlateformeClient.InitGame();
            // On applique immédiatement l'état "muet" courant de la Plateforme a tous les sons Unity.
            ApplyMuteState(PlateformeClient.SoundMuteState);
        }

        // Au demarrage, on s'assure que le panel est caché.
        ResetRoundState();

        // On affiche le Score initial.
        RefreshScore();
    }

    // Reinitialise les variables d'une nouvelle partie (utile quand on arrive dans la scene Game).
    private void ResetRoundState()
    {
        // On autorise a nouveau un "GameOver".
        _isGameOver = false;
        // On remet le temps normal.
        Time.timeScale = 1f;
        // On remet le score à zero au début d'une nouvelle partie.
        Score = 0; 

        // Si le panel existe deja, on le cache.
        if (GameOverPanel != null)
        {
            GameOverPanel.SetActive(false); 
        }
    }

    // Fonction pour retrouver les références UI dans la scène courante.
    private void BindSceneUIReferences()
    {
        // On tente donc de retrouver les références dans la scène courante si elles sont null.

        if (ScoreText == null)
        {
            // On récupère tous les textes TMP de la scène.
            TextMeshProUGUI[] allTmpTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None); 
            // On parcourt chaque texte TMP trouvé.
            foreach (TextMeshProUGUI tmp in allTmpTexts) 
            {
                // Si Unity renvoie une référence invalide, on l'ignore.
                if (tmp == null) 
                {
                    continue; 
                }
                // Nom du GameObject qui porte le texte.
                string objectNameLower = tmp.gameObject.name.ToLower();
                // Nom du parent si il existe.
                string parentNameLower = "";
                // Si un parent existe
                if (tmp.transform.parent != null) 
                {
                    // On prend le nom du parent.
                    parentNameLower = tmp.transform.parent.gameObject.name.ToLower(); 
                }
                // le texte est probablement un score si le nom contient "score".
                bool looksLikeScore = objectNameLower.Contains("score") || parentNameLower.Contains("score");
                // Si le texte est dans un bouton, on l'exclut.
                bool isInsideButton = tmp.GetComponentInParent<Button>() != null; 

                if (looksLikeScore && !isInsideButton)
                {
                    // On assigne ce texte comme affichage officiel du score.
                    ScoreText = tmp;
                    // On arrête ici car on a trouvé un bon candidat.
                    break; 
                }
            }
        }

        // Si la référence est perdue, on le retrouve.
        if (GameOverPanel == null)
        {
            // On récupère les objets racines de la scène active.
            GameObject[] sceneRoots = GetActiveSceneRoots();
            // On parcourt chaque racine pour chercher le panel.
            foreach (GameObject root in sceneRoots) 
            {
                // On récupère tous les enfants.
                Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);
                // On parcourt tous les objets de l'UI.
                foreach (Transform t in allTransforms) 
                {
                    // On normalise le nom pour comparer.
                    string nameLower = t.gameObject.name.ToLower(); 
                    if (nameLower.Contains("gameover") || nameLower.Contains("mort"))
                    {
                        // On mémorise ce GameObject comme panel de mort.
                        GameOverPanel = t.gameObject;
                        // On arrête la recherche dans cette branche.
                        break; 
                    }
                }

                if (GameOverPanel != null)
                {
                    // On arrête la recherche globale dès qu'on a trouvé le panel.
                    break; 
                }
            }
        }
    }

    // Fonction qui retourne les GameObjects racines de la scène active.
    private static GameObject[] GetActiveSceneRoots()
    {
        // On récupère la scène actuellement chargée.
        Scene sceneActive = SceneManager.GetActiveScene();
        // On retourne les racines des gameObjects.
        return sceneActive.GetRootGameObjects(); 
    }

    

    private void OnDestroy()
    {
        if (Instance == this)
        {
            // Si c'est l'instance globale, on la remet a null lors de la destruction.
            Instance = null;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;

        // On se désinscrit des delegates pour éviter des fuites si la scene recharge.
        PlateformeClient.InitPlateformeApp -= OnPlatformAppInitialized; 
        PlateformeClient.SoundChange -= OnPlatformSoundChanged; 
    }

    // La Plateforme appelle cette fonction une fois l'app chargée.
    private void OnPlatformAppInitialized()
    {
        // On se désinscrit immédiatement parce qu'on veut executer cette initialisation une seule fois.
        PlateformeClient.InitPlateformeApp -= OnPlatformAppInitialized; 

        if (Application.platform != RuntimePlatform.WebGLPlayer)
        {
            // En éditeur, on ne doit pas appeler les fonctions externes JavaScript donc on sort de la fonction.
            return;
        }

        // On dit à la Plateforme qu'elle peut lancer l'application correctement.
        PlateformeClient.InitApp(); 
        // On applique le mute au moment où la Plateforme confirme le chargement.
        ApplyMuteState(PlateformeClient.SoundMuteState); 
    }

    private void OnPlatformSoundChanged(bool muteState)
    {
        // On applique l'état du son reçu par la Plateform a tous les AudioSource
        ApplyMuteState(muteState); 
    }

    private static void ApplyMuteState(bool muteState)
    {
        // On cherche tous les AudioSource actifs dans la scene
        AudioSource[] allSounds = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (AudioSource source in allSounds)
        {
            // et on applique l'état muet reçu de la Plateforme.
            source.mute = muteState;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Quand on change de scène, on réapplique le mute.
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            ApplyMuteState(PlateformeClient.SoundMuteState);
        }

        BindSceneUIReferences();
        ResetRoundState();
        RefreshScore();
    }

    // On ajoute les points prévus pour l'arrivée a une porte.
    public void AddGateArrivalPoints()
    {
        AddScore(GateArrivalPoints);
    }

    // On ajoute les points prévus pour un décollage terminé.
    public void AddTakeoffPoints()
    {
        AddScore(TakeoffPoints);
    }

    // On ajoute un montant au Score.
    public void AddScore(long amount)
    {
        // On augmente le Score avec le montant reçu.
        Score += amount;
        // On met le texte à jour pour que l'UI suive le Score.
        RefreshScore();
    }

    // Fonction pour mettre à jour le texte de Score dans l'interface.
    private void RefreshScore()
    {
        // Si on n'a pas encore reussi a retrouver le texte de score dans la scene, on ne fait rien.
        if (ScoreText == null)
        {
            return;
        }

        // N0 affiche le nombre avec des separateurs de milliers et aucun chiffre decimal.
        ScoreText.text = Score.ToString("N0");
    }

    // Déclenche la fin de partie.
    public void GameOver()
    {
        // Si le game over est deja actif, on ne le redéclenche pas.
        if (_isGameOver) 
        {
            return;
        } 

        // On mémorise que la partie est terminée.
        _isGameOver = true;

        // On lance la séquence de game over.
        StartCoroutine(GameOverCoroutine());
    }

    public IEnumerator GameOverCoroutine()
    {
        // Time.timeScale a 0 met le jeu en pause 
        Time.timeScale = 0f;

        // On sauvegarde le highscore via la Plateforme.
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            int highscore;
            if (Score > int.MaxValue)
            {
                highscore = int.MaxValue;
            }
            else
            {
                highscore = (int)Score;
            }
            PlateformeClient.SaveHighscore(highscore);
        }

        // On joue le son de collision.
        if (SFXSource != null && CollisionSound != null)
        {
            SFXSource.PlayOneShot(CollisionSound);

            // On attend la fin du son.
            yield return new WaitForSecondsRealtime(CollisionSound.length);
        }

        // On arrête la musique de fond
        if (BackgroundAudio != null)
        {
            BackgroundAudio.Stop();
        }

        // Si le panel est assigné, on l'affiche.
        if (GameOverPanel != null)
        {
            GameOverPanel.SetActive(true);
        }


        // Si le texte final est assigné, on affiche le Score final dedans.
        if (GameOverScoreText != null)
        {
            GameOverScoreText.text = "Votre score était de : " + Score.ToString("N0");
        }

    }

    // Fonction pour recommencer le jeu.
    public void RestartGame()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            PlateformeClient.Restart();
            return;
        }

        // On remet le temps normal avant de recharger la scène.
        Time.timeScale = 1f;
        // On recharge la scène actuelle avec son index dans les Build Settings.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
