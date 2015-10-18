using SFXChallenger.Abstracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFXChallenger.Enumerations;
using SFXChallenger.Wrappers;
using LeagueSharp;

namespace SFXChallenger.Champions
{
    class Kindred : Champion
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
        }

        protected override void Combo()
        {
        }

        protected override void Flee()
        {
        }

        protected override void Harass()
        {
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
        }

        protected override void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q);

            W = new Spell(SpellSlot.W);

            E = new Spell(SpellSlot.E);

            R = new Spell(SpellSlot.R);
        }
    }
}
