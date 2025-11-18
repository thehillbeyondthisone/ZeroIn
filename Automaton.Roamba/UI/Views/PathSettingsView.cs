using AOSharp.Core.UI;
using AutomatonRoamba;

namespace Buddy.Shared.UI
{
    public class PathSettingsView : CustomView
    {
        private TextInputView _tauntRangeTextView;
        private TextInputView _pathRangeTextView;
        private TextInputView _wanderLimitTextView;
        private TextInputView _fightTimeoutTextView;
        private TextInputView _lootTimeoutTextView;
        private TextInputView _hpPercentTextView;
        private TextInputView _npPercentTextView;
        private TextInputView _followTarget;
        private TextInputView _attackPadding;
        private TextInputView _attackRange;
        private Checkbox _attackMobsCheckbox;
        private Checkbox _useTauntItemCheckbox;
        private Checkbox _pathToMobsCheckbox;
        private Checkbox _pathToCorpses;
        private Checkbox _priorityNamesOnlyCheckbox;
        private Checkbox _disableIfHpCheckbox;
        private Checkbox _disableIfNpCheckbox;
        private Checkbox _disableIfAttackedCheckbox;
        private Checkbox _syncSettingsCheckbox;
        private Checkbox _overrideAttackCheckbox;

        public PathSettingsView(string xmlPath) : base(xmlPath)
        {
            if (Root.FindChild("AttackMobs", out _attackMobsCheckbox)) { }
            if (Root.FindChild("PathToCorpses", out _pathToCorpses)) { }
            if (Root.FindChild("TauntItem", out _useTauntItemCheckbox)) { }
            if (Root.FindChild("PathToMobs", out _pathToMobsCheckbox)) { }
            if (Root.FindChild("TauntRange", out _tauntRangeTextView)) { }
            if (Root.FindChild("PathRange", out _pathRangeTextView)) { }
            if (Root.FindChild("WanderLimit", out _wanderLimitTextView)) { }
            if (Root.FindChild("FightTimeout", out _fightTimeoutTextView)) { }
            if (Root.FindChild("LootTimeout", out _lootTimeoutTextView)) { }
            if (Root.FindChild("DisableIfHp", out _disableIfHpCheckbox)) { }
            if (Root.FindChild("DisableIfNp", out _disableIfNpCheckbox)) { }
            if (Root.FindChild("DisableIfAttacked", out _disableIfAttackedCheckbox)) { }
            if (Root.FindChild("HpPercent", out _hpPercentTextView)) { }
            if (Root.FindChild("NpPercent", out _npPercentTextView)) { }
            if (Root.FindChild("FollowTarget", out _followTarget)) { }
            if (Root.FindChild("AttackPadding", out _attackPadding)) { }
            if (Root.FindChild("SetAttackRange", out _overrideAttackCheckbox)) { }
            if (Root.FindChild("AttackRange", out _attackRange)) { }
            if (Root.FindChild("PriorityNamesOnly", out _priorityNamesOnlyCheckbox)) { }
            if (Root.FindChild("SyncSettings", out _syncSettingsCheckbox)) { }
        }

        private int GetValue(TextInputView textView)
        {
            return int.TryParse(textView.Text, out int range) ? range : 0;
        }

        public PathConfig GetData()
        {
            PathConfig pathingConfig = new PathConfig
            {
                AttackMobs = _attackMobsCheckbox.IsChecked,
                UseTauntItem = _useTauntItemCheckbox.IsChecked,
                PathToMobs = _pathToMobsCheckbox.IsChecked,
                PathToCorpses = _pathToCorpses.IsChecked,
                TauntRange = GetValue(_tauntRangeTextView),
                PathRange = GetValue(_pathRangeTextView),
                WanderLimit = GetValue(_wanderLimitTextView),
                FightTimeoutPeriod = GetValue(_fightTimeoutTextView),
                LootTimeoutPeriod = GetValue(_lootTimeoutTextView),
                DisableIfHp = _disableIfHpCheckbox.IsChecked,
                DisableIfNp = _disableIfNpCheckbox.IsChecked,
                DisableIfAttacked = _disableIfAttackedCheckbox.IsChecked,
                SyncSettings = _syncSettingsCheckbox.IsChecked,
                HealthPercent = GetValue(_hpPercentTextView),
                NanoPercent = GetValue(_npPercentTextView),
                FollowTargetName = _followTarget.Text,
                AttackPadding = GetValue(_attackPadding),
                OverrideAttack = _overrideAttackCheckbox.IsChecked,
                AttackRange = GetValue(_attackRange),
                AttackPriorityOnly = _priorityNamesOnlyCheckbox.IsChecked
            };

            return pathingConfig;
        }

        public void SetData(PathConfig config)
        {
            _attackMobsCheckbox.SetValue(config.AttackMobs);
            _useTauntItemCheckbox.SetValue(config.UseTauntItem);
            _pathToMobsCheckbox.SetValue(config.PathToMobs);
            _pathToCorpses.SetValue(config.PathToCorpses);
            _tauntRangeTextView.Text = config.TauntRange.ToString();
            _pathRangeTextView.Text = config.PathRange.ToString();
            _wanderLimitTextView.Text = config.WanderLimit.ToString();
            _fightTimeoutTextView.Text = config.FightTimeoutPeriod.ToString();
            _lootTimeoutTextView.Text = config.LootTimeoutPeriod.ToString();
            _disableIfHpCheckbox.SetValue(config.DisableIfHp);
            _disableIfNpCheckbox.SetValue(config.DisableIfNp);
            _disableIfAttackedCheckbox.SetValue(config.DisableIfAttacked);
            _hpPercentTextView.Text = config.HealthPercent.ToString();
            _npPercentTextView.Text = config.NanoPercent.ToString();
            _followTarget.Text = config.FollowTargetName;
            _attackPadding.Text = config.AttackPadding.ToString();
            _overrideAttackCheckbox.SetValue(config.OverrideAttack); 
            _attackRange.Text = config.AttackRange.ToString();
            _priorityNamesOnlyCheckbox.SetValue(config.AttackPriorityOnly);
            _syncSettingsCheckbox.SetValue(config.SyncSettings);
        }
    }
}