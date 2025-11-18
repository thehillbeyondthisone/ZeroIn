
namespace AutomatonRoamba
{
    public class PathConfig
    {
        public bool AttackMobs;
        public bool UseTauntItem;
        public bool PathToCorpses;
        public bool PathToMobs;
        public bool AttackPriorityOnly;
        public bool OverrideAttack;
        public int AttackRange;
        public int AttackPadding;
        public int TauntRange;
        public int PathRange;
        public int WanderLimit;
        public int FightTimeoutPeriod;
        public int LootTimeoutPeriod;
        public string FollowTargetName;
        public bool DisableIfHp;
        public bool DisableIfNp;
        public bool DisableIfAttacked;
        public int HealthPercent;
        public int NanoPercent;

        public PathConfig()
        {
            AttackPadding = 1;
            TauntRange = 40;
            PathRange = 30;
            WanderLimit = 70;
            FightTimeoutPeriod = 60;
            LootTimeoutPeriod = 5;
            AttackRange = 10;
            OverrideAttack = false;
            PathToMobs = true;
            AttackMobs = true;
            UseTauntItem = true;
            PathToCorpses = false;
            DisableIfHp = false;
            DisableIfNp = false;
            DisableIfAttacked = false;
            AttackPriorityOnly = false;
            HealthPercent = 66;
            NanoPercent = 66;
            FollowTargetName = "None1\nNone2\nNone3";
        }
    }
}