namespace OpenMyGame.Core.Level.Data
{
    [System.Serializable]
    public sealed class LevelConfigData
    {
        public string LevelId;
        public int Width;
        public int Height;
        public int[] Cells;
    }
}