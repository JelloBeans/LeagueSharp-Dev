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
using SFXChallenger.Library.Extensions.NET;
using SFXChallenger.Library.Logger;
using SFXChallenger.Managers;
using SharpDX;
using DamageType = SFXChallenger.Enumerations.DamageType;
using MinionManager = SFXChallenger.Library.MinionManager;
using MinionTeam = SFXChallenger.Library.MinionTeam;
using MinionTypes = SFXChallenger.Library.MinionTypes;
using Orbwalking = SFXChallenger.Wrappers.Orbwalking;
using Spell = SFXChallenger.Wrappers.Spell;
using TargetSelector = SFXChallenger.SFXTargetSelector.TargetSelector;
using Utils = SFXChallenger.Helpers.Utils;

namespace SFXChallenger.Champions
{
    class Jinx : Champion
    {
        /// <summary>
        /// The minigun range
        /// </summary>
        private const float MinigunRange = 520f;

        private UltimateManager _ultimateManager;

        private Spell UltimateExplosion { get; set; }

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

        /// <summary>
        /// Attach events
        /// </summary>
        protected override void OnLoad()
        {
            GapcloserManager.OnGapcloser += GapcloserManager_OnGapcloser;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
        }

        /// <summary>
        /// Sets up the spells
        /// </summary>
        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q);
            Q.SetSkillshot(0.5f, 100f, Player.BasicAttack.MissileSpeed, false, SkillshotType.SkillshotCircle);

            W = new Spell(SpellSlot.W, 1450f);
            W.SetSkillshot(0.6f, 60f, 3300f, true, SkillshotType.SkillshotLine);

            E = new Spell(SpellSlot.E, 900f);
            E.SetSkillshot(0.3f, 120f, 3300f, false, SkillshotType.SkillshotCircle); // We don't set the delay to it's correct value as the prediction messes around.

            R = new Spell(SpellSlot.R, 3200f);
            R.SetSkillshot(0.6f, 140f, 1700f, true, SkillshotType.SkillshotLine);

            UltimateExplosion = new Spell(SpellSlot.R, 300f);
            UltimateExplosion.SetSkillshot(0f, 300f, 1500f, false, SkillshotType.SkillshotCircle);

