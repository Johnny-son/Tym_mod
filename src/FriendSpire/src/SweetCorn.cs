using BaseLib.Abstracts;
using BaseLib.Utils;
using BaseLib.Utils.NodeFactories;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;

namespace FriendSpire;

[ModInitializer(nameof(Initialize))]
public static class Mod
{
    public const string Images = "res://FriendSpire/images/";
    public static void Initialize() => new Harmony("FriendSpire.SweetCorn").PatchAll(typeof(Mod).Assembly);
}

public class SweetCorn : PlaceholderCharacterModel
{
    public override Color NameColor => new("e6bd51");
    public override CharacterGender Gender => CharacterGender.Masculine;
    public override int StartingHp => 185;
    public override CardPoolModel CardPool => ModelDb.CardPool<CornCards>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<CornRelics>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<CornPotions>();
    public override IReadOnlyList<RelicModel> StartingRelics => [ModelDb.Relic<PureWater>()];
    public override IEnumerable<CardModel> StartingDeck =>
        Enumerable.Repeat<CardModel>(ModelDb.Card<UsedCar>(), 5)
            .Concat(Enumerable.Repeat<CardModel>(ModelDb.Card<Dumplings>(), 4))
            .Append(ModelDb.Card<Confession>()).Append(ModelDb.Card<Period>());
    public override string CustomVisualPath => "res://FriendSpire/fighter.tscn";
    public override string CustomRestSiteAnimPath => "res://FriendSpire/rest.tscn";
    public override string CustomMerchantAnimPath => "res://FriendSpire/shop.tscn";
    public override string CustomCharacterSelectBg => "res://FriendSpire/select.tscn";
    public override string CustomCharacterSelectIconPath => Mod.Images + "portrait.png";
    public override string CustomCharacterSelectLockedIconPath => CustomCharacterSelectIconPath;
    public override string CustomIconTexturePath => CustomCharacterSelectIconPath;
    public override string CustomIconOutlineTexturePath => CustomIconTexturePath;
    public override string CustomMapMarkerPath => CustomCharacterSelectIconPath;
    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }
}

public class CornCards : CustomCardPoolModel
{
    public override string Title => "sweet_corn";
    public override string EnergyColorName => "ironclad";
    public override float H => 0.12f;
    public override float S => 0.85f;
    public override float V => 1f;
    public override Color DeckEntryCardColor => new("e6bd51");
    public override bool IsColorless => false;
}

public class CornRelics : CustomRelicPoolModel
{
    public override Color LabOutlineColor => new("e6bd51");
    public override string EnergyColorName => "ironclad";
}

public class CornPotions : CustomPotionPoolModel
{
    public override Color LabOutlineColor => new("e6bd51");
    public override string EnergyColorName => "ironclad";
}

[Pool(typeof(CornRelics))]
public abstract class CornRelic(string icon) : CustomRelicModel
{
    public override string PackedIconPath => Mod.Images + icon + ".svg";
    protected override string PackedIconOutlinePath => PackedIconPath;
    protected override string BigIconPath => PackedIconPath;
}

public class PureWater() : CornRelic("water")
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner || player.PlayerCombatState?.TurnNumber != 1) return;
        var miracle = player.Creature.CombatState!.CreateCard<Miracle>(player);
        await CardPileCmd.AddGeneratedCardToCombat(miracle, PileType.Hand, player);
    }
}

public class CornLotus() : CornRelic("lotus")
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
}

[HarmonyPatch(typeof(AncientEventModel), "GenerateInitialOptionsWrapper")]
internal static class ActTwoLotus
{
    private static readonly System.Reflection.MethodInfo MakeOption = AccessTools.Method(
        typeof(AncientEventModel), "RelicOption", [typeof(RelicModel), typeof(string)]);

    private static void Postfix(AncientEventModel __instance, IReadOnlyList<EventOption> __result)
    {
        var player = __instance.Owner;
        if (player?.Character is not SweetCorn || player.RunState.CurrentActIndex != 1 ||
            player.Relics.Any(r => r is CornLotus) || __result is not List<EventOption> options ||
            options.Count == 0 || !options.Any(o => o.Relic != null)) return;
        var lotus = ModelDb.Relic<CornLotus>().ToMutable();
        if (lotus.IsAllowed(player.RunState))
            options[^1] = (EventOption)MakeOption.Invoke(__instance, [lotus, "INITIAL"])!;
    }
}
