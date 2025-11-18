using AOSharp.Core;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Core.UI;
using System;
using System.Linq;

namespace AutomatonRoamba
{
    public static class Extensions
    {
        public static Vector3 GetPathPos(this SimpleChar simpleChar)
        {
            return simpleChar.IsPathing && !simpleChar.IsAttacking ? simpleChar.Position + simpleChar.Rotation * Vector3.Forward * 2f : simpleChar.Position;
        }

        public static float GetLowestWeaponAttackRange(this LocalPlayer localPlayer)
        {
            var rangePrc = localPlayer.GetStat(Stat.RangeIncreaserWeapon) / 100f;

            if (localPlayer.Weapons.Count == 0)
                return 4.5f * (1f + rangePrc);

            return localPlayer.Weapons.Values.Select(x => x.AttackRange).OrderBy(x => x).FirstOrDefault() * (1f + rangePrc);
        }

        public static bool IsInWeaponHitRange(this SimpleChar target, float padding)
        {
            return Vector3.Distance(target.GetPathPos(), DynelManager.LocalPlayer.Position) < Mathf.Max(0, DynelManager.LocalPlayer.GetLowestWeaponAttackRange() - padding);
        }

        public static void SetText(this ComboBox comboBox, string text)
        {
            IntPtr pTextView = TextInputView_c.GetTextView(comboBox.Pointer);

            if (pTextView == IntPtr.Zero)
                return;

            TextView_c.SetText(pTextView, StdString.Create(text).Pointer);
        }

        public static string GetText(this ComboBox comboBox)
        {
            IntPtr pTextView = TextInputView_c.GetTextView(comboBox.Pointer);

            if (pTextView == IntPtr.Zero)
                return string.Empty;

            Variant var = Variant.Create();
            IntPtr pStr = TextView_c.GetValue(pTextView, var.Pointer);

            if (pStr == IntPtr.Zero)
                return string.Empty;

            return var.AsString();
        }
    }
}
