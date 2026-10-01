using UnityEngine;

namespace Echo.Presentation
{
    /// <summary>
    /// Switches this GameObject off at load time when its feature is hidden by the current
    /// presentation phase. Nothing is deleted: the object is still in the scene, just inactive.
    /// </summary>
    public class PresentationGate : MonoBehaviour
    {
        [SerializeField] Feature requires;

        void Awake()
        {
            if (!PresentationMode.IsEnabled(requires))
                gameObject.SetActive(false);
        }
    }
}
