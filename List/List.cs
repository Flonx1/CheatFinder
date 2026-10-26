using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace CheatFinder_RECODE.List
{
    internal class List
    {
        private const string BaseUrl = "https://raw.githubusercontent.com/Flonx1/CheatFinder/refs/heads/main/";

        // Списки качаются один раз за запуск (раньше каждый new List() качал все 4 файла заново)
        private static readonly Lazy<List> _instance = new Lazy<List>(() => new List());
        public static List Instance => _instance.Value;

        public string[] CheatFiles { get; }
        public string[] CheatNames { get; }
        public string[] CheatExtension { get; }
        public string[] Signatures { get; }

        // Строки, которых нет в ванильном майнкрафте — матчатся с учётом регистра, чтобы не было ложных срабатываний
        public string[] StrongSignatures { get; } = Builtin.StrongSignatures;

        public List()
        {
            CheatFiles = Download("CheatFiles");
            CheatNames = Download("CheatNames");
            CheatExtension = Download("CheatExtension");
            Signatures = Download("Signatures")
                .Where(s => !Builtin.VanillaFalsePositives.Contains(s, StringComparer.OrdinalIgnoreCase))
                .ToArray();
        }

        private static string[] Download(string name)
        {
            try
            {
                using (var client = new TimeoutWebClient())
                {
                    return client.DownloadString(BaseUrl + name)
                        .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .Where(x => x.Length > 0)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                }
            }
            catch (Exception ex)
            {
                Program._logger.Log($"[ERROR] Failed to download list '{name}': {ex.Message}");
                return new string[0];
            }
        }

        private class TimeoutWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri address)
            {
                var request = base.GetWebRequest(address);
                request.Timeout = 10000;
                return request;
            }
        }
    }

    internal static class Builtin
    {
        // Эти слова есть в самом майнкрафте (debug-хитбоксы, PanicGoal, CombatTracker и т.д.) — в памяти/модах дают ложные срабатывания
        public static readonly string[] VanillaFalsePositives = { "hitbox", "panic", "combat", "aura" };

        public static readonly string[] StrongSignatures =
        {
            "KillAura", "TriggerBot", "AutoTotem", "CrystalAura", "AutoCrystal", "ElytraTarget",
            "AimAssist", "AutoClicker", "ClickGui", "ClickGUI", "SelfDestruct",
            "net/wurstclient", "meteordevelopment/meteorclient", "net/ccbluex/liquidbounce",
            "baritone/api"
        };
    }
}
