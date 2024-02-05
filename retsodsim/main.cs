using Force.DeepCloner;
using Newtonsoft.Json;

namespace retsodsim
{
    class Program
    {
        static (Dictionary<string,double>,Dictionary<string,Ability>,Dictionary<string,OnHitUseStat>,Dictionary<string,Ability>) GetStats(string talents, Dictionary<string,double>? statModifiers = null)
        {
            var onUse = new Dictionary<string, OnHitUseStat>();
            var dmgProcs = new Dictionary<string, Ability>();
            Dictionary<string, double> stats =new Dictionary<string, double>
            {
                { "agi", 46 +25 +8}, //ASUMMES SCROLLS DONT STACK
                { "sta", 0 },
                { "stg", 70 + 25 +8},
                { "ap", 240 +45 +85 +85},
                { "speed", 0 },
                { "mindmg", 3 }, // ecnhant
                { "maxdmg", 3 },
                { "crit", 3},
                { "hit", 0 },
                { "sp", 42},
                { "sp_hit", 3 },
                { "sp_crit", 3.8+3 +4}, // no idea where 3.8 base crit comes from
                { "int", 49 +15+8}, 
                {"mana",552 + 320}, // this assume one lesser mana potion
                {"spirit",56+8}, 
                {"haste",100 +10},
                {"hp_hit",0},
                {"hp_crit",0},
                {"%manaPer3",0}
            };
            if (statModifiers != null)
            {
                foreach (var entry in statModifiers)
                {
                    stats[entry.Key] += entry.Value;
                }
            }
            string jsonFilePath = Directory.GetCurrentDirectory()+"\\items.json";
            string json = File.ReadAllText(jsonFilePath);
            var  allIds = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string,dynamic>>>(json);
            string itemsIdsTxt = File.ReadAllText(Directory.GetCurrentDirectory() + "\\saves.json");
            var itemsIdsDict = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(itemsIdsTxt);
                //List<string> testSet = itemsIdsDict["save1"];
            List<string> testSet = ["215166", "213344", "213304", "213307", "213313", "19581", "216506", "213319", "213325", "213332", "9637","19515", "213284", "211449", "216505"];
            onUse.Add("5",new OnHitUseStat(10, 600,"haste",10));
            foreach (var entry in testSet)
            {
                if (allIds[entry].ContainsKey("proc"))
                {
                    Dictionary<string, string> proc = JsonConvert.DeserializeObject<Dictionary<string, string>>(allIds[entry]["proc"].ToString());
                    if (allIds[entry]["proc"].stat != null)
                    {
                        onUse.Add(allIds[entry]["name"], new OnHitUseStat(Convert.ToDouble(proc["duration"]),Convert.ToDouble(proc["cd"]),proc["stat"],Convert.ToDouble(proc["amount"])));
                    }
                    else
                    {
                        double dmg = 0;
                        double procChance = 10; // this assumes 10% proc chance unless otherwise stated.
                        string school = "spell";
                        if (allIds[entry]["proc"].tick != null)
                        {
                            double amountOfTicks =
                                Convert.ToDouble(proc["duration"]) / Convert.ToDouble(proc["interval"]);
                            dmg += Convert.ToDouble(proc["tick"]) * amountOfTicks;
                        }
                        else if (allIds[entry]["proc"].dmg != null)
                        {
                            dmg += Convert.ToDouble(proc["dmg"]);
                        }
                        else if (allIds[entry]["proc"].ppm != null)
                        {
                            procChance = 100 * (Convert.ToDouble(proc["ppm"]) * stats["speed"]) / 60; // this wont work 
                        }
                        else if (allIds[entry]["proc"].chance != null)
                        {
                            procChance = Convert.ToDouble(proc["chance"]);
                        }
                        else if (allIds[entry]["proc"].bleed != null)
                        {
                            school = "physical";
                        }
                        dmgProcs.Add(allIds[entry]["name"],
                            new Ability(0, new Func<Dictionary<string, double>, double>((stats) => dmg), 1000, school,
                                allIds[entry]["name"], 0,procChance : procChance));
                    }
                }
                foreach (var statType in (stats.Keys))
                {
                    try
                    {
                        stats[statType] += allIds[entry][statType];
                    }
                    catch{}
                }
            }
            Dictionary<string, int> sets = new Dictionary<string, int>();
            foreach (var entry in testSet)
            {
                try
                {
                    if (sets.ContainsKey(allIds[entry]["set"]))
                    {
                        sets[allIds[entry]["set"]] += 1;
                    }
                    else
                    {
                        sets.Add(allIds[entry]["set"],1);
                    }
                }
                catch{}
            }
            foreach (var entry in sets)
            {
                switch (entry.Key)
                {
                    case "InsulatedAp":
                        if (entry.Value >= 2)
                        {
                            stats["crit"] += 1;
                        }
                        break;
                    case "Electromantic":
                        if (entry.Value >= 2)
                        {
                            stats["ap"] += 24;
                            if (entry.Value==3)
                            {
                                // mana back
                            }
                        }
                        break;
                    case "H.A.Z.A.R.D.":
                        if (entry.Value >= 2)
                        {
                            stats["ap"] += 12;
                            if (entry.Value == 3)
                            {
                                stats["hit"] += 1;
                                stats["sp_hit"] += 1;
                            }
                        }
                        break;
                }
            }
            if (statModifiers != null)
            {
                foreach (var entry in statModifiers)
                {
                    stats[entry.Key] += entry.Value;
                }
            }
            stats["agi"] *= 1.1; //lion buff
            stats["crit"] += stats["agi"] / 20 ;// only works as no talents can change agi
            var statAbilitys = TalentHandler.return_ability_stats(stats, talents);
            stats = statAbilitys.Item1;
            var abilitys = statAbilitys.Item2;
            foreach (var entry in statAbilitys.Item3)
            {
                dmgProcs.Add(entry.Key,entry.Value);
            }
            // apply % buffs after talents? idk if this is correct
            stats["stg"] *= 1.1; //lion buff
            stats["ap"] += 2 * stats["stg"];
            var dps = ((stats["mindmg"] + stats["maxdmg"]) / 2)/stats["speed"];
            var j = (((stats["mindmg"] + stats["maxdmg"]) / 2) / stats["speed"] + stats["ap"] / 14) * stats["speed"];
            stats.Add("dmg",(dps+stats["ap"]/14)*stats["speed"]);
            stats["mana"] += stats["int"] * 15;
            stats["sp_crit"] += stats["int"] / 54;
            stats["hp_hit"] += stats["sp_hit"];
            stats["hp_crit"] += stats["sp_crit"];
            return (stats, abilitys,onUse,dmgProcs);
        }
        private static Dictionary<string,List<double>> RunSim(int iterations, double time,Dictionary<string,double> stats,Dictionary<string,Ability> abilities,Dictionary<string,OnHitUseStat> onHitUseStat,Dictionary<string,Ability> procs)
        {
            var instArray = new Instance[iterations];
                var tasks = new List<Task>();
                for (int i = 0; i < iterations; i++) 
                {
                    var f = new Instance(abilities.DeepClone(), stats.DeepClone(), time,onHitUseStat.DeepClone(),procs.DeepClone()); //this onHitUseStat.DeepClone() doesnt work
                    instArray[i] = f;
                    var task = Task.Run(() => f.RunInstance());
                    tasks.Add(task);
                }
                Task.WaitAll(tasks.ToArray());
                Dictionary<string, List<double>> dmg = instArray[0].Output();
                for (int i = 1; i < iterations; i++)
                {
                    foreach (var entry in instArray[i].Output()) // sum all dmg from each instance
                    {
                        dmg[entry.Key][0] += entry.Value[0];
                        dmg[entry.Key][1] += entry.Value[1];
                    }
                }
                return dmg;
        }

