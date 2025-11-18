
using SmokeLounge.AOtomation.Messaging.Serialization;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace ZeroIn
{
    public class PathConfig
    {
        [AoMember(0)]
        public bool AttackMobs { get; set; }

        [AoMember(1)]
        public bool UseTauntItem { get; set; }

        [AoMember(3)]
        public bool PathToCorpses { get; set; }

        [AoMember(4)]
        public bool PathToMobs { get; set; }

        [AoMember(5)]
        public bool AttackPriorityOnly { get; set; }

        [AoMember(6)]
        public bool OverrideAttack { get; set; }

        [AoMember(7)]
        public int AttackRange { get; set; }

        [AoMember(8)]
        public int AttackPadding { get; set; }

        [AoMember(9)]
        public int TauntRange { get; set; }

        [AoMember(10)]
        public int PathRange { get; set; }

        [AoMember(11)]
        public int WanderLimit { get; set; }

        [AoMember(12)]
        public int FightTimeoutPeriod { get; set; }

        [AoMember(13)]
        public int LootTimeoutPeriod { get; set; }

        [AoMember(14, SerializeSize = ArraySizeType.Byte)]
        public string FollowTargetName { get; set; }

        [AoMember(15)]
        public bool DisableIfHp { get; set; }

        [AoMember(16)]
        public bool DisableIfNp { get; set; }

        [AoMember(17)]
        public bool DisableIfAttacked { get; set; }

        [AoMember(18)]
        public bool SyncSettings { get; set; }

        [AoMember(19)]
        public int HealthPercent { get; set; }

        [AoMember(20)]
        public int NanoPercent { get; set; }

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
            SyncSettings = false;
            HealthPercent = 66;
            NanoPercent = 66;
            FollowTargetName = "None1\nNone2\nNone3";
        }
    }
}