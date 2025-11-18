using AOSharp.Core;
using AOSharp.Core.UI;
using System.Linq;
using AOSharp.Common.GameData;
using System.Collections.Generic;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using SmokeLounge.AOtomation.Messaging.Messages;
using AOSharp.Core.Inventory;
using System;

namespace ZeroIn
{
    public class MobTargeting
    {
        private List<Identity> _ignoredIdentities;
        private List<int> _tauntItemIds;
        private ZeroInConfig _config;
        private List<CorpseTarget> _corpses;

        public MobTargeting(ZeroInConfig config)
        {
            _config = config;
            _tauntItemIds = new List<int> { 83919, 83920, 253187, 244655, 152029, 151692, 151693, 158045, 158046 };
            _ignoredIdentities = new List<Identity>();
            _corpses = new List<CorpseTarget>();

            Network.N3MessageReceived += OnN3MessageReceived;
            Inventory.ContainerOpened += OnCorpseOpened;
        }

        public bool TryGetNextTarget(out SimpleChar target, out bool shouldAttack, out Item tauntItem)
        {
            shouldAttack = _config.PathingConfig.AttackMobs;
            tauntItem = GetTauntItem();

            var filteredTargets = FilterTargets(GetPossibleTargets(), tauntItem);
            filteredTargets = OrderTargets(filteredTargets);
            target = filteredTargets.FirstOrDefault();

            return target != null;
        }

        public void RemoveCorpse(Identity corpseIdentity)
        {
            var corpseTarget = _corpses.FirstOrDefault(x => x.CorpseIdentity == corpseIdentity);

            if (corpseIdentity != null)
                _corpses.Remove(corpseTarget);
        }

        public bool TryGetNextCorpse(out Corpse corpse)
        {
            corpse = null;

            if (!_config.PathingConfig.PathToCorpses)
                return false;

            foreach (var cachedCorpse in _corpses.ToList())
            {
                if (!cachedCorpse.HasExpired())
                    continue;

                _corpses.Remove(cachedCorpse);
            }

            var range = _config.PathingConfig.PathToMobs ? _config.PathingConfig.PathRange : 5;

            corpse = DynelManager.Corpses
                .Where(x => !x.IsOpen && _corpses.Any(y => y.CorpseIdentity == x.Identity && !y.Looted))
                .Where(x => Vector3.Distance(x.Position, DynelManager.LocalPlayer.Position) <= range)
                .OrderBy(x => Vector3.Distance(x.Position, DynelManager.LocalPlayer.Position))
                .FirstOrDefault();

            return corpse != null;
        }

        private IEnumerable<SimpleChar> GetPossibleTargets()
        {
            var sharedQuery = DynelManager.NPCs
                .Where(x => !x.IsPet && !x.IsPlayer && x.IsAlive && x.IsInLineOfSight && !_ignoredIdentities.Contains(x.Identity));

            // ZeroIn simplified targeting:
            // 1. Level range filter
            sharedQuery = sharedQuery.Where(x => x.Level >= _config.MinMobLevel && x.Level <= _config.MaxMobLevel);

            // 2. Blacklist filter
            if (_config.MobBlacklist.Count > 0)
                sharedQuery = sharedQuery.Where(x => !_config.MobBlacklist.Contains(x.Name));

            // 3. Hostile only filter (if enabled, only attack mobs that are aggressive or already attacking)
            if (_config.HostileMobsOnly)
                sharedQuery = sharedQuery.Where(x => x.FightingTarget != null || x.IsAggressive);

            // Debug output
            if (_config.VerboseDebug)
            {
                var targets = sharedQuery.ToList();
                if (targets.Count > 0)
                    Chat.WriteLine($"[ZeroIn] Found {targets.Count} possible targets: {string.Join(", ", targets.Select(t => $"{t.Name}(L{t.Level})"))}",
                        AOSharp.Core.UI.ChatColor.Gray);
            }

            // Legacy support: also check RoamPath.Rules if they exist
            if (ZeroIn.RoamPath?.Rules != null)
            {
                if (_config.PathingConfig.AttackPriorityOnly)
                    return sharedQuery.Where(x => ZeroIn.RoamPath.Rules.PriorityNames.Contains(x.Name));
                else if (ZeroIn.RoamPath.Rules.IgnoredNames.Count > 0)
                    return sharedQuery.Where(x => !ZeroIn.RoamPath.Rules.IgnoredNames.Contains(x.Name));
            }

            return sharedQuery;
        }

