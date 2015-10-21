using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Abstracts;
using SFXChallenger.Enumerations;
using SFXChallenger.Helpers;
using SFXChallenger.Library.Logger;
using SFXChallenger.Managers;
using Spell = SFXChallenger.Wrappers.Spell;
using SFXChallenger.Args;
using SFXChallenger.Library.Extensions.SharpDX;
using SharpDX;
using Color = System.Drawing.Color;
using MinionManager = SFXChallenger.Library.MinionManager;
using TargetSelector = SFXChallenger.SFXTargetSelector.TargetSelector;
using SFXChallenger.Library.Extensions.NET;

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
        /// The powder keg link radius
        /// </summary>
        private const float PowderKegLinkRadius = 650f;

        /// <summary>
        /// The last powder keg tracker
        /// </summary>
        private LastPowderKegTracker _lastPowderKegTracker;

        /// <summary>
        /// Initializes a new instance of the <see cref="Gangplank"/> class.
        /// </summary>
        public Gangplank() : base(590f + PowderKegExplosionRadius)
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

            Orbwalking.OnNonKillableMinion += Orbwalking_OnNonKillableMinion;

            AttackableUnit.OnDamage += AttackableUnit_OnDamage;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;

            Drawing.OnDraw += Drawing_OnDraw;
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
            E.SetSkillshot(0.25f, 50, float.MaxValue, false, SkillshotType.SkillshotCircle);

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
                new ResourceManagerArgs("harass", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
                {
                    Advanced = true,
                    MaxValue = 101,
                    LevelRanges = new SortedList<int, int> { { 1, 6 }, { 6, 12 }, { 12, 18 } },
                    DefaultValues = new List<int> { 50, 30, 30 }
                });

            harassMenu.AddItem(new MenuItem(string.Format("{0}.q", harassMenu.Name), "Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem(string.Format("{0}.q-lasthit", harassMenu.Name), "Q Last hit").SetValue(true));

            harassMenu.AddItem(new MenuItem(string.Format("{0}.separator", harassMenu.Name), string.Empty));

            harassMenu.AddItem(new MenuItem(string.Format("{0}.e", harassMenu.Name), "Use E").SetValue(true));
            harassMenu.AddItem(new MenuItem(string.Format("{0}.e-stacks", harassMenu.Name), "E Min. Stacks").SetValue(new Slider(1, 0, 5)));

            harassMenu.AddItem(new MenuItem(string.Format("{0}.q-e", harassMenu.Name), "Use Q on E").SetValue(true));
            harassMenu.AddItem(new MenuItem(string.Format("{0}.q-e-bounces", harassMenu.Name), "E Max Bounces").SetValue(new Slider(2, 0, 4)));
        }

        /// <summary>
        /// Creates the lane clear menu.
        /// </summary>
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
            laneClearMenu.AddItem(new MenuItem(string.Format("{0}.q-e-bounces", laneClearMenu.Name), "E Max Bounces").SetValue(new Slider(2, 0, 4)));
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
            if (!ResourceManager.Check("harass"))
            {
                return;
            }

            var useQ = Menu.Item(Menu.Name + ".harass.q").GetValue<bool>();
            var useQLastHit = Menu.Item(Menu.Name + ".harass.q-lasthit").GetValue<bool>();
            var useE = Menu.Item(Menu.Name + ".harass.e").GetValue<bool>();
            var useParrley = Menu.Item(Menu.Name + ".harass.q-e").GetValue<bool>();

            if (useQLastHit)
            {
                var minion = Orbwalker.GetTarget(Q.Range) as Obj_AI_Minion;
                if (minion != null && Q.GetDamage(minion) > minion.Health)
                {
                    Casting.TargetSkill(minion, Q);
                }
            }

            HarassExplodePowderKegs(useParrley);

            if (useE)
            {
                var maximumBounces = Menu.Item(Menu.Name + ".harass.q-e-bounces").GetValue<Slider>().Value;

                var target = TargetSelector.GetTarget(Q.Range + (maximumBounces * PowderKegLinkRadius));
                if (target == null)
                {
                    return;
                }

                var powderKeg = _powderKegs
                    .Where(p => IsPowderKegExplodable(p, useParrley) && p.Minion.Distance(target) <= PowderKegLinkRadius + PowderKegExplosionRadius)
                    .OrderBy(p => p.Minion.Distance(target))
                    .FirstOrDefault();

                if (powderKeg == null)
                {
                    return;
                }

                var availablePositions = GetBestPowderKegPositions(powderKeg, target);
                if (availablePositions == null)
                {
                    return;
                }

                var closestPosition = availablePositions.Where(p => p.Distance(Player) <= E.Range).OrderBy(p => p.Distance(target)).FirstOrDefault();
                if (closestPosition == null)
                {
                    return;
                }

                if (closestPosition.Distance(Player) > E.Range)
                {
                    return;
                }

                E.Cast(closestPosition);
            }
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
            var useParrley = Menu.Item(Menu.Name + ".laneclear.q-e").GetValue<bool>();

            var minHit = Menu.Item(Menu.Name + ".laneclear.e-min").GetValue<Slider>().Value;
            var minStacks = Menu.Item(Menu.Name + ".laneclear.e-stacks").GetValue<Slider>().Value;

            LaneClearExplodePowderKegs(useParrley, minHit);

            if (useQ)
            {
                var minion = MinionManager.GetMinions(Q.Range).FirstOrDefault(m => Q.IsKillable(m));
                if (minion != null)
                {
                    Casting.TargetSkill(minion, Q);
                }
            }

            if (useE)
            {
                LaneClearPlacePowderKeg(minStacks, minHit);
            }
        }

        /// <summary>
        /// Places a powder keg for lane clear
        /// </summary>
        /// <param name="minStacks">The minimum stacks.</param>
        /// <param name="minHit">The minimum hit.</param>
        private void LaneClearPlacePowderKeg(int minStacks, int minHit)
        {
            if (!E.IsReady() || minStacks >= E.Instance.Ammo)
            {
                return;
            }

            //var minions = MinionManager.GetMinions(E.Range);
            //var location = E.GetCircularFarmLocation(minions, PowderKegExplosionRadius);
            //if (location.MinionsHit >= minHit)
            //{
            //    if (_powderKegs.Any(p => p.Minion.Distance(location.Position) <= PowderKegExplosionRadius))
            //    {
            //        return;
            //    }

            //    E.Cast(location.Position);
            //}
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
            var useParrley = Menu.Item(Menu.Name + ".lasthit.q-e").GetValue<bool>();

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

            if (useParrley)
            {
                var target = Orbwalker.GetTarget(Q.Range + PowderKegExplosionRadius) as Obj_AI_Base;
                if (target != null && Q.GetDamage(target) >= target.Health)
                {
                    LastHitExplodePowderKegs(target);
                }
            }
        }

        #region Events

        /// <summary>
        /// Called when spell cast from <see cref="Obj_AI_Base"/>.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="GameObjectProcessSpellCastEventArgs"/> instance containing the event data.</param>
        private void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }

            var spellName = args.SData.Name;
            if (spellName.ToLower().Contains("gangplanke"))
            {
                _lastPowderKegTracker = new LastPowderKegTracker
                {
                    Position = args.End,
                    TickCount = Environment.TickCount
                };
            }
        }

        /// <summary>
        /// Called when unit takes damage.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="AttackableUnitDamageEventArgs"/> instance containing the event data.</param>
        private void AttackableUnit_OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            var powderKeg = _powderKegs.FirstOrDefault(p => p.NetworkId == args.TargetNetworkId);
            if (powderKeg == null)
            {
                return;
            }

            powderKeg.ActivationTime = GetPowderKegActivationTime(powderKeg.CreationTime, powderKeg.Minion.Health - 2);
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
                var count = 0;
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

                    var color = Color.AliceBlue;
                    switch(count)
                    {
                        case 1:
                            color = Color.BlueViolet;
                            break;
                        case 2:
                            color = Color.Green;
                            break;
                        case 3:
                            color = Color.Yellow;
                            break;
                        case 4:
                            color = Color.Red;
                            break;
                    }

                    var position = powderKeg.Minion.Position.To2D();

                    foreach (var keg in _powderKegs.Where(k => k.Minion.Distance(powderKeg.Minion) <= PowderKegLinkRadius && k.NetworkId != powderKeg.NetworkId))
                    {
                        Drawing.DrawLine(position, keg.Minion.Position.To2D(), 5f, color);
                    }

                    count++;
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

            var powderKeg = new PowderKeg(minion, GetPowderKegActivationTime(Environment.TickCount));
            _powderKegs.Add(powderKeg);
        }

        /// <summary>
        /// Called when [GameObject OnDelete]
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            _powderKegs.RemoveWhere(p => p.NetworkId == sender.NetworkId);
        }

        #endregion

        #region Powder Kegs

        private IEnumerable<Vector2> GetBestPowderKegPositions(PowderKeg powderKeg, Obj_AI_Hero target)
        {
            var powderKegPosition = powderKeg.Minion.Position.To2D();
            var targetPosition = target.Position.To2D();

            var explosionRadius = PowderKegExplosionRadius + target.BoundingRadius;

            if (targetPosition.Distance(powderKegPosition) <= explosionRadius)
            {
                return null;
            }
            
            var powderKegLinkRadius = new Geometry.Polygon.Circle(powderKegPosition, PowderKegLinkRadius);

            return powderKegLinkRadius.Points.Where(p => targetPosition.IsInsideCircle(p, explosionRadius));
        }

        /// <summary>
        /// Explode powder kegs in harass
        /// </summary>
        private void HarassExplodePowderKegs(bool parrley)
        {
            if (!Q.IsReady())
            {
                return;
            }

            var maximumBounces = Menu.Item(Menu.Name + ".harass.q-e-bounces").GetValue<Slider>().Value;
            
            var target = TargetSelector.GetTarget(Q.Range + (maximumBounces * PowderKegLinkRadius));
            if (target == null)
            {
                return;
            }
            
            var powderKeg = FindClosestExplodablePowderKeg(target, parrley);
            
            if (powderKeg != null)
            {                
                //var prediction = Prediction.GetPrediction(target, E.Delay + GetParrleyTravelTime(powderKeg.Minion));
                //if (powderKeg.Minion.Distance(prediction.CastPosition) > PowderKegExplosionRadius)
                //{
                //    return;
                //}

                if (parrley)
                {
                    Casting.TargetSkill(powderKeg.Minion, Q);
                }
                else
                {
                    Orbwalker.ForceTarget(powderKeg.Minion);
                }
            }
        }

        /// <summary>
        /// Explode powder kegs in lane clear
        /// </summary>
        private void LaneClearExplodePowderKegs(bool parrley, int minHit)
        {
            if (!Q.IsReady())
            {
                return;
            }

            var maximumBounces = Menu.Item(Menu.Name + ".laneclear.q-e-bounces").GetValue<Slider>().Value;
            var minions = MinionManager.GetMinions(Q.Range + (maximumBounces * PowderKegLinkRadius));

            var powderKeg = (from p in _powderKegs
                             let minionCount = minions.Count(m => m.Distance(p.Minion) <= PowderKegExplosionRadius)
                             where minionCount >= minHit
                             orderby minionCount, p.Minion.Distance(Player)
                             select p).FirstOrDefault();

            if (powderKeg == null)
            {
                return;
            }
            
            powderKeg = FindClosestExplodablePowderKeg(powderKeg, parrley);

            if (powderKeg != null)
            {
                if (parrley)
                {
                    Casting.TargetSkill(powderKeg.Minion, Q);
                }
                else
                {
                    Orbwalker.ForceTarget(powderKeg.Minion);
                }
            }
        }

        /// <summary>
        /// Explode powder kegs in last hit
        /// </summary>
        private void LastHitExplodePowderKegs(GameObject target)
        {
            if (!Q.IsReady())
            {
                return;
            }

            var minions = MinionManager.GetMinions(Q.Range + PowderKegExplosionRadius);

            var powderKeg = (from p in _powderKegs
                             let explodable = IsPowderKegExplodable(p, true)
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
        /// Finds the closest explodable powder keg.
        /// </summary>
        /// <param name="powderKeg">The powder keg.</param>
        /// <param name="parrley">if set to <c>true</c> [parrley].</param>
        /// <returns></returns>
        private PowderKeg FindClosestExplodablePowderKeg(PowderKeg powderKeg, bool parrley)
        {
            if (IsPowderKegExplodable(powderKeg, parrley))
            {
                return powderKeg;
            }

            var linkedPowderKeg = FindLinkedExplodablePowderKeg(powderKeg, parrley);
            return linkedPowderKeg;
        }

        /// <summary>
        /// Finds the closest powder keg.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="parrley">if set to <c>true</c> [parrley].</param>
        /// <returns></returns>
        private PowderKeg FindClosestExplodablePowderKeg(Obj_AI_Base target, bool parrley)
        {
            var powderKeg = _powderKegs
                .Where(p => p.Minion.Distance(target) <= PowderKegExplosionRadius)
                .OrderBy(p => p.Minion.Distance(Player))
                .FirstOrDefault();

            if (powderKeg == null)
            {
                return null;
            }

            if (IsPowderKegExplodable(powderKeg, parrley))
            {
                return powderKeg;
            }

            Console.WriteLine("Closest not explodable, trying linked... ");

            var linkedPowderKeg = FindLinkedExplodablePowderKeg(powderKeg, parrley);

            return linkedPowderKeg;
        }

        /// <summary>
        /// Finds a linked explodable powder keg.
        /// </summary>
        /// <param name="powderKeg">The powder keg.</param>
        /// <param name="parrley">if set to <c>true</c> [parrley].</param>
        /// <returns></returns>
        private PowderKeg FindLinkedExplodablePowderKeg(PowderKeg powderKeg, bool parrley)
        {
            foreach (var keg in _powderKegs.Where(k => k.Minion.Distance(powderKeg.Minion) <= PowderKegLinkRadius && k.NetworkId != powderKeg.NetworkId))
            {
                if (IsPowderKegExplodable(keg, parrley))
                {
                    return keg;
                }

                var linked = FindLinkedExplodablePowderKeg(keg, parrley);
                if (linked != null)
                {
                    return linked;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the powder keg activation time after creation.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <param name="health">The health.</param>
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
        /// <param name="parrley">if set to <c>true</c> [parrley].</param>
        /// <returns></returns>
        private bool IsPowderKegExplodable(PowderKeg powderKeg, bool parrley)
        {
            if (parrley && powderKeg.Minion.Distance(Player) > Q.Range)
            {
                return false;
            }

            if (!parrley && powderKeg.Minion.Distance(Player) > Orbwalking.GetRealAutoAttackRange(powderKeg.Minion))
            {
                return false;
            }

            if (powderKeg.Minion.Health == 1f)
            {
                return true;
            }

            var travelTime = parrley ? GetParrleyTravelTime(powderKeg.Minion) : Player.AttackDelay;
            var activationTime = powderKeg.ActivationTime;
            var remainder = activationTime - Environment.TickCount - travelTime;

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
        internal class LastPowderKegTracker
        {
            /// <summary>
            /// Gets or sets the position.
            /// </summary>
            /// <value>
            /// The position.
            /// </value>
            public Vector3 Position { get; set; }

            /// <summary>
            /// Gets or sets the tick count.
            /// </summary>
            /// <value>
            /// The tick count.
            /// </value>
            public int TickCount { get; set; }

            /// <summary>
            /// Gets a value indicating whether this <see cref="LastPowderKegTracker"/> is ready.
            /// </summary>
            /// <value>
            ///   <c>true</c> if ready; otherwise, <c>false</c>.
            /// </value>
            public bool Ready
            {
                get { return Environment.TickCount - TickCount > 500; }
            }
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
            /// Gets the creation time.
            /// </summary>
            public float CreationTime { get; set; }

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
                CreationTime = Environment.TickCount;
            }
        }

        #endregion
    }
}
