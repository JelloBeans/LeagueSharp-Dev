using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Abstracts;
using SFXChallenger.Args;
using SFXChallenger.Enumerations;
using SFXChallenger.Helpers;
using SFXChallenger.Library;
using SFXChallenger.Library.Logger;
using SFXChallenger.Managers;
using SFXChallenger.SFXTargetSelector;
using SharpDX;
using Collision = LeagueSharp.Common.Collision;
using DamageType = SFXChallenger.Enumerations.DamageType;
using ItemData = LeagueSharp.Common.Data.ItemData;
using MinionManager = SFXChallenger.Library.MinionManager;
using MinionOrderTypes = SFXChallenger.Library.MinionOrderTypes;
using MinionTeam = SFXChallenger.Library.MinionTeam;
using MinionTypes = SFXChallenger.Library.MinionTypes;
using Orbwalking = SFXChallenger.Wrappers.Orbwalking;
using Spell = SFXChallenger.Wrappers.Spell;
using TargetSelector = SFXChallenger.SFXTargetSelector.TargetSelector;
using Utils = SFXChallenger.Helpers.Utils;

namespace SFXChallenger.Champions
{
    class Kennen : Champion
    {
        protected override ItemFlags ItemFlags
        {
            get
            {
                return ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee;
            }
        }

        protected override ItemUsageType ItemUsage
        {
            get
            {
                return ItemUsageType.AfterAttack;
            }
        }

        protected override void AddToMenu()
        {
            #region Combo Menu
            var comboMenu = Menu.AddSubMenu(new Menu("Combo", string.Format("{0}.combo", Menu.Name)));
            comboMenu.AddItem(new MenuItem(string.Format("{0}.q", comboMenu.Name), "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem(string.Format("{0}.w", comboMenu.Name), "Use W").SetValue(true));

            var comboHitChanceMenu = comboMenu.AddSubMenu(new Menu("Hitchance", string.Format("{0}.hitchance", comboMenu.Name)));
            var comboHitChanceDictionary = new Dictionary<string, HitChance>
            {
                { "Q", HitChance.VeryHigh }
            };
            HitchanceManager.AddToMenu(comboHitChanceMenu, "combo", comboHitChanceDictionary);
            #endregion

            #region Harass Menu
            var harassMenu = Menu.AddSubMenu(new Menu("Harass", string.Format("{0}.harass", Menu.Name)));
            harassMenu.AddItem(new MenuItem(string.Format("{0}.q", harassMenu.Name), "Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem(string.Format("{0}.w", harassMenu.Name), "Use W").SetValue(true));

            var harassHitChanceMenu = harassMenu.AddSubMenu(new Menu("Hitchance", string.Format("{0}.hitchance", harassMenu.Name)));
            var harassHitChanceDictionary = new Dictionary<string, HitChance>
            {
                { "Q", HitChance.High }
            };
            HitchanceManager.AddToMenu(harassHitChanceMenu, "harass", harassHitChanceDictionary);
            #endregion
        }

        protected override void Combo()
        {
            try
            {
                var useQ = Menu.Item(Menu.Name + ".combo.q").GetValue<bool>() && W.IsReady();
                var useW = Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady();

                if (useQ)
                {
                    Casting.SkillShot(Q, Q.GetHitChance("combo"));
                }

                if (useW)
                {
                    CastW();
                }
            } catch(Exception)
            {

            }
        }

        protected override void Flee()
        {
            try
            {
                if (!Player.HasBuff("KennenLightningRush"))
                {
                    E.Cast();
                }
            }
            catch (Exception)
            {

            }
        }

        protected override void Harass()
        {
            try
            {
                var useQ = Menu.Item(Menu.Name + ".harass.q").GetValue<bool>() && W.IsReady();
                var useW = Menu.Item(Menu.Name + ".harass.w").GetValue<bool>() && W.IsReady();

                if (useQ)
                {
                    Casting.SkillShot(Q, Q.GetHitChance("harass"));
                }

                if (useW)
                {
                    CastW();
                }
            }
            catch (Exception)
            {

            }
        }        

        protected override void JungleClear()
        {
        }

        protected override void Killsteal()
        {
        }

        protected override void LaneClear()
        {
        }

        protected override void OnLoad()
        {
        }

        protected override void OnPostUpdate()
        {
        }

        protected override void OnPreUpdate()
        {
            if (Player.HasBuff("KennenLightningRush"))
            {
                Orbwalker.SetAttack(false);
            }
            else
            {
                Orbwalker.SetAttack(true);
            }
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 1050);
            Q.SetSkillshot(0.125f, 50, 1650, true, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 900);

            E = new Spell(SpellSlot.E);

            R = new Spell(SpellSlot.R, 500);
        }

        private void CastW()
        {
            try {
                var target = Orbwalker.GetTarget();
                var hero = target as Obj_AI_Hero;
                if (hero != null && Player.Distance(hero) <= W.Range)
                {
                    var buff = GetElectricalSurge(hero);
                    if (buff.Count > 0)
                    {
                        W.Cast();
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        public bool HasElectricalSurge(Obj_AI_Base target)
        {
            return GetElectricalSurge(target) != null;
        }

        public BuffInstance GetElectricalSurge(Obj_AI_Base target)
        {
            return target.Buffs.FirstOrDefault(b => b.Caster.IsMe && b.IsValid && b.DisplayName.Equals("KennenMarkOfStorm", StringComparison.OrdinalIgnoreCase));
        }
    }
}