            _ultimateManager = new UltimateManager
            {
                Combo = true,
                Assisted = true,
                Auto = true,
                Flash = false,
                Required = true,
                Force = true,
                Gapcloser = false,
                GapcloserDelay = false,
                Interrupt = false,
                InterruptDelay = false,
                Spells = Spells,
                DamageCalculation = (hero, resMulti, rangeCheck) => CalculateComboDamage(hero, rangeCheck, true)
            };
        }

        /// <summary>
        /// Menu
        /// </summary>
        protected override void AddToMenu()
        {
            _ultimateManager.AddToMenu(Menu);

            #region Combo Menu
            var comboMenu = Menu.AddSubMenu(new Menu("Combo", string.Format("{0}.combo", Menu.Name)));
            comboMenu.AddItem(new MenuItem(string.Format("{0}.q", comboMenu.Name), "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem(string.Format("{0}.q-range", comboMenu.Name), "Q Min. Distance").SetValue(new Slider(570, 525, 590)));
            comboMenu.AddItem(new MenuItem(string.Format("{0}.q-aoe", comboMenu.Name), "Q AoE").SetValue(new Slider(2, 1, 5)));
            comboMenu.AddItem(new MenuItem(string.Format("{0}.w", comboMenu.Name), "Use W").SetValue(true));
            comboMenu.AddItem(new MenuItem(string.Format("{0}.e", comboMenu.Name), "Use E").SetValue(true));

            var comboHitChanceMenu = comboMenu.AddSubMenu(new Menu("Hitchance", string.Format("{0}.hitchance", comboMenu.Name)));
            var comboHitChanceDictionary = new Dictionary<string, HitChance>
            {
                { "W", HitChance.VeryHigh },
                { "E", HitChance.VeryHigh },
                { "R", HitChance.VeryHigh }
            };
            HitchanceManager.AddToMenu(comboHitChanceMenu, "combo", comboHitChanceDictionary);

            ResourceManager.AddToMenu(comboMenu, new ResourceManagerArgs("combo-w", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
            {
                Prefix = "W",
                DefaultValue = 15
            });
            #endregion

            #region Harass Menu
            var harassMenu = Menu.AddSubMenu(new Menu("Harass", string.Format("{0}.harass", Menu.Name)));
            harassMenu.AddItem(new MenuItem(string.Format("{0}.q", harassMenu.Name), "Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem(string.Format("{0}.q-range", harassMenu.Name), "Q Min. Distance").SetValue(new Slider(570, 525, 590)));
            harassMenu.AddItem(new MenuItem(string.Format("{0}.q-aoe", harassMenu.Name), "Q AoE").SetValue(new Slider(2, 1, 5)));
            harassMenu.AddItem(new MenuItem(string.Format("{0}.w", harassMenu.Name), "Use W").SetValue(true));

            var harassHitChanceMenu = harassMenu.AddSubMenu(new Menu("Hitchance", string.Format("{0}.hitchance", harassMenu.Name)));
            var harassHitChanceDictionary = new Dictionary<string, HitChance>
            {
                { "W", HitChance.High }
            };
            HitchanceManager.AddToMenu(harassHitChanceMenu, "harass", harassHitChanceDictionary);

            ResourceManager.AddToMenu(harassMenu, new ResourceManagerArgs("harass-q", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
            {
                Prefix = "Q",
                DefaultValue = 15
            });

            ResourceManager.AddToMenu(harassMenu, new ResourceManagerArgs("harass-w", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
            {
                Prefix = "W",
                DefaultValue = 15
            });
            #endregion

            #region Lane Clear Menu
            var laneclearMenu = Menu.AddSubMenu(new Menu("Lane Clear", string.Format("{0}.lane-clear", Menu.Name)));
            laneclearMenu.AddItem(new MenuItem(string.Format("{0}.q", laneclearMenu.Name), "Use Q").SetValue(true));
            laneclearMenu.AddItem(new MenuItem(string.Format("{0}.q-min", laneclearMenu.Name), "Q Min.").SetValue(new Slider(3, 1, 5)));

            ResourceManager.AddToMenu(laneclearMenu, new ResourceManagerArgs("lane-clear-q", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
            {
                Prefix = "Q",
                Advanced = true,
                MaxValue = 101,
                LevelRanges = new SortedList<int, int> { { 1, 6 }, { 6, 12 }, { 12, 18 } },
                DefaultValues = new List<int> { 30, 30, 30 }
            });
            #endregion

            #region Flee Menu
            var fleeMenu = Menu.AddSubMenu(new Menu("Flee", string.Format("{0}.flee", Menu.Name)));
            fleeMenu.AddItem(new MenuItem(string.Format("{0}.e", fleeMenu.Name), "Use E").SetValue(true));
            #endregion

            #region Misc Menu
            // Misc menu
            var miscMenu = Menu.AddSubMenu(new Menu("Misc", string.Format("{0}.miscellaneous", Menu.Name)));

            // Gapcloser menu
            var eGapcloserMenu = miscMenu.AddSubMenu(new Menu("E Gapcloser", string.Format("{0}.e-gapcloser", miscMenu.Name)));
            GapcloserManager.AddToMenu(eGapcloserMenu, new HeroListManagerArgs("e-gapcloser")
            {
                IsWhitelist = false,
                Allies = false,
                Enemies = true,
                DefaultValue = false,
                Enabled = false
            }, true);
            BestTargetOnlyManager.AddToMenu(eGapcloserMenu, "e-gapcloser");

            // Immobile menu
            var eImmobileMenu = miscMenu.AddSubMenu(new Menu("E Immobile", string.Format("{0}.e-immobile", miscMenu.Name)));
            HeroListManager.AddToMenu(eImmobileMenu, new HeroListManagerArgs("e-immobile")
            {
                IsWhitelist = false,
                Allies = false,
                Enemies = true,
                DefaultValue = false
            });
            BestTargetOnlyManager.AddToMenu(eImmobileMenu, "e-immobile", true);
            #endregion

            // Indicator manager
            IndicatorManager.AddToMenu(DrawingManager.Menu, true);
            IndicatorManager.Add(W);
            IndicatorManager.Add(R);
            IndicatorManager.Finale();
        }

        /// <summary>
        /// Pre update
        /// </summary>
        protected override void OnPreUpdate()
        {
        }

        /// <summary>
        /// Post update
        /// </summary>
        protected override void OnPostUpdate()
        {
            // Assisted Ultimate
            if (_ultimateManager.IsActive(UltimateModeType.Assisted) && R.IsReady())
            {
                if (_ultimateManager.ShouldMove(UltimateModeType.Assisted))
                {
                    Orbwalking.MoveTo(Game.CursorPos, Orbwalker.HoldAreaRadius);
                }

                if (!CastUltimate(UltimateModeType.Assisted, TargetSelector.GetTarget(R)))
                {
                    CastUltimateSingle(UltimateModeType.Assisted);
                }
            }

            // Auto Ultimate
            if (_ultimateManager.IsActive(UltimateModeType.Auto) && R.IsReady())
            {
                if (!CastUltimate(UltimateModeType.Auto, TargetSelector.GetTarget(R)))
                {
                    CastUltimateSingle(UltimateModeType.Auto);
                }
            }

            // E Immobile targets
            if (HeroListManager.Enabled("e-immobile") && E.IsReady())
            {
                var target = GameObjects.EnemyHeroes.FirstOrDefault(t => 
                    t.IsValidTarget(E.Range) && 
                    HeroListManager.Check("e-immobile", t) && 
                    BestTargetOnlyManager.Check("e-immobile", E, t) && 
                    Utils.IsImmobile(t)
                );

                if (target != null)
                {
                    E.Cast(target);
                }
            }
        }

        /// <summary>
        /// Combo
        /// </summary>
        protected override void Combo()
        {
            var useW = Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady();
            var useE = Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady();
            var useR = _ultimateManager.IsActive(UltimateModeType.Combo) && R.IsReady();

            Obj_AI_Hero target;

            if (useR)
            {
                target = TargetSelector.GetTarget(R.Range, R.DamageType);
                if (target.IsValidTarget())
                {
                    if (!CastUltimate(UltimateModeType.Combo, target))
                    {
                        CastUltimateSingle(UltimateModeType.Combo);
                    }
                }
            }

            if (useW)
            {
                Casting.SkillShot(W, W.GetHitChance("combo"));
            }

            if (useE)
            {
                Casting.SkillShot(E, E.GetHitChance("combo"));
            }

            target = TargetSelector.GetTarget(GetFishbonesRange(), DamageType.Physical);
            if (target.IsValidTarget())
            {
                var fishbones = FishbonesComboHarassLogic(target);
                if (fishbones != null)
                {
                    Orbwalker.ForceTarget(fishbones.Item2);
                }
            }
        }

        /// <summary>
        /// Harass 
        /// </summary>
        protected override void Harass()
        {
            var useW = Menu.Item(Menu.Name + ".harass.w").GetValue<bool>() && W.IsReady();

            if (ResourceManager.Check("harass-w") && useW)
            {
                Casting.SkillShot(W, W.GetHitChance("harass"));
            }

            var target = TargetSelector.GetTarget(GetFishbonesRange(), DamageType.Physical);
            if (target.IsValidTarget())
            {
                var fishbones = FishbonesComboHarassLogic(target);
                if (fishbones != null)
                {
                    Orbwalker.ForceTarget(fishbones.Item2);
                }
            }
        }

        /// <summary>
        /// Lane Clear
        /// </summary>
        protected override void LaneClear()
        {
            var fishBones = IsUsingFishbones();
            var useQ = Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>() && Q.IsReady();

            if (!ResourceManager.Check("lane-clear-q"))
            {
                // Switch to minigun if we don't have the mana for fishbones
                if (fishBones && useQ)
                {
                    Q.Cast();
                }
            }
        }

        /// <summary>
        /// Jungle Clear
        /// </summary>
        protected override void JungleClear()
        {
        }

        /// <summary>
        /// Flee
        /// </summary>
        protected override void Flee()
        {
            if (Menu.Item(Menu.Name + ".flee.e").GetValue<bool>() && E.IsReady())
            {
                E.Cast(Player.Position.Extend(Game.CursorPos, E.Range / 2));
            }
        }

        /// <summary>
        /// Killsteal
        /// </summary>
        protected override void Killsteal()
        {
        }

        /// <summary>
        /// Gap closer event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void GapcloserManager_OnGapcloser(object sender, GapcloserManagerArgs args)
        {
            try
            {
                if (args.UniqueId == "e-gapcloser" && E.IsReady() &&
                    BestTargetOnlyManager.Check("e-gapcloser", E, args.Hero) &&
                    args.End.Distance(Player.Position) <= E.Range)
                {
                    E.Cast(args.End);
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        /// <summary>
        /// Before attack event
        /// </summary>
        /// <param name="args"></param>
        private void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            try
            {
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
                {
                    var fishbones = FishbonesLaneClearLogic(args);
                    if (fishbones == null)
                    {
                        return;
                    }

                    Orbwalker.ForceTarget(fishbones.Item2);
                    args.Process = false;
                } 
                else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo ||
                         Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                {
                    var target = args.Target as Obj_AI_Base;
                    if (target == null)
                    {
                        return;
                    }

                    var fishbones = FishbonesComboHarassLogic(target);
                    if (fishbones == null)
                    {
                        return;
                    }

                    Orbwalker.ForceTarget(fishbones.Item2);
                    args.Process = false;
                }
                else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
                {
                    var fishbones = IsUsingFishbones();
                    if (fishbones)
                    {
                        var target = args.Target as Obj_AI_Base;
                        if (target == null)
                        {
                            return;
                        }

                        if (Player.Distance(target.ServerPosition) <= GetRealAutoAttackRange(MinigunRange, target))
                        {
                            Q.Cast();
                            Orbwalker.ForceTarget(target);
                            args.Process = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        /// <summary>
        /// Attempts to cast the ultimate on a group of targets
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private bool CastUltimate(UltimateModeType mode, Obj_AI_Hero target)
        {
            try
            {
                if (!_ultimateManager.IsActive(mode))
                {
                    return false;
                }

                var hits = GetUltimateExplosionHits(target);
                if (_ultimateManager.Check(mode, hits.Item2))
                {
                    R.Cast(hits.Item3);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }

            return false;
        }

        /// <summary>
        /// Attempts to cast the ultimate on a single target
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        private void CastUltimateSingle(UltimateModeType mode)
        {
            try
            {
                if (!_ultimateManager.ShouldSingle(mode))
                {
                    return;
                }

                foreach (var target in GameObjects.EnemyHeroes.Where(t => _ultimateManager.CheckSingle(mode, t)))
                {
                    var hits = GetUltimateExplosionHits(target);
                    if (hits.Item1 > 0)
                    {
                        R.Cast(hits.Item3);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        /// <summary>
        /// Gets fishbones range based on level
        /// </summary>
        /// <returns></returns>
        private float GetFishbonesRange()
        {
            try
            {
                var level = Q.Level;
                return 550 + 25 * level;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return Orbwalking.GetRealAutoAttackRange(null);
        }

        /// <summary>
        /// True if we are using fish bones aoe
        /// </summary>
        /// <returns></returns>
        private bool IsUsingFishbones()
        {
            try
            {
                return Player.HasBuff("JinxQ");
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return false;
        }

        /// <summary>
        /// Get the ultimate explosion hit count with best location
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private Tuple<int, List<Obj_AI_Hero>, Vector3> GetUltimateExplosionHits(Obj_AI_Hero target)
        {
            var hits = new List<Obj_AI_Hero>();
            var castPosition = Vector3.Zero;

            try
            {
                var prediction = R.GetPrediction(target);
                if (prediction.Hitchance >= R.GetHitChance("combo"))
                {
                    castPosition = prediction.CastPosition;
                    hits.Add(target);

                    var explosion = new PredictionInput
                    {
                        Range = UltimateExplosion.Range,
                        Delay = Player.Position.Distance(castPosition) / R.Speed + 0.1f,
                        From = castPosition,
                        RangeCheckFrom = castPosition,
                        Radius = UltimateExplosion.Width,
                        Type = SkillshotType.SkillshotCircle,
                        Speed = UltimateExplosion.Speed
                    };

                    var explosionCircle = new Geometry.Polygon.Circle(castPosition, UltimateExplosion.Width);

                    foreach (var enemy in GameObjects.EnemyHeroes.Where(e => e.IsValidTarget() && e.NetworkId != target.NetworkId))
                    {
                        explosion.Unit = enemy;
                        var explosionPrediction = Prediction.GetPrediction(explosion);
                        if (!explosionPrediction.UnitPosition.Equals(Vector3.Zero))
                        {
                            var enemyPosition = new Geometry.Polygon.Circle(enemy.Position, enemy.BoundingRadius);
                            if (enemyPosition.Points.Any(p => explosionCircle.IsInside(p)))
                            {
                                hits.Add(enemy);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }

            return new Tuple<int, List<Obj_AI_Hero>, Vector3>(hits.Count, hits, castPosition);
        }

        /// <summary>
        /// Get the best minion to cast aoe
        /// </summary>
        /// <returns></returns>
        private Tuple<int, Obj_AI_Base> BestFishBonesMinion()
        {
            var minions = MinionManager.GetMinions(GetFishbonesRange(), MinionTypes.All, MinionTeam.NotAlly)
                    .ToList();

            var possibilities = ListExtensions.ProduceEnumeration(minions.Select(p => p.ServerPosition.To2D()).ToList())
                    .Where(p => p.Count > 0 && p.Count < 8)
                    .ToList();

            var hits = 0;
            var center = Vector2.Zero;
            var radius = float.MaxValue;

            foreach (var possibility in possibilities)
            {
                var mec = MEC.GetMec(possibility);
                if (mec.Radius < Q.Width * 1.5f)
                {
                    if (possibility.Count > hits || possibility.Count == hits && mec.Radius < radius)
                    {
                        hits = possibility.Count;
                        radius = mec.Radius;
                        center = mec.Center;
                        if (hits == minions.Count)
                        {
                            break;
                        }
                    }
                }
            }

            if (hits > 0 && !center.Equals(Vector2.Zero))
            {
                return new Tuple<int, Obj_AI_Base>(hits, minions.OrderBy(m => m.Position.Distance(center.To3D())).FirstOrDefault());
            }

            return null;
        }

        /// <summary>
        /// Should we use fish bones aoe in lane clear
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private Tuple<int, Obj_AI_Base> FishbonesLaneClearLogic(Orbwalking.BeforeAttackEventArgs args)
        {
            var useQ = Menu.Item(Menu.Name + ".lane-clear.q").GetValue<bool>() && Q.IsReady();
            if (useQ)
            {
                return null;
            }

            if (!ResourceManager.Check("lane-clear-q"))
            {
                return null;
            }

            // If target is a hero don't change anything
            var target = args.Target as Obj_AI_Hero;
            if (target != null)
            {
                return null;
            }

            var usingFishBones = IsUsingFishbones();
            
            var bestMinion = BestFishBonesMinion();
            if (bestMinion == null)
            {
                if (usingFishBones)
                {
                    Q.Cast();
                }
                return null;
            }
            
            var fishBonesMinimum = Menu.Item(Menu.Name + ".lane-clear.q-min").GetValue<Slider>().Value;

            if (bestMinion.Item1 < fishBonesMinimum)
            {
                if (usingFishBones)
                {
                    Q.Cast();
                }
                return null;
            }

            if (!usingFishBones)
            {
                Q.Cast();
            }

            return bestMinion;
        }

        /// <summary>
        /// Should we use fish bones aoe/range in combo/harass
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private Tuple<int, Obj_AI_Base> FishbonesComboHarassLogic(Obj_AI_Base target)
        {
            var type = Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo ? "combo" : "harass";
            var useQ = Menu.Item(string.Format("{0}.{1}.q", Menu.Name, type)).GetValue<bool>() && Q.IsReady();
            var switchRange = Menu.Item(string.Format("{0}.{1}.q-range", Menu.Name, type)).GetValue<Slider>().Value;
            var switchAoe = Menu.Item(string.Format("{0}.{1}.q-aoe", Menu.Name, type)).GetValue<Slider>().Value;

            if (useQ)
            {
                return null;
            }

            if (!ResourceManager.Check(type + "-q"))
            {
                return null;
            }

            var hero = target as Obj_AI_Hero;
            if (hero == null)
            {
                return null;
            }

            var usingFishBones = IsUsingFishbones();

            var enemiesInAoeRange = target.ServerPosition.CountEnemiesInRange(Q.Width);

            if (enemiesInAoeRange >= switchAoe)
            {
                if (!usingFishBones)
                {
                    Q.Cast();
                }
                return new Tuple<int, Obj_AI_Base>(enemiesInAoeRange, target);
            }

            var distance = Player.Distance(target.ServerPosition);
            if (distance > switchRange)
            {
                if (!usingFishBones)
                {
                    Q.Cast();
                }
                return new Tuple<int, Obj_AI_Base>(1, target);
            }

            if (usingFishBones)
            {
                Q.Cast();
            }
            
            return new Tuple<int, Obj_AI_Base>(1, target);
        }

        /// <summary>
        /// Gets the real auto attack range 
        /// </summary>
        /// <param name="range"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public float GetRealAutoAttackRange(float range, AttackableUnit target)
        {
            var result = range + Player.BoundingRadius;
            if (target.IsValidTarget())
            {
                return result + target.BoundingRadius;
            }
            return result;
        }

        /// <summary>
        /// Calculates the combo damage
        /// </summary>
        /// <param name="target"></param>
        /// <param name="rangeCheck"></param>
        /// <param name="ult"></param>
        /// <returns></returns>
        private float CalculateComboDamage(Obj_AI_Hero target, bool rangeCheck, bool ult)
        {
            try
            {
                if (target == null)
                {
                    return 0;
                }

                var damage = 0f;
                if (ult && R.IsReady() && (!rangeCheck || R.IsInRange(target)))
                {
                    //damage += R.GetDamage(target);

                    var level = R.Level;
                    var percentHealth = 20f + (level * 5); // 25, 30, 35
                    var targetMissingHealth = target.MaxHealth - target.Health;
                    var percentMissingHealth = percentHealth / 100 * targetMissingHealth;

                    var physicalDamageModifier = 15f + (level * 10) + 0.1 * Player.FlatPhysicalDamageMod; // 25, 35, 45
                    var distanceDamageModifier = Math.Min((1 + Player.Distance(target.ServerPosition) / 15 * 0.09d), 10);

                    var amount = percentMissingHealth + (physicalDamageModifier * distanceDamageModifier);

                    damage += (float)Player.CalcDamage(target, Damage.DamageType.Physical, amount);
                }

                if (!rangeCheck || target.Distance(Player) <= Orbwalking.GetRealAutoAttackRange(target) * 0.85f)
                {
                    damage += 2 * (float)Player.GetAutoAttackDamage(target, true);
                }

                damage += ItemManager.CalculateComboDamage(target, rangeCheck);
                damage += SummonerManager.CalculateComboDamage(target, rangeCheck);

                return damage;
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
            return 0;
        }
    }
}
