using Newtonsoft.Json;

namespace ZeroIn
{
    public class PathConfig
    {
        public bool AttackMobs { get; set; }
        public bool UseTauntItem { get; set; }
        public bool PathToCorpses { get; set; }
        public bool PathToMobs { get; set; }
        public bool AttackPriorityOnly { get; set; }
        public bool OverrideAttack { get; set; }
        public int AttackRange { get; set; }
        public int AttackPadding { get; set; }
        public int TauntRange { get; set; }
        public int PathRange { get; set; }
        public int WanderLimit { get; set; }
        public int FightTimeoutPeriod { get; set; }
        public int LootTimeoutPeriod { get; set; }
        public string FollowTargetName { get; set; }
        public bool DisableIfHp { get; set; }
        public bool DisableIfNp { get; set; }
        public bool DisableIfAttacked { get; set; }
        public int HealthPercent { get; set; }
        public int NanoPercent { get; set; }
        public bool SyncSettings { get; set; }

        public PathConfig()
        {
            AttackMobs = false;
            UseTauntItem = false;
            PathToCorpses = true;
            PathToMobs = true;
            AttackPriorityOnly = false;
            OverrideAttack = false;
            AttackRange = 5;
            AttackPadding = 0;
            TauntRange = 15;
            PathRange = 30;
            WanderLimit = 50;
            FightTimeoutPeriod = 30;
            LootTimeoutPeriod = 10;
            FollowTargetName = "";
            DisableIfHp = false;
            DisableIfNp = false;
            DisableIfAttacked = false;
            HealthPercent = 50;
            NanoPercent = 30;
            SyncSettings = false;
        }
    }
}
