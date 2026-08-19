namespace Magma.Data
{
    /// <summary>Noms des scènes du projet, centralisés pour bannir tout littéral en dur.</summary>
    public static class SceneNames
    {
        /// <summary>Menu principal.</summary>
        public const string MainMenu = "Main_Menu";

        /// <summary>Écran de sélection de l'artiste à accompagner.</summary>
        public const string ArtistSelection = "Artist_Selection";

        /// <summary>Hub proposant les mini-jeux de coaching disponibles.</summary>
        public const string WorkshopHub = "Workshop_Hub";

        /// <summary>Carte de sélection de concert.</summary>
        public const string MapConcert = "Map_Concert";

        /// <summary>Mini-jeu de répétition au piano (Composition).</summary>
        public const string PianoTitle = "Piano_Title";

        /// <summary>Mini-jeu de coaching scénique (Prestation Scénique).</summary>
        public const string MiniGameStage = "MiniGame_Stage";

        /// <summary>Mini-jeu de direction artistique (Marketing/Affiche).</summary>
        public const string MiniGamePoster = "MiniGame_Poster";

        /// <summary>Scène de test des mécaniques.</summary>
        public const string TestMechanic = "Test_Mechanic";

        /// <summary>Scène d'exemple par défaut.</summary>
        public const string SampleScene = "SampleScene";

        /// <summary>Résout le nom de scène du mini-jeu correspondant à l'identifiant donné.</summary>
        public static string ForMiniGame(MiniGameId miniGameId)
        {
            switch (miniGameId)
            {
                case MiniGameId.Music:
                    return PianoTitle;
                case MiniGameId.Stage:
                    return MiniGameStage;
                case MiniGameId.Poster:
                    return MiniGamePoster;
                default:
                    return null;
            }
        }
    }
}
