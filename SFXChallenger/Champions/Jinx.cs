using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SFXChallenger.Abstracts;
using SFXChallenger.Args;
using SFXChallenger.Enumerations;
using SFXChallenger.Library;
using SFXChallenger.Library.Logger;
using SFXChallenger.Managers;
using SharpDX;
using SFXChallenger.Helpers;

using Spell = SFXChallenger.Wrappers.Spell;
using TargetSelector = SFXChallenger.SFXTargetSelector.TargetSelector;
using Orbwalking = SFXChallenger.Wrappers.Orbwalking;
using MinionManager = SFXChallenger.Library.MinionManager;
using MinionOrderTypes = SFXChallenger.Library.MinionOrderTypes;
using MinionTeam = SFXChallenger.Library.MinionTeam;
using MinionTypes = SFXChallenger.Library.MinionTypes;
using SFXChallenger.Library.Extensions.NET;

namespace SFXChallenger.Champions
{
    class Jinx : Champion
    {
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
        protected override void OnLoad()
        {
            GapcloserManager.OnGapcloser += GapcloserManager_OnGapcloser;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
        }        

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q);
            Q.SetSkillshot(0.5f, 100f, Player.BasicAttack.MissileSpeed, false, SkillshotType.SkillshotCircle);

            W = new Spell(SpellSlot.W, 1450f);
            W.SetSkillshot(0.6f, 60f, 3300f, true, SkillshotType.SkillshotLine);

            E = new Spell(SpellSlot.E, 900f);
            E.SetSkillshot(0.7f, 120f, 1750f, false, SkillshotType.SkillshotCircle);

            R = new Spell(SpellSlot.R, 3200f);
            R.SetSkillshot(0.6f, 140f, 1700f, false, SkillshotType.SkillshotLine); // Set collision to false as we calculate collision our selves (radius impact)

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
                    damage += R.GetDamage(target);
                }

                if (!rangeCheck || target.Distance(Player) <= Orbwalking.GetRealAutoAttackRange(target) * 0.85f)
                {
                    damage += 2 * (float) Player.GetAutoAttackDamage(target, true);
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

        protected override void AddToMenu()
        {
            _ultimateManager.AddToMenu(Menu);

            // Combo menu
            var comboMenu = Menu.AddSubMenu(new Menu("Combo", string.Format("{0}.combo", Menu.Name)));
            comboMenu.AddItem(new MenuItem(string.Format("{0}.q", comboMenu.Name), "Use Q").SetValue(true));
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

            // Harass menu
            var harassMenu = Menu.AddSubMenu(new Menu("Harass", string.Format("{0}.harass", Menu.Name)));
            harassMenu.AddItem(new MenuItem(string.Format("{0}.q", harassMenu.Name), "Use Q").SetValue(true));
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

            // Laneclear menu
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

            // Flee menu
            var fleeMenu = Menu.AddSubMenu(new Menu("Flee", string.Format("{0}.flee", Menu.Name)));
            fleeMenu.AddItem(new MenuItem(string.Format("{0}.e", fleeMenu.Name), "Use E").SetValue(true));

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

            // Indicator manager
            IndicatorManager.AddToMenu(DrawingManager.Menu, true);
            IndicatorManager.Add(W);
            IndicatorManager.Add(R);
            IndicatorManager.Finale();
        }

        protected override void OnPreUpdate()
        {
        }

        protected override void OnPostUpdate()
        {
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

            if (_ultimateManager.IsActive(UltimateModeType.Auto) && R.IsReady())
            {
                if (!CastUltimate(UltimateModeType.Auto, TargetSelector.GetTarget(R)))
                {
                    CastUltimateSingle(UltimateModeType.Auto);
                }
            }
        }

        protected override void Combo()
        {
            var useW = Menu.Item(Menu.Name + ".combo.w").GetValue<bool>() && W.IsReady();
            var useE = Menu.Item(Menu.Name + ".combo.e").GetValue<bool>() && E.IsReady();
            var useR = _ultimateManager.IsActive(UltimateModeType.Combo) && R.IsReady();

            if (useR)
            {
                var target = TargetSelector.GetTarget(R.Range, R.DamageType);
                if (target != null)
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
        }

        protected override void Harass()
        {
        }

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
                return;
            }

            
            if (useQ)
            {
                var fishBonesMinimum = Menu.Item(Menu.Name + ".lane-clear.q-min").GetValue<Slider>().Value;
                var range = 575 + (25 * Q.Level); // Bonus Range: 75, 100, 125, 150, 175
                var minions = MinionManager.GetMinions(range);
                if (minions.Count >= fishBonesMinimum && !fishBones)
                {
                    Q.Cast();
                }
                else if (minions.Count < fishBonesMinimum && fishBones)
                {
                    Q.Cast();
                }
            }
        }

        protected override void JungleClear()
        {
        }

        protected override void Flee()
        {
        }

        protected override void Killsteal()
        {
        }

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

        private void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            try
            { 
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
                {
                    if (IsUsingFishbones())
                    {
                        var bestMinion = BestFishBonesMinion();
                        if (bestMinion != null && bestMinion.NetworkId != args.Target.NetworkId)
                        {
                            Orbwalker.ForceTarget(bestMinion);
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

        private bool CastUltimateSingle(UltimateModeType mode)
        {
            try
            {
                if (!_ultimateManager.ShouldSingle(mode))
                {
                    return false;
                }

                foreach (var target in GameObjects.EnemyHeroes.Where(t => _ultimateManager.CheckSingle(mode, t)))
                {
                    var hits = GetUltimateExplosionHits(target);
                    if (hits.Item1 > 0)
                    {
                        R.Cast(hits.Item3);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }

            return false;
        }

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

        private Obj_AI_Base BestFishBonesMinion()
        {
            var minions = MinionManager.GetMinions(float.MaxValue, MinionTypes.All, MinionTeam.NotAlly)
                    .Where(Orbwalking.InAutoAttackRange)
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
                return minions.OrderBy(m => m.Position.Distance(center.To3D())).FirstOrDefault();
            }

            return null;
        }
    }
}
