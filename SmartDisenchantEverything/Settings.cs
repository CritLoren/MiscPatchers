using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis.Settings;

namespace SmartDisenchantEverything
{
    public class Settings
    {
        [SynthesisSettingName(
            "Patch items with effects that require specific equipment (some enchantments look for item sets)"
        )]
        [SynthesisDescription(
            "Some enchantments might look for non-item keywords, like checking if your equipment is comprised of light armour"
        )]
        public bool PatchEffectCond { get; set; } = false;

        [SynthesisSettingName(
            "Patch items with non-quest script properties (some items have spells attached via scripts instead of enchantments)"
        )]
        [SynthesisDescription(
            "Sometimes items have the enchantment effect attached as a script, sometimes it's just a way to push a quest forward"
        )]
        public bool PatchScriptVDAM { get; set; } = false;

        [SynthesisSettingName("Skip Daedric artifacts")]
        public bool SkipDaedric { get; set; } = false;

        [SynthesisSettingName("Blacklist items")]
        public List<IFormLinkGetter<IItemGetter>> ItemBlacklist { get; set; } = new();

        [SynthesisSettingName("Blacklist keywords")]
        public List<IFormLinkGetter<IKeywordGetter>> KywdBlacklist { get; set; } = new();
    }
}
