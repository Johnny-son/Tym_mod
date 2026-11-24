using BaseLib.Abstracts;
using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace FriendSpire;

public enum Form { Normal, Labor, Emo, Sigma }

public static class Stances
{
    public static async Task Switch(PlayerChoiceContext ctx, Player player, Form next, CardModel? card = null)
    {
        var body = player.Creature;
        var current = body.Powers.OfType<StancePower>().SingleOrDefault();
        if ((current?.Form ?? Form.Normal) == next || body.IsDead) return;
        if (current != null) await PowerCmd.Remove(current);
        if (current is Emo)
            await PlayerCmd.GainEnergy(player.Relics.Any(r => r is CornLotus) ? 3 : 2, player);

        switch (next)
        {
            case Form.Labor: await PowerCmd.Apply<Labor>(ctx, body, 1, body, card); break;
            case Form.Emo: await PowerCmd.Apply<Emo>(ctx, body, 1, body, card); break;
            case Form.Sigma:
                await PowerCmd.Apply<Sigma>(ctx, body, 1, body, card);
                await PlayerCmd.GainEnergy(3, player);
                break;
        }

        if (body.Player?.Character is SweetCorn && NCombatRoom.Instance?.GetCreatureNode(body)?.Body is Sprite2D sprite)
            sprite.Texture = ResourceLoader.Load<Texture2D>(Mod.Images + next.ToString().ToLowerInvariant() + ".jpg");
        if (body.GetPowerAmount<KnifeGuard>() is > 0 and var block)
            await CreatureCmd.GainBlock(body, block, ValueProp.Unpowered, null);
        if (next == Form.Labor && body.GetPowerAmount<Attached>() is > 0 and var draw)
            await CardPileCmd.Draw(ctx, draw, player);
    }

    public static async Task AddInjury(PlayerChoiceContext ctx, Player player, int amount, CardModel? card)
    {
        var body = player.Creature;
        var injury = await PowerCmd.Apply<Injury>(ctx, body, amount, body, card);
        if (injury is not { Amount: >= 10 }) return;
        await PowerCmd.ModifyAmount(ctx, injury, -(injury.Amount / 10 * 10), body, card);
        await Switch(ctx, player, Form.Sigma, card);
    }
}

public abstract class CornPower(string icon) : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => Mod.Images + icon + ".svg";
    public override string CustomBigIconPath => CustomPackedIconPath;
}

public abstract class StancePower(Form form, string icon) : CornPower(icon)
{
    public Form Form => form;
    public override PowerStackType StackType => PowerStackType.Single;
}

public class Labor() : StancePower(Form.Labor, "wrath")
{
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount,
        ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (!props.IsPoweredAttack_()) return 1;
        if (target == Owner && dealer?.Side == CombatSide.Enemy) return 4;
        return dealer == Owner && cardSource?.Type == CardType.Attack ? 2 : 1;
    }
}

public class Emo() : StancePower(Form.Emo, "calm");
public class Sigma() : StancePower(Form.Sigma, "divinity")
{
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount,
        ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        dealer == Owner && target?.Side != Owner.Side ? 3 : 1;

    private int _enteredTurn;
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _enteredTurn = Owner.Player!.PlayerCombatState!.TurnNumber;
        return Task.CompletedTask;
    }
    public override Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState state) =>
        player == Owner.Player && player.PlayerCombatState!.TurnNumber > _enteredTurn
            ? Stances.Switch(ctx, player, Form.Normal) : Task.CompletedTask;
}

public class Injury() : CornPower("mantra");
public class Attached() : CornPower("wrath");
public class KnifeGuard() : CornPower("jade");
public class Bickering() : CornPower("mantra")
{
    public override Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player) =>
        player == Owner.Player ? Stances.AddInjury(ctx, player, Amount, null) : Task.CompletedTask;
}

public class LastTurn() : CornPower("divinity")
{
    private int _playedTurn;
    public override PowerStackType StackType => PowerStackType.Single;
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _playedTurn = Owner.Player!.PlayerCombatState!.TurnNumber;
        return Task.CompletedTask;
    }
    public override Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState state) =>
        player == Owner.Player && player.PlayerCombatState!.TurnNumber > _playedTurn
            ? CreatureCmd.Kill(Owner, force: true) : Task.CompletedTask;
}
