using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Aspects;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

namespace SmartDisenchantEverything
{
    public class Program
    {
        static Lazy<Settings> _settings = null!;
        public static Settings Settings => _settings.Value;

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline
                .Instance.AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "SmartDisenchantEverything.esp")
                .SetAutogeneratedSettings("Settings", "settings.json", out _settings)
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            List<string> PatchedItems = new();
            List<string> SkippedItems = new();
            List<string> ManualCheckItems = new();

            foreach (var item in state.LoadOrder.PriorityOrder.IItem().WinningOverrides())
            {
                if (Settings.ItemBlacklist.Contains(item))
                    continue;

                // check if item has enchantment
                if (
                    item is not IEnchantableGetter itemEnchants
                    || itemEnchants.ObjectEffect == null
                )
                    continue;

                // skip armor that is non-playable
                if (item is IArmorGetter armorGetter && armorGetter.MajorFlags.HasFlag(Armor.MajorFlag.NonPlayable)) continue;

                // skip weapon that is non-playable
                if (item is IWeaponGetter weaponGetter && weaponGetter.MajorFlags.HasFlag(Weapon.MajorFlag.NonPlayable)) continue;

                // check if item has keywords
                if (item is not IKeywordedGetter itemKeywords || itemKeywords.Keywords == null)
                    continue;

                // skip if item has blacklisted keywords
                bool skip = false;
                foreach (IFormLinkGetter<IKeywordGetter> itemKeyword in itemKeywords.Keywords)
                {
                    if (Settings.KywdBlacklist.Contains(itemKeyword))
                    {
                        skip = true;
                        break;
                    }
                }
                if (skip) continue;

                // check if item has magic disallow enchanting keyword
                if (!itemKeywords.Keywords.Contains(Skyrim.Keyword.MagicDisallowEnchanting))
                    continue;

                // skip if item is daedric item
                if (
                    Settings.SkipDaedric == true
                    && itemKeywords.Keywords.Contains(Skyrim.Keyword.DaedricArtifact)
                )
                    continue;

                if (
                    Settings.PatchScriptVDAM == false
                    && item is IScriptedGetter itemScripts
                    && itemScripts.VirtualMachineAdapter != null
                    && itemScripts.VirtualMachineAdapter.Scripts.Any(e =>
                        e.Properties.Any(r =>
                            r is ScriptObjectProperty rObject
                            && (
                                !state.LinkCache.TryResolve<IQuestGetter>(
                                    rObject.Object.FormKey,
                                    out var questRecord
                                )
                                && !state.LinkCache.TryResolve<ILinkedReferenceGetter>(
                                    rObject.Object.FormKey,
                                    out var referenceRecord
                                )
                                && !state.LinkCache.TryResolve<IMessageGetter>(
                                    rObject.Object.FormKey,
                                    out var messageRecord
                                )
                            )
                        )
                    )
                )
                {
                    if (item.EditorID != null)
                        ManualCheckItems.Add(item.EditorID);
                    continue;
                }

                if (
                    // check if enchantment requires specific item keywords
                    Settings.PatchEffectCond == false
                    && (
                        !state.LinkCache.TryResolve<IObjectEffectGetter>(
                            itemEnchants.ObjectEffect.FormKey,
                            out var enchantment
                        )
                        || enchantment.Effects.Any(o =>
                            o.Conditions != null
                            && o.Conditions.Any(e =>
                                e.Data is WornApparelHasKeywordCountConditionData
                                || e.Data is WornHasKeywordConditionData
                                || e.Data is GetEquippedConditionData
                            )
                        )
                    )
                )
                {
                    if (item.EditorID != null)
                        SkippedItems.Add(item.EditorID);
                    continue;
                }
                ;

                if (item is IWeaponGetter)
                {
                    var modifiedWeapon = state.PatchMod.Weapons.GetOrAddAsOverride(item);

                    if (modifiedWeapon is not IKeywordedGetter || modifiedWeapon.Keywords == null)
                        continue;

                    modifiedWeapon.Keywords.Remove(Skyrim.Keyword.MagicDisallowEnchanting);
                }
                else if (item is IArmorGetter)
                {
                    var modifiedArmor = state.PatchMod.Armors.GetOrAddAsOverride(item);

                    if (modifiedArmor is not IKeywordedGetter || modifiedArmor.Keywords == null)
                        continue;

                    modifiedArmor.Keywords.Remove(Skyrim.Keyword.MagicDisallowEnchanting);
                }

                if (item.EditorID != null)
                    PatchedItems.Add(item.EditorID);
            }

            // end reports
            if (PatchedItems.Count > 0)
            {
                Console.WriteLine(string.Empty);
                Console.WriteLine(string.Empty);
                Console.WriteLine("============================================");
                Console.WriteLine(string.Empty);
                Console.WriteLine("Successfully patched items:");
                Console.WriteLine("--------------");
                foreach (string item in PatchedItems)
                    Console.WriteLine(item);
            }

            if (SkippedItems.Count > 0)
            {
                Console.WriteLine(string.Empty);
                Console.WriteLine(string.Empty);
                Console.WriteLine("============================================");
                Console.WriteLine(string.Empty);
                Console.WriteLine(
                    "Items with an empty enchantment field or items that have conditions requiring specific keywords and/or equipped items:"
                );
                Console.WriteLine("--------------");
                foreach (string item in SkippedItems)
                    Console.WriteLine(item);
            }

            if (ManualCheckItems.Count > 0)
            {
                Console.WriteLine(string.Empty);
                Console.WriteLine(string.Empty);
                Console.WriteLine("============================================");
                Console.WriteLine(string.Empty);
                Console.WriteLine(
                    "Items with non-quest scripts attached (Abilities might be partially contained within scripts rather than enchantment only):"
                );
                Console.WriteLine("--------------");
                foreach (string item in ManualCheckItems)
                    Console.WriteLine(item);
            }
        }
    }
}
