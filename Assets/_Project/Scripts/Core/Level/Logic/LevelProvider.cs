using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using OpenMyGame.Core.Level.Data;
using OpenMyGame.Core.Level.Logic.Abstractions;
using UnityEngine;
using UnityEngine.Networking;

namespace OpenMyGame.Core.Level.Logic
{
    public sealed class LevelProvider : ILevelProvider
    {
        private const string LevelsFolderName = "Levels";
        private const string CatalogFileName = "level_catalog.json";

        private readonly List<string> _levelIds = new();

        private bool _isInitialized;

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            if (_isInitialized)
                return;

            var catalogRelativePath = Path.Combine(LevelsFolderName, CatalogFileName);
            var catalogJson = await LoadStreamingAssetTextAsync(catalogRelativePath, cancellationToken);

            if (string.IsNullOrWhiteSpace(catalogJson))
                throw new Exception("[LevelProvider] Level catalog is empty.");

            var catalogDto = JsonUtility.FromJson<LevelCatalogDto>(catalogJson);

            if (catalogDto == null || catalogDto.LevelIds == null || catalogDto.LevelIds.Count == 0)
                throw new Exception("[LevelProvider] Level catalog is invalid or empty.");

            _levelIds.Clear();
            _levelIds.AddRange(catalogDto.LevelIds);

            _isInitialized = true;
        }

        public bool HasLevel(string levelId)
        {
            EnsureInitialized();
            return !string.IsNullOrWhiteSpace(levelId) && _levelIds.Contains(levelId);
        }

        public string GetLevelIdByCompletedLevelsCount(int completedLevelsCount)
        {
            EnsureInitialized();

            if (completedLevelsCount < 0)
                throw new ArgumentOutOfRangeException(nameof(completedLevelsCount));

            var index = completedLevelsCount % _levelIds.Count;
            return _levelIds[index];
        }

        public UniTask<LevelConfigData> LoadLevelByCompletedLevelsCountAsync(
            int completedLevelsCount,
            CancellationToken cancellationToken
        )
        {
            var levelId = GetLevelIdByCompletedLevelsCount(completedLevelsCount);
            return LoadLevelByIdAsync(levelId, cancellationToken);
        }

        public async UniTask<LevelConfigData> LoadLevelByIdAsync(
            string levelId,
            CancellationToken cancellationToken
        )
        {
            EnsureInitialized();

            if (!HasLevel(levelId))
                throw new Exception($"[LevelProvider] Level does not exist in catalog: {levelId}");

            var fileName = $"{levelId}.json";
            var relativePath = Path.Combine(LevelsFolderName, fileName);

            var json = await LoadStreamingAssetTextAsync(relativePath, cancellationToken);

            if (string.IsNullOrWhiteSpace(json))
                throw new Exception($"[LevelProvider] Level json is empty: {levelId}");

            var levelConfigData = JsonUtility.FromJson<LevelConfigData>(json);

            if (levelConfigData == null)
                throw new Exception($"[LevelProvider] Failed to parse level json: {levelId}");

            return levelConfigData;
        }

        private static async UniTask<string> LoadStreamingAssetTextAsync(
            string relativePath,
            CancellationToken cancellationToken
        )
        {
            var fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);

            using var request = UnityWebRequest.Get(fullPath);
            await request.SendWebRequest().WithCancellation(cancellationToken);

            if (request.result != UnityWebRequest.Result.Success)
                throw new Exception($"[LevelProvider] Failed to load file: {relativePath}. Error: {request.error}");

            return request.downloadHandler.text;
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("[LevelProvider] Provider is not initialized.");
        }
    }
}