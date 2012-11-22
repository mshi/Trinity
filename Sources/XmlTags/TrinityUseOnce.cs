﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zeta.CommonBot.Profile;
using Zeta.CommonBot.Profile.Composites;
using Zeta.TreeSharp;
using Zeta.XmlEngine;

namespace GilesTrinity.XmlTags
{
    // * TrinityUseOnce ensures a sequence of tags is only ever used once during this profile
    [XmlElement("TrinityUseOnce")]
    public class TrinityUseOnce : ComplexNodeTag
    {
        private bool? bComplexDoneCheck;
        private bool? bAlreadyCompleted;
        private Func<bool> funcConditionalProcess;
        private static Func<ProfileBehavior, bool> funcBehaviorProcess;
        private int iUniqueID;
        private int iMaxRedo;
        private string sDisablePrevious;

        protected override Composite CreateBehavior()
        {
            PrioritySelector decorated = new PrioritySelector(new Composite[0]);
            foreach (ProfileBehavior behavior in base.GetNodes())
            {
                decorated.AddChild(behavior.Behavior);
            }
            return new Zeta.TreeSharp.Decorator(new CanRunDecoratorDelegate(CheckNotAlreadyDone), decorated);
        }

        public bool GetConditionExec()
        {
            // See if we've EVER hit this ID before
            if (GilesTrinity.hashUseOnceID.Contains(ID))
            {

                // See if we've hit it more than or equal to the max times before
                if (GilesTrinity.dictUseOnceID[ID] >= Max || GilesTrinity.dictUseOnceID[ID] < 0)
                    return false;

                // Add 1 to our hit count, and let it run this time
                GilesTrinity.dictUseOnceID[ID]++;
                return true;
            }

            // Never hit this before, so create the entry and let it run

            // First see if we should disable all other ID's currently hit to prevent them ever being run again this run
            if (DisablePrevious != null && DisablePrevious.ToLower() == "true")
            {
                foreach (int thisid in GilesTrinity.hashUseOnceID)
                {
                    if (thisid != ID)
                    {
                        GilesTrinity.dictUseOnceID[thisid] = -1;
                    }
                }
            }

            // Now store the fact we have hit this ID and set up the dictionary entry for it
            GilesTrinity.hashUseOnceID.Add(ID);
            GilesTrinity.dictUseOnceID.Add(ID, 1);
            return true;
        }

        private bool CheckNotAlreadyDone(object object_0)
        {
            return !IsDone;
        }

        public override void ResetCachedDone()
        {
            foreach (ProfileBehavior behavior in Body)
            {
                behavior.ResetCachedDone();
            }
            bComplexDoneCheck = null;
        }

        private static bool CheckBehaviorIsDone(ProfileBehavior profileBehavior)
        {
            return profileBehavior.IsDone;
        }

        [XmlAttribute("id")]
        public int ID
        {
            get
            {
                return iUniqueID;
            }
            set
            {
                iUniqueID = value;
            }
        }

        [XmlAttribute("disableprevious")]
        public string DisablePrevious
        {
            get
            {
                return sDisablePrevious;
            }
            set
            {
                sDisablePrevious = value;
            }
        }

        [XmlAttribute("max")]
        public int Max
        {
            get
            {
                return iMaxRedo;
            }
            set
            {
                iMaxRedo = value;
            }
        }

        public Func<bool> Conditional
        {
            get
            {
                return funcConditionalProcess;
            }
            set
            {
                funcConditionalProcess = value;
            }
        }

        public override bool IsDone
        {
            get
            {
                // Make sure we've not already completed this tag
                if (bAlreadyCompleted.HasValue && bAlreadyCompleted == true)
                {
                    return true;
                }
                if (!bComplexDoneCheck.HasValue)
                {
                    bComplexDoneCheck = new bool?(GetConditionExec());
                }
                if (bComplexDoneCheck == false)
                {
                    return true;
                }
                if (funcBehaviorProcess == null)
                {
                    funcBehaviorProcess = new Func<ProfileBehavior, bool>(CheckBehaviorIsDone);
                }
                bool bAllChildrenDone = Body.All<ProfileBehavior>(funcBehaviorProcess);
                if (bAllChildrenDone)
                {
                    bAlreadyCompleted = true;
                }
                return bAllChildrenDone;
            }
        }
    }
}
