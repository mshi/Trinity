﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Windows.Markup;
using Zeta;
using Zeta.Common;
using Zeta.Common.Plugins;
using Zeta.CommonBot;
using Zeta.CommonBot.Profile;
using Zeta.CommonBot.Profile.Composites;
using Zeta.Internals;
using Zeta.Internals.Actors;
using Zeta.Internals.Actors.Gizmos;
using Zeta.Internals.SNO;
using Zeta.Navigation;
using Zeta.TreeSharp;
using Zeta.XmlEngine;
namespace GilesTrinity
{
    public partial class GilesTrinity : IPlugin
    {
// These constants are used for item scoring and stashing
        private const int DEXTERITY = 0;
        private const int INTELLIGENCE = 1;
        private const int STRENGTH = 2;
        private const int VITALITY = 3;
        private const int LIFEPERCENT = 4;
        private const int LIFEONHIT = 5;
        private const int LIFESTEAL = 6;
        private const int LIFEREGEN = 7;
        private const int MAGICFIND = 8;
        private const int GOLDFIND = 9;
        private const int MOVEMENTSPEED = 10;
        private const int PICKUPRADIUS = 11;
        private const int SOCKETS = 12;
        private const int CRITCHANCE = 13;
        private const int CRITDAMAGE = 14;
        private const int ATTACKSPEED = 15;
        private const int MINDAMAGE = 16;
        private const int MAXDAMAGE = 17;
        private const int BLOCKCHANCE = 18;
        private const int THORNS = 19;
        private const int ALLRESIST = 20;
        private const int RANDOMRESIST = 21;
        private const int TOTALDPS = 22;
        private const int ARMOR = 23;
        private const int MAXDISCIPLINE = 24;
        private const int MAXMANA = 25;
        private const int ARCANECRIT = 26;
        private const int MANAREGEN = 27;
        private const int GLOBEBONUS = 28;
        private const int TOTALSTATS = 29; 
// starts at 0, remember... 0-26 = 1-27!
// Readable names of the above stats that get output into the trash/stash log files
        private static readonly string[] StatNames = new string[29] { "Dexterity", "Intelligence", "Strength", "Vitality", "Life %", "Life On Hit", "Life Steal %", "Life Regen", 
            "Magic Find %", "Gold Find   %", "Movement Speed %", "Pickup Radius", "Sockets", "Crit Chance %", "Crit Damage %", "Attack Speed %", "+Min Damage", 
            "+Max Damage", "Total Block %", "Thorns", "+All Resist", "+Highest Single Resist", "DPS", "Armor", "Max Disc.", "Max Mana", "Arcane-On-Crit", "Mana Regen", "Globe Bonus"};
// Stores the apparent maximums of each stat for each item slot
// Note that while these SHOULD be *actual* maximums for most stats - for things like DPS, these can just be more sort of "what a best-in-slot DPS would be"
//												                Dex  Int  Str  Vit  Life%     LOH Steal%  LPS Magic% Gold% MSPD Rad. Sox Crit% CDam% ASPD Min+ Max+ Block% Thorn Allres Res   DPS ARMOR Disc.Mana Arc. Regen  Globes
        private static double[] iMaxWeaponOneHand = new double[29] { 320, 320, 320, 320, 0, 850, 2.8, 0, 0, 0, 0, 0, 1, 0, 85, 0, 0, 0, 0, 0, 0, 0, 1210, 0, 10, 150, 10, 14, 0 };
        private static double[] iMaxWeaponTwoHand = new double[29] { 530, 530, 530, 530, 0, 1800, 5.8, 0, 0, 0, 0, 0, 1, 0, 170, 0, 0, 0, 0, 0, 0, 0, 1590, 0, 10, 119, 10, 14, 0 };
        private static double[] iMaxWeaponRanged = new double[29] { 320, 320, 320, 320, 0, 850, 2.8, 0, 0, 0, 0, 0, 1, 0, 85, 0, 0, 0, 0, 0, 0, 0, 1410, 0, 0, 0, 0, 14, 0 };
        private static double[] iMaxOffHand = new double[29] { 300, 300, 300, 300, 9, 0, 0, 234, 18, 20, 0, 0, 1, 8.5, 0, 15, 110, 402, 0, 979, 0, 0, 0, 0, 10, 119, 10, 11, 5977 };
        private static double[] iMaxShield = new double[29] { 330, 330, 330, 330, 16, 0, 0, 342, 20, 25, 0, 0, 1, 10, 0, 0, 0, 0, 30, 2544, 80, 60, 0, 397, 0, 0, 0, 0, 12794 };
        private static double[] iMaxRing = new double[29] { 200, 200, 200, 200, 12, 479, 0, 234, 20, 25, 0, 0, 1, 6, 50, 9, 36, 100, 0, 979, 80, 60, 0, 240, 0, 0, 0, 0, 5977 };
        private static double[] iMaxAmulet = new double[29] { 350, 350, 350, 350, 16, 959, 0, 410, 45, 50, 0, 0, 1, 10, 100, 9, 36, 100, 0, 1712, 80, 60, 0, 360, 0, 0, 0, 0, 5977 };
        private static double[] iMaxShoulders = new double[29] { 200, 200, 300, 200, 12, 0, 0, 342, 20, 25, 0, 7, 0, 0, 0, 0, 0, 0, 0, 2544, 80, 60, 0, 265, 0, 0, 0, 0, 12794 };
        private static double[] iMaxHelm = new double[29] { 200, 300, 200, 200, 12, 0, 0, 342, 20, 25, 0, 7, 1, 6, 0, 0, 0, 0, 0, 1454, 80, 60, 0, 397, 0, 0, 0, 0, 12794 };
        private static double[] iMaxPants = new double[29] { 200, 200, 200, 300, 0, 0, 0, 342, 20, 25, 0, 7, 2, 0, 0, 0, 0, 0, 0, 1454, 80, 60, 0, 397, 0, 0, 0, 0, 12794 };
        private static double[] iMaxGloves = new double[29] { 300, 300, 200, 200, 0, 0, 0, 342, 20, 25, 0, 7, 0, 10, 50, 9, 0, 0, 0, 1454, 80, 60, 0, 265, 0, 0, 0, 0, 12794 };
        private static double[] iMaxChest = new double[29] { 200, 200, 200, 300, 12, 0, 0, 599, 20, 25, 0, 7, 3, 0, 0, 0, 0, 0, 0, 2544, 80, 60, 0, 397, 0, 0, 0, 0, 12794 };
        private static double[] iMaxBracer = new double[29] { 200, 200, 200, 200, 0, 0, 0, 342, 20, 25, 0, 7, 0, 6, 0, 0, 0, 0, 0, 1454, 80, 60, 0, 265, 0, 0, 0, 0, 12794 };
        private static double[] iMaxBoots = new double[29] { 300, 200, 200, 200, 0, 0, 0, 342, 20, 25, 12, 7, 0, 0, 0, 0, 0, 0, 0, 1454, 80, 60, 0, 265, 0, 0, 0, 0, 12794 };
        private static double[] iMaxBelt = new double[29] { 200, 200, 300, 200, 12, 0, 0, 342, 20, 25, 0, 7, 0, 0, 0, 0, 0, 0, 0, 2544, 80, 60, 0, 265, 0, 0, 0, 0, 12794 };
        private static double[] iMaxCloak = new double[29] { 200, 200, 200, 300, 12, 0, 0, 410, 20, 25, 0, 7, 3, 0, 0, 0, 0, 0, 0, 2544, 70, 50, 0, 397, 10, 0, 0, 0, 12794 };
        private static double[] iMaxMightyBelt = new double[29] { 200, 200, 300, 200, 12, 0, 3, 342, 20, 25, 0, 7, 0, 0, 0, 0, 0, 0, 0, 2544, 70, 50, 0, 265, 0, 0, 0, 0, 12794 };
        private static double[] iMaxSpiritStone = new double[29] { 200, 300, 200, 200, 12, 0, 0, 342, 20, 25, 0, 7, 1, 6, 0, 0, 0, 0, 0, 1454, 70, 50, 0, 397, 0, 0, 0, 0, 12794 };
        private static double[] iMaxVoodooMask = new double[29] { 200, 300, 200, 200, 12, 0, 0, 342, 20, 25, 0, 7, 1, 6, 0, 0, 0, 0, 0, 1454, 70, 50, 0, 397, 0, 119, 0, 11, 12794 };
        private static double[] iMaxWizardHat = new double[29] { 200, 300, 200, 200, 12, 0, 0, 342, 20, 25, 0, 7, 1, 6, 0, 0, 0, 0, 0, 1454, 70, 50, 0, 397, 0, 0, 10, 0, 12794 };
        private static double[] iMaxFollower = new double[29] { 300, 300, 300, 200, 0, 300, 0, 234, 0, 0, 0, 0, 0, 0, 55, 0, 0, 0, 0, 0, 50, 40, 0, 0, 0, 0, 0, 0, 0 };
// Stores the total points this stat is worth at the above % point of maximum
// Note that these values get all sorts of bonuses, multipliers, and extra things applied in the actual scoring routine. These values are more of a "base" value.
//                                                              Dex    Int    Str    Vit    Life%  LOH    Steal% LPS   Magic%  Gold%  MSPD   Rad  Sox    Crit%  CDam%  ASPD   Min+  Max+ Block% Thorn Allres Res   DPS    ARMOR  Disc.  Mana  Arc.  Regen  Globes
        private static double[] iWeaponPointsAtMax = new double[29] { 14000, 14000, 14000, 14000, 13000, 20000, 7000, 1000, 6000, 6000, 6000, 500, 16000, 15000, 15000, 0, 0, 0, 0, 1000, 11000, 0, 64000, 0, 10000, 8500, 8500, 10000, 8000 };
//                                                              Dex    Int    Str    Vit    Life%  LOH    Steal% LPS   Magic%  Gold%  MSPD   Rad. Sox    Crit%  CDam%  ASPD   Min+  Max+ Block% Thorn Allres Res   DPS    ARMOR  Disc.  Mana  Arc.  Regen  Globes
        private static double[] iArmorPointsAtMax = new double[29] { 11000, 11000, 11000, 9500, 9000, 10000, 4000, 1200, 3000, 3000, 3500, 1000, 4300, 9000, 6100, 7000, 3000, 3000, 5000, 1200, 7500, 1500, 0, 5000, 4000, 3000, 3000, 6000, 5000 };
        private static double[] iJewelryPointsAtMax = new double[29] { 11500, 11000, 11000, 10000, 8000, 11000, 4000, 1200, 4500, 4500, 3500, 1000, 3500, 7500, 6300, 6800, 800, 800, 5000, 1200, 7500, 1500, 0, 4500, 4000, 3000, 3000, 6000, 5000 };
// Some special values for score calculations
// BonusThreshold is a percentage of the "max-stat possible", that the stat starts to get a multiplier on it's score. 1 means it has to be above 100% of the "max-stat" to get a multiplier (so only possible if the max-stat isn't ACTUALLY the max possible)
// MinimumThreshold is a percentage of the "max-stat possible", that the stat will simply be ignored for being too low. eg if set to .5 - then anything less than 50% of the max-stat will be ignored.
// MinimumPrimary is used for some stats only - and means that at least ONE primary stat has to be above that level, to get score. Eg magic-find has .5 - meaning any item without at least 50% of a max-stat primary, will ignore magic-find scoring.
//                                                             Dex  Int  Str  Vit  Life%  LOH  Steal%   LPS Magic% Gold% MSPD Radi  Sox  Crit% CDam% ASPD  Min+  Max+  Block%  Thorn  Allres  Res   DPS  ARMOR   Disc. Mana  Arc. Regen  Globes
        private static double[] iBonusThreshold = new double[29] { .75, .75, .75, .75, .80, .70, .8, 1, 1, 1, .95, 1, 1, .70, .90, 1, .9, .9, .83, 1, .85, .95, .80, .90, 1, 1, 1, .9, 1.0 };
        private static double[] iMinimumThreshold = new double[29] { .40, .40, .40, .30, .60, .35, .6, .7, .40, .40, .75, .8, .4, .40, .60, .40, .2, .2, .65, .6, .40, .55, .40, .80, .7, .7, .7, .7, .40 };
        private static double[] iStatMinimumPrimary = new double[29] { 0, 0, 0, 0, 0, 0, 0, .2, .40, .40, .30, 0, 0, 0, 0, 0, .40, .40, .40, .40, .40, .40, 0, .40, .40, .40, .40, .4, .40 };
    }
}