        private static void OutputDetailed(Dictionary<string, List<double>> dmg,double time,int iterations)
        {
            double dmgTotal = 0;
            foreach (var entry in dmg) 
            {
                dmgTotal += 1.15 * entry.Value[1] / (time * iterations);
                Console.WriteLine(entry.Key + ": " + entry.Value[0] / iterations + " attacks, " +
                                  1.15 * entry.Value[1] / (double)iterations + " total dmg, " +
                                  1.15 * entry.Value[1] / (time * iterations) + " dps, " +
                                  1.15 * entry.Value[1] / (double)entry.Value[0] + " average dmg");
            }
            Console.WriteLine(dmgTotal + " dps");
        }

        private static void StatWeights(int iterations, double time,string talents) //could optimize this a lot but cba
        {
            var statsThatDoDmg = new Dictionary<string, double>
            {
                {"spirit",0},
                { "agi", 0},
                { "stg", 0},
                { "ap", 0},
                { "crit", 0},
                { "hit", 0 },
                { "sp", 0 },
                { "sp_crit",0},
                { "int", 0}, 
                {"haste",0},
            };
            
            foreach (var entry in statsThatDoDmg)
            {
                var statAbilites = GetStats(talents,statModifiers : new Dictionary<string,double> {{entry.Key,50}});
                var dmg = RunSim(iterations, time, statAbilites.Item1, statAbilites.Item2, statAbilites.Item3,
                    statAbilites.Item4);
                double dmgTotal = 0;
                foreach (var varible in dmg) 
                {
                    dmgTotal += 1.15 * varible.Value[1] / (time * iterations);
                }
                statsThatDoDmg[entry.Key] = dmgTotal;
            }
            var basestatAbilites = GetStats(talents);
            var baseDmg = RunSim(iterations, time, basestatAbilites.Item1, basestatAbilites.Item2, basestatAbilites.Item3,
                basestatAbilites.Item4);
            var baseDmgTotal = 0.0;
            foreach (var varible in baseDmg) 
            {
                baseDmgTotal += 1.15 * varible.Value[1] / (time * iterations);
            }
            foreach (var entry in statsThatDoDmg)
            {
                statsThatDoDmg[entry.Key] = (entry.Value-baseDmgTotal)/50; //compare to base dmg
            }
            Console.WriteLine("+1 or 1% for crit/haste/hit statweights (this has a large error):");
            foreach (var entry in statsThatDoDmg)
            {
                Console.WriteLine(entry.Key+":"+(entry.Value)/statsThatDoDmg["ap"]);
                 // hugeeeeeeeeee variance on this
            }
        }
        
        static void Main(string[] args)
            {
                Console.WriteLine("Time to sim?");
                double time =Convert.ToDouble(Console.ReadLine()) ;
                Console.WriteLine("Iterations?");
                int iterations =Convert.ToInt32(Console.ReadLine()) ;
                Console.WriteLine("Input Talents: ");
                string talents = Console.ReadLine();
                var statAbilites = GetStats(talents);
                Console.WriteLine("press 1 for sim, 2 for stat weights (this will do iterations*11 iterations)");
                if (Console.ReadLine() == "1")
                {
                    var results = RunSim(iterations, time, statAbilites.Item1, statAbilites.Item2, statAbilites.Item3,statAbilites.Item4);
                    OutputDetailed(results,time,iterations);
                }
                else
                {
                    StatWeights(iterations,time,talents);
                }
                Console.ReadLine();
            }
        }
    }
/* wf ap?
 seal of blood can proc every same as melee + can dodge etc?
 seal of blood changes with new patch?
 check new runes
 crit procs can be optimsed but prob not needed
 55550100501051--50205051_166wb86sp
 --55230051100315_156p266wa76sn86sk96xka6nx
 weopon procs dont take talents modifiers? this could be correct
 seal of wisdom
 sor dmg with new rank
 */




