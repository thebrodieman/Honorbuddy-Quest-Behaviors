﻿using System;
using System.Collections.Generic;
using System.Linq;

using CommonBehaviors.Actions;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.CommonBot.Routines;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Action = Styx.TreeSharp.Action;


namespace Honorbuddy.Quest_Behaviors.SpecificQuests.BattleForTheSkies
{
    [CustomBehaviorFileName(@"SpecificQuests\29786-PandaStarter-BattleForTheSkies")]
    public class BattlefortheSkies : CustomForcedBehavior
    {
        public BattlefortheSkies(Dictionary<string, string> args)
            : base(args)
        {

                QuestId = 29786;//GetAttributeAsQuestId("QuestId", true, null) ?? 0;
 
        }
        public int QuestId { get; set; }
        private bool _isBehaviorDone;

        //<Vendor Name="Zhao-Ren" Entry="55786" Type="Repair" X="713.9167" Y="4168.126" Z="213.846" />
        

        private Composite _root;
        
        public QuestCompleteRequirement questCompleteRequirement = QuestCompleteRequirement.NotComplete;
        public QuestInLogRequirement questInLogRequirement = QuestInLogRequirement.InLog;
        
        public override bool IsDone
        {
            get
            {
                return _isBehaviorDone;
            }
        }
        private LocalPlayer Me
        {
            get { return (StyxWoW.Me); }
        }

        public override void OnStart()
        {
            OnStart_HandleAttributeProblem();
            if (!IsDone)
            {
                TreeHooks.Instance.InsertHook("Questbot_Main", 0, CreateBehavior_QuestbotMain());

                PlayerQuest Quest = StyxWoW.Me.QuestLog.GetQuestById((uint)QuestId);
                TreeRoot.GoalText = ((Quest != null) ? ("\"" + Quest.Name + "\"") : "In Progress");
            }
        }


        public WoWUnit Dargon
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>(true).FirstOrDefault(u => u.Entry == 55786);
            }
        }


        public WoWUnit Launcher
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>(true).Where(u => u.Entry == 64507 && u.Flags == 295680).OrderBy(r=>r.Location.Distance(Dargon.Location.RayCast(Dargon.MovementInfo.Heading,20f))).FirstOrDefault();
            }
        }

        public bool IsQuestComplete()
        {
            var quest = StyxWoW.Me.QuestLog.GetQuestById((uint)QuestId);
            return quest == null || quest.IsCompleted;
        }
        private bool IsObjectiveComplete(int objectiveId, uint questId)
        {
            if (Me.QuestLog.GetQuestById(questId) == null)
            {
                return false;
            }
            int returnVal = Lua.GetReturnVal<int>("return GetQuestLogIndexByID(" + questId + ")", 0);
            return
                Lua.GetReturnVal<bool>(
                    string.Concat(new object[] { "return GetQuestLogLeaderBoard(", objectiveId, ",", returnVal, ")" }), 2);
        }

        public Composite DoneYet
        {
            get
            {
                return
                    new Decorator(ret => IsQuestComplete(), new Action(delegate
                    {
                        TreeRoot.StatusText = "Finished!";
                        _isBehaviorDone = true;
                        return RunStatus.Success;
                    }));

            }
        }


        
        public Composite DpsHim
        {
            get
            {
                return new PrioritySelector(
                    new Decorator(r=> Me.CurrentTarget == null && Dargon != null && Me.CurrentTarget != Dargon, new Action(r=>Dargon.Target())),
                    new Decorator(r => Dargon != null && Dargon.HasAura(125992), RoutineManager.Current.CombatBehavior)
                    
                    );
            }
        }

        public Composite Boom
        {
            get
            {
                return new Decorator(r => Dargon != null && Launcher != null, new Action(r =>
                                                                        {
                                                                            //Navigator.MoveTo(Launcher.Location);
                                                                            Launcher.Interact(true);
                                                                        }));
            }
        }



        protected Composite CreateBehavior_QuestbotMain()
        {
            return _root ?? (_root = new Decorator(ret => !_isBehaviorDone, new PrioritySelector(DoneYet, DpsHim, Boom, new ActionAlwaysSucceed())));
        }

        #region Cleanup

        ~BattlefortheSkies()
        {
            Dispose(false);
        }

        private bool _isDisposed;

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool isExplicitlyInitiatedDispose)
        {
            if (!_isDisposed)
            {
                // NOTE: we should call any Dispose() method for any managed or unmanaged
                // resource, if that resource provides a Dispose() method.

                // Clean up managed resources, if explicit disposal...
                if (isExplicitlyInitiatedDispose)
                {
                    TreeHooks.Instance.RemoveHook("Questbot_Main", CreateBehavior_QuestbotMain());
                }

                // Clean up unmanaged resources (if any) here...
                TreeRoot.GoalText = string.Empty;
                TreeRoot.StatusText = string.Empty;

                // Call parent Dispose() (if it exists) here ...
                base.Dispose();
            }

            _isDisposed = true;
        }

        #endregion
    }
}
