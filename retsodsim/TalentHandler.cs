//returns a stats dict with the increase in stats from talents returns [new stats(dict),abilities (dict)]

using System.Text.RegularExpressions;
using Force.DeepCloner;

namespace retsodsim;

public class TalentHandler
{
    public static (Dictionary<string, double>,Dictionary<string, Ability>,Dictionary<string, Ability>) return_ability_stats(Dictionary<string, double> cStats, string talentsRunes)
    {
        var runes = talentsRunes.Split("_")[1];
        var talents = talentsRunes.Split("_")[0];
        var procs = new Dictionary<string, Ability>
        {
            { "seal", new Ability(0,(stats)=>0,100,"holy","sor") } //add sor
        };
        var abilities = new Dictionary<string, Ability>()
        {
            { "judge", new Ability(8,(stats)=>0,100,"holy","jor") }, //add sor
            {
                "heavy Dynamite",
                new Ability(60,(stats)=> Ability.GetRandomDouble(128,178) ,100,"spell","Heavy Dynamite")
            }
        };
        var dmgChanges = new Dictionary<string, (double, double)>
        {
            { "aa", (0, 1) },
            { "physical", (0, 1) },
            { "spell", (0, 1) },
            { "holy", (0, 1) }
        };
        Console.WriteLine("Sim Ghamoo-ra? (y/n)");
        if (Console.ReadLine() == "y")
        {
            dmgChanges["spell"] = (0,2);
            dmgChanges["holy"] = (0,2);
        }
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
                    break;
                     // add in rightusenoss % dmg increase
                }
                case 5:
                {
                    abilities.Add("consec",new Ability(8,(stats)=>64+0.336*stats["sp"],5,"holy","Consecration"));
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
        for (int i = 0; i < retTalents.Length; i++)
        {
            switch(i)
            {
                case 0:
                    modifiers.Add(("ap",(stats) => 55*(0.04*retTalents[0])));
                    break;
                
                case 2:
                    cdChanges.Add("judge", -1 * retTalents[2]);
                    break;
                case 3:
                    dmgChanges["holy"] = (dmgChanges["holy"].Item1+30, dmgChanges["holy"].Item2); // think this is right
                    break;
                case 6:
                    modifiers.Add(("crit",(stats)=>retTalents[6]));
                    break;
                case 7:
                    abilities["judge"] = new Ability(10,(stats)=>Ability.GetRandomDouble(60,64),3,"physical","Judgment of Command",dmgType:"holy");
                    procs["seal"] = new Ability(0,(stats)=>0.7*stats["dmg"],19999,"physical","Seal of Command",procChance:100*(7*cStats["speed"])/60,dmgType:"holy");
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
        Regex chestR = new Regex(@"156.{2}");
        Regex handR = new Regex(@"6n.");
        Regex legsR = new Regex(@"76.{2}");
        var chest = chestR.Match(runes).Value.Remove(0,3);
        var hand = handR.Match(runes).Value.Remove(0,2);
        var legs = legsR.Match(runes).Value.Remove(0,2);
        if (chest == "p2")
        {
            abilities.Add("ds", new Ability(10,(stats)=>1.1*stats["dmg"],0,"physical","Divine Storm"));
        }
        else if (chest == "p3")
        {
            procs["seal"] = new Ability(0,(stats)=>0.35*stats["dmg"],1000,"physical","Seal of Blood",dmgType:"holy");
            abilities["judge"] = new Ability(10,(stats)=>0.7*stats["dmg"],3,"physical","Judgement of Blood",dmgType:"holy");
        }

        if (hand == "x")
        {
            abilities.Add("cs", new Ability(6,(stats)=>0.76*stats["dmg"],1,"physical","Crusader Strike"));
        }

        if (legs == "sn")
        {
            abilities.Add("exo", new Ability(15,(stats)=> Ability.GetRandomDouble(90+stats["sp"]*0.429,102+stats["sp"]*0.429),2,"holy","Exocism"));
        }
        procs.Add("wf",
            new Ability(0, (stats) => ((stats["mindmg"]+stats["maxdmg"])/2 / stats["speed"] + (1.2 * stats["ap"]) / 14) * stats["speed"],199999,"aa","Windfury",procChance:20,procNames:["seal"])); // think this should be deep copy
        abilities.Add("melee",
            new Ability(cStats["speed"], (stats) => stats["dmg"], 100, "aa", "Melee", procNames: ["wf","seal"]));
        // doesnt change with haste
        Ability.Procs = procs;
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
        foreach (KeyValuePair<string,Ability> entry in Ability.Procs)
        {   
            entry.Value.Flatmod = dmgChanges[entry.Value.DmgType].Item1;
            entry.Value.PercentMod = dmgChanges[entry.Value.DmgType].Item2;
        }
        return (resStats, abilities,procs); 
    }
}

// does 2 hander spec increase wf dmg?



