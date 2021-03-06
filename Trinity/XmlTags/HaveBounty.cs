﻿using System.Linq;
using Zeta.Game;
using Zeta.Game.Internals;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
namespace Trinity.XmlTags
{
    [XmlElement("HaveBounty")]
    public class HaveBounty : BaseComplexNodeTag
    {
        protected override Composite CreateBehavior()
        {
            return
            new Decorator(ret => !IsDone,
                new PrioritySelector(
                    base.GetNodes().Select(b => b.Behavior).ToArray()
                )
            );
        }

        public override bool GetConditionExec()
        {
            return ZetaDia.ActInfo.Bounties.Where(bounty => bounty.Info.QuestSNO == QuestId && bounty.Info.State != QuestState.Completed).FirstOrDefault() != null;
        }

        private bool CheckNotAlreadyDone(object obj)
        {
            return !IsDone;
        }
    }
}
