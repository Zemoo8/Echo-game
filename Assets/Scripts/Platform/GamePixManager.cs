using System;
using Echo.Presentation;
using GamePix;
using UnityEngine;

namespace Echo.Platform
{
    /// <summary>
    /// Thin wrapper around the GamePix plugin (Gpx). The plugin already handles loading,
    /// pause/resume and storage (PlayerPrefs maps to GamePix.localStorage); this class only
    /// reports platform pauses and shows ads. In the editor Gpx uses stubs, so it is safe to call.
    /// </summary>
    public class GamePixManager : MonoBehaviour
    {
        public static GamePixManager Instance { get; private set; }

        /// <summary>True while GamePix has paused the game (ad on screen, portal pause).</summary>
        public static bool IsPlatformPaused => Gpx.IsGamePaused;

        /// <summary>Raised when GamePix pauses (true) or resumes (false) the game.</summary>
        public static event Action<bool> PlatformPauseChanged;

        bool wasPaused;
        bool interstitialPending;
        Action interstitialDone;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // Gpx exposes no pause event, only this flag, so poll it (one bool compare per frame).
        void Update()
        {
            bool paused = Gpx.IsGamePaused;
            if (paused == wasPaused)
                return;

            wasPaused = paused;
            PlatformPauseChanged?.Invoke(paused);
        }

        /// <summary>
        /// Shows an interstitial ad, e.g. between levels. <paramref name="onDone"/> always runs once:
        /// after the ad, when it fails, or immediately if ads are off for this presentation phase.
        /// </summary>
        public void ShowInterstitial(Action onDone)
        {
            // Calling Gpx twice before the first ad finishes is ignored by the plugin, which would
            // swallow the second callback, so only one request is ever in flight.
            if (interstitialPending || !PresentationMode.IsEnabled(Feature.GamePixAds))
            {
                onDone?.Invoke();
                return;
            }

            interstitialPending = true;
            interstitialDone = onDone;
            Gpx.Ads.InterstitialAd(FinishInterstitial, FinishInterstitial);
        }

        void FinishInterstitial()
        {
            interstitialPending = false;
            var done = interstitialDone;
            interstitialDone = null;
            done?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => PlatformPauseChanged = null;
    }
}
