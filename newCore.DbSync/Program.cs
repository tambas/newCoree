﻿using Giny.Core;
using Giny.Core.IO;
using Giny.IO;
using Giny.IO.D2I;
using Giny.IO.D2O;
using Giny.ORM;
using Giny.ORM.Interfaces;
using Giny.ORM.IO;
using Giny.World.Managers.Entities.Look;
using Giny.World.Records;
using Giny.World.Records.Breeds;
using Giny.World.Records.Challenges;
using Giny.World.Records.Characters;
using Giny.World.Records.Effects;
using Giny.World.Records.Idols;
using Giny.World.Records.Items;
using Giny.World.Records.Jobs;
using Giny.World.Records.Maps;
using Giny.World.Records.Monsters;
using Giny.World.Records.Npcs;
using Giny.World.Records.Spells;
using Giny.World.Records.Tinsel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Giny.DatabaseSynchronizer
{
    class Program
    {
        public static D2IFile D2IFileFR;
        public static D2IFile D2IFileEN;

        public static string ClientPath;

        static void Main(string[] args)
        {
            Logger.DrawLogo();

            ClientPath = ConfigurationManager.AppSettings["clientPath"];

            if (!Directory.Exists(ClientPath))
            {
                Logger.Write("Unable to locate dofus client. Edit App.config", Channels.Warning);
                Console.ReadLine();
                return;
            }

            D2IFileFR = new D2IFile(Path.Combine(ClientPath, ClientConstants.i18nPathFR));
            //D2IFileEN = new D2IFile(Path.Combine(ClientPath, ClientConstants.i18nPathEN));

            DatabaseManager.Instance.Initialize(Assembly.GetAssembly(typeof(BreedRecord)),
              "127.0.0.1", "giny_world", "root", "");

            DatabaseManager.Instance.DropTableIfExists<RecipeRecord>();
            DatabaseManager.Instance.DropTableIfExists<SubareaRecord>();
            DatabaseManager.Instance.DropTableIfExists<ItemSetRecord>();
            DatabaseManager.Instance.DropTableIfExists<IdolRecord>();
            DatabaseManager.Instance.DropTableIfExists<BreedRecord>();
            DatabaseManager.Instance.DropTableIfExists<ExperienceRecord>();
            DatabaseManager.Instance.DropTableIfExists<HeadRecord>();
            DatabaseManager.Instance.DropTableIfExists<EffectRecord>();
            DatabaseManager.Instance.DropTableIfExists<MapScrollActionRecord>();
            DatabaseManager.Instance.DropTableIfExists<SpellRecord>();
            DatabaseManager.Instance.DropTableIfExists<SpellVariantRecord>();
            DatabaseManager.Instance.DropTableIfExists<ItemRecord>();
            DatabaseManager.Instance.DropTableIfExists<SpellStateRecord>();
            DatabaseManager.Instance.DropTableIfExists<WeaponRecord>();
            DatabaseManager.Instance.DropTableIfExists<LivingObjectRecord>();
            DatabaseManager.Instance.DropTableIfExists<EmoteRecord>();
            DatabaseManager.Instance.DropTableIfExists<SpellLevelRecord>();
            DatabaseManager.Instance.DropTableIfExists<OrnamentRecord>();
            DatabaseManager.Instance.DropTableIfExists<TitleRecord>();
            DatabaseManager.Instance.DropTableIfExists<MonsterRecord>();
            DatabaseManager.Instance.DropTableIfExists<SkillRecord>();
            DatabaseManager.Instance.DropTableIfExists<MapPositionRecord>();
            DatabaseManager.Instance.DropTableIfExists<NpcRecord>();
            DatabaseManager.Instance.DropTableIfExists<ChallengeRecord>();
            DatabaseManager.Instance.DropTableIfExists<MapRecord>();

            DatabaseManager.Instance.CreateAllTablesIfNotExists();

            D2OSynchronizer.Synchronize();

            MapSynchronizer.Synchronize();

            Logger.WriteColor1("Build finished.");
            Console.Read();

        }



    }
}
