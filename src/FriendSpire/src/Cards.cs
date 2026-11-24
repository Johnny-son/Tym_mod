using BaseLib.Abstracts;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace FriendSpire;

[Pool(typeof(CornCards))]
public abstract class CornCard(int cost, CardType type, CardRarity rarity, TargetType target, string art)
    : CustomCardModel(cost, type, rarity, target)
{
    public override string PortraitPath => $"res://FriendSpire/cards/{art}.png";
    public override string CustomPortraitPath => PortraitPath;
    public override string BetaPortraitPath => PortraitPath;

    protected Task<AttackCommand> Hit(PlayerChoiceContext ctx, CardPlay play, int count = 1) =>
        DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play).Targeting(play.Target!)
            .WithHitCount(count).WithHitFx(VfxCmd.slashPath).Execute(ctx);
    protected Task Block(CardPlay play) => CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
    protected Task Switch(PlayerChoiceContext ctx, Form form) => Stances.Switch(ctx, Owner, form, this);
    protected Task Apply<T>(PlayerChoiceContext ctx, int amount) where T : PowerModel =>
        PowerCmd.Apply<T>(ctx, Owner.Creature, amount, Owner.Creature, this);
}

public class UsedCar() : CornCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy, "strike")
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9, ValueProp.Move)];
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Hit(ctx, play);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}

public class Confession() : CornCard(2, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy, "eruption")
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(12, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Hit(ctx, play);
        await Switch(ctx, Form.Labor);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public class EmptyFist() : CornCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, "empty_fist")
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(12, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Hit(ctx, play);
        await Switch(ctx, Form.Normal);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(6);
}

public class CarCrash() : CornCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, "tantrum")
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4, ValueProp.Move), new HitCountVar(3)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Hit(ctx, play, DynamicVars.HitCount.IntValue);
        await Switch(ctx, Form.Labor);
    }
    protected override void OnUpgrade() => DynamicVars.HitCount.UpgradeValueBy(1);
}

public class MyMistake() : CornCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, "fear_no_evil")
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(12, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        bool attacking = play.Target?.Monster?.IntendsToAttack == true;
        await Hit(ctx, play);
        if (attacking) await Switch(ctx, Form.Emo);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}

public class Regret() : CornCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, "wallop")
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(20, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var attack = await Hit(ctx, play);
        var damage = attack.Results.SelectMany(hit => hit).Sum(result => result.UnblockedDamage);
        await CreatureCmd.GainBlock(Owner.Creature, damage, ValueProp.Unpowered, play);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(10);
}

public class Ragnarok() : CornCard(3, CardType.Attack, CardRarity.Rare, TargetType.RandomEnemy, "ragnarok")
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(12, ValueProp.Move), new HitCountVar(5)];
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(DynamicVars.HitCount.IntValue)
            .FromCard(this, play).TargetingRandomOpponents(CombatState!).WithHitFx(VfxCmd.slashPath).Execute(ctx);
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars.HitCount.UpgradeValueBy(1);
    }
}

public class Dumplings() : CornCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self, "defend")
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(8, ValueProp.Move)];
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Block(play);
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2);
}

public class Period() : CornCard(2, CardType.Skill, CardRarity.Basic, TargetType.Self, "vigilance")
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(10, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Block(play);
        await Switch(ctx, Form.Emo);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(5);
}

public abstract class Opening<T>(Form form, string art) : CornCard(1, CardType.Skill, CardRarity.Common, TargetType.AllEnemies, art) where T : PowerModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Switch(ctx, form);
        await PowerCmd.Apply<T>(ctx, CombatState!.HittableEnemies.ToArray(), 2, Owner.Creature, this);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public class FirstLove() : Opening<VulnerablePower>(Form.Labor, "crescendo");
public class SoftHeart() : Opening<WeakPower>(Form.Emo, "tranquility");

public class BikeSeat() : CornCard(0, CardType.Skill, CardRarity.Common, TargetType.Self, "prostrate")
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Injury", 2), new BlockVar(8, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Stances.AddInjury(ctx, Owner, DynamicVars["Injury"].IntValue, this);
        await Block(play);
    }
    protected override void OnUpgrade() => DynamicVars["Injury"].UpgradeValueBy(2);
}

public class SoWhat() : CornCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, "inner_peace")
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (Owner.Creature.HasPower<Emo>()) await CardPileCmd.Draw(ctx, DynamicVars.Cards.IntValue, Owner);
        else await Switch(ctx, Form.Emo);
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

public class EmptyMind() : CornCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, "empty_mind")
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CardPileCmd.Draw(ctx, DynamicVars.Cards.IntValue, Owner);
        await Switch(ctx, Form.Normal);
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

public class Argument() : CornCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self, "worship")
{
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Stances.AddInjury(ctx, Owner, 6, this);
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

public class GoSigma() : CornCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self, "blasphemy")
{
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await Switch(ctx, Form.Sigma);
        if (!Owner.Creature.HasPower<LastTurn>()) await Apply<LastTurn>(ctx, 1);
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

public class Heartbroken() : CornCard(3, CardType.Skill, CardRarity.Rare, TargetType.Self, "omniscience")
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var choices = await CardSelectCmd.FromCombatPile(ctx, PileType.Draw.GetPile(Owner), Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1));
        var selected = choices.FirstOrDefault();
        if (selected == null) return;

        // The engine replays an action before moving it to its destination pile.
        selected.BaseReplayCount++;
        var oldExhaust = selected.ExhaustOnNextPlay;
        selected.ExhaustOnNextPlay = true;
        try
        {
            if (selected.Type == CardType.Power) HeartbrokenPowerDestination.Selected.Add(selected);
            await CardCmd.AutoPlay(ctx, selected, null);
        }
        finally
        {
            HeartbrokenPowerDestination.Selected.Remove(selected);
            selected.BaseReplayCount--;
            selected.ExhaustOnNextPlay = oldExhaust;
        }
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

// A played power normally disappears. Only the specific power chosen by Heartbroken goes to Exhaust.
[HarmonyPatch(typeof(CardModel), "GetResultLocationForCardPlay")]
internal static class HeartbrokenPowerDestination
{
    internal static readonly HashSet<CardModel> Selected = [];
    private static void Postfix(CardModel __instance, ref CardLocation __result)
    {
        if (Selected.Contains(__instance))
            __result = new CardLocation(__instance.Owner, PileType.Exhaust, CardPilePosition.Bottom);
    }
}

public class Inseparable() : CornCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, "rushdown")
{
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Apply<Attached>(ctx, 2);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public class KnifeSkills() : CornCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, "mental_fortress")
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Guard", 5)];
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Apply<KnifeGuard>(ctx, DynamicVars["Guard"].IntValue);
    protected override void OnUpgrade() => DynamicVars["Guard"].UpgradeValueBy(3);
}

public class Bicker() : CornCard(1, CardType.Power, CardRarity.Rare, TargetType.Self, "devotion")
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Injury", 2)];
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => Apply<Bickering>(ctx, DynamicVars["Injury"].IntValue);
    protected override void OnUpgrade() => DynamicVars["Injury"].UpgradeValueBy(2);
}

[Pool(typeof(TokenCardPool))]
public class Miracle() : CustomCardModel(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override string PortraitPath => "res://FriendSpire/cards/miracle.png";
    public override string CustomPortraitPath => PortraitPath;
    public override string BetaPortraitPath => PortraitPath;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1);
}
