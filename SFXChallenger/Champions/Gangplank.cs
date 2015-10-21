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
using SFXChallenger.Args;

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
        /// The powder kegs
        /// </summary>
        private readonly HashSet<PowderKeg> _powderKegs = new HashSet<PowderKeg>();

        /// <summary>
        /// The powder keg explosion radius
        /// </summary>
        private const float PowderKegExplosionRadius = 400f;

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
            AttackableUnit.OnDamage += AttackableUnit_OnDamage;
        }        

        /// <summary>
        /// Setups the spells.
        /// </summary>
        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 620f);
            Q.SetTargetted(0.25f, 2200f);

            W = new Spell(SpellSlot.W);

            E = new Spell(SpellSlot.E, 1000f);
            E.SetSkillshot(0.8f, 50, float.MaxValue, false, SkillshotType.SkillshotCircle);

            R = new Spell(SpellSlot.R);
            R.SetSkillshot(1f, 100, float.MaxValue, false, SkillshotType.SkillshotCircle);
        }

        /// <summary>
        /// Adds to menu.
        /// </summary>
        protected override void AddToMenu()
        {
            CreateComboMenu();
            CreateHarassMenu();
            CreateLaneClearMenu();
            CreateLastHitMenu();
        }

        /// <summary>
        /// Creates the combo menu
        /// </summary>
        private void CreateComboMenu()
        {
            var comboMenu = Menu.AddSubMenu(new Menu("Combo", string.Format("{0}.combo", Menu.Name)));
            comboMenu.AddItem(new MenuItem(string.Format("{0}.q", comboMenu.Name), "Use Q").SetValue(true));
        }

        /// <summary>
        /// Creates the harass menu
        /// </summary>
        private void CreateHarassMenu()
        {
            var harassMenu = Menu.AddSubMenu(new Menu("Harass", string.Format("{0}.harass", Menu.Name)));
            ResourceManager.AddToMenu(harassMenu,
                new ResourceManagerArgs("harass-q", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    Prefix = "Q",
                    DefaultValue = 30
                });

            harassMenu.AddItem(new MenuItem(string.Format("{0}.q", harassMenu.Name), "Use Q").SetValue(true));            
        }

        private void CreateLaneClearMenu()
        {
            var laneClearMenu = Menu.AddSubMenu(new Menu("Lane Clear", string.Format("{0}.laneclear", Menu.Name)));
            ResourceManager.AddToMenu(laneClearMenu,
                new ResourceManagerArgs("laneclear", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    Advanced = true,
                    MaxValue = 101,
                    LevelRanges = new SortedList<int, int> { { 1, 6 }, { 6, 12 }, { 12, 18 } },
                    DefaultValues = new List<int> { 50, 30, 30 }
                });

            laneClearMenu.AddItem(new MenuItem(string.Format("{0}.q", laneClearMenu.Name), "Use Q").SetValue(true));

            laneClearMenu.AddItem(new MenuItem(string.Format("{0}.separator", laneClearMenu.Name), string.Empty));
            
            laneClearMenu.AddItem(new MenuItem(string.Format("{0}.e", laneClearMenu.Name), "Use E").SetValue(true));
            laneClearMenu.AddItem(new MenuItem(string.Format("{0}.e-stacks", laneClearMenu.Name), "E Min. Stacks").SetValue(new Slider(1, 0, 5)));

            laneClearMenu.AddItem(new MenuItem(string.Format("{0}.q-e", laneClearMenu.Name), "Use Q on E").SetValue(true));
            laneClearMenu.AddItem(new MenuItem(string.Format("{0}.e-min", laneClearMenu.Name), "E Min. Hit").SetValue(new Slider(3, 0, 10)));
            

        }

        /// <summary>
        /// Creates the last hit menu
        /// </summary>
        private void CreateLastHitMenu()
        {
            var lastHitMenu = Menu.AddSubMenu(new Menu("Last Hit", string.Format("{0}.lasthit", Menu.Name)));
            ResourceManager.AddToMenu(lastHitMenu,
                new ResourceManagerArgs("lasthit", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    Advanced = true,
                    MaxValue = 101,
                    LevelRanges = new SortedList<int, int> { { 1, 6 }, { 6, 12 }, { 12, 18 } },
                    DefaultValues = new List<int> { 30, 20, 20 }
                });

            lastHitMenu.AddItem(new MenuItem(string.Format("{0}.q", lastHitMenu.Name), "Use Q").SetValue(true));
            lastHitMenu.AddItem(new MenuItem(string.Format("{0}.q-unkillable", lastHitMenu.Name), "Q Unkillable").SetValue(true));
            lastHitMenu.AddItem(new MenuItem(string.Format("{0}.q-e", lastHitMenu.Name), "Use Q on E").SetValue(true));
        }

        /// <summary>
        /// Called when [pre update].
        /// </summary>
        protected override void OnPreUpdate()
        {
            _powderKegs.RemoveWhere(b => !b.Minion.IsValidTarget());
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
            if (!ResourceManager.Check("laneclear"))
            {
                return;
            }

            var useQ = Menu.Item(Menu.Name + ".laneclear.q").GetValue<bool>();
            var useE = Menu.Item(Menu.Name + ".laneclear.e").GetValue<bool>();
            var explode = Menu.Item(Menu.Name + ".laneclear.q-e").GetValue<bool>();

            if (explode)
            {
                LaneClearExplodePowderKegs();
            }
        }

        /// <summary>
        /// Explode powder kegs in lane clear
        /// </summary>
        private void LaneClearExplodePowderKegs()
        {
            var minimum = Menu.Item(Menu.Name + ".laneclear.e-min").GetValue<Slider>().Value;
            if (minimum == 0)
            {
                return;
            }

            if (!Q.IsReady())
            {
                return;
            }

            var minions = Library.MinionManager.GetMinions(Player.ServerPosition, Q.Range + PowderKegExplosionRadius);

            var powderKeg = (from p in _powderKegs
                            let explodable = IsPowderKegExplodable(p)
                            let minionCount = minions.Count(m => m.Distance(p.Minion) <= PowderKegExplosionRadius)
                            where explodable && minionCount >= minimum
                            orderby minionCount
                            select p.Minion).FirstOrDefault();

            if (powderKeg == null)
            {
                return;
            }

            Casting.TargetSkill(powderKeg, Q);
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
            var explode = Menu.Item(Menu.Name + ".lasthit.q-e").GetValue<bool>();

            if (!ResourceManager.Check("lasthit"))
            {
                return;
            }

            if (useQ)
            {
                var target = Orbwalker.GetTarget(Q.Range) as Obj_AI_Base;

                if (target != null && Q.GetDamage(target) >= target.Health)
                {
                    Casting.TargetSkill(target, Q);
                }
            }

            if (explode)
            {
                var target = Orbwalker.GetTarget(Q.Range + PowderKegExplosionRadius) as Obj_AI_Base;
                if (target != null && Q.GetDamage(target) >= target.Health)
                {
                    LastHitExplodePowderKegs(target);
                }
            }
        }

        /// <summary>
        /// Explode powder kegs in last hit
        /// </summary>
        private void LastHitExplodePowderKegs(Obj_AI_Base target)
        {
            if (!Q.IsReady())
            {
                return;
            }

            var minions = Library.MinionManager.GetMinions(Player.ServerPosition, Q.Range + PowderKegExplosionRadius);

            var powderKeg = (from p in _powderKegs
                             let explodable = IsPowderKegExplodable(p)
                             let minion = minions.FirstOrDefault(m => m.NetworkId == target.NetworkId && m.Distance(p.Minion) <= PowderKegExplosionRadius)
                             where explodable && minion != null
                             select p.Minion).FirstOrDefault();

            if (powderKeg == null)
            {
                return;
            }

            Casting.TargetSkill(powderKeg, Q);
        }

        /// <summary>
        /// Called when attack unit takes damage
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// param name="args">The <see cref="AttackableUnitDamageEventArgs"/> instance containing the event data.</param>
        private void AttackableUnit_OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            var powderKeg = _powderKegs.FirstOrDefault(p => p.NetworkId == args.TargetNetworkId);
            if (powderKeg == null)
            {
                return;
            }            

            powderKeg.ActivationTime = GetPowderKegActivationTime(Environment.TickCount, powderKeg.Minion.Health);
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
                foreach (var powderKeg in _powderKegs)
                {
                    var activationTime = powderKeg.ActivationTime;
                    var remainder = activationTime - Environment.TickCount;                    

                    var healthBarPosition = powderKeg.Minion.HPBarPosition;

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

            _powderKegs.Add(new PowderKeg(minion, GetPowderKegActivationTime(Environment.TickCount)));
        }

        /// <summary>
        /// Called when [GameObject OnDelete]
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            _powderKegs.RemoveWhere(b => b.NetworkId == sender.NetworkId);
        }        

        /// <summary>
        /// Gets the powder keg activation time after creation.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <returns></returns>
        private float GetPowderKegActivationTime(float time, float health = 2f)
        {
            if (Player.Level >= 13)
            {
                // .5s per health * 2 = 1s
                return time + (500 * health);
            }

            if (Player.Level >= 7)
            {
                // 1s per health * 2 = 2s
                return time + (1000 * health);
            }

            // 2s per health * 2 = 4s
            return time + (2000 * health);
        }

        /// <summary>
        /// [true] if explodable else [false]
        /// </summary>
        /// <param name="powderKeg">the powder keg</param>
        /// <returns></returns>
        private bool IsPowderKegExplodable(PowderKeg powderKeg)
        {
            if (powderKeg.Minion.Distance(Player) > Q.Range)
            {
                return false;
            }

            if (powderKeg.Minion.Health == 1)
            {
                return true;
            }

            var travelTime = GetParrleyTravelTime(powderKeg.Minion);
            var activationTime = powderKeg.ActivationTime;
            var remainder = activationTime - Environment.TickCount - GetParrleyTravelTime(powderKeg.Minion);

            return remainder <= 0;
        }

        /// <summary>
        /// Gets the travel time of q to the target
        /// </summary>
        /// <param name="target">the target</param>
        /// <returns></returns>
        private float GetParrleyTravelTime(Obj_AI_Base target)
        {
            return (Player.Distance(target) / Q.Speed + Q.Delay) * 1000;
        }

        /// <summary>
        /// 
        /// </summary>
        internal class PowderKeg
        {
            /// <summary>
            /// Gets the network id.
            /// </summary>
            /// <value>
            /// The network id.
            /// </value>
            public int NetworkId { get; private set; }

            /// <summary>
            /// Gets the minion.
            /// </summary>
            public Obj_AI_Minion Minion { get; private set; }

            /// <summary>
            /// Gets the activation time.
            /// </summary>
            /// <value>
            /// The activation time.
            /// </value>
            public float ActivationTime { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="PowderKeg" /> class.
            /// </summary>
            /// <param name="target">The target.</param>
            /// <param name="activationTime">The activation time.</param>
            public PowderKeg(Obj_AI_Minion target, float activationTime)
            {
                NetworkId = target.NetworkId;
                Minion = target;
                ActivationTime = activationTime;
            }
        }
    }
}
