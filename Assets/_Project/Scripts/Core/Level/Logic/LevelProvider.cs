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
        private int _currentLevelIndex;

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            if (_isInitialized)
                return;

            string catalogRelativePath = Path.Combine(LevelsFolderName, CatalogFileName);
            string catalogJson = await LoadStreamingAssetTextAsync(catalogRelativePath, cancellationToken);

            if (string.IsNullOrWhiteSpace(catalogJson))
                throw new Exception("[LevelProvider] Level catalog is empty.");

            LevelCatalogDto catalogDto = JsonUtility.FromJson<LevelCatalogDto>(catalogJson);

            if (catalogDto == null || catalogDto.LevelIds == null || catalogDto.LevelIds.Count == 0)
                throw new Exception("[LevelProvider] Level catalog is invalid or empty.");

            _levelIds.Clear();
            _levelIds.AddRange(catalogDto.LevelIds);

            _currentLevelIndex = 0;
            _isInitialized = true;
        }

        public async UniTask<LevelConfigData> LoadCurrentLevelAsync(CancellationToken cancellationToken)
        {
            EnsureInitialized();

            string levelId = _levelIds[_currentLevelIndex];
            return await LoadLevelByIdAsync(levelId, cancellationToken);
        }

        public async UniTask<LevelConfigData> LoadNextLevelAsync(CancellationToken cancellationToken)
        {
            EnsureInitialized();

            _currentLevelIndex = (_currentLevelIndex + 1) % _levelIds.Count;

            string levelId = _levelIds[_currentLevelIndex];
            return await LoadLevelByIdAsync(levelId, cancellationToken);
        }

        private static async UniTask<LevelConfigData> LoadLevelByIdAsync(
            string levelId,
            CancellationToken cancellationToken
        )
        {
            string fileName = $"{levelId}.json";
            string relativePath = Path.Combine(LevelsFolderName, fileName);

            string json = await LoadStreamingAssetTextAsync(relativePath, cancellationToken);

            if (string.IsNullOrWhiteSpace(json))
                throw new Exception($"[LevelProvider] Level json is empty: {levelId}");

            LevelConfigData levelConfigData = JsonUtility.FromJson<LevelConfigData>(json);

            if (levelConfigData == null)
                throw new Exception($"[LevelProvider] Failed to parse level json: {levelId}");

            return levelConfigData;
        }

        private static async UniTask<string> LoadStreamingAssetTextAsync(
            string relativePath,
            CancellationToken cancellationToken
        )
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);

            using UnityWebRequest request = UnityWebRequest.Get(fullPath);
            await request.SendWebRequest().WithCancellation(cancellationToken);

            if (request.result != UnityWebRequest.Result.Success)
                throw new Exception(
                    $"[LevelProvider] Failed to load file: {relativePath}. Error: {request.error}"
                );

            return request.downloadHandler.text;
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("[LevelProvider] Provider is not initialized.");
        }
    }
}