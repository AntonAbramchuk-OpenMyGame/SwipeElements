using UnityEngine;

namespace OpenMyGame.Utils
{
    public sealed class TimeScaler : MonoBehaviour
    {
        [SerializeField] [Range(0.1f, 5.0f)] private float timeScale = 1.0f;

        private void OnValidate()
        {
            Time.timeScale = timeScale;
        }
    }
}