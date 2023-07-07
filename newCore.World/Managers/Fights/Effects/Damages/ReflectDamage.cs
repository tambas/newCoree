﻿using Giny.Protocol.Custom.Enums;
using Giny.Protocol.Enums;
using Giny.World.Managers.Effects;
using Giny.World.Managers.Fights.Cast;
using Giny.World.Managers.Fights.Cast.Units;
using Giny.World.Managers.Fights.Fighters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Giny.World.Managers.Fights.Effects.Damages
{
    [SpellEffectHandler(EffectsEnum.Effect_ReflectDamage)]
    public class ReflectDamage : SpellEffectHandler
    {
        public ReflectDamage(EffectDice effect, SpellCastHandler castHandler) : base(effect, castHandler)
        {
        }

        protected override void Apply(IEnumerable<Fighter> targets)
        {
            Damage damage = GetTriggerToken<Damage>();

            if (damage != null)
            {
                ReflectDamages(damage);
            }
            else
            {
                foreach (var target in targets)
                {
                    AddStatBuff(target, (short)Effect.Min, target.Stats[CharacteristicEnum.REFLECT], FightDispellableEnum.DISPELLABLE);
                }
            }
        }

        private void ReflectDamages(Damage damage)
        {
            short reflected = (short)Effect.Min;
            Damage reflectDamage = new Damage(damage.Target, damage.Source, damage.EffectSchool, reflected, reflected, damage.GetEffectHandler());
            reflectDamage.IgnoreBoost = true;
            reflectDamage.IgnoreResistances = true;
            damage.Source.InflictDamage(reflectDamage);
            damage.Target.OnDamageReflected(damage.Source);
        }
    }
}
