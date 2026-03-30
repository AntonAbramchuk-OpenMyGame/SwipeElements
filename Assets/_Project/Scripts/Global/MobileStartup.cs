using UnityEngine;

namespace OpenMyGame.Global
{
    public static class MobileStartup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
    }
}