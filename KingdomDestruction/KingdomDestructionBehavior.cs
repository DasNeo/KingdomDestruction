using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace KingdomDestruction
{
    public class KingdomDestructionBehavior : CampaignBehaviorBase
    {
        public static Dictionary<Kingdom, CampaignTime> KingdomsOnTimer = new Dictionary<Kingdom, CampaignTime>();
        public static List<Clan> DisplacedRulingClans = new List<Clan>();

        public KingdomDestructionBehavior(CampaignGameStarter starter)
        {
            this.AddDialogs(starter);
        }

        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, this.checkKingdoms);
            CampaignEvents.ClanChangedKingdom.AddNonSerializedListener(this, this.displacedClanFoundOtherRuler);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("KingdomsOnTimer", ref KingdomsOnTimer);
            dataStore.SyncData("DisplacedRulingClans", ref DisplacedRulingClans);
        }

        private void setDestroyTimerTo(Kingdom kingdom)
        {
            KingdomsOnTimer[kingdom] = CampaignTime.DaysFromNow((float)Config.GetKeyValue("destroyKingdomTimerInDays"));
        }

        private void destroyKingdom(Kingdom kingdom)
        {
            foreach (Clan clan in kingdom.Clans.ToList())
            {
                if (clan.Leader.Occupation == Occupation.Lord)
                    DisplacedRulingClans.Add(clan);
                clan.Kingdom = null;
            }
            DestroyKingdomAction.Apply(kingdom);
        }

        private void checkKingdoms()
        {
            bool checkVassals = (float)Config.GetKeyValue("vassalsNeedToBeGone") == 1.0;

            foreach (Kingdom kingdom in Campaign.Current.Kingdoms.ToList())
            {
                if (kingdom.IsEliminated)
                    continue;

                if (!KingdomsOnTimer.ContainsKey(kingdom))
                {
                    if (!kingdom.Leader.Equals(Hero.MainHero) && kingdom.Fiefs.Count < 1)
                    {
                        if (checkVassals)
                        {
                            if (Utils.ExcludeMercenaryClansFrom(kingdom.Clans.ToList()).Count == 1)
                            {
                                setDestroyTimerTo(kingdom);
                            }
                        }
                        else
                        {
                            setDestroyTimerTo(kingdom);
                        }
                    }
                }
                else
                {
                    if (kingdom.Leader.Equals(Hero.MainHero) || ( checkVassals&&Utils.ExcludeMercenaryClansFrom(kingdom.Clans.ToList()).Count > 1 ) || kingdom.Fiefs.Count > 0)
                    {
                        KingdomsOnTimer.Remove(kingdom);
                        return;
                    }

                    CampaignTime timeToDie = KingdomsOnTimer[kingdom];
                    if (CampaignTime.Now >= timeToDie)
                    {
                        this.destroyKingdom(kingdom);
                        KingdomsOnTimer.Remove(kingdom);
                    }
                }
            }
        }

        private void displacedClanFoundOtherRuler(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail actionDetail, bool showNotif)
        {
            if (DisplacedRulingClans.Contains(clan))
                DisplacedRulingClans.Remove(clan);
        }

        private int goldNeededForRulingClanToJoin = 0;
        private void AddDialogs(CampaignGameStarter starter)
        {
            starter.AddPlayerLine("TalkToDisplacedRulerStart", "hero_main_options", "TalkToDisplacedRulerStartOutput",
                "I heard you're looking for a new sovereign.",
                () =>
                {
                    if (DisplacedRulingClans.Count > 0 && Hero.MainHero.Clan.Kingdom != null && Hero.MainHero.Clan.Kingdom.Leader.Equals(Hero.MainHero))
                        foreach (Clan clan in DisplacedRulingClans.ToList())
                            if (Hero.OneToOneConversationHero.Equals(clan.Leader))
                            {
                                float relationWithPlayer = Hero.OneToOneConversationHero.GetRelationWithPlayer();
                                int amtAwayFromJoinFree = (int)Math.Abs(relationWithPlayer - (float)Config.GetKeyValue("relationNeededForDisplacedClanToJoinFree"));
                                goldNeededForRulingClanToJoin = amtAwayFromJoinFree * (int)Config.GetKeyValue("goldNeededMultiplierForDisplacedClanToJoin");
                                return true;
                            }
                    return false;
                }, null, 200, null, null);
                starter.AddDialogLine("TalkToDisplacedRulerStartAccepted", "TalkToDisplacedRulerStartOutput", "lord_pretalk",
                    "Alright, I suppose I could do worse - ugh! The shame of it all. There is no need for further bloodshed. I will serve you well, from this day until the end of my days.",
                    () =>
                    {
                        return Hero.OneToOneConversationHero.GetRelationWithPlayer() >= (float)Config.GetKeyValue("relationNeededForDisplacedClanToJoinFree");
                    }, () => Conversation_DisplacedRulerJoinsPlayerKingdom(), 100);

                starter.AddDialogLine("TalkToDisplacedRulerStartDenied", "TalkToDisplacedRulerStartOutput", "lord_pretalk",
                    "Serve you? Not now, not never.",
                    () =>
                    {
                        return Hero.OneToOneConversationHero.GetRelationWithPlayer() <= (float)Config.GetKeyValue("relationNeededForDisplacedClanToNeverJoin");
                    }, null, 90);

                starter.AddDialogLine("TalkToDisplacedRulerStartMoneyNeeded", "TalkToDisplacedRulerStartOutput", "TalkToDisplacedRulerMoneyNeededConfirm",
                        "You really think I'd join you? Not for free at least, give me and my kin tribute and I'll think on it.",
                        null, null, 80, null);
                    starter.AddPlayerLine("TalkToDisplacedRulerMoneyNeededPay", "TalkToDisplacedRulerMoneyNeededConfirm", "TalkToDisplacedRulerMoneyNeededAccepted",
                        "That can be arranged.",
                        null, null, 100, (out TextObject explain) =>
                        {
                            explain = new TextObject(goldNeededForRulingClanToJoin + " gold needed");
                            return Hero.MainHero.Gold >= goldNeededForRulingClanToJoin;
                        });
                    starter.AddPlayerLine("TalkToDisplacedRulerMoneyNeededRefuse", "TalkToDisplacedRulerMoneyNeededConfirm", "TalkToDisplacedRulerMoneyNeededNotMet",
                        "I don't think that will be happening.",
                        () =>
                        {
                            return Hero.MainHero.Gold >= goldNeededForRulingClanToJoin;
                        }, null, 90, null);
                    starter.AddPlayerLine("TalkToDisplacedRulerMoneyNeededNotEnough", "TalkToDisplacedRulerMoneyNeededConfirm", "TalkToDisplacedRulerMoneyNeededNotMet",
                        "I don't think I have enough gold to make amends. I'll have to check the treasury.",
                        () =>
                        {
                            return Hero.MainHero.Gold < goldNeededForRulingClanToJoin;
                        }, null, 80, null);
                    starter.AddDialogLine("TalkToDisplacedRulerEndMoneyNeededAccepted", "TalkToDisplacedRulerMoneyNeededAccepted", "lord_pretalk",
                        "Alright, I suppose that's enough to make bygones be bygones. I will serve you well... my liege.",
                        null, () => Conversation_DisplacedRulerJoinsPlayerKingdom(goldNeededForRulingClanToJoin), 100, null);
                    starter.AddDialogLine("TalkToDisplacedRulerEndMoneyNeededNotMet", "TalkToDisplacedRulerMoneyNeededNotMet", "lord_pretalk",
                        "Then, *I* don't think we have much more to be discussing...",
                        null, null, 100, null);
        }
        private void Conversation_DisplacedRulerJoinsPlayerKingdom(int cost = 0)
        {
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, Hero.OneToOneConversationHero, cost);
            ChangeKingdomAction.ApplyByJoinToKingdom(Hero.OneToOneConversationHero.Clan, Hero.MainHero.Clan.Kingdom);
        }
    }

    public class CustomSaveDefiner : SaveableTypeDefiner
    {
        // use a big number and ensure that no other mod is using a close range
        public CustomSaveDefiner() : base(938506839) { }

        protected override void DefineClassTypes()
        {
            // The Id's here are local and will be related to the Id passed to the constructor
            //AddClassDefinition(typeof(CustomMapNotification), 1);
            //AddStructDefinition(typeof(ExampleStruct), 2);
            //AddClassDefinition(typeof(ExampleNested), 3);
            //AddStructDefinition(typeof(NestedStruct), 4);
        }

        protected override void DefineContainerDefinitions()
        {
            //ConstructContainerDefinition(typeof(List<CustomMapNotification>));
            // Both of these are necessary: order isn't important
            //ConstructContainerDefinition(typeof(List<NestedStruct>));
            //ConstructContainerDefinition(typeof(Dictionary<string, List<NestedStruct>>));
            //ConstructContainerDefinition(typeof(List<Hero>));
            //ConstructContainerDefinition(typeof(Dictionary<Kingdom, CampaignTime>));
            //ConstructContainerDefinition(typeof(List<Clan>));
        }
    }
}
