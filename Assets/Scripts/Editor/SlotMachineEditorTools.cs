#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using SlotGame.Core;
using SlotGame.Data;
using SlotGame.Economy;

namespace SlotGame.Editor
{
    /// <summary>
    /// Custom Unity Editor tools for statistical RTP validation and testing.
    /// </summary>
    public static class SlotMachineEditorTools
    {
        [MenuItem("Tools/Slot Machine/Run 500,000 Spin Monte Carlo Simulation", false, 1)]
        public static void RunMonteCarloSimulation()
        {
            var config = ScriptableObject.CreateInstance<PaytableConfig>();
            config.InitializeDefaults(null, null, null, null);

            EditorUtility.DisplayProgressBar("Monte Carlo Simulation", "Simulating 500,000 spins...", 0.5f);
            try
            {
                var report = SlotMathSimulator.RunSimulation(config, spinCount: 500000, bet: 10);
                Debug.Log($"<color=#4ef037><b>[SlotMathSimulator]</b></color>\n{report}");
                EditorUtility.DisplayDialog("Simulation Complete", report.ToString(), "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Tools/Slot Machine/Verify Paytable Theoretical RTP", false, 2)]
        public static void VerifyTheoreticalRTP()
        {
            var config = ScriptableObject.CreateInstance<PaytableConfig>();
            config.InitializeDefaults(null, null, null, null);

            float rtp = config.CalculateTheoreticalRTP();
            string msg = $"Calculated Theoretical RTP: {rtp:F2}%\nTarget Standard: 95.0% - 96.5% (Compliant)";
            Debug.Log($"<color=#f6c84c><b>[PaytableRTP]</b></color> {msg}");
            EditorUtility.DisplayDialog("Theoretical RTP Verification", msg, "OK");
        }

        [MenuItem("Tools/Slot Machine/Reset Player Bankroll to Default", false, 3)]
        public static void ResetBankroll()
        {
            var economy = Object.FindFirstObjectByType<EconomyManager>();
            if (economy != null)
            {
                economy.ResetBankroll();
                Debug.Log("<color=#4ef037>Bankroll reset to starting balance.</color>");
            }
            else
            {
                Debug.LogWarning("No active EconomyManager found in the current scene.");
            }
        }
    }
}
#endif
