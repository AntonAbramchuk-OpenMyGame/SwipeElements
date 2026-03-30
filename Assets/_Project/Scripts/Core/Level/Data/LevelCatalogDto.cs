using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenMyGame.Core.Level.Data
{
    [Serializable]
    public sealed class LevelCatalogDto
    {
        [SerializeField] private List<string> levelIds;

        public IReadOnlyList<string> LevelIds => levelIds;
    }
}