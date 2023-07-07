﻿
using Giny.Core.DesignPattern;
using Giny.Core.Extensions;
using Giny.ORM;
using Giny.Protocol.Custom.Enums;
using Giny.Protocol.Enums;
using Giny.Protocol.Messages;
using Giny.Protocol.Types;
using Giny.World.Api;
using Giny.World.Handlers.Roleplay.Maps.Paths;
using Giny.World.Managers.Bidshops;
using Giny.World.Managers.Breeds;
using Giny.World.Managers.Dialogs;
using Giny.World.Managers.Dialogs.DialogBox;
using Giny.World.Managers.Entities.Characters.HumanOptions;
using Giny.World.Managers.Entities.Look;
using Giny.World.Managers.Entities.Merchants;
using Giny.World.Managers.Entities.Npcs;
using Giny.World.Managers.Exchanges;
using Giny.World.Managers.Exchanges.Jobs;
using Giny.World.Managers.Experiences;
using Giny.World.Managers.Fights;
using Giny.World.Managers.Fights.Fighters;
using Giny.World.Managers.Generic;
using Giny.World.Managers.Guilds;
using Giny.World.Managers.Idols;
using Giny.World.Managers.Items;
using Giny.World.Managers.Items.Collections;
using Giny.World.Managers.Maps;
using Giny.World.Managers.Maps.Elements;
using Giny.World.Managers.Parties;
using Giny.World.Managers.Shortcuts;
using Giny.World.Managers.Skills;
using Giny.World.Managers.Spells;
using Giny.World.Managers.Stats;
using Giny.World.Network;
using Giny.World.Records;
using Giny.World.Records.Bidshops;
using Giny.World.Records.Breeds;
using Giny.World.Records.Characters;
using Giny.World.Records.Guilds;
using Giny.World.Records.Items;
using Giny.World.Records.Maps;
using Giny.World.Records.Npcs;
using Giny.World.Records.Spells;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Giny.World.Managers.Entities.Characters
{
    public class Character : Entity
    {
        public override long Id => Record.Id;

        public override string Name => Record.Name;

        public CharacterRecord Record
        {
            get;
            private set;
        }

        public Party Party
        {
            get;
            set;
        }

        public List<Party> GuestedParties
        {
            get;
            set;
        }

        public bool HasParty => Party != null;

        public GameContextEnum? Context
        {
            get;
            private set;
        }

        public bool Busy => Dialog != null || RequestBox != null || ChangeMap || Collecting || IsMoving;

        public CharacterFighter Fighter
        {
            get;
            private set;
        }
        public GeneralShortcutBar GeneralShortcutBar
        {
            get;
            private set;
        }
        public SpellShortcutBar SpellShortcutBar
        {
            get;
            private set;
        }
        private List<CharacterHumanOption> HumanOptions
        {
            get;
            set;
        }
        public IdolsInventory IdolsInventory
        {
            get;
            private set;
        }
        public EntityStats Stats
        {
            get
            {
                return Record.Stats;
            }
        }
        public BreedRecord Breed
        {
            get;
            private set;
        }
        public Guild Guild
        {
            get;
            private set;
        }
        [WIP("useless?")]
        public GuildMemberRecord GuildMember
        {
            get;
            private set;
        }
        public bool HasGuild => Record.GuildId != 0;

        public bool JustCreated
        {
            get;
            set;
        }
        public WorldClient Client
        {
            get;
            private set;
        }
        public DateTime LastSalesChatMessage
        {
            get;
            set;
        }
        public DateTime LastSeekChatMessage
        {
            get;
            set;
        }
        public bool ChangeMap
        {
            get;
            private set;
        }
        public override short CellId
        {
            get
            {
                return Record.CellId;
            }
            set
            {
                Record.CellId = value;
            }
        }


        private short m_level;

        public short Level
        {
            get
            {
                return m_level;
            }

            private set
            {
                this.m_level = value;
                this.LowerBoundExperience = ExperienceManager.Instance.GetCharacterXPForLevel(Level);
                this.UpperBoundExperience = ExperienceManager.Instance.GetCharacterXPForNextLevel(Level);
            }
        }


        public short SafeLevel
        {
            get
            {
                return Level > ExperienceManager.MaxLevel ? ExperienceManager.MaxLevel : Level;
            }
        }
        public override DirectionsEnum Direction
        {
            get
            {
                return Record.Direction;
            }
            set
            {
                Record.Direction = value;
            }
        }
        public bool IsMoving
        {
            get;
            private set;
        }
        public short[] MovementKeys
        {
            get;
            private set;
        }
        public long LowerBoundExperience
        {
            get;
            private set;
        }
        public long UpperBoundExperience
        {
            get;
            private set;
        }
        public long Experience
        {
            get
            {
                return this.Record.Experience;
            }
            private set
            {
                this.Record.Experience = value;

                if (value >= this.UpperBoundExperience && this.Level < ExperienceManager.MaxLevelOmega || value < this.LowerBoundExperience)
                {
                    short level = this.Level;
                    this.Level = ExperienceManager.Instance.GetCharacterLevel(Record.Experience);
                    short difference = (short)(this.Level - level);
                    this.OnLevelChanged(level, difference);
                }
            }
        }


        public override ServerEntityLook Look
        {
            get
            {
                return Record.Look;
            }
            set
            {
                Record.Look = value;
            }
        }
        public bool CharacterLoadingComplete
        {
            get;
            set;
        }

        public short MovedCell
        {
            get;
            set;
        }
        public bool Fighting => Fighter != null;

        public Inventory Inventory
        {
            get;
            private set;
        }

        public FighterRefusedReasonEnum CanRequestFight(Character target)
        {
            FighterRefusedReasonEnum result;

            if (target.Fighting || target.Busy)
            {
                result = FighterRefusedReasonEnum.OPPONENT_OCCUPIED;
            }
            else
            {
                if (this.Fighting || this.Busy)
                {
                    result = FighterRefusedReasonEnum.IM_OCCUPIED;
                }
                else
                {
                    if (target == this)
                    {
                        result = FighterRefusedReasonEnum.FIGHT_MYSELF;
                    }
                    else
                    {
                        if (this.ChangeMap || target.ChangeMap || target.Map != Map || !Map.Position.AllowFightChallenges)
                        {
                            result = FighterRefusedReasonEnum.WRONG_MAP;
                        }
                        else
                        {
                            result = FighterRefusedReasonEnum.FIGHTER_ACCEPTED;
                        }
                    }
                }
            }
            return result;
        }



        public MerchantItemCollection MerchantItems
        {
            get;
            private set;
        }
        public BankItemCollection BankItems
        {
            get;
            private set;
        }
        public Dialog Dialog
        {
            get;
            set;
        }
        public RequestBox RequestBox
        {
            get;
            set;
        }

        public bool Collecting
        {
            get;
            set;
        }
        public List<SkillRecord> SkillsAllowed
        {
            get;
            private set;
        }
        public double XpBonusPercent
        {
            get;
            set;
        }
        public double XpRatioMount
        {
            get;
            set;
        }
        public double XpGuildGivenPercent
        {
            get;
            set;
        }
        public double XpAlliancePrismBonusPercent
        {
            get;
            set;
        }
        [WIP("pokefus , companions (verification cellule)")]
        public int FighterCount => 1;

        public Character(WorldClient client, CharacterRecord record) : base(null)
        {
            this.Record = record;
            this.Client = client;
            this.CharacterLoadingComplete = false;
            this.Level = ExperienceManager.Instance.GetCharacterLevel(Experience);
            this.Breed = BreedRecord.GetBreed(record.BreedId);

            this.Inventory = new Inventory(this, CharacterItemRecord.GetCharacterItems(Id));
            this.MerchantItems = new MerchantItemCollection(this, MerchantItemRecord.GetMerchantItems(Id));
            this.BankItems = new BankItemCollection(this, BankItemRecord.GetBankItems(Client.Account.Id));
            this.GuestedParties = new List<Party>();
            this.GeneralShortcutBar = new GeneralShortcutBar(this);
            this.SpellShortcutBar = new SpellShortcutBar(this);
            this.HumanOptions = new List<CharacterHumanOption>();
            this.SkillsAllowed = SkillsManager.Instance.GetAllowedSkills(this);
            this.IdolsInventory = new IdolsInventory(this);
            this.Collecting = false;
        }

        public void CheckMerchantState()
        {
            MerchantRecord merchant = MerchantRecord.GetMerchant(Id);

            if (merchant != null)
            {
                MerchantsManager.Instance.RemoveMerchant(merchant);
                merchant.RemoveElement();
            }

        }

        private void CheckSoldItems()
        {
            BidShopItemRecord[] bidHouseItems = BidshopsManager.Instance.GetSoldItem(this).ToArray();
            MerchantItemRecord[] merchantItems = MerchantItemRecord.GetMerchantItemsSolded(this.Id).ToArray();

            if (bidHouseItems.Count() > 0 || merchantItems.Count() > 0)
            {
                foreach (var item in bidHouseItems)
                {
                    Client.WorldAccount.BankKamas += item.Price;
                    Client.WorldAccount.UpdateElement();
                    BidshopsManager.Instance.RemoveItem(item.BidShopId, item);
                }

                Client.Send(new ExchangeOfflineSoldItemsMessage(bidHouseItems.Select(x => x.GetObjectItemQuantityPriceDateEffects()).ToArray(),
                  merchantItems.Select(x => x.GetObjectItemQuantityPriceDateEffects()).ToArray()));

                foreach (var item in merchantItems)
                {
                    this.AddKamas(item.Price * item.QuantitySold);

                    if (item.Sold)
                    {
                        MerchantItems.RemoveItem(item.UId);
                        item.RemoveElement();
                    }

                    item.QuantitySold = 0;

                    item.UpdateElement();
                }


            }

        }

        public void OnInitiateFight(Fight fight)
        {
            if (Party != null)
            {
                Party.OnInitiateFight(this, fight);
            }
        }

        public void DestroyContext()
        {
            Client.Send(new GameContextDestroyMessage());
            this.Context = null;
        }
        public void SendServerExperienceModificator()
        {
            Client.Send(new ServerExperienceModificatorMessage((short)(ConfigFile.Instance.XpRate * 100d)));
        }
        public void DebugHighlightCells(Color color, IEnumerable<CellRecord> cells)
        {
            Client.Send(new DebugHighlightCellsMessage(color.ToArgb(), cells.Select(x => x.Id).ToArray()));
        }
        public void DebugClearHighlightCells()
        {
            Client.Send(new DebugClearHighlightCellsMessage());
        }
        public void CreateContext(GameContextEnum context)
        {
            if (Context.HasValue)
            {
                DestroyContext();
            }

            this.Context = context;
            Client.Send(new GameContextCreateMessage((byte)Context));
        }

        public void CreateHumanOptions()
        {
            this.HumanOptions.Add(new CharacterHumanOptionFollowers());

            if (Record.ActiveOrnamentId > 0)
            {
                HumanOptions.Add(HumanOptionsManager.Instance.CreateHumanOptionOrnament(this));
            }
            if (Record.ActiveTitleId > 0)
            {
                HumanOptions.Add(HumanOptionsManager.Instance.CreateHumanOptionTitle(this));
            }

            if (Guild != null)
            {
                HumanOptions.Add(HumanOptionsManager.Instance.CreateHumanOptionGuild());
            }

            CharacterEventApi.HumanOptionsCreated(this);

        }

        public void UpdateSpells(short oldLevel, short newLevel)
        {
            foreach (var spell in Record.Spells)
            {
                if (spell.ActiveSpellRecord.MinimumLevel > oldLevel && spell.ActiveSpellRecord.MinimumLevel <= Level)
                {
                    if (SpellShortcutBar.CanAdd())
                    {
                        SpellShortcutBar.Add(spell.SpellId);
                        TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 3, spell.SpellId);
                    }
                }
            }
            RefreshShortcuts();
        }
        public void LearnSpell(short spellId, bool notify)
        {
            if (!HasSpell(spellId))
            {
                var spell = new CharacterSpell(spellId);
                Record.Spells.Add(spell);

                if (spell.Learned(this) && SpellShortcutBar.CanAdd())
                {
                    SpellShortcutBar.Add(spellId);

                    if (notify)
                    {
                        RefreshShortcuts();
                        TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 3, spellId);
                    }
                }

                if (notify)
                {
                    RefreshSpells();
                }
            }
        }
        public bool HasSpell(short spellId)
        {
            return Record.Spells.Any(x => x.ActiveSpellRecord.Id == spellId);
        }
        public void RefreshSpells()
        {
            Client.Send(new SpellListMessage(false, Record.Spells.Select(x => x.GetSpellItem(this)).ToArray()));
        }
        public void OnExchangeError(ExchangeErrorEnum error)
        {
            this.Client.Send(new ExchangeErrorMessage((byte)error));
        }
        public void OnChatError(ChatErrorEnum error)
        {
            Client.Send(new ChatErrorMessage((byte)error));
        }
        public bool AddKamas(long value)
        {
            if (value <= long.MaxValue)
            {
                if (Record.Kamas + value >= Inventory.MaximumKamas)
                {
                    Record.Kamas = Inventory.MaximumKamas;
                }
                else
                    Record.Kamas += value;

                Inventory.RefreshKamas();
                return true;
            }
            return false;
        }
        public bool RemoveKamas(long value)
        {
            if (Record.Kamas >= value)
            {
                Record.Kamas -= value;
                Inventory.RefreshKamas();
                return true;
            }
            else
            {
                return false;
            }
        }
        public void OnKamasGained(long amount)
        {
            this.TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 45, new object[] { amount });
        }
        public void OnKamasLost(long amount)
        {
            this.TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 46, new object[] { amount });
        }
        public void RefreshShortcuts()
        {
            SpellShortcutBar.Refresh();
            GeneralShortcutBar.Refresh();
        }
        public void RefreshJobs()
        {
            Client.Send(new JobCrafterDirectorySettingsMessage(Record.Jobs.Select(x => x.GetDirectorySettings()).ToArray()));
            Client.Send(new JobDescriptionMessage(Record.Jobs.Select(x => x.GetJobDescription()).ToArray()));
            Client.Send(new JobExperienceMultiUpdateMessage(Record.Jobs.Select(x => x.GetJobExperience()).ToArray()));
        }


        public void RefreshGuild()
        {
            if (HasGuild)
            {
                Guild = GuildsManager.Instance.GetGuild(Record.GuildId);
                GuildMember = Guild.Record.GetMember(Id);
                SendGuildMembership();
            }
        }
        public void SendGuildMembership()
        {
            Client.Send(new GuildMembershipMessage()
            {
                guildInfo = Guild.GetGuildInformations(),
                rankId = GuildMember.Rank,
            });
        }
        public CharacterJob GetJob(JobTypeEnum jobType)
        {
            return Record.Jobs.FirstOrDefault(x => x.JobId == (byte)jobType);
        }
        public CharacterJob GetJob(byte jobType)
        {
            return Record.Jobs.FirstOrDefault(x => x.JobId == jobType);
        }
        public void AddJobExp(byte jobType, long amount)
        {
            CharacterJob job = GetJob(jobType);
            short currentLevel = job.Level;
            long highest = ExperienceManager.Instance.GetCharacterXPForLevel(ExperienceManager.MaxLevel);

            if (job.Experience + amount > highest)
                job.Experience = highest;
            else
                job.Experience += amount;

            Client.Send(new JobExperienceUpdateMessage(job.GetJobExperience()));

            if (currentLevel != job.Level)
            {
                Client.Send(new JobLevelUpMessage((byte)job.Level, job.GetJobDescription()));
                this.SkillsAllowed = SkillsManager.Instance.GetAllowedSkills(this);
            }

        }
        public void OpenMerchantAsSellerExchange(CharacterMerchant merchant)
        {
            this.OpenDialog(new MerchantSellerExchange(this, merchant));
        }
        public void OpenMerchantAsVendorExchange()
        {
            this.OpenDialog(new MerchantVendorExchange(this));
        }
        public void OpenCraftExchange(SkillRecord skill)
        {
            this.OpenDialog(new CraftExchange(this, skill));
        }
        public void OpenRuneTradeExchange()
        {
            this.OpenDialog(new RuneTradeExchange(this));
        }
        public void OpenSmithmagicExchange(SkillRecord skill)
        {
            this.OpenDialog(new SmithmagicExchange(this, skill));
        }
        public void OpenBuySellExchange(Npc npc, ItemRecord[] itemToSell, short tokenId)
        {
            this.OpenDialog(new BuySellExchange(this, npc, itemToSell, tokenId));
        }
        public void OpenBuyExchange(BidShopRecord bidshop)
        {
            this.OpenDialog(new BuyExchange(this, bidshop));
        }
        public void OpenSellExchange(BidShopRecord bidshop)
        {
            this.OpenDialog(new SellExchange(this, bidshop));
        }
        public void OpenBank()
        {
            this.OpenDialog(new BankExchange(this, BankItems));
        }
        public void OpenZaap(MapElement element)
        {
            this.OpenDialog(new ZaapDialog(this, element));
        }
        public void OpenBookDialog(int documentId)
        {
            this.OpenDialog(new BookDialog(this, documentId));
        }
        public void OpenGuildCreationDialog()
        {
            this.OpenDialog(new GuildCreationDialog(this));
        }
        public void OpenZaapi(MapElement element)
        {
            this.OpenDialog(new ZaapiDialog(this, element));
        }
        public void TalkToNpc(Npc npc, NpcActionRecord action)
        {
            this.OpenDialog(new NpcTalkDialog(this, npc, action));
        }
        public void OpenDialog(Dialog dialog)
        {
            if (!Busy)
            {
                try
                {
                    this.Dialog = dialog;
                    this.Dialog.Open();
                }
                catch
                {
                    ReplyError("Impossible d'éxecuter l'action.");
                    LeaveDialog();
                }
            }
            else
            {
                ReplyError("Unable to open dialog while busy...");
            }
        }
        public void LeaveDialog()
        {
            if (this.Dialog == null && !this.IsInRequest())
            {
                this.ReplyWarning("Unknown dialog...");
                return;
            }
            else
            {
                if (this.IsInRequest())
                {
                    this.CancelRequest();
                }
                if (this.Dialog != null)
                    this.Dialog.Close();
            }
        }
        public void CancelRequest()
        {
            if (this.IsInRequest())
            {
                if (this.IsRequestSource())
                {
                    this.RequestBox.Cancel();
                }
                else
                {
                    if (this.IsRequestTarget())
                    {
                        this.DenyRequest();
                    }
                }
            }
        }
        public bool IsInDialog()
        {
            return Dialog != null;
        }
        public bool IsInDialog<T>() where T : Dialog
        {
            return Dialog != null && Dialog is T;
        }
        public bool HasRequestBoxOpen<T>() where T : RequestBox
        {
            return RequestBox != null && RequestBox is T;
        }
        public bool IsInDialog(DialogTypeEnum type)
        {
            if (Dialog == null)
                return false;
            return Dialog.DialogType == type;
        }
        public bool IsInExchange(ExchangeTypeEnum type)
        {
            var exchange = GetDialog<Exchange>();
            if (exchange != null)
                return exchange.ExchangeType == type;
            else
                return false;
        }
        public void DenyRequest()
        {
            if (this.IsInRequest() && this.RequestBox.Target == this)
            {
                this.RequestBox.Deny();
            }
        }
        public bool IsRequestSource()
        {
            return this.IsInRequest() && this.RequestBox.Source == this;
        }
        public bool IsRequestTarget()
        {
            return this.IsInRequest() && this.RequestBox.Target == this;
        }
        public bool IsInRequest()
        {
            return this.RequestBox != null;
        }
        public ShortcutBar GetShortcutBar(ShortcutBarEnum barEnum)
        {
            switch (barEnum)
            {
                case ShortcutBarEnum.GENERAL_SHORTCUT_BAR:
                    return GeneralShortcutBar;
                case ShortcutBarEnum.SPELL_SHORTCUT_BAR:
                    return SpellShortcutBar;
            }

            throw new Exception("Unknown shortcut bar, " + barEnum);
        }
        public T GetDialog<T>() where T : Dialog
        {
            return (T)Dialog;
        }
        public void OpenUIByObject(ObjectUITypeEnum type, int itemUId)
        {
            Client.Send(new ClientUIOpenedByObjectMessage()
            {
                type = (byte)type,
                uid = itemUId,
            });
        }
        public void OpenRequestBox(RequestBox box)
        {
            box.Source.RequestBox = box;
            this.RequestBox = box;
            box.Open();
        }
        public void RefreshStats()
        {
            Client.Send(new CharacterStatsListMessage(Record.Stats.GetCharacterCharacteristicsInformations(this)));
        }

        public void OnStatUpgradeResult(StatsUpgradeResultEnum result, short nbCharacBoost)
        {
            Client.Send(new StatsUpgradeResultMessage((byte)result, nbCharacBoost));
        }
        public void SpawnPoint()
        {
            MapRecord targetMap = MapRecord.GetMap(Record.SpawnPointMapId);

            if (targetMap.HasZaap())
            {
                TeleportToZaap(targetMap);
            }
            else
            {
                Teleport(targetMap);
            }
        }
        public void TeleportToZaap(MapRecord map)
        {
            Teleport(map, map.GetNearCell(InteractiveTypeEnum.ZAAP16));
        }
        public void SetDirection(DirectionsEnum direction)
        {
            Record.Direction = direction;
            SendMap(new GameMapChangeOrientationMessage(new ActorOrientation(Id, (byte)direction)));
        }
        public void Teleport(long mapId, short? cellId = null)
        {
            Teleport(MapRecord.GetMap(mapId), cellId);
        }
        public void Teleport(MapRecord teleportMap, short? cellId = null)
        {
            if (Fighting)
                return;
            if (Busy)
                return;

            if (teleportMap != null)
            {

                if (Record.MapId != teleportMap.Id)
                    ChangeMap = true;

                if (cellId < 0 || cellId > 560)
                    cellId = teleportMap.RandomWalkableCell().Id;

                if (!cellId.HasValue || (cellId.HasValue && !teleportMap.IsCellWalkable(cellId.Value)))
                {
                    cellId = teleportMap.RandomWalkableCell().Id;
                }

                MovementKeys = null;

                this.IsMoving = false;


                if (cellId != null)
                    this.Record.CellId = cellId.Value;


                this.Record.MapId = teleportMap.Id;
                if (Map != null)
                    Map.Instance.RemoveEntity(this.Id);

                CurrentMapMessage(teleportMap.Id);
            }
            else
            {
                Client.Character.ReplyError("Unknown map.");
            }
        }
        public void EndMove()
        {
            this.Record.CellId = this.MovedCell;
            this.MovedCell = 0;
            this.IsMoving = false;
            this.MovementKeys = null;

            var element = Map.Instance.GetElements<MapElement>().Where(x => x.Record.CellId == this.Record.CellId).FirstOrDefault();

            if (element != null && element.Record.Skill != null && element.Record.Skill.ActionIdentifier == GenericActionEnum.Teleport)
            {
                Map.Instance.UseInteractive(this, element.Record.Identifier, 0);
            }
        }
        public void CancelMove(short cellId)
        {
            IsMoving = false;
            Record.CellId = cellId;
            Client.Send(new BasicNoOperationMessage());
        }
        public void MoveOnMap(short[] keyMovements)
        {
            if (!Busy)
            {
                short clientCellId = PathReader.ReadCell(keyMovements.First());

                if (clientCellId == CellId)
                {
                    if (Look.RemoveAura())
                        RefreshActorOnMap();
                    this.Direction = (DirectionsEnum)PathReader.GetDirection(keyMovements.Last());
                    this.MovedCell = PathReader.ReadCell(keyMovements.Last());
                    this.IsMoving = true;
                    this.MovementKeys = keyMovements;
                    this.SendMap(new GameMapMovementMessage(keyMovements, 0, this.Id));
                }
                else
                {
                    this.NoMove();
                }
            }
            else
            {
                this.NoMove();
            }
        }
        public void RefreshEmotes()
        {
            Client.Send(new EmoteListMessage(Record.KnownEmotes.ToArray()));
        }
        public bool LearnEmote(byte id)
        {
            if (!Record.KnownEmotes.Contains(id))
            {
                Record.KnownEmotes.Add(id);
                Client.Send(new EmoteAddMessage(id));
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool ForgetEmote(byte id)
        {
            if (Record.KnownEmotes.Contains(id))
            {
                Record.KnownEmotes.Remove(id);
                Client.Send(new EmoteRemoveMessage(id));
                return true;
            }
            else
            {
                return false;
            }
        }
        public void PlayEmote(short emoteId)
        {
            EmoteRecord template = EmoteRecord.GetEmote(emoteId);

            if (!ChangeMap)
            {
                if (Look.RemoveAura())
                    RefreshActorOnMap();

                if (template.IsAura)
                {
                    short bonesId = EntityLookManager.Instance.GetAuraBones(this, emoteId);
                    this.Look.AddAura(bonesId);
                    this.RefreshActorOnMap();
                }
                else
                {
                    this.SendMap(new EmotePlayMessage(Id, Client.Account.Id, emoteId, 0));
                }
            }
        }

        public void OnItemAdded(CharacterItemRecord item)
        {
            if (item.Record.TypeEnum == ItemTypeEnum.IDOL)
            {
                IdolsInventory.Update(this);
            }

            if (HasParty && Party.Leader == this)
            {
                Party.IdolsInventory.Update(this);
            }
        }
        public void OnItemRemoved(CharacterItemRecord item)
        {
            if (item.Record.TypeEnum == ItemTypeEnum.IDOL)
            {
                IdolsInventory.Update(this);
            }

            if (HasParty && Party.Leader == this)
            {
                Party.IdolsInventory.Update(this);
            }
        }
        public void NotifyItemGained(short gid, int quantity)
        {
            this.TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 21, new object[] { quantity, gid });
        }
        public void NotifyItemSelled(short gid, int quantity, long price)
        {
            this.TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 65, new object[] { price, string.Empty, gid, quantity });
        }
        public void NotifyItemLost(short gid, int quantity)
        {
            this.TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_MESSAGE, 22, new object[] { quantity, gid });
        }
        [WIP]
        public void OnLevelChanged(short oldLevel, short amount)
        {
            if (CharacterLoadingComplete)
            {
                this.SendMap(new CharacterLevelUpInformationMessage(Name, Id, Level));
                Client.Send(new CharacterLevelUpMessage(Level));

            }

            UpdateSpells(oldLevel, Level);

            if (Level > oldLevel)
            {
                if (oldLevel <= ExperienceManager.MaxLevel)
                {
                    if (Level > ExperienceManager.MaxLevel)
                    {
                        amount = (short)(ExperienceManager.MaxLevel - oldLevel);
                    }

                    Record.Stats.LifePoints += (5 * amount);
                    Record.Stats.MaxLifePoints += (5 * amount);
                    Record.StatsPoints += (short)(5 * amount);
                }

            }

            CharacterLevelRewardManager.Instance.OnCharacterLevelUp(this, oldLevel, Level);

            if (HasParty)
            {
                Party.UpdateMember(this);
            }

            if (CharacterLoadingComplete)
            {
                RefreshActorOnMap();
                RefreshStats();
            }
        }

        public void OnCharacterLoadingComplete()
        {
            OnConnected();
            this.CharacterLoadingComplete = true;
            Client.Send(new CharacterLoadingCompleteMessage());
        }
        private void OnConnected()
        {
            this.TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 89, new string[0]); // not only when just created in th

            this.Client.Send(new AlmanachCalendarDateMessage(1)); // for monsters!

            this.Reply(ConfigFile.Instance.WelcomeMessage, Color.CornflowerBlue);
            CheckSoldItems();
            Guild?.OnConnected(this);
        }
        public void NoMove()
        {
            this.Client.Send(new GameMapNoMovementMessage((short)Point.X, (short)Point.Y));
        }

        /*    public void RegisterArena()
            {
                this.ArenaMember = ArenaProvider.Instance.Register(this);
                this.ArenaMember.UpdateStep(true, PvpArenaStepEnum.ARENA_STEP_REGISTRED);
            }
            public void UnregisterArena()
            {
                if (InArena)
                {
                    ArenaProvider.Instance.Unregister(this);
                    this.ArenaMember.UpdateStep(false, PvpArenaStepEnum.ARENA_STEP_UNREGISTER);
                    this.ArenaMember = null;
                }
            }
            public void AnwserArena(bool accept)
            {
                if (InArena)
                {
                    ArenaMember.Anwser(accept);
                }
            }
        */

        [WIP]
        public void RefreshArenaInfos()
        {
            var infos = new ArenaRankInfos(new ArenaRanking(1, 2), new ArenaLeagueRanking(1, 1, 200, 100, 1), 1, 2, 3);
            Client.Send(new GameRolePlayArenaUpdatePlayerInfosAllQueuesMessage(infos, infos, infos));

        }
        [WIP]
        public void OnEnterMap()
        {

            this.ChangeMap = false;

            if (this.Busy)
                this.LeaveDialog();

            if (!Fighting)
            {
                this.Map.Instance.AddEntity(this);

                this.Map.Instance.SendMapComplementary(Client);
                this.Map.Instance.SendMapFightCount(Client);

                foreach (Character current in this.Map.Instance.GetEntities<Character>())
                {
                    if (current.IsMoving)
                    {
                        Client.Send(new GameMapMovementMessage(current.MovementKeys, 0, current.Id));
                        Client.Send(new BasicNoOperationMessage());
                    }
                }

                Client.Send(new BasicNoOperationMessage());
                Client.Send(new BasicTimeMessage(DateTime.Now.GetUnixTimeStampDouble(), 1));
            }
            if (HasParty)
            {
                Party.UpdateMember(this);
            }
        }
        public void OnDisconnected()
        {
            Record.UpdateElement();

            Record.InGameContext = false;
            Guild?.OnDisconnected(this);

            if (Dialog != null)
                Dialog.Close();

            if (IsInRequest())
                CancelRequest();

            if (HasParty)
                Party.Leave(this);

            if (Fighting)
            {
                Fighter.OnDisconnected();
            }
            else
            {
                Map?.Instance?.RemoveEntity(this.Id);
            }
        }
        object ApplyPolice(object value, bool bold, bool underline)
        {
            if (bold)
                value = "<b>" + value + "</b>";
            if (underline)
                value = "<u>" + value + "</u>";
            return value;
        }
        public void Reply(object value, bool bold = false, bool underline = false)
        {
            value = ApplyPolice(value, bold, underline);
            Client.Send(new TextInformationMessage(0, 0, new string[] { value.ToString() }));
        }
        public void ReplyWarning(object value)
        {
            Reply(value, Color.DarkOrange, false, false);
        }
        public void ReplyError(object value)
        {
            Reply(value, Color.DarkRed, false, false);
        }
        public void Reply(object value, Color color, bool bold = false, bool underline = false)
        {
            value = ApplyPolice(value, bold, underline);
            Client.Send(new TextInformationMessage(0, 0, new string[] { string.Format("<font color=\"#{0}\">{1}</font>", color.ToArgb().ToString("X"), value) }));
        }
        public void TextInformation(TextInformationTypeEnum msgType, short msgId, params object[] parameters)
        {
            Client.Send(new TextInformationMessage((byte)msgType, msgId,
                (from entry in parameters
                 select entry.ToString()).ToArray()));
        }
        public void CurrentMapMessage(long mapId)
        {
            Client.Send(new CurrentMapMessage(mapId));
        }
        public ActorAlignmentInformations GetActorAlignmentInformations()
        {
            return new ActorAlignmentInformations(0, 0, 0, 0);
        }
        public override GameRolePlayActorInformations GetActorInformations()
        {
            return new GameRolePlayCharacterInformations(GetActorAlignmentInformations(),
                 Id, new EntityDispositionInformations(CellId, (byte)Direction),
                 Look.ToEntityLook(), Name, new HumanInformations(GetActorRestrictions(), Record.Sex, HumanOptions.Select(x => x.GetHumanOption(this)).ToArray()),
                 Record.AccountId);
        }
        public bool LearnOrnament(short id, bool notify)
        {
            if (!Record.KnownOrnaments.Contains(id))
            {
                Record.KnownOrnaments.Add(id);
                if (notify)
                    Client.Send(new OrnamentGainedMessage(id));
                return true;
            }
            else
            {
                return false;
            }

        }
        public bool ForgetOrnament(short id, bool notify)
        {
            if (Record.KnownOrnaments.Contains(id))
            {
                Record.KnownOrnaments.Remove(id);

                if (Record.ActiveOrnamentId == id)
                {
                    RemoveAllHumanOption<CharacterHumanOptionOrnament>(true);
                }

                if (notify)
                {
                    Client.Send(new OrnamentLostMessage(id));
                }
                return true;
            }
            return false;

        }
        public bool HasOrnament(short id)
        {
            return Record.KnownOrnaments.Contains(id);
        }
        public bool ActiveOrnament(short id)
        {
            if (id == 0)
            {
                RemoveAllHumanOption<CharacterHumanOptionOrnament>(false);
                Record.ActiveOrnamentId = 0;
                RefreshActorOnMap();
                return true;
            }
            if (Record.KnownOrnaments.Contains(id))
            {
                RemoveAllHumanOption<CharacterHumanOptionOrnament>(false);
                Record.ActiveOrnamentId = id;
                HumanOptions.Add(HumanOptionsManager.Instance.CreateHumanOptionOrnament(this));
                RefreshActorOnMap();
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool LearnTitle(short id)
        {
            if (!Record.KnownTitles.Contains(id))
            {
                Record.KnownTitles.Add(id);
                Client.Send(new TitleGainedMessage(id));
                return true;

            }
            return false;
        }

        public bool ForgetTitle(short id)
        {
            if (Record.KnownTitles.Contains(id))
            {
                if (Record.ActiveTitleId == id)
                {
                    ActiveTitle(0);
                }
                Record.KnownTitles.Remove(id);
                Client.Send(new TitleLostMessage(id));
                return true;
            }
            return false;

        }
        public bool HasTitle(short id)
        {
            return Record.KnownTitles.Contains(id) ? true : false;
        }

        public bool ActiveTitle(short id)
        {
            if (id == 0)
            {
                Record.ActiveTitleId = id;
                RemoveAllHumanOption<CharacterHumanOptionTitle>(true);
                return true;
            }
            if (HasTitle(id))
            {
                if (Record.ActiveTitleId == id)
                    return false;

                Record.ActiveTitleId = id;
                RemoveAllHumanOption<CharacterHumanOptionTitle>(true);
                AddHumanOption(HumanOptionsManager.Instance.CreateHumanOptionTitle(this), true);
                return true;

            }
            return false;

        }
        public ActorRestrictionsInformations GetActorRestrictions()
        {
            return new ActorRestrictionsInformations(false, false, false, false, false, false, false, false, false, false, false,
                false, false, false, false, false, false, false, false, false);
        }

        public void SetExperience(long value)
        {
            if (this.Level >= ExperienceManager.MaxLevelOmega)
            {
                return;
            }
            Experience = value;

            if (Experience >= this.UpperBoundExperience || Experience < this.LowerBoundExperience)
            {
                this.Level = ExperienceManager.Instance.GetCharacterLevel(Experience);
            }

            RefreshStats();

        }
        public void AddExperience(long value, bool notify = true)
        {
            SetExperience(Experience + value);

            if (notify)
            {
                this.Client.Send(new CharacterExperienceGainMessage(value, 0, 0, 0));
            }
        }
        public void UseItem(int uid, bool send)
        {
            var item = this.Inventory.GetItem(uid);

            if (item != null)
            {
                if (ItemsManager.Instance.UseItem(Client.Character, item))
                    this.Inventory.RemoveItem(item.UId, 1);

                if (send)
                {
                    this.RefreshStats();
                }
            }
        }
        public void AddHumanOption(CharacterHumanOption characterHumanOption, bool notify = false)
        {
            HumanOptions.Add(characterHumanOption);

            if (notify)
            {
                RefreshActorOnMap();
            }
        }
        public T GetHumanOption<T>() where T : CharacterHumanOption
        {
            return HumanOptions.OfType<T>().FirstOrDefault();
        }
        public void RemoveHumanOption(CharacterHumanOption characterHumanOption, bool notify = false)
        {
            HumanOptions.Remove(characterHumanOption);

            if (notify)
            {
                RefreshActorOnMap();
            }
        }
        public void RemoveAllHumanOption<T>(bool notify) where T : CharacterHumanOption
        {
            if (HumanOptions.RemoveAll(x => x is T) > 0 && notify)
            {
                RefreshActorOnMap();
            }
        }
        public CharacterSpell GetSpellByVariant(short spellId)
        {
            return Record.Spells.FirstOrDefault(x => x.VariantSpellRecord != null && x.VariantSpellRecord.Id == spellId);
        }
        public CharacterSpell GetSpell(short spellId)
        {
            return Record.Spells.FirstOrDefault(x => x.ActiveSpellRecord.Id == spellId);
        }

        public void SendTitlesAndOrnamentsList()
        {
            Client.Send(new TitlesAndOrnamentsListMessage(Record.KnownTitles.ToArray(), Record.KnownOrnaments.ToArray(), Record.ActiveTitleId, Record.ActiveOrnamentId)); ;
        }
        [WIP("still working?")]
        public void Restat()
        {
            int vitality = this.Stats[CharacteristicEnum.VITALITY].Base;
            this.Stats.LifePoints -= vitality;
            this.Stats.MaxLifePoints -= vitality;
            this.Stats[CharacteristicEnum.VITALITY].Base = 0;
            this.Stats.Agility.Base = 0;
            this.Stats.Intelligence.Base = 0;
            this.Stats.Chance.Base = 0;
            this.Stats.Strength.Base = 0;
            this.Stats.Wisdom.Base = 0;
            this.Record.StatsPoints = (short)(5 * SafeLevel - 5);
            RefreshStats();
        }
        public Fighter CreateFighter(FightTeam team)
        {
            if (Look.RemoveAura())
                RefreshActorOnMap();

            this.MovementKeys = null;
            this.IsMoving = false;
            this.Map.Instance.RemoveEntity(this.Id);
            this.DestroyContext();
            this.CreateContext(GameContextEnum.FIGHT);
            this.RefreshStats();
            SendGameFightStartingMessage(team.Fight);
            this.Fighter = new CharacterFighter(this, team, GetCell());
            return Fighter;
        }

        public void RejoinMap(long mapId, FightTypeEnum fightType, bool winner, bool spawnJoin)
        {
            DestroyContext();
            CreateContext(GameContextEnum.ROLE_PLAY);
            this.RefreshStats();
            this.Fighter = null;

            if (spawnJoin && !winner)
            {
                if (Client.Account.Role >= ServerRoleEnum.Administrator)
                {
                    CurrentMapMessage(Record.MapId);
                }
                else
                {
                    SpawnPoint();
                }
            }
            else
            {
                if (mapId == Record.MapId)
                {
                    CurrentMapMessage(mapId);
                }
                else
                {
                    Teleport(mapId);
                }
            }

        }
        private void SendGameFightStartingMessage(Fight fight)
        {
            this.Client.Send(new GameFightStartingMessage((byte)fight.FightType,
            (short)fight.Id, (double)fight.BlueTeam.TeamId, (double)fight.RedTeam.TeamId, fight.ContainsBoss()));
        }
        public void DisplayNotification(string message)
        {
            Client.Send(new NotificationByServerMessage(24, new string[] { message }, true));
        }
        public void DisplayNotificationError(string message)
        {
            Client.Send(new NotificationByServerMessage(30, new string[] { message }, true));
        }
        public void DisplaySystemMessage(bool hangUp, short msgId, params string[] parameters)
        {
            Client.Send(new SystemMessageDisplayMessage(hangUp, msgId, parameters));
        }
        public void DisplayPopup(byte lockDuration, string author, string content)
        {
            Client.Send(new PopupWarningMessage(lockDuration, author, content));
        }
        public PlayerStatus GetPlayerStatus()
        {
            return new PlayerStatus((byte)PlayerStatusEnum.PLAYER_STATUS_AVAILABLE);
        }
        public override string ToString()
        {
            return "Character: " + Name;
        }
        public bool CanEnableMerchantMode()
        {
            if (MerchantItems.Count == 0)
            {
                TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 23);
                return false;
            }

            /*  if (!Map.Position.AllowHumanVendor)
              {
                  TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 237);
                  return false;
              } */

            if (Map.Instance.IsMerchantLimitReached())
            {
                TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 25, ConfigFile.Instance.MaxMerchantPerMap);
                return false;
            }

            if (!Map.Instance.IsCellFree(CellId, CellId))
            {
                TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 24);
                return false;
            }

            if (Record.Kamas < MerchantItems.GetMerchantTax())
            {
                TextInformation(TextInformationTypeEnum.TEXT_INFORMATION_ERROR, 76);
                return false;
            }

            return true;
        }
        public void EnterMerchantMode()
        {
            if (!CanEnableMerchantMode())
            {
                return;
            }

            if (!RemoveKamas(MerchantItems.GetMerchantTax()))
            {
                return;
            }
            Client.Disconnect();
            MerchantsManager.Instance.AddMerchant(this);
        }

        public void AddFollower(ServerEntityLook look)
        {
            GetHumanOption<CharacterHumanOptionFollowers>().Add(look);
        }

        public void RemoveFollower(ServerEntityLook look)
        {
            GetHumanOption<CharacterHumanOptionFollowers>().Remove(look);
        }

        public PartyMemberInformations GetPartyMemberInformations()
        {
            return new PartyMemberInformations(Stats.LifePoints, Stats.MaxLifePoints, Stats[CharacteristicEnum.PROSPECTING].TotalInContext(),
                0, (short)Stats.TotalInitiative, 0, (short)Map.Position.X, (short)Map.Position.Y,
                Record.MapId, Map.SubareaId, GetPlayerStatus(),
                new PartyEntityBaseInformation[0], Id, Name, Level, Look.ToEntityLook(), Record.BreedId, Record.Sex);
        }

        public void RefreshIdols()
        {
            short[] chosenIdols = IdolsInventory.GetActiveIdols().Select(x => (short)x.Id).ToArray();

            short[] partyChosenIdols = new short[0];

            PartyIdol[] partyIdols = new PartyIdol[0];

            if (this.HasParty)
            {
                partyChosenIdols = Party.IdolsInventory.GetActiveIdols().Select(x => (short)x.Id).ToArray();
                partyIdols = Party.IdolsInventory.GetAllIdols().Select(x => x.GetPartyIdol(this.Id)).ToArray();
            }

            this.Client.Send(new IdolListMessage(chosenIdols, partyChosenIdols, partyIdols));
        }
        public void SelectIdol(short idolId, bool activate, bool party)
        {
            if (Fighting && Fighter.Fight.Started)
            {
                return;
            }

            if (party)
            {
                if (HasParty && Party.Leader == this)
                {
                    if (Party.IdolsInventory.Select(idolId, activate))
                    {
                        this.Client.Send(new IdolSelectedMessage((short)idolId, activate, party));

                        foreach (var member in Party.Members.Values)
                        {
                            member.RefreshIdols();
                        }
                    }
                }
                else
                {
                    return;
                }
            }
            else
            {
                if (IdolsInventory.Select(idolId, activate))
                {
                    this.Client.Send(new IdolSelectedMessage((short)idolId, activate, party));
                }
            }

            if (Fighting)
            {
                Fighter.Fight.Send(new IdolFightPreparationUpdateMessage(0, IdolsInventory.GetActiveIdols().Select(x => x.GetIdol()).ToArray()));
            }




        }

        public PartyInvitationMemberInformations GetPartyInvitationMemberInformations()
        {
            return new PartyInvitationMemberInformations((short)Map.Position.X, (short)Map.Position.Y,
                Map.Id, Map.SubareaId, new PartyEntityBaseInformation[0], Id, Name, Level, Look.ToEntityLook(), Record.BreedId, Record.Sex);
        }
        public PartyGuestInformations GetPartyGuestInformations(Party party)
        {
            return new PartyGuestInformations(Id, party.Leader.Id, Name, Look.ToEntityLook(), Record.BreedId, Record.Sex, GetPlayerStatus(),
                new PartyEntityBaseInformation[0]);

        }
        public void OnPartyJoinError(int partyId, PartyJoinErrorEnum reason)
        {
            Client.Send(new PartyCannotJoinErrorMessage((byte)reason, partyId));
        }

        public void InviteParty(Character character)
        {
            if (!this.HasParty)
            {
                Party party = PartyManager.Instance.CreateParty(this);
                party.Create(this, character);
            }
            else
            {
                if (!Party.IsFull)
                {
                    Party.OnInvited(character, this);
                }
            }
        }

        public void ReconnectToFight()
        {
            this.Map = MapRecord.GetMap(Record.MapId);

            this.CurrentMapMessage(Map.Id);

            this.Map.Instance.SendMapComplementary(Client);

            Fighter = FightManager.Instance.GetConnectedFighter(this);

            if (this.Fighter == null)
            {
                return;
            }

            SendGameFightStartingMessage(Fighter.Fight);

            Fighter.OnReconnect(this);


        }

        public void OnGuildCreate(GuildCreationResultEnum result)
        {
            Client.Send(new GuildCreationResultMessage((byte)result));

            if (result == GuildCreationResultEnum.GUILD_CREATE_OK)
            {
                CharacterItemRecord item = this.Inventory.GetFirstItem(1575, 1);

                if (item != null)
                {
                    Client.Character.Inventory.RemoveItem(item, 1);
                }

                Dialog.Close();
            }
        }
        public void OnGuildJoined(Guild guild, GuildMemberRecord memberRecord)
        {
            this.Guild = guild;
            this.GuildMember = memberRecord;
            this.Record.GuildId = Guild.Id;

            this.AddHumanOption(HumanOptionsManager.Instance.CreateHumanOptionGuild(), true);

            Client.Send(new GuildJoinedMessage(Guild.GetGuildInformations(), memberRecord.Rank));
        }
        public void OnGuildKick(Guild guild)
        {
            Guild = null;
            GuildMember = null;
            Record.GuildId = 0;
            RemoveAllHumanOption<CharacterHumanOptionGuild>(true);
            Client.Send(new GuildLeftMessage());
        }

        public CharacterMinimalInformations GetCharacterMinimalInformations()
        {
            return new CharacterMinimalInformations(Level, Id, Name);
        }
    }

}
