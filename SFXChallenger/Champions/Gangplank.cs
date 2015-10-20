using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Abstracts;
using SFXChallenger.Enumerations;
using SFXChallenger.Helpers;
using SFXChallenger.Library;
using SFXChallenger.Library.Logger;
using SFXChallenger.Managers;
using Spell = SFXChallenger.Wrappers.Spell;

namespace SFXChallenger.Champions
{
    /// <summary>
    /// 
    /// </summary>
    internal class Gangplank : TChampion
    {
        /// <summary>
        /// The ultimate manager
        /// </summary>
        private UltimateManager _ultimateManager;

        /// <summary>
        /// The barrels
        /// </summary>
        private readonly HashSet<Barrel> _barrels = new HashSet<Barrel>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Gangplank"/> class.
        /// </summary>
        public Gangplank() : base(1500f)
        {

        }

        /// <summary>
        /// Gets the item flags.
        /// </summary>
        /// <value>
        /// The item flags.
        /// </value>
        protected override ItemFlags ItemFlags
        {
            get { return ItemFlags.Defensive | ItemFlags.Offensive | ItemFlags.Flee; }
        }

        /// <summary>
        /// Gets the item usage.
        /// </summary>
        /// <value>
        /// The item usage.
        /// </value>
        protected override ItemUsageType ItemUsage
        {
            get { return ItemUsageType.Custom; }
        }

        /// <summary>
        /// Called when [load].
        /// </summary>
        protected override void OnLoad()
        {
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalking.OnNonKillableMinion += Orbwalking_OnNonKillableMinion;
        }

        /// <summary>
        /// Setups the spells.
        /// </summary>
        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 590f);
            Q.SetTargetted(0.25f, 2200f);

            W = new Spell(SpellSlot.W);

            E = new Spell(SpellSlot.E, 950f);
            E.SetSkillshot(0.8f, 50, float.MaxValue, false, SkillshotType.SkillshotCircle);

            R = new Spell(SpellSlot.R);
            R.SetSkillshot(1f, 100, float.MaxValue, false, SkillshotType.SkillshotCircle);
        }

        /// <summary>
        /// Adds to menu.
        /// </summary>
        protected override void AddToMenu()
        {
        }

        /// <summary>
        /// Called when [pre update].
        /// </summary>
        protected override void OnPreUpdate()
        {
            _barrels.RemoveWhere(b => !b.Target.IsValid);
        }

        /// <summary>
        /// Called when [post update].
        /// </summary>
        protected override void OnPostUpdate()
        {
        }

        /// <summary>
        /// Combo.
        /// </summary>
        protected override void Combo()
        {
        }

        /// <summary>
        /// Harass.
        /// </summary>
        protected override void Harass()
        {
        }

        /// <summary>
        /// Lanes clear.
        /// </summary>
        protected override void LaneClear()
        {
        }

        /// <summary>
        /// Jungles clear.
        /// </summary>
        protected override void JungleClear()
        {
        }

        /// <summary>
        /// Flee.
        /// </summary>
        protected override void Flee()
        {
        }

        /// <summary>
        /// Killsteal.
        /// </summary>
        protected override void Killsteal()
        {
        }

        /// <summary>
        /// Last hit.
        /// </summary>
        protected override void LastHit()
        {
            var useQ = Menu.Item(Menu.Name + ".lasthit.q").GetValue<bool>();
            if (!useQ || !ResourceManager.Check("lasthit"))
            {
                return;
            }

            var target = Orbwalker.GetTarget() as Obj_AI_Base;
            if (target != null && Q.GetDamage(target) >= target.Health)
            {
                Casting.TargetSkill(target, Q);
            }
        }

        /// <summary>
        /// Called when we have unkillable minion 
        /// </summary>
        /// <param name="minion">The minion.</param>
        private void Orbwalking_OnNonKillableMinion(AttackableUnit minion)
        {
            try
            {
                var useQ = Menu.Item(Menu.Name + ".lasthit.q-unkillable").GetValue<bool>();
                if (!useQ || !ResourceManager.Check("lasthit"))
                {
                    return;
                }

                var target = minion as Obj_AI_Base;
                if (target != null && Q.GetDamage(target) >= target.Health)
                {
                    Casting.TargetSkill(target, Q);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        /// <summary>
        /// Called when [Drawing OnDraw]
        /// </summary>
        /// <param name="args">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void Drawing_OnDraw(EventArgs args)
        {
            try
            {
                foreach (var barrel in _barrels.Where(b => b.Target.Distance(Player) < 1400))
                {
                    var activationTime = barrel.ActivationTime;
                    var remainder = Game.Time - activationTime;

                    var healthBarPosition = barrel.Target.HPBarPosition;

                    if (remainder <= 0)
                    {
                        Drawing.DrawText(healthBarPosition.X + 5, healthBarPosition.Y - 30, Color.Red, "Ready");
                    }
                    else
                    {
                        Drawing.DrawText(healthBarPosition.X + 5, healthBarPosition.Y - 30, Color.White, string.Format("{0:0.00}", remainder / 1000));
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        /// <summary>
        /// Called when [GameObject OnCreate]
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            var minion = sender as Obj_AI_Minion;
            if (minion == null || !minion.CharData.BaseSkinName.ToLower().Contains("gangplankbarrel"))
            {
                return;
            }

            _barrels.Add(new Barrel(minion, GetBarrelActivationTime(Game.Time)));
        }

        /// <summary>
        /// Called when [GameObject OnDelete]
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            _barrels.RemoveWhere(b => b.Target.NetworkId == sender.NetworkId);
        }

        /// <summary>
        /// Gets the barrel activation time after creation.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <returns></returns>
        private float GetBarrelActivationTime(float time)
        {
            if (Player.Level >= 13)
            {
                // .5s per health * 2 = 1s
                return time + 1000;
            }

            if (Player.Level >= 7)
            {
                // 1s per health * 2 = 2s
                return time + 2000;
            }

            // 2s per health * 2 = 4s
            return time + 4000;
        }

        /// <summary>
        /// 
        /// </summary>
        internal class Barrel
        {
            /// <summary>
            /// Gets the target.
            /// </summary>
            /// <value>
            /// The target.
            /// </value>
            public Obj_AI_Minion Target { get; private set; }

            /// <summary>
            /// Gets the activation time.
            /// </summary>
            /// <value>
            /// The activation time.
            /// </value>
            public float ActivationTime { get; private set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Barrel" /> class.
            /// </summary>
            /// <param name="target">The target.</param>
            /// <param name="activationTime">The activation time.</param>
            public Barrel(Obj_AI_Minion target, float activationTime)
            {
                Target = target;
                ActivationTime = activationTime;
            }
        }
    }
}
