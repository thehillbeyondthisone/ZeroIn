using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AOSharp.Core;
using AOSharp.Core.UI;
using AOSharp.Common.GameData;

namespace ZeroIn
{
    /// <summary>
    /// ProximityGuard from Omega - detects nearby players and can auto-pause automation.
    /// Adapted for ZeroIn with whitelist system and configurable threat detection.
    /// </summary>
    public sealed class ProximityGuard
    {
        public struct ThreatInfo
        {
            public string Name;
            public float DistanceM;
            public ThreatInfo(string name, float distanceM)
            {
                Name = name;
                DistanceM = distanceM;
            }
        }

        public bool Enabled { get; set; } = false;
        public float TriggerRangeM { get; set; } = 50f;
        public float ClearSeconds { get; set; } = 8f;
        public float ScanHz { get; set; } = 5f;
        public bool AnnounceThreats { get; set; } = true;
        public float AnnounceIntervalSec { get; set; } = 5f;
        public int MaxNamesInAnnouncement { get; set; } = 3;

        private readonly HashSet<string> _whitelist;
        private readonly Action _pause;
        private readonly Action _resume;
        private readonly Func<bool> _isRunning;
        private readonly Func<bool> _isPausedByUser;
        private readonly Action<string> _info;
        private readonly Action<string> _warn;
        private readonly Func<string> _whoAmI;

        private bool _pausedByGuard;
        private float _accum;
        private DateTime _lastThreatTimeUtc = DateTime.MinValue;
        private DateTime _lastAnnounceUtc = DateTime.MinValue;
        private string _lastAnnounceKey = "";

        public ProximityGuard(
            Action pause,
            Action resume,
            Func<bool> isRunning,
            Func<bool> isPausedByUser,
            Action<string> info,
            Action<string> warn,
            Func<string> whoAmI,
            IEnumerable<string> initialWhitelist = null)
        {
            _pause = pause;
            _resume = resume;
            _isRunning = isRunning;
            _isPausedByUser = isPausedByUser;
            _info = info ?? (_ => { });
            _warn = warn ?? (_ => { });
            _whoAmI = whoAmI;
            _whitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (initialWhitelist != null)
            {
                foreach (var n in initialWhitelist)
                {
                    if (!string.IsNullOrWhiteSpace(n))
                        _whitelist.Add(n.Trim());
                }
            }
        }

        public IEnumerable<string> Whitelist => _whitelist;
        public void AddWhitelist(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
                _whitelist.Add(name.Trim());
        }

        public bool RemoveWhitelist(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && _whitelist.Remove(name.Trim());
        }

        public void ClearWhitelist() => _whitelist.Clear();

        public void Update(float dt)
        {
            if (!Enabled)
            {
                _accum = 0f;
                return;
            }

            _accum += dt;
            var interval = 1f / Math.Max(ScanHz, 0.1f);
            if (_accum < interval) return;
            _accum = 0f;

            var threats = GetThreatsSnapshot();
            var anyThreat = threats.Count > 0;

            if (AnnounceThreats)
            {
                var now = DateTime.UtcNow;
                var key = MakeAnnounceKey(threats);
                var due = (now - _lastAnnounceUtc).TotalSeconds >= AnnounceIntervalSec;

                if (anyThreat && (due || key != _lastAnnounceKey))
                {
                    AnnounceThreatsList(threats);
                    _lastAnnounceUtc = now;
                    _lastAnnounceKey = key;
                }
                else if (!anyThreat && key != _lastAnnounceKey && _lastAnnounceKey != "")
                {
                    _info("[ZeroIn Proximity] Area clear.");
                    _lastAnnounceUtc = now;
                    _lastAnnounceKey = key;
                }
            }

            if (anyThreat)
            {
                _lastThreatTimeUtc = DateTime.UtcNow;
                if (_isRunning() && !_pausedByGuard && !_isPausedByUser())
                {
                    _pause();
                    _pausedByGuard = true;
                    var first = threats[0];
                    _warn($"[ZeroIn Proximity] Threat in range ({first.Name}, {first.DistanceM:0.#} m). Auto-paused.");
                }
            }
            else if (_pausedByGuard)
            {
                var clearFor = (DateTime.UtcNow - _lastThreatTimeUtc).TotalSeconds;
                if (clearFor >= ClearSeconds && !_isPausedByUser())
                {
                    _resume();
                    _pausedByGuard = false;
                    _info("[ZeroIn Proximity] Area clear. Auto-resumed.");
                }
            }
        }

        public List<ThreatInfo> GetThreatsSnapshot()
        {
            var list = new List<ThreatInfo>();
            var me = DynelManager.LocalPlayer;
            if (me == null) return list;

            var myPos = me.Position;
            var myName = _whoAmI();

            foreach (var sc in EnumeratePlayersSafe())
            {
                try
                {
                    if (!string.IsNullOrEmpty(myName) &&
                        sc.Name.Equals(myName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (_whitelist.Contains(sc.Name))
                        continue;

                    var d = Vector3.Distance(sc.Position, myPos);
                    if (d <= TriggerRangeM)
                        list.Add(new ThreatInfo(sc.Name, d));
                }
                catch { }
            }

            list.Sort((a, b) => a.DistanceM.CompareTo(b.DistanceM));
            return list;
        }

        private void AnnounceThreatsList(List<ThreatInfo> threats)
        {
            if (threats == null || threats.Count == 0) return;

            var show = Math.Min(MaxNamesInAnnouncement, threats.Count);
            var parts = new List<string>(show);

            for (int i = 0; i < show; i++)
                parts.Add($"{threats[i].Name} ({threats[i].DistanceM:0.#}m)");

            var suffix = threats.Count > show ? $" +{threats.Count - show} more" : "";
            _warn($"[ZeroIn Proximity] {threats.Count} player(s) within {TriggerRangeM:0.#}m: {string.Join(", ", parts.ToArray())}{suffix}");
        }

        private string MakeAnnounceKey(List<ThreatInfo> threats)
        {
            if (threats == null || threats.Count == 0) return "";

            var keys = new List<string>();
            foreach (var t in threats)
                keys.Add($"{t.Name}:{Math.Round(t.DistanceM, 1)}");

            return string.Join("|", keys.ToArray());
        }

        private static IEnumerable<SimpleChar> EnumeratePlayersSafe()
        {
            var list = new List<SimpleChar>();

            try
            {
                foreach (var c in DynelManager.Characters)
                {
                    if (c != null && SafeIsPlayer(c))
                        list.Add(c);
                }
            }
            catch { }

            // Omega's dual enumeration approach for comprehensive detection
            try
            {
                var mi = typeof(DynelManager).GetMethod("GetAll", BindingFlags.Public | BindingFlags.Static);
                if (mi != null)
                {
                    var result = mi.Invoke(null, null) as IEnumerable;
                    if (result != null)
                    {
                        foreach (var d in result)
                        {
                            var sc = d as SimpleChar;
                            if (sc != null && SafeIsPlayer(sc))
                            {
                                bool exists = false;
                                foreach (var existing in list)
                                {
                                    if (existing.Identity == sc.Identity)
                                    {
                                        exists = true;
                                        break;
                                    }
                                }
                                if (!exists)
                                    list.Add(sc);
                            }
                        }
                    }
                }
            }
            catch { }

            return list;
        }

        private static bool SafeIsPlayer(SimpleChar sc)
        {
            try
            {
                return sc.IsPlayer;
            }
            catch
            {
                try { return !sc.IsNpc; }
                catch { return true; }
            }
        }
    }
}
