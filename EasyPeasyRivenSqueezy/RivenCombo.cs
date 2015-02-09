﻿using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;

namespace EasyPeasyRivenSqueezy
{
    internal class RivenCombo
    {
        private static Items.Item Hydra
        {
            get { return ItemData.Ravenous_Hydra_Melee_Only.GetItem(); }
        }

        private static Items.Item Tiamat
        {
            get { return ItemData.Tiamat_Melee_Only.GetItem(); }
        }

        private static Items.Item Ghostblade
        {
            get { return ItemData.Youmuus_Ghostblade.GetItem(); }
        }

        public static void OnGameUpdate(EventArgs args)
        {
            if (Riven.Ignite.IsReady())
            {
                IgniteKillSecure();
            }
            if (Environment.TickCount - Riven.LastQ >= 4000 - Game.Ping / 2 - Riven.Q.Delay * 1000 && Riven.QCount != 0 &&
                !Riven.Player.IsRecalling() && Riven.GetBool("KeepQAlive"))
            {
                Riven.Q.Cast(Game.CursorPos);
            }

            switch (Riven.Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    DoCombo();
                    break;
            }
        }

        private static void IgniteKillSecure()
        {
            var targets = ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(Riven.Ignite.Range));

            var objAiHeroes = targets as Obj_AI_Hero[] ?? targets.ToArray();
            if (Riven.GetBool("IgniteKillable"))
            {
                var bestTarget =
                    objAiHeroes.Where(
                        x => Riven.Player.GetSummonerSpellDamage(x, Damage.SummonerSpell.Ignite) > x.Health)
                        .OrderByDescending(x => x.Distance(Riven.Player))
                        .FirstOrDefault();

                if (bestTarget != null)
                {
                    Riven.Ignite.CastOnUnit(bestTarget);
                }
            }

            if (Riven.GetBool("IgniteKS"))
            {
                var bestTarget =
                    objAiHeroes.Where(
                        x => Riven.Player.GetSummonerSpellDamage(x, Damage.SummonerSpell.Ignite) / 5 > x.Health)
                        .OrderByDescending(x => x.Distance(Riven.Player))
                        .FirstOrDefault();

                if (bestTarget != null)
                {
                    Riven.Ignite.CastOnUnit(bestTarget);
                }
            }
        }

        private static void UseWindSlash(Obj_AI_Base target)
        {
            // W -> R
            if (Riven.W.IsReady() &&
                ObjectManager.Get<Obj_AI_Hero>()
                    .Any(
                        x =>
                            x.IsValidTarget(Riven.W.Range) &&
                            Riven.Player.GetSpellDamage(target, SpellSlot.W) +
                            Riven.Player.GetSpellDamage(target, SpellSlot.R) > target.Health))
            {
                Riven.R.Cast(target, false, true);
                Riven.W.Cast();
            }
            // Q -> Q -> E -> R -> Q
            else if (Riven.QCount == 2 && Riven.Q.IsReady() && Riven.E.IsReady() &&
                     Riven.Player.GetSpellDamage(target, SpellSlot.Q) + Riven.Player.GetSpellDamage(target, SpellSlot.R) +
                     Riven.PassiveDmg + Riven.Player.GetAutoAttackDamage(target) > target.Health)
            {
                Riven.E.Cast();
                Riven.R.Cast(target, false, true);
            }
            else if (GetCircleThingDamage(target) + Riven.Player.GetSpellDamage(target, SpellSlot.R) > target.Health)
            {
                CastCircleThing();
                Riven.R.Cast(target, false, true);
            }
            else if (Riven.Player.GetSpellDamage(target, SpellSlot.R) > target.Health)
            {
                Riven.R.Cast(target, false, true);
            }
        }

        private static double GetCircleThingDamage(Obj_AI_Base target)
        {
            if (Hydra.IsReady())
            {
                return Riven.Player.GetItemDamage(target, Damage.DamageItems.Hydra);
            }
            if (Tiamat.IsReady())
            {
                return Riven.Player.GetItemDamage(target, Damage.DamageItems.Tiamat);
            }

            return 0;
        }