        private List<SimpleChar> FilterTargets(IEnumerable<SimpleChar> possibleTargets, Item tauntItem)
        {
            var finalTargets = new List<SimpleChar>();

            if (!_config.PathingConfig.AttackMobs && !_config.PathingConfig.UseTauntItem && !_config.PathingConfig.PathToMobs)
                return finalTargets;

            if (_config.PathingConfig.UseTauntItem && tauntItem != null)
                finalTargets.AddRange(possibleTargets.Where(x => Vector3.Distance(x.Position, DynelManager.LocalPlayer.Position) <= _config.PathingConfig.TauntRange));

            if (_config.PathingConfig.PathToMobs)
                finalTargets.AddRange(possibleTargets.Where(x => Vector3.Distance(x.Position, DynelManager.LocalPlayer.Position) <= _config.PathingConfig.PathRange));
          
            if (_config.PathingConfig.AttackMobs)
                finalTargets.AddRange(possibleTargets.Where(x => IsInWeaponsRange(x)));

            if (_config.PathingConfig.DisableIfAttacked)
                finalTargets.AddRange(possibleTargets.Where(x => x.FightingTarget?.Identity == DynelManager.LocalPlayer.Identity));

            finalTargets = finalTargets
              .Distinct(new SimpleCharComparer())
              .Where(x => ZeroIn.RoamPath.SPath.Waypoints.Any(c => Vector3.Distance(c, x.Position) < _config.PathingConfig.WanderLimit))
              .ToList();

            if (Team.IsInTeam && !Team.IsLeader)
            {
                var teamMemberTarget = Team.Members?
                    .Where(x => x.Character?.Identity != DynelManager.LocalPlayer.Identity && x.Character?.FightingTarget != null)
                    .OrderByDescending(x => x.Name == _config.PathingConfig.FollowTargetName)
                    .ThenByDescending(x => x.Level)
                    .FirstOrDefault()?.Character?.FightingTarget;

                if (teamMemberTarget != null && finalTargets.Any(x => x.Identity == teamMemberTarget.Identity))
                {
                    return new List<SimpleChar> { teamMemberTarget };
                }

            }

            return finalTargets;
        }

        private Item GetTauntItem()
        {
            if (!_config.PathingConfig.UseTauntItem)
                return null;

            return Inventory.Items.Where(x => _tauntItemIds.Contains(x.Id)).FirstOrDefault(x => x.MeetsSelfUseReqs());
        }

        private List<SimpleChar> OrderTargets(List<SimpleChar> finalTargets)
        {
            if (finalTargets.Count == 1)
                return finalTargets;

            return finalTargets
                .OrderBy(x =>
                {
                    int index = ZeroIn.RoamPath.Rules.PriorityNames.IndexOf(x.Name);
                    return index != -1 ? index : int.MaxValue;
                })
                .ThenBy(x => x.FightingTarget?.Identity != DynelManager.LocalPlayer.Identity)
                .ThenBy(x => Vector3.Distance(x.Position, DynelManager.LocalPlayer.Position))
                .ToList();
        }

        public bool IsInTauntRange(SimpleChar target)
        {
            return target != null && _config.PathingConfig.UseTauntItem && target.IsInLineOfSight && Vector3.Distance(target.Position, DynelManager.LocalPlayer.Position) <= ZeroIn.Config.PathingConfig.TauntRange;
        }


        public bool IsInPathRange(SimpleChar target)
        {
            return _config.PathingConfig.PathToMobs && Vector3.Distance(target.Position, DynelManager.LocalPlayer.Position) <= _config.PathingConfig.PathRange;
        }

        public bool IsInWeaponsRange(SimpleChar target)
        {
            if (target == null)
                return false;

            var distanceCheck = _config.PathingConfig.OverrideAttack ? 
                target.DistanceFrom(DynelManager.LocalPlayer) < _config.PathingConfig.AttackRange : 
                target.IsInWeaponHitRange(_config.PathingConfig.AttackPadding);

            return target.IsInLineOfSight && distanceCheck;
        }

        public void AddToIgnoreList(Identity identity)
        {
            if (!_ignoredIdentities.Contains(identity))
                _ignoredIdentities.Add(identity);
        }

        private void OnCorpseOpened(object sender, Container container)
        {
            if (container.Identity.Type != IdentityType.Corpse)
                return;

            var corpse = _corpses.FirstOrDefault(x => x.CorpseIdentity == container.Identity);

            if (corpse != null)
                corpse.Looted = true;
        }

        private void OnN3MessageReceived(object sender, N3Message n3Msg)
        {
            if (n3Msg is AttackMessage attackMsg)
            {
                _ignoredIdentities.Remove(attackMsg.Identity);
            }
            else if (n3Msg is CorpseFullUpdateMessage corpseMsg)
            {
                if (_corpses.Any(x => x.CorpseIdentity == corpseMsg.Identity))
                    return;

                var expiretyTimer = corpseMsg.Stats.FirstOrDefault(x => x.Value1 == Stat.TimeExist).Value2 / 100f;

                _corpses.Add(new CorpseTarget(corpseMsg.Identity, expiretyTimer));
            }
            else if (n3Msg is DespawnMessage despawnMsg)
            {
                RemoveCorpse(despawnMsg.Identity);
            }
        }
    }

    public class CorpseTarget
    {
        private DateTime _expiretyTimer;
        public Identity CorpseIdentity;
        public bool Looted;

        public CorpseTarget(Identity identity, float secondsToExpire)
        {
            CorpseIdentity = identity;
            _expiretyTimer = DateTime.Now.AddSeconds(secondsToExpire);
            Looted = false;
        }

        public bool HasExpired() => DateTime.Now > _expiretyTimer;
    }

    public class SimpleCharComparer : IEqualityComparer<SimpleChar>
    {
        public bool Equals(SimpleChar x, SimpleChar y)
        {
            return x.Identity == y.Identity;
        }

        public int GetHashCode(SimpleChar simpleChar)
        {
            return simpleChar.Identity.GetHashCode();
        }
    }
}
