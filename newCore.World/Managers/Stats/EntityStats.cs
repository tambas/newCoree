using Giny.Core.DesignPattern;
using Giny.Protocol.Custom.Enums;
using Giny.Protocol.Types;
using Giny.World.Managers.Breeds;
using Giny.World.Managers.Entities.Characters;
using Giny.World.Managers.Experiences;
using Giny.World.Managers.Formulas;
using Giny.World.Records;
using Giny.World.Records.Breeds;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Giny.World.Managers.Stats
{
    [ProtoContract]
    public class EntityStats
    {
        public const short BaseSummonsCount = 1;

        public event Action LifePointsChanged;

        private int m_lifePoints;

        public int LifePoints
        {
            get
            {
                LifePointsChanged?.Invoke();
                return m_lifePoints;
            }
            set
            {
                m_lifePoints = value;
            }
        }

        [WIP("formule inexact (arrondi)")]
        public double LifePercentage => (LifePoints / (double)MaxLifePoints) * 100;


        [ProtoMember(1)]
        public int MaxLifePoints
        {
            get;
            set;
        }

        public int MissingLife
        {
            get
            {
                return MaxLifePoints - LifePoints;
            }
        }
        [ProtoMember(2)]
        public short MaxEnergyPoints
        {
            get;
            set;
        }
        /// <summary>
        /// Deprecated ? 
        /// </summary>
        [ProtoMember(3)]
        public short CriticalHitWeapon
        {
            get;
            set;
        }
        /// <summary>
        /// Deprecated ? 
        /// </summary>
        [ProtoMember(4)]
        public short GlobalDamageReduction
        {
            get;
            set;
        }
        [ProtoMember(5)]
        private Dictionary<CharacteristicEnum, Characteristic> Characteristics
        {
            get;
            set;
        } = new Dictionary<CharacteristicEnum, Characteristic>();

        public short Energy
        {
            get;
            set;
        }
        public int TotalInitiative
        {
            get
            {
                return StatsFormulas.Instance.TotalInitiative(this);
            }
        }

        public Characteristic Strength => this[CharacteristicEnum.STRENGTH];
        public Characteristic Wisdom => this[CharacteristicEnum.WISDOM];
        public Characteristic Chance => this[CharacteristicEnum.CHANCE];
        public Characteristic Agility => this[CharacteristicEnum.AGILITY];
        public Characteristic Intelligence => this[CharacteristicEnum.INTELLIGENCE];
        public Characteristic ActionPoints => this[CharacteristicEnum.ACTION_POINTS];
        public Characteristic MovementPoints => this[CharacteristicEnum.MOVEMENT_POINTS];

        public void Initialize()
        {
            this.LifePoints = this.MaxLifePoints;
            this.Energy = this.MaxEnergyPoints;

            ((RelativeCharacteristic)this[CharacteristicEnum.DODGE_PALOST_PROBABILITY]).Bind(Wisdom);
            ((RelativeCharacteristic)this[CharacteristicEnum.PAATTACK]).Bind(Wisdom);

            ((RelativeCharacteristic)this[CharacteristicEnum.DODGE_PMLOST_PROBABILITY]).Bind(Wisdom);
            ((RelativeCharacteristic)this[CharacteristicEnum.PMATTACK]).Bind(Wisdom);


            ((RelativeCharacteristic)this[CharacteristicEnum.TACKLE_BLOCK]).Bind(Agility);
            ((RelativeCharacteristic)this[CharacteristicEnum.TACKLE_EVADE]).Bind(Agility);

            ((RelativeCharacteristic)this[CharacteristicEnum.PROSPECTING]).Bind(Chance);
        }
        public Characteristic GetCharacteristic(StatsBoostEnum statId)
        {
            switch (statId)
            {
                case StatsBoostEnum.STRENGTH:
                    return Strength;
                case StatsBoostEnum.VITALITY:
                    return this[CharacteristicEnum.VITALITY];
                case StatsBoostEnum.WISDOM:
                    return Wisdom;
                case StatsBoostEnum.CHANCE:
                    return Chance;
                case StatsBoostEnum.AGILITY:
                    return Agility;
                case StatsBoostEnum.INTELLIGENCE:
                    return Intelligence;
            }
            return null;
        }

        public int Total()
        {
            return Strength.Total() + Chance.Total() + Intelligence.Total() + Agility.Total();
        }

        public virtual CharacterCharacteristic[] GetCharacterCharacteristics(CharacteristicEnum[] selected = null)
        {
            List<CharacterCharacteristic> results = new List<CharacterCharacteristic>();

            if (selected == null)
            {
                foreach (KeyValuePair<CharacteristicEnum, Characteristic> stat in this.GetCharacteristics())
                {
                    var characterCharacteristic = stat.Value.GetCharacterCharacteristicDetailed(stat.Key);
                    results.Add(characterCharacteristic);
                }
            }
            else
            {
                foreach (KeyValuePair<CharacteristicEnum, Characteristic> stat in this.GetCharacteristics().Where(x => selected.Contains(x.Key)))
                {
                    var characterCharacteristic = stat.Value.GetCharacterCharacteristicDetailed(stat.Key);
                    results.Add(characterCharacteristic);
                }
            }
            results.Add(new CharacterCharacteristicValue(LifePoints, (short)CharacteristicEnum.LIFE_POINTS));
            results.Add(new CharacterCharacteristicValue(MaxLifePoints, (short)CharacteristicEnum.MAX_LIFE_POINTS));
            results.Add(new CharacterCharacteristicValue(MaxEnergyPoints, (short)CharacteristicEnum.MAX_ENERGY_POINTS));
            results.Add(new CharacterCharacteristicValue(Energy, (short)CharacteristicEnum.ENERGY_POINTS));

            return results.ToArray();
        }
        public CharacterCharacteristicsInformations GetCharacterCharacteristicsInformations(Character character)
        {
            var alignementInfos = new ActorExtendedAlignmentInformations(0, 0, 0, 0, 0, 0, 0, 0);

            return new CharacterCharacteristicsInformations()
            {
                alignmentInfos = alignementInfos,
                experienceBonusLimit = 0,
                characteristics = GetCharacterCharacteristics(),
                probationTime = 0,
                spellModifications = new CharacterSpellModification[0],
                criticalHitWeapon = CriticalHitWeapon,
                experience = character.Record.Experience,
                kamas = character.Record.Kamas,
                experienceLevelFloor = character.LowerBoundExperience,
                experienceNextLevelFloor = character.UpperBoundExperience,
            };
        }
        public Characteristic this[CharacteristicEnum characteristicEnum]
        {
            get
            {
                return this.Characteristics[characteristicEnum];
            }
            set
            {
                if (!this.Characteristics.ContainsKey(characteristicEnum))
                {
                    this.Characteristics.Add(characteristicEnum, value);
                }
                else
                {
                    this.Characteristics[characteristicEnum] = value;
                }
            }
        }
        public static EntityStats New(short level, byte breedId)
        {
            var stats = new EntityStats()
            {
                LifePoints = BreedManager.BreedDefaultLife,
                MaxLifePoints = BreedManager.BreedDefaultLife,
                MaxEnergyPoints = (short)(level * 100),
                Energy = (short)(level * 100),
                CriticalHitWeapon = 0,
            };

            stats[CharacteristicEnum.ACTION_POINTS] = ApCharacteristic.New(ConfigFile.Instance.StartAp);
            stats[CharacteristicEnum.MOVEMENT_POINTS] = MpCharacteristic.New(ConfigFile.Instance.StartMp);
            stats[CharacteristicEnum.AGILITY] = Characteristic.Zero();
            stats[CharacteristicEnum.AIR_DAMAGE_BONUS] = Characteristic.Zero();
            stats[CharacteristicEnum.AIR_ELEMENT_REDUCTION] = Characteristic.Zero();
            stats[CharacteristicEnum.AIR_ELEMENT_RESIST_PERCENT] = ResistanceCharacteristic.Zero();
            stats[CharacteristicEnum.ALL_DAMAGES_BONUS] = Characteristic.Zero();
            stats[CharacteristicEnum.DAMAGES_BONUS_PERCENT] = Characteristic.Zero();
            stats[CharacteristicEnum.CHANCE] = Characteristic.Zero();
            stats[CharacteristicEnum.CRITICAL_DAMAGE_BONUS] = Characteristic.Zero();
            stats[CharacteristicEnum.CRITICAL_DAMAGE_REDUCTION] = Characteristic.Zero();
            stats[CharacteristicEnum.CRITICAL_HIT] = Characteristic.Zero();
            stats[CharacteristicEnum.INITIATIVE] = Characteristic.Zero();
            stats[CharacteristicEnum.DODGE_PALOST_PROBABILITY] = PointDodgeCharacteristic.Zero();
            stats[CharacteristicEnum.DODGE_PMLOST_PROBABILITY] = PointDodgeCharacteristic.Zero();
            stats[CharacteristicEnum.EARTH_DAMAGE_BONUS] = Characteristic.Zero();
            stats[CharacteristicEnum.EARTH_ELEMENT_REDUCTION] = Characteristic.Zero();
            stats[CharacteristicEnum.EARTH_ELEMENT_RESIST_PERCENT] = ResistanceCharacteristic.Zero();
            stats[CharacteristicEnum.FIRE_DAMAGE_BONUS] = Characteristic.Zero();
            stats[CharacteristicEnum.FIRE_ELEMENT_REDUCTION] = Characteristic.Zero();
            stats[CharacteristicEnum.FIRE_ELEMENT_RESIST_PERCENT] = ResistanceCharacteristic.Zero();
            stats[CharacteristicEnum.GLYPH_POWER] = Characteristic.Zero();
            stats[CharacteristicEnum.RUNE_POWER] = Characteristic.Zero();
            stats[CharacteristicEnum.PERMANENT_DAMAGE_PERCENT] = Characteristic.Zero();
            stats[CharacteristicEnum.HEAL_BONUS] = Characteristic.Zero();
            stats[CharacteristicEnum.INTELLIGENCE] = Characteristic.Zero();
            stats[CharacteristicEnum.NEUTRAL_DAMAGE_BONUS] = Characteristic.Zero();
            stats[CharacteristicEnum.NEUTRAL_ELEMENT_REDUCTION] = Characteristic.Zero();
            stats[CharacteristicEnum.NEUTRAL_ELEMENT_RESIST_PERCENT] = ResistanceCharacteristic.Zero();
            stats[CharacteristicEnum.PROSPECTING] = RelativeCharacteristic.New(BreedManager.BreedDefaultProspecting);
            stats[CharacteristicEnum.PUSH_DAMAGE_BONUS] = Characteristic.Zero();
            stats[CharacteristicEnum.PUSH_DAMAGE_REDUCTION] = Characteristic.Zero();
            stats[CharacteristicEnum.RANGE] = RangeCharacteristic.Zero();
            stats[CharacteristicEnum.REFLECT] = Characteristic.Zero();
            stats[CharacteristicEnum.STRENGTH] = Characteristic.Zero();
            stats[CharacteristicEnum.SUMMONABLE_CREATURES_BOOST] = Characteristic.New(BaseSummonsCount);
            stats[CharacteristicEnum.TRAP_BONUS] = Characteristic.Zero();
            stats[CharacteristicEnum.TRAP_BONUS_PERCENT] = Characteristic.Zero();
            stats[CharacteristicEnum.VITALITY] = Characteristic.Zero();
            stats[CharacteristicEnum.WATER_DAMAGE_BONUS] = Characteristic.Zero();
            stats[CharacteristicEnum.WATER_ELEMENT_REDUCTION] = Characteristic.Zero();
            stats[CharacteristicEnum.WATER_ELEMENT_RESIST_PERCENT] = ResistanceCharacteristic.Zero();
            stats[CharacteristicEnum.WEAPON_DAMAGES_BONUS_PERCENT] = Characteristic.Zero();
            stats[CharacteristicEnum.WISDOM] = Characteristic.Zero();
            stats[CharacteristicEnum.TACKLE_BLOCK] = RelativeCharacteristic.Zero();
            stats[CharacteristicEnum.TACKLE_EVADE] = RelativeCharacteristic.Zero();
            stats[CharacteristicEnum.PAATTACK] = RelativeCharacteristic.Zero();
            stats[CharacteristicEnum.PMATTACK] = RelativeCharacteristic.Zero();
            stats[CharacteristicEnum.MELEE_DAMAGE_DONE_PERCENT] = Characteristic.Zero();
            stats[CharacteristicEnum.MELEE_DAMAGE_RECEIVED_PERCENT] = Characteristic.Zero();
            stats[CharacteristicEnum.RANGED_DAMAGE_DONE_PERCENT] = Characteristic.Zero();
            stats[CharacteristicEnum.RANGED_DAMAGE_RECEIVED_PERCENT] = Characteristic.Zero();
            stats[CharacteristicEnum.SPELL_DAMAGE_DONE_PERCENT] = Characteristic.Zero();
            stats[CharacteristicEnum.SPELL_DAMAGE_RECEIVED_PERCENT] = Characteristic.Zero();
            stats[CharacteristicEnum.WEAPON_DAMAGE_DONE_PERCENT] = Characteristic.Zero();
            stats[CharacteristicEnum.WEAPON_DAMAGE_RECEIVED_PERCENT] = Characteristic.Zero();
            stats[CharacteristicEnum.WEIGHT] = Characteristic.Zero();
            stats.Initialize();

            return stats;
        }

        public Dictionary<CharacteristicEnum, Characteristic> GetCharacteristics()
        {
            return Characteristics;
        }

    }
}
