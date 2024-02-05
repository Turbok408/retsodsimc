//returns a stats dict with the increase in stats from talents returns [new stats(dict),abilities (dict)]

using System.Text.RegularExpressions;
using Force.DeepCloner;

namespace retsodsim;

public class TalentHandler
{
    private const double baseMana = 552;

    private static string TryReduceString(string theString)
    {
        string theStringToreturn = "null";
        try
        {
            theStringToreturn = theString.Remove(0, 2);
        }
        catch{}

        return theStringToreturn;
    }
    public static (Dictionary<string, double>,Dictionary<string, Ability>,Dictionary<string, Ability>) return_ability_stats(Dictionary<string, double> cStats, string talentsRunes)
    {
        string? runes = null;
        try
        {
            runes = talentsRunes.Split("_")[1];
        }catch{}
        string talents = talentsRunes.Split("_")[0];
        var procs = new Dictionary<string, Ability>
        {
            { "seal", new Ability(0,(stats)=>23.74*stats["speed"]+0.125*stats["sp"],100,"holy","Seal of Righteousness",0) } //only correct for 2 handers
        };
        var abilities = new Dictionary<string, Ability>()
        {
            { "judge", new Ability(8,(stats)=>0.5*stats["sp"]+Ability.GetRandomDouble(96 ,105 ),100,"holy","Judgement of Righteousness",60+0.06*baseMana) }, //add sor
            {
                "heavy Dynamite",
                new Ability(60,(stats)=> Ability.GetRandomDouble(128,178) ,100,"spell","Heavy Dynamite",0)
            }
        };
        var dmgChanges = new Dictionary<string, (double, double)>
        {
            { "aa", (0, 1) },
            { "physical", (0, 1) },
            { "spell", (0, 1) },
            { "holy", (0, 1) }
        };
        /*
        Console.WriteLine("Sim Ghamoo-ra? (y/n)");
        if (Console.ReadLine() == "y")
        {
            dmgChanges["spell"] = (0,2);
            dmgChanges["holy"] = (0,2);
        }*/
        var cdChanges = new Dictionary<string, double>();
        var modifiers = new List<(string, Func<Dictionary<string,double>,double>)>();
        int[] holyTalents = [0], protTalents = [0], retTalents = [0];
        var talentSplit = talents.Split("-");
        for(int i = 0; i<talentSplit.Length;i++)
        {
            if (i == 0)
            {
                holyTalents = talentSplit.ElementAt(0).Select(o=> Convert.ToInt32(o) - 48 ).ToArray();
            }
            else if (i == 1)
            {
                if (talentSplit.ElementAt(1)!= "")
                {
                    protTalents = talentSplit.ElementAt(1).Select(o=> Convert.ToInt32(o) - 48 ).ToArray();
                }
            }
            else if (i == 2)
            {
                if (talentSplit.ElementAt(2) != "")
                {
                    retTalents = talentSplit.ElementAt(2).Select(o=> Convert.ToInt32(o) - 48 ).ToArray();
                }
            }
        }
        for (int i = 0; i < holyTalents.Length; i++)
        {
            switch (i)
            {
                case 0:
                    modifiers.Add(("stg", (stats) => stats["stg"] * (0.02 * holyTalents[0])));
                    break;
                case 1:
                    modifiers.Add(("int",(stats) => stats["int"] * (0.02 * holyTalents[1])));
                    break;
                case 3:
                {
                    procs["seal"].PercentMod += 0.03 * holyTalents[3]; // no need to check for name as cant be any other seal currently
                    abilities["judge"].PercentMod += 0.03 * holyTalents[3];
                    break;
                }
                case 5:
                {
                    abilities.Add("consec",new Ability(8,(stats)=>64+0.336*stats["sp"],5,"holy","Consecration",135,cancrit : false));
                    break;
                }
                case 12:
                {
                    modifiers.Add(("sp_crit",(stats) => holyTalents[12]));
                    break;
                }
                case 13:
                {
                    abilities.Add("holyShock",new Ability(30,(stats)=>0.43*stats["sp"]+Ability.GetRandomDouble(204,220),-2,"holy","Holy Shock",225));
                    break; 
                }
            }
        }
        for (int i = 0; i < protTalents.Length; i++)
        {
            if (i == 2)
            {
                modifiers.Add(("hit",(stats) => protTalents[2]));
            }
        }

        double judgeManaReduction = 1; // this is stupid
        double retCritMods = 0; // this is disgusting is cba to re write this
        for (int i = 0; i < retTalents.Length; i++)
        {
            switch(i)
            {
                case 0:
                    modifiers.Add(("ap",(stats) => 85*(0.04*retTalents[0])));
                    break;
                case 1 :
                    judgeManaReduction -= 0.03 * retTalents[1];
                    abilities["judge"].ManaCost -=0.03 * retTalents[1];
                    break;
                case 2:
                    cdChanges.Add("judge", -1 * retTalents[2]);
                    break;
                case 3:
                    dmgChanges["holy"] = (dmgChanges["holy"].Item1+30, dmgChanges["holy"].Item2); // think this is right
                    break;
                case 6:
                    modifiers.Add(("crit",(stats)=>retTalents[6]));
                    retCritMods += retTalents[6];
                    break;
                case 7:
                    abilities["judge"] = new Ability(10,(stats)=>Ability.GetRandomDouble(60,64)+0.429*stats["sp"],3,"physical","Judgment of Command",65+0.06*baseMana,dmgType:"holy");
                    procs["seal"] = new Ability(0,(stats)=>0.7*stats["dmg"]+0.2*stats["sp"],19999,"physical","Seal of Command",0,procChance:100*(7*cStats["speed"])/60,dmgType:"holy");
                    break;
                case 11:
                    dmgChanges["aa"] = (dmgChanges["aa"].Item1, dmgChanges["aa"].Item2 + 0.02 * retTalents[11]);
                    break;
                case 12:
                    dmgChanges["holy"] = (dmgChanges["holy"].Item1, dmgChanges["holy"].Item2 + 0.1);
                    break;
                case 13:
                    
                    dmgChanges["holy"] = (dmgChanges["holy"].Item1,
                        dmgChanges["holy"].Item2 + 0.03 * retTalents[13]);
                    dmgChanges["aa"] = (dmgChanges["aa"].Item1, dmgChanges["aa"].Item2 + 0.03 * retTalents[13]);
                    dmgChanges["physical"] = (dmgChanges["physical"].Item1,
                        dmgChanges["physical"].Item2 + 0.03 * retTalents[13]);
                    break;
            }
            
        }
        // should prob make this a list or smth will do next week fr fr ong
        Regex chestR = new Regex(@"56.{2}");
        Regex handR = new Regex(@"6n.{1}");
        Regex legsR = new Regex(@"76.{2}");
        Regex headR = new Regex(@"16.{2}");
        Regex wristR = new Regex(@"96.{2}");
        Regex waistR = new Regex(@"66.{2}");
        Regex feetR = new Regex(@"86.{2}");
        string? chest = "null";
        string? hand = "null";
        string? legs = "null";
        string? head = "null";
        string? wrist = "null";
        string? waist = "null";
        string? feet  = "null";
        if (runes != null)
        {
            chest = TryReduceString(chestR.Match(runes).ToString());
            hand = TryReduceString(handR.Match(runes).ToString());
            legs = TryReduceString(legsR.Match(runes).ToString());
            head = TryReduceString(headR.Match(runes).ToString());
            wrist = TryReduceString(wristR.Match(runes).ToString());
            waist = TryReduceString(waistR.Match(runes).ToString());
            feet = TryReduceString(feetR.Match(runes).ToString());
        }
        if (chest == "p2")
        {
            abilities.Add("ds", new Ability(10,(stats)=>1.1*stats["dmg"],0,"physical","Divine Storm",0.12*baseMana));
        }
        else if (chest == "p3")
        {
            procs["seal"] = new Ability(0,(stats)=>0.4*stats["dmg"],1000,"physical","Seal of Blood",0,dmgType:"holy");
            abilities["judge"] = new Ability(10,(stats)=>0.7*stats["dmg"],3,"physical","Judgement of Blood",0.05*baseMana,dmgType:"holy");
        }
        if (hand == "x")
        {
            abilities.Add("cs", new Ability(6,(stats)=>0.75*stats["dmg"],1,"physical","Crusader Strike",-0.05*baseMana));
        }
        if (legs == "sn")
        {
            abilities.Add("exo", new Ability(15,(stats)=> Ability.GetRandomDouble(225 +stats["sp"]*0.429,253 +stats["sp"]*0.429),2,"holy","Exocism",85));
        }
        if (head == "xg")
        {
            cStats["hp_crit"] += 18;
        }else if(head =="xh")
        {
            try
            {
                abilities["consec"].CanCrit = true;
                abilities["consec"].modCrit += cStats["crit"] + retCritMods;
            }catch{}
            try
            {
                abilities["exo"].modCrit += cStats["crit"]+ retCritMods;
            }catch{}
            try
            {
                abilities["holyShock"].modCrit += cStats["crit"]+ retCritMods;
            }catch{}
            try
            {
                abilities["holyWrath"].modCrit += cStats["crit"]+ retCritMods;
            }catch{}
        }
        if (wrist == "xj")
        {
            try
            {
                abilities["exo"].Cd /= 0.5;
            }catch{}
            // holy wrath cd -50% add holy wrath 
        }else if (wrist == "xk")
        {
            // hammer of wrath 0 cd for 10% of time add holy wrath
        }
        if (waist == "w9")
        {
            cStats["sp_hit"] = 17;
        }else if (waist == "wa")
        {
            cStats["sp"] += cStats["ap"] * 0.2;
        }
        else if (waist == "wb")
        {
            try
            {
                abilities["holyShock"].PercentMod += 0.2;
                abilities["holyShock"].OnCritProc = (["holyShock", "exo"], abilities["holyShock"].ManaCost);
            }catch{}
            // crits with holy shock -100% cd on shock exo refund holy shock mana
        }
        procs.Add("wf",
            new Ability(0, (stats) => ((stats["dmg"]/stats["speed"]-stats["ap"]/ 14) + (1.2 * stats["ap"]) / 14 )* stats["speed"],199999,"aa","Windfury",0,procChance:20,procNames:["seal"])); // think this should be deep copy idk if this works with ap procs
        abilities.Add("melee",
            new Ability(cStats["speed"]/cStats["haste"], (stats) => stats["dmg"], -100, "aa", "Melee",0, procNames: ["wf","seal"]));
        if (feet == "sk")
        {
            cStats["%manaPer3"] += 0.05;
        }else if (feet == "sp")
        {
            try
            {
                abilities["melee"].OnCritProc = (["holyShock", "exo"], 0);
                // aa crits => shock + exo -100% cd
            }catch{}
        }
        var tempDict = cStats.DeepClone();
        foreach (var VARIABLE in tempDict)
        {
            tempDict[VARIABLE.Key] = 0;
        }
        for (int i = 0; i < modifiers.Count; i++)
        {
            tempDict[modifiers.ElementAt(i).Item1] += modifiers.ElementAt(i).Item2(cStats);
        }
        var resStats = cStats.Concat(tempDict).GroupBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.Sum(y=>y.Value));
        foreach (KeyValuePair<string,double> entry in cdChanges)
        {
            abilities[entry.Key].Cd += entry.Value;
        }
        foreach (KeyValuePair<string,Ability> entry in abilities)
        {   
            entry.Value.Flatmod = dmgChanges[entry.Value.DmgType].Item1;
            entry.Value.PercentMod = dmgChanges[entry.Value.DmgType].Item2;
        }
        foreach (KeyValuePair<string,Ability> entry in procs)
        {   
            entry.Value.Flatmod = dmgChanges[entry.Value.DmgType].Item1;
            entry.Value.PercentMod = dmgChanges[entry.Value.DmgType].Item2;
        }
        return (resStats, abilities,procs); 
    }
}

// does 2 hander spec increase wf dmg?



