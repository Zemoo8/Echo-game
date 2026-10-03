using System.Collections;
using Echo.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Echo.UI
{
    /// <summary>
    /// The ending: captions over the calm reactor, the rescue ship's answer to the beacon, then
    /// "THE END". Jump, dash, Enter or a tap skips a line; at the end it returns to the menu.
    /// Finishing the story clears Continue, so the menu offers a new game.
    /// </summary>
    public class EndingSequence : MonoBehaviour
    {
        const float CharactersPerSecond = 38f;
        const float FadeTime = 0.6f;

        [SerializeField] CanvasGroup captionGroup;
        [SerializeField] Text caption;
        [SerializeField] CanvasGroup transmission;
        [SerializeField] Text transmissionBody;
        [SerializeField] CanvasGroup theEnd;
        [SerializeField] CanvasGroup prompt;
        [SerializeField, TextArea] string[] captions;
        [SerializeField, TextArea] string message;

        bool leaving;

        /// <summary>True once THE END and the prompt are showing.</summary>
        public bool AtTheEnd { get; private set; }

        IEnumerator Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Ending);
            SaveData.Clear();
            captionGroup.alpha = transmission.alpha = theEnd.alpha = prompt.alpha = 0f;

            yield return Hold(1.5f);
            foreach (string line in captions)
            {
                yield return Fade(captionGroup, 1f);
                yield return Type(caption, line);
                yield return Hold(2.8f);
                yield return Fade(captionGroup, 0f);
            }

            yield return Fade(transmission, 1f);
            yield return Type(transmissionBody, message);
            yield return Hold(3.5f);
            yield return Fade(transmission, 0f);

            yield return Fade(theEnd, 1f);
            yield return Hold(1f);
            yield return Fade(prompt, 1f);
            AtTheEnd = true;
            while (!SkipPressed())
                yield return null;
            Leave();
        }

        void Leave()
        {
            if (leaving)
                return;
            leaving = true;
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.Load(SceneNames.MainMenu);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.MainMenu);
        }

        // Types the line out; a press shows the rest at once.
        IEnumerator Type(Text target, string line)
        {
            float shown = 0f;
            target.text = string.Empty;
            yield return null;
            while (shown < line.Length)
            {
                if (SkipPressed())
                    break;
                shown += Time.deltaTime * CharactersPerSecond;
                target.text = line.Substring(0, Mathf.Min(line.Length, Mathf.FloorToInt(shown)));
                yield return null;
            }
            target.text = line;
            yield return null;
        }

        IEnumerator Hold(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until && !SkipPressed())
                yield return null;
            yield return null;
        }

        static IEnumerator Fade(CanvasGroup group, float to)
        {
            while (!Mathf.Approximately(group.alpha, to))
            {
                group.alpha = Mathf.MoveTowards(group.alpha, to, Time.deltaTime / FadeTime);
                yield return null;
            }
        }

        static bool SkipPressed()
        {
            var pointer = Pointer.current;
            var keyboard = Keyboard.current;
            return GameInput.Jump.WasPressedThisFrame() || GameInput.Dash.WasPressedThisFrame()
                   || (keyboard != null && keyboard.enterKey.wasPressedThisFrame)
                   || (pointer != null && pointer.press.wasPressedThisFrame);
        }
    }
}
