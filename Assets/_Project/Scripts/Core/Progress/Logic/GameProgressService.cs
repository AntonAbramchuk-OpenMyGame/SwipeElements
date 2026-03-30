using System;
using System.IO;
using OpenMyGame.Core.Progress.Data;
using OpenMyGame.Core.Progress.Logic.Abstractions;
using UnityEngine;

namespace OpenMyGame.Core.Progress.Logic
{
    public sealed class GameProgressService : IGameProgressService
    {
        private const string FileName = "game_progress.json";

        private GameProgressData _cachedProgressData;

        public void Initialize()
        {
            var filePath = GetFilePath();

            if (!File.Exists(filePath))
            {
                _cachedProgressData = null;
                return;
            }

            try
            {
                var json = File.ReadAllText(filePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    _cachedProgressData = null;
                    return;
                }

                var progressData = JsonUtility.FromJson<GameProgressData>(json);
                NormalizeProgressData(progressData);

                if (!IsValid(progressData))
                {
                    Debug.LogWarning("[GameProgressService] Save file is invalid. Progress will be cleared.");

                    SafeDeleteFile(filePath);
                    _cachedProgressData = null;
                    return;
                }

                _cachedProgressData = progressData;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                SafeDeleteFile(filePath);
                _cachedProgressData = null;
            }
        }

        public int GetCompletedLevelsCount()
        {
            return _cachedProgressData?.completedLevelsCount ?? 0;
        }

        public LevelRunSnapshotData GetLevelRunSnapshot()
        {
            return _cachedProgressData?.activeLevelSnapshot;
        }

        public void SaveCurrentRun(string levelId, BoardSaveData boardSaveData)
        {
            if (string.IsNullOrWhiteSpace(levelId))
                throw new ArgumentException(nameof(levelId));

            if (boardSaveData == null)
                throw new ArgumentNullException(nameof(boardSaveData));

            var progressData = GetOrCreateProgressData();

            progressData.activeLevelSnapshot = new LevelRunSnapshotData
            {
                levelId = levelId,
                board = boardSaveData
            };

            SaveInternal(progressData);
        }

        public void MarkLevelCompleted()
        {
            var progressData = GetOrCreateProgressData();

            progressData.completedLevelsCount++;
            progressData.activeLevelSnapshot = null;

            SaveInternal(progressData);
        }

        private void SaveInternal(GameProgressData progressData)
        {
            NormalizeProgressData(progressData);

            if (!IsValid(progressData))
                throw new ArgumentException("[GameProgressService] Progress data is invalid.");

            var json = JsonUtility.ToJson(progressData, true);
            var filePath = GetFilePath();

            File.WriteAllText(filePath, json);
            _cachedProgressData = progressData;
        }

        private GameProgressData GetOrCreateProgressData()
        {
            return _cachedProgressData ?? new GameProgressData
            {
                completedLevelsCount = 0,
                activeLevelSnapshot = null
            };
        }

        private static string GetFilePath()
        {
            return Path.Combine(Application.persistentDataPath, FileName);
        }

        private static void NormalizeProgressData(GameProgressData progressData)
        {
            var snapshot = progressData?.activeLevelSnapshot;

            if (snapshot == null)
                return;

            var isEmptySnapshot =
                string.IsNullOrWhiteSpace(snapshot.levelId) ||
                snapshot.board?.cells == null ||
                snapshot.board.cells.Length == 0;

            if (isEmptySnapshot)
            {
                progressData.activeLevelSnapshot = null;
            }
        }

        private static bool IsValid(GameProgressData progressData)
        {
            if (progressData == null)
                return false;

            if (progressData.completedLevelsCount < 0)
                return false;

            if (progressData.activeLevelSnapshot == null)
                return true;

            if (string.IsNullOrWhiteSpace(progressData.activeLevelSnapshot.levelId))
                return false;

            var board = progressData.activeLevelSnapshot.board;

            if (board == null)
                return false;

            if (board.width <= 0 || board.height <= 0)
                return false;

            if (board.cells == null)
                return false;

            var expectedCount = board.width * board.height;
            return board.cells.Length == expectedCount;
        }

        private static void SafeDeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}