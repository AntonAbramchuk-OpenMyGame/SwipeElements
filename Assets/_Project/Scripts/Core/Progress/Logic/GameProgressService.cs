using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using OpenMyGame.Core.Progress.Data;
using OpenMyGame.Core.Progress.Logic.Abstractions;
using UnityEngine;

namespace OpenMyGame.Core.Progress.Logic
{
    public sealed class GameProgressService : IGameProgressService
    {
        private const string FileName = "game_progress.json";

        private GameProgressData _cachedProgressData;

        public UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            string filePath = GetFilePath();

            if (!File.Exists(filePath))
            {
                _cachedProgressData = null;
                return UniTask.CompletedTask;
            }

            try
            {
                string json = File.ReadAllText(filePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    _cachedProgressData = null;
                    return UniTask.CompletedTask;
                }

                GameProgressData progressData = JsonUtility.FromJson<GameProgressData>(json);

                if (!IsValid(progressData))
                {
                    Debug.LogWarning("[GameProgressService] Save file is invalid. Progress will be cleared.");
                    SafeDeleteFile(filePath);
                    _cachedProgressData = null;
                    return UniTask.CompletedTask;
                }

                _cachedProgressData = progressData;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SafeDeleteFile(filePath);
                _cachedProgressData = null;
            }

            return UniTask.CompletedTask;
        }

        public bool TryGetProgress(out GameProgressData progressData)
        {
            progressData = _cachedProgressData;
            return progressData != null;
        }

        public UniTask SaveAsync(GameProgressData progressData, CancellationToken cancellationToken)
        {
            if (!IsValid(progressData))
                throw new ArgumentException("[JsonGameProgressService] Progress data is invalid.");

            string json = JsonUtility.ToJson(progressData, true);
            string filePath = GetFilePath();

            File.WriteAllText(filePath, json);
            _cachedProgressData = progressData;

            return UniTask.CompletedTask;
        }

        public UniTask ClearAsync(CancellationToken cancellationToken)
        {
            string filePath = GetFilePath();

            SafeDeleteFile(filePath);
            _cachedProgressData = null;

            return UniTask.CompletedTask;
        }

        private static string GetFilePath()
        {
            return Path.Combine(Application.persistentDataPath, FileName);
        }

        private static bool IsValid(GameProgressData progressData)
        {
            if (progressData == null)
                return false;

            if (progressData.version <= 0)
                return false;

            if (progressData.completedLevelsCount < 0)
                return false;

            if (progressData.activeLevelSnapshot == null)
                return true;

            if (string.IsNullOrWhiteSpace(progressData.activeLevelSnapshot.levelId))
                return false;

            BoardSaveData board = progressData.activeLevelSnapshot.board;

            if (board == null)
                return false;

            if (board.width <= 0 || board.height <= 0)
                return false;

            if (board.cells == null)
                return false;

            int expectedCount = board.width * board.height;
            return board.cells.Length == expectedCount;
        }

        private static void SafeDeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}