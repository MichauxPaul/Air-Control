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
    // Bouton pour envoyer le score a la Plateforme (visible seulement en GameOver).
    public GameObject SendHighscoreButton;
    // Bouton "Recommencer" dans le panel de GameOver.
    public GameObject RestartButton;
    private Button _restartButtonComponent;

    [Header("Audio")]
    // Ambience sonore du jeu pendant la partie
    public AudioSource BackgroundAudio;
    // AudioSource pour les SFX
    public AudioSource SFXSource;
    // Son joué lors d'une collision
    public AudioClip CollisionSound;

    // on empêche de déclencher plusieurs game over en même temps.
    private bool _isGameOver;
    // On évite d'envoyer plusieurs fois le même score.
    private bool _scoreSent;
    // On évite d'appeler InitApp/InitGame plusieurs fois (GameManager persiste entre les scènes).
    private bool _platformInitialized;
    // Utilisé pour démarrer l'audio rapidement après un clic sur "Jouer".
    private bool _startBkgAudio;
    // Empêche de compter deux fois la même partie.
    private bool _gameSessionAlreadyCounted;
    // On cache le composant Button pour brancher le OnClick même si le GameManager persiste entre les scenes.
    private Button _sendHighscoreButtonComponent;


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

        // Au demarrage, on s'assure que le panel est caché.
        ResetRoundState();

        // On affiche le Score initial.
        RefreshScore();
    }

    // Fonction pour réinitialiser les variables lors d'une nouvelle partie .
    private void ResetRoundState()
    {
        // On autorise a nouveau un "GameOver".
        _isGameOver = false;
        // A chaque nouvelle partie, on autorise a nouveau l'envoi du score.
        _scoreSent = false;
        // On remet le temps normal.
        Time.timeScale = 1f;
        // On remet le score à zero au début d'une nouvelle partie.
        Score = 0; 

        // Si le panel existe deja, on le cache.
        if (GameOverPanel != null)
        {
            GameOverPanel.SetActive(false); 
        }

        // Par defaut, le bouton d'envoi du score n'est pas visible hors GameOver.
        if (SendHighscoreButton != null)
        {
            SendHighscoreButton.SetActive(false);
        }

        // Par defaut, le bouton recommencer n'est pas visible hors GameOver.
        if (RestartButton != null)
        {
            RestartButton.SetActive(false);
        }
    }

    // Fonction pour retrouver les références UI dans la scène courante.
    private void BindSceneUIReferences()
    {
        // On tente donc de retrouver les références dans la scène courante si elles sont null.
        if (ScoreText == null)
        {
            GameObject scoreGo = GameObject.Find("AffichageScore");
            if (scoreGo != null)
            {
                ScoreText = scoreGo.GetComponent<TextMeshProUGUI>();
                if (ScoreText == null)
                {
                    ScoreText = scoreGo.GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }
        }

        // Score dans le panel de GameOver
        if (GameOverScoreText == null)
        {
            GameObject gameOverScoreGo = GameObject.Find("TextScore");
            if (gameOverScoreGo != null)
            {
                GameOverScoreText = gameOverScoreGo.GetComponent<TextMeshProUGUI>();
                if (GameOverScoreText == null)
                {
                    GameOverScoreText = gameOverScoreGo.GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }
        }

        // Audio d'ambiance
        if (BackgroundAudio == null)
        {
            GameObject ambienceGo = GameObject.Find("SoundAmbiance");
            if (ambienceGo != null)
            {
                BackgroundAudio = ambienceGo.GetComponent<AudioSource>();
                if (BackgroundAudio == null)
                {
                    BackgroundAudio = ambienceGo.GetComponentInChildren<AudioSource>(true);
                }
            }
        }

        // Audio de SFX
        if (SFXSource == null)
        {
            GameObject sfxGo = GameObject.Find("SFX");
            if (sfxGo != null)
            {
                SFXSource = sfxGo.GetComponent<AudioSource>();
                if (SFXSource == null)
                {
                    SFXSource = sfxGo.GetComponentInChildren<AudioSource>(true);
                }
            }
        }

        // AudioClip de collision 
        if (CollisionSound == null)
        {
            GameObject crashGo = GameObject.Find("crash");
            if (crashGo != null)
            {
                AudioSource crashAudioSource = crashGo.GetComponent<AudioSource>();
                if (crashAudioSource == null)
                {
                    crashAudioSource = crashGo.GetComponentInChildren<AudioSource>(true);
                }

                if (crashAudioSource != null && crashAudioSource.clip != null)
                {
                    CollisionSound = crashAudioSource.clip;
                }
            }
        }

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

        // Une fois le panel trouvé, on essaie de retrouver et brancher le bouton d'envoi du score.
        TryBindSendHighscoreButton();
        TryBindRestartButton();
    }

    // Cherche un enfant par nom dans toute la hiérarchie (récursif).
    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child == null)
            {
                continue;
            }

            if (child.name == childName)
            {
                return child;
            }

            Transform found = FindDeepChild(child, childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private void TryBindSendHighscoreButton()
    {
        // Si on n'a pas de panel de GameOver, on ne peut pas chercher le bouton.
        if (GameOverPanel == null)
        {
            return;
        }

        // On cherche EXACTEMENT le composant Button dont le GameObject s'appelle "BtnSubmitScore".
        // Important: éviter GetComponentInParent/GetComponentInChildren car ça peut binder un autre bouton
        // (ex: Quit/Close) et fermer le jeu au clic.
        Button[] buttons = GameOverPanel.GetComponentsInChildren<Button>(true);
        _sendHighscoreButtonComponent = null;
        SendHighscoreButton = null;
        foreach (Button button in buttons)
        {
            if (button == null)
            {
                continue;
            }

            if (button.gameObject.name == "BtnSubmitScore")
            {
                _sendHighscoreButtonComponent = button;
                SendHighscoreButton = button.gameObject;
                break;
            }
        }

        // Si on a trouvé le Button, on branche l'événement OnClick.
        if (_sendHighscoreButtonComponent != null)
        {
            // Ce bouton sert uniquement à soumettre le score: on remplace les actions existantes
            // pour éviter qu'un ancien OnClick (ex: Quit/Close/Restart) ferme le jeu.
            // IMPORTANT: RemoveAllListeners ne retire pas toujours les "Persistent Calls" configurés dans l'Inspector.
            // On remplace donc complètement l'événement onClick.
            _sendHighscoreButtonComponent.onClick = new Button.ButtonClickedEvent();
            _sendHighscoreButtonComponent.onClick.AddListener(sendHightScore);
        }
    }

    private void TryBindRestartButton()
    {
        if (GameOverPanel == null)
        {
            return;
        }

        // On cherche EXACTEMENT le composant Button dont le GameObject s'appelle "RecommencerBtn".
        Button[] buttons = GameOverPanel.GetComponentsInChildren<Button>(true);
        _restartButtonComponent = null;
        RestartButton = null;
        foreach (Button button in buttons)
        {
            if (button == null)
            {
                continue;
            }

            if (button.gameObject.name == "RecommencerBtn")
            {
                _restartButtonComponent = button;
                RestartButton = button.gameObject;
                break;
            }
        }

        if (_restartButtonComponent != null)
        {
            // Ce bouton sert uniquement à recommencer: on remplace les actions existantes.
            _restartButtonComponent.onClick = new Button.ButtonClickedEvent();
            _restartButtonComponent.onClick.AddListener(RestartGame);
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

        if (_platformInitialized)
        {
            return;
        }

        // On dit à la Plateforme qu'elle peut lancer l'application correctement.
        PlateformeClient.InitApp(); 
        // On applique le mute au moment où la Plateforme confirme le chargement.
        ApplyMuteState(PlateformeClient.SoundMuteState); 

        _platformInitialized = true;
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

        // Quand on arrive dans la scène de jeu, on indique à la Plateforme qu'une partie commence.
        if (Application.platform == RuntimePlatform.WebGLPlayer && _platformInitialized && scene.name == "Game")
        {
            if (!_gameSessionAlreadyCounted)
            {
                PlateformeClient.InitGame();
                _gameSessionAlreadyCounted = true;
            }

            // On tente de démarrer l'ambiance le plus tôt possible.
            if (BackgroundAudio != null && !BackgroundAudio.isPlaying)
            {
                BackgroundAudio.Play();
            }

            _startBkgAudio = false;
        }
        else
        {
            // En dehors de la scène Game, on reset pour que la prochaine partie soit comptée.
            _gameSessionAlreadyCounted = false;
        }
    }

    public void PlayGame()
    {
        if (Application.platform != RuntimePlatform.WebGLPlayer)
        {
            return;
        }

        if (!_platformInitialized)
        {
            return;
        }

        PlateformeClient.InitGame();
        _gameSessionAlreadyCounted = true;
        _startBkgAudio = true;
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

        // Le bouton d'envoi du score n'est visible que dans le panel de GameOver.
        if (SendHighscoreButton != null)
        {
            // On l'affiche seulement à la mort (pas pendant la partie).
            SendHighscoreButton.SetActive(true);
        }

        // Le bouton "Recommencer" doit aussi être visible dans le panel de GameOver.
        if (RestartButton != null)
        {
            RestartButton.SetActive(true);
        }

        // Si le texte final est assigné, on affiche le Score final dedans.
        if (GameOverScoreText != null)
        {
            GameOverScoreText.text = "Votre score était de : " + Score.ToString("N0");
        }

    }

    // Fonction appelé par le bouton d'envoie du score dans le panel de GameOver pour envoyer le score a la Plateforme.
    public void sendHightScore()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            // La Plateforme doit être initialisée avant de pouvoir ouvrir la fenêtre de score.
            if (!_platformInitialized)
            {
                return;
            }

            // Si on a déjà envoyé notre score alors on ne renvoie pas une deuxième fois.
            if (_scoreSent)
            {
                return;
            }

            int scoreToSend;
            if (Score > int.MaxValue)
            {
                scoreToSend = int.MaxValue;
            }
            else if (Score < 0)
            {
                scoreToSend = 0;
            }
            else
            {
                scoreToSend = (int)Score;
            }

            _scoreSent = true;
            PlateformeClient.SaveHighscore(scoreToSend);
        }    
    }

    // Fonction pour recommencer le jeu.
    public void RestartGame()
    {
        // On remet le temps normal avant de recharger la scène.
        Time.timeScale = 1f;

        // On autorise à la Plateforme de compter une nouvelle partie après le reload.
        _gameSessionAlreadyCounted = false;

        // Sur WebGL, PlateformeClient.Restart() relance toute l'application sur la Plateforme.
        // Ici on veut juste recharger la scène "Game".
        SceneManager.LoadScene("Game");
    }
}
