﻿using System;
using System.Linq;
using Zeta.Bot.Navigation;
using Zeta.Bot.Profile;
using Zeta.Game;
using Zeta.Game.Internals.Actors;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
using Action = Zeta.TreeSharp.Action;

namespace Trinity.XmlTags
{
    // * TrinityMoveTo moves in a straight line without any navigation hits, and allows tag-skips
    [XmlElement("TrinityMoveToSNO")]
    public class TrinityMoveToSNO : ProfileBehavior
    {
        private bool m_IsDone;
        private float fPathPrecision;
        private int iSNOID;
        private string sDestinationName;

        protected override Composite CreateBehavior()
        {
            return
            new PrioritySelector(
                new Decorator(ret => CheckDistanceWithinPathPrecision(),
                    new Action(ret => FlagTagAsCompleted())
                ),
                new Action(ret => MoveTo())
            );
        }

        private RunStatus MoveTo()
        {
            DiaObject tempObject = ZetaDia.Actors.GetActorsOfType<DiaObject>(true, false).FirstOrDefault<DiaObject>(a => a.ActorSNO == SNOID);
            if (tempObject != null)
            {
                Navigator.PlayerMover.MoveTowards(tempObject.Position);
                return RunStatus.Success;
            }
            return RunStatus.Success;
        }

        private bool CheckDistanceWithinPathPrecision()
        {
            DiaObject tempObject = ZetaDia.Actors.GetActorsOfType<DiaObject>(true, false).FirstOrDefault<DiaObject>(a => a.ActorSNO == SNOID);
            if (tempObject != null)
            {
                return (Trinity.Player.Position.Distance(tempObject.Position) <= Math.Max(PathPrecision, Navigator.PathPrecision));
            }
            return false;
        }

        private void FlagTagAsCompleted()
        {
            m_IsDone = true;
        }

        public override void ResetCachedDone()
        {
            m_IsDone = false;
            base.ResetCachedDone();
        }

        public override bool IsDone
        {
            get
            {
                if (IsActiveQuestStep)
                {
                    return m_IsDone;
                }
                return true;
            }
        }

        [XmlAttribute("name")]
        public string Name
        {
            get
            {
                return sDestinationName;
            }
            set
            {
                sDestinationName = value;
            }
        }

        [XmlAttribute("pathPrecision")]
        public float PathPrecision
        {
            get
            {
                return fPathPrecision;
            }
            set
            {
                fPathPrecision = value;
            }
        }

        [XmlAttribute("snoid")]
        public int SNOID
        {
            get
            {
                return iSNOID;
            }
            set
            {
                iSNOID = value;
            }
        }
    }
}
