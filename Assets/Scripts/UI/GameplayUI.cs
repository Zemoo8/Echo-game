using Echo.Core;
using Echo.Presentation;
using UnityEngine;

namespace Echo.UI
{
    /// <summary>Shows the in-game HUD only while a level is running.</summary>
    public class GameplayUI : MonoBehaviour
    {
        [SerializeField] GameObject hudRoot;

        void Start()
        {
            var game = GameManager.Instance;
            if (game != null)
                game.StateChanged += OnStateChanged;
            Refresh(game != null ? game.State : GameState.Boot);
        }

        void OnDestroy()
        {
            var game = GameManager.Instance;
            if (game != null)
                game.StateChanged -= OnStateChanged;
        }

        void OnStateChanged(GameState previous, GameState current) => Refresh(current);

        void Refresh(GameState state)
        {
            bool inLevel = state == GameState.Playing || state == GameState.Paused
                           || state == GameState.LevelComplete || state == GameState.GameOver;
            hudRoot.SetActive(inLevel && PresentationMode.IsEnabled(Feature.Hud));
        }
    }
}