        private static void DoCombo()
        {           
            var target = TargetSelector.GetTarget(Riven.EWRange, TargetSelector.DamageType.Physical);

            if (!target.IsValidTarget())
            {
                //Riven.Orbwalker.SetOrbwalkingPoint(Game.CursorPos);
                return;
            }

            if (Riven.GetBool("FollowTarget"))
            {
               // Riven.Orbwalker.SetOrbwalkingPoint(target.ServerPosition);
            }

            if (Ghostblade.IsReady())
            {
                Ghostblade.Cast();
            }

            if (Riven.RActivated && Riven.CanWindSlash)
            {
                UseWindSlash(target);
            }

            // Use R logic
            if (CanHardEngage(target) && !Riven.RActivated && Riven.R.IsReady())
            {
                // E -> R
                if (Riven.E.IsReady())
                {
                    var distE = Riven.Player.ServerPosition.Extend(target.Position, Riven.E.Range);

                    if (Riven.GetBool("DontEIntoWall") && distE.IsWall())
                    {
                        return;
                    }

                    Riven.E.Cast(target.ServerPosition);
                    var timeE = Game.Ping / 2f + Riven.E.Delay + (Riven.Player.Distance(distE) / Riven.E.Speed);

                    Utility.DelayAction.Add((int) timeE, () => Riven.R.Cast(Riven.Player));
                }
                    // Q -> Q -> E -> R -> Q
                else if (Riven.QCount == 2 && Riven.Q.IsReady() && Riven.E.IsReady())
                {
                    Riven.E.Cast();
                    Riven.R.Cast(target);
                }
                    // R -> W
                else if (Riven.W.IsReady() && ObjectManager.Get<Obj_AI_Hero>().Any(x => x.IsValidTarget(Riven.W.Range)))
                {
                    Riven.R.Cast(Riven.Player);
                    Riven.W.Cast();
                }
                else if(target.Distance(Riven.Player) < Orbwalking.GetRealAutoAttackRange(Riven.Player))
                {
                    Riven.R.Cast(Riven.Player);
                }
            }

            if (target.Distance(Riven.Player) < Riven.EWRange &&
                (!Riven.Q.IsReady() || target.Distance(Riven.Player) > Riven.Q.Range) && Riven.E.IsReady() &&
                Riven.W.IsReady())
            {
                var distE = Riven.Player.ServerPosition.Extend(target.Position, Riven.E.Range);

                if (Riven.GetBool("DontEIntoWall") && distE.IsWall())
                {
                    return;
                }

                Riven.E.Cast(target.Position);
                var timeE = Game.Ping / 2f + Riven.E.Delay + (Riven.Player.Distance(distE) / Riven.E.Speed);

                Utility.DelayAction.Add(
                    (int) timeE, delegate
                    {
                        if (Hydra.IsReady() || Tiamat.IsReady())
                        {
                            CastCircleThing();
                        }
                        else
                        {
                            CastW();
                        }
                    });
            }
            else if ((Hydra.IsReady() || Tiamat.IsReady()) && Riven.W.IsReady())
            {
                CastCircleThing();
            }
            else if (Riven.W.IsReady())
            {
                CastW();
            }
            else if (Riven.E.IsReady() && Riven.GetBool("GapcloseE"))
            {
                if (target.Distance(Riven.Player) < Orbwalking.GetRealAutoAttackRange(Riven.Player) &&
                    Riven.GetBool("DontEInAARange"))
                {
                    return;
                }

                var distE = Riven.Player.ServerPosition.Extend(target.Position, Riven.E.Range);

                if (Riven.GetBool("DontEIntoWall") && distE.IsWall())
                {
                    return;
                }

                Riven.E.Cast(target.Position);
            }
            else if (Riven.Q.IsReady() && Environment.TickCount - Riven.LastQ >= 2000 &&
                     Riven.Player.Distance(target) > Riven.Q.Range && Riven.GetBool("GapcloseQ"))
            {
                Riven.Q.Cast(target.Position);
            }
        }

        private static bool CanHardEngage(Obj_AI_Hero target)
        {
            var dmg = GetDamage(target);
            switch (Riven.Menu.Item("UseROption").GetValue<StringList>().SelectedValue)
            {
                case "Hard":
                    return dmg < target.Health;

                case "Easy":
                    return dmg > target.Health;

                case "Probably":
                    return dmg * 2 > target.Health;
            }

            return false;
        }

        public static void CastW()
        {
            if (ObjectManager.Get<Obj_AI_Hero>().Any(x => x.IsValidTarget(Riven.W.Range)))
            {
                Riven.W.Cast();
            }
        }

        public static void CastCircleThing()
        {
            if (ObjectManager.Get<Obj_AI_Hero>().Any(x => x.IsValidTarget(ItemData.Ravenous_Hydra_Melee_Only.Range)))
            {
                Hydra.Cast();
                Tiamat.Cast();
            }
        }

        public static float GetDamage(Obj_AI_Hero hero)
        {
            var dmg = 0f;

            if (Riven.Q.IsReady())
            {
                dmg += Riven.Q.GetDamage(hero) * 3;
                dmg += Riven.PassiveDmg * 3;
                dmg += (float) Riven.Player.GetAutoAttackDamage(hero, true) * 3;
            }

            if (Riven.E.IsReady())
            {
                dmg += Riven.PassiveDmg;
            }

            if (Riven.W.IsReady())
            {
                dmg += Riven.W.GetDamage(hero);
                dmg += Riven.PassiveDmg;
            }

            if (Hydra.IsReady())
            {
                dmg += (float) Riven.Player.GetItemDamage(hero, Damage.DamageItems.Hydra);
            }

            if (Tiamat.IsReady())
            {
                dmg += (float) Riven.Player.GetItemDamage(hero, Damage.DamageItems.Tiamat);
            }

            dmg += (float) Riven.Player.GetAutoAttackDamage(hero, true);

            return dmg;
        }

        private static bool KillableBy(SpellSlot slot, Obj_AI_Base target)
        {
            return Riven.Player.GetSpellDamage(target, slot) > target.Health;
        }
    }
}