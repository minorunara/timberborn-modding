using System;
using ModSettings.Core;
using ModSettings.Common;
using Timberborn.Modding;
using Timberborn.SettingsSystem;

namespace ToriiGatesLanternMod
{
    public class WindSwingAnimatorSettings : ModSettingsOwner
    {
        public WindSwingAnimatorSettings(ISettings settings,
                                         ModSettingsOwnerRegistry modSettingsOwnerRegistry,
                                         ModRepository modRepository)
            : base(settings, modSettingsOwnerRegistry, modRepository)
        {
        }

        // アニメーションの有効化/無効化の設定
        public ModSetting<bool> WindSwingEnabledSetting { get; } = new(
            true, ModSettingDescriptor.CreateLocalized("minorunara.ToriiGatesLantern.SettingsDiscription")
            .SetLocalizedTooltip("minorunara.ToriiGatesLantern.SettingsTooltip"));

        public override string HeaderLocKey => "minorunara.ToriiGatesLantern.SettingsHeader";
        public override int Order => 10;
        protected override string ModId => "ToriiLanterns";
    }
}
