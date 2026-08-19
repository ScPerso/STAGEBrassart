using System;
using System.Collections.Generic;
using Magma.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Magma.Gameplay
{
    /// <summary>
    /// Orchestre la boucle de jeu complète : sélection de l'artiste, hub de mini-jeux,
    /// crédits de formation et concerts. Persiste entre les scènes (singleton).
    /// </summary>
    public class GameFlowManager : MonoBehaviour
    {
        /// <summary>Nombre de crédits de formation accordés à chaque rendez-vous.</summary>
        private const int CreditsPerTrainingPhase = 2;

        /// <summary>Instance unique et persistante du gestionnaire de boucle de jeu.</summary>
        public static GameFlowManager Instance { get; private set; }

        /// <summary>État courant de la boucle de jeu.</summary>
        public GameState CurrentState { get; private set; } = GameState.Boot;

        /// <summary>Phase de concert courante (mi-saison ou finale).</summary>
        public ConcertPhase CurrentPhase { get; private set; } = ConcertPhase.MidSeason;

        /// <summary>Crédits de formation restants pour la phase courante.</summary>
        public int CreditsRemaining { get; private set; } = CreditsPerTrainingPhase;

        /// <summary>Cumul des gains de statistique de l'artiste sur la session.</summary>
        public IReadOnlyDictionary<StatType, int> ArtistStats => artistStats;
        private readonly Dictionary<StatType, int> artistStats = new Dictionary<StatType, int>();

        /// <summary>Déclenché quand le concert final est résolu, pour afficher le bilan de fin de parcours.</summary>
        public event Action FinalReportReady;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }

        /// <summary>Relance une nouvelle session depuis le menu principal (bouton "Recommencer").</summary>
        public void StartNewSession()
        {
            CurrentPhase = ConcertPhase.MidSeason;
            CreditsRemaining = CreditsPerTrainingPhase;
            artistStats.Clear();
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        /// <summary>Ouvre l'écran de sélection de l'artiste. Branché sur le bouton "Jouer" du menu.</summary>
        public void GoToArtistSelection()
        {
            SceneManager.LoadScene(SceneNames.ArtistSelection);
        }

        /// <summary>Confirme l'artiste choisi et ouvre le premier rendez-vous d'accompagnement.</summary>
        public void ConfirmArtistSelection()
        {
            CreditsRemaining = CreditsPerTrainingPhase;
            SceneManager.LoadScene(SceneNames.WorkshopHub);
        }

        /// <summary>
        /// Remontée du résultat d'un mini-jeu ou d'un concert. Applique le gain de statistique,
        /// consomme un crédit de formation si on sort d'un mini-jeu, puis enchaîne vers la bonne scène.
        /// </summary>
        public void ReportMiniGameResult(MiniGameResult result)
        {
            if (!result.Completed)
            {
                return;
            }

            AddStatGain(result.TargetStat, result.StatGain);

            if (CurrentState == GameState.Concert)
            {
                ResolveConcert();
            }
            else
            {
                ResolveMiniGame();
            }
        }

        private void ResolveMiniGame()
        {
            CreditsRemaining = Mathf.Max(0, CreditsRemaining - 1);

            SceneManager.LoadScene(CreditsRemaining > 0 ? SceneNames.WorkshopHub : SceneNames.MapConcert);
        }

        private void ResolveConcert()
        {
            if (CurrentPhase == ConcertPhase.MidSeason)
            {
                CurrentPhase = ConcertPhase.Final;
                CreditsRemaining = CreditsPerTrainingPhase;
                SceneManager.LoadScene(SceneNames.WorkshopHub);
            }
            else
            {
                FinalReportReady?.Invoke();
            }
        }

        private void AddStatGain(StatType stat, int gain)
        {
            artistStats.TryGetValue(stat, out int current);
            artistStats[stat] = current + gain;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CurrentState = ResolveState(scene.name);
        }

        private static GameState ResolveState(string sceneName)
        {
            if (sceneName == SceneNames.MainMenu) return GameState.Boot;
            if (sceneName == SceneNames.ArtistSelection) return GameState.ArtistSelection;
            if (sceneName == SceneNames.WorkshopHub) return GameState.WorkshopHub;
            if (sceneName == SceneNames.MapConcert) return GameState.Concert;
            if (sceneName == SceneNames.PianoTitle
                || sceneName == SceneNames.MiniGameStage
                || sceneName == SceneNames.MiniGamePoster) return GameState.MiniGame;
            return GameState.Boot;
        }
    }
}
