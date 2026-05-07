using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Instance permet aux autres scripts d'acceder facilement au GameManager sans chercher l'objet dans la scene.
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

    // on empêche de declencher plusieurs game over en même temps.
    private bool _isGameOver;

    private void Awake()
    {
        // Si un autre GameManager existe deja
        if (Instance != null && Instance != this)
        {
            // On le detruit 
            Destroy(gameObject);
            // On quitte la fonction
            return;
        }

        // On garde ce GameManager comme instance globale.
        Instance = this;
        // On remet le temps normal au cas ou on revient d'un game over.
        Time.timeScale = 1f;

        // Si le panel est assigne, on le cache au debut de la partie.
        if (GameOverPanel != null)
            GameOverPanel.SetActive(false);

        // On affiche le Score initial.
        RefreshScore();
    }

    // On ajoute les points prevus pour l'arrivée a une porte.
    public void AddGateArrivalPoints()
    {
        AddScore(GateArrivalPoints);
    }

    // On ajoute les points prévus pour un décollage terminé.
    public void AddTakeoffPoints()
    {
        AddScore(TakeoffPoints);
    }

    // Ajoute un montant au Score.
    public void AddScore(long amount)
    {
        // On augmente le Score avec le montant recu.
        Score += amount;
        // On met immediatement le texte a jour pour que l'UI suive le Score.
        RefreshScore();
    }

    // Met a jour le texte de Score dans l'interface.
    private void RefreshScore()
    {
        // Si aucun texte n'est assigne dans l'inspecteur, on evite une erreur.
        if (ScoreText == null) return;

        // N0 affiche le nombre avec des separateurs de milliers et aucun chiffre decimal.
        ScoreText.text = Score.ToString("N0");
    }

    // Declenche la fin de partie.
    public void GameOver()
    {
        // Si le game over est deja actif, on ne le redéclenche pas.
        if (_isGameOver) return;

        // On mémorise que la partie est terminée.
        _isGameOver = true;

        // Si le panel est assigne, on l'affiche.
        if (GameOverPanel != null)
            GameOverPanel.SetActive(true);

        // Si le texte final est assigne, on affiche le Score final dedans.
        if (GameOverScoreText != null)
            GameOverScoreText.text = "Score : " + Score.ToString("N0");

        // Time.timeScale a 0 met le jeu en pause 
        Time.timeScale = 0f;
    }

    // Fonction pour recommencer le jeu.
    public void RestartGame()
    {
        // On remet le temps normal avant de recharger la scene.
        Time.timeScale = 1f;
        // On recharge la scene actuelle avec son index dans les Build Settings.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
