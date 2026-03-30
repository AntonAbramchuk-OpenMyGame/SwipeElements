using System;
using UnityEngine;
using UnityEngine.UI;

namespace OpenMyGame.Core.UI
{
    public sealed class GameHudView : MonoBehaviour
    {
        public event Action RestartClicked;
        public event Action SkipClicked;
        public event Action NextClicked;

        [SerializeField] private Button restartButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private GameObject winScreenRoot;
        [SerializeField] private Button nextButton;

        private void Awake()
        {
            if (restartButton)
                restartButton.onClick.AddListener(OnRestartClicked);

            if (skipButton)
                skipButton.onClick.AddListener(OnSkipClicked);

            if (nextButton)
                nextButton.onClick.AddListener(OnNextClicked);

            HideWinScreen();
        }

        private void OnDestroy()
        {
            if (restartButton)
                restartButton.onClick.RemoveListener(OnRestartClicked);

            if (skipButton)
                skipButton.onClick.RemoveListener(OnSkipClicked);

            if (nextButton)
                nextButton.onClick.RemoveListener(OnNextClicked);
        }

        public void ShowWinScreen()
        {
            if (winScreenRoot)
                winScreenRoot.SetActive(true);
        }

        public void HideWinScreen()
        {
            if (winScreenRoot)
                winScreenRoot.SetActive(false);
        }

        private void OnRestartClicked()
        {
            RestartClicked?.Invoke();
        }

        private void OnSkipClicked()
        {
            SkipClicked?.Invoke();
        }

        private void OnNextClicked()
        {
            NextClicked?.Invoke();
        }
    }
}