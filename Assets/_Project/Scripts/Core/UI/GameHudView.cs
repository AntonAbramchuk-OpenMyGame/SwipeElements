using System;
using UnityEngine;
using UnityEngine.UI;

namespace OpenMyGame.Core.UI
{
    public sealed class GameHudView : MonoBehaviour
    {
        public event Action RestartClicked;
        public event Action SkipClicked;

        [SerializeField] private Button restartButton;
        [SerializeField] private Button skipButton;

        private void Awake()
        {
            if (restartButton)
                restartButton.onClick.AddListener(OnRestartClicked);

            if (skipButton)
                skipButton.onClick.AddListener(OnSkipClicked);
        }

        private void OnDestroy()
        {
            if (restartButton)
                restartButton.onClick.RemoveListener(OnRestartClicked);

            if (skipButton)
                skipButton.onClick.RemoveListener(OnSkipClicked);
        }

        private void OnRestartClicked()
        {
            RestartClicked?.Invoke();
        }

        private void OnSkipClicked()
        {
            SkipClicked?.Invoke();
        }
    }
}