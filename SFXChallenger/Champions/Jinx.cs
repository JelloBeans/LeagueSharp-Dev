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
using Spell = SFXChallenger.Wrappers.Spell;

namespace SFXChallenger.Champions
{
    class Jinx : Champion
    {
        private UltimateManager _ultimateManager;

        private Spell UltimateExplosion { get; set; }

        protected override ItemFlags ItemFlags => ItemFlags.Offensive | ItemFlags.Defensive | ItemFlags.Flee;

        protected override ItemUsageType ItemUsage => ItemUsageType.AfterAttack;

        protected override void OnLoad()
        {
            GapcloserManager.OnGapcloser += GapcloserManager_OnGapcloser;
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q);

            W = new Spell(SpellSlot.W, 1450f);
            W.SetSkillshot(1.1f, 60f, 3300f, true, SkillshotType.SkillshotLine);

            E = new Spell(SpellSlot.E, 900f);
            E.SetSkillshot(1.1f, 1f, 1750f, false, SkillshotType.SkillshotCircle);

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
            // Combo menu
            var comboMenu = Menu.AddSubMenu(new Menu("Combo", $"{Menu.Name}.combo"));
            comboMenu.AddItem(new MenuItem($"{comboMenu.Name}.q", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem($"{comboMenu.Name}.w", "Use W").SetValue(true));
            comboMenu.AddItem(new MenuItem($"{comboMenu.Name}.e", "Use E").SetValue(true));

            var comboHitChanceMenu = comboMenu.AddSubMenu(new Menu("Hitchance", $"{comboMenu.Name}.hitchance"));
            var comboHitChanceDictionary = new Dictionary<string, HitChance>
            {
                { "W", HitChance.VeryHigh },
                { "R", HitChance.VeryHigh }
            };
            HitchanceManager.AddToMenu(comboHitChanceMenu, "combo", comboHitChanceDictionary);

            ResourceManager.AddToMenu(comboMenu, new ResourceManagerArgs("combo-w", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
            {
                Prefix = "W",
                DefaultValue = 15
            });

            // Harass menu
            var harassMenu = Menu.AddSubMenu(new Menu("Harass", $"{Menu.Name}.harass"));
            harassMenu.AddItem(new MenuItem($"{harassMenu.Name}.q", "Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem($"{harassMenu.Name}.w", "Use W").SetValue(true));

            var harassHitChanceMenu = harassMenu.AddSubMenu(new Menu("Hitchance", $"{harassMenu.Name}.hitchance"));
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
            var laneclearMenu = Menu.AddSubMenu(new Menu("Lane Clear", $"{Menu.Name}.lane-clear"));
            laneclearMenu.AddItem(new MenuItem($"{laneclearMenu.Name}.q", "Use Q").SetValue(true));
            laneclearMenu.AddItem(new MenuItem($"{laneclearMenu.Name}.q-min", "Q Min.").SetValue(new Slider(3, 1, 5)));

            ResourceManager.AddToMenu(laneclearMenu, new ResourceManagerArgs("lane-clear-q", ResourceType.Mana, ResourceValueType.Percent, ResourceCheckType.Minimum)
            {
                Prefix = "Q",
                Advanced = true,
                MaxValue = 101,
                LevelRanges = new SortedList<int, int> { { 1, 6 }, { 6, 12 }, { 12, 18 } },
                DefaultValues = new List<int> { 30, 30, 30 }
            });

            // Flee menu
            var fleeMenu = Menu.AddSubMenu(new Menu("Flee", $"{Menu.Name}.flee"));
            fleeMenu.AddItem(new MenuItem($"{fleeMenu.Name}.e", "Use E").SetValue(true));

            // Misc menu
            var miscMenu = Menu.AddSubMenu(new Menu("Misc", $"{Menu.Name}.miscellaneous"));

            // Gapcloser menu
            var eGapcloserMenu = miscMenu.AddSubMenu(new Menu("E Gapcloser", $"{miscMenu.Name}.e-gapcloser"));
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
        }

        protected override void Combo()
        {
        }

        protected override void Harass()
        {
        }

        protected override void LaneClear()
        {
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
    }
}
