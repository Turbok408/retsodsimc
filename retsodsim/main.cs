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
            Dictionary<string, double> stats = new Dictionary<string, double>
            {
                { "agi", 34 + 8 + 4 + 3 },
                { "sta", 0 },
                { "stg", 48 + 8 + 3 + 4 },
                { "ap", 151 + 20 + 60 + 55 },
                { "speed", 0 },
                { "mindmg", 3 }, // ecnhant
                { "maxdmg", 3 },
                { "crit", 2 },
                { "hit", 0 },
                { "sp", 25 },
                { "sp_crit", 4.12 + 3}, // no idea where 4.12 base crit comes from
                { "int", 36 + 5 +7 }, // this is just base int +int buff + int pot no int stat in database
                {"mana",552 + 320}, // this assume one lesser mana potion
                {"spirit",38}, // just base spirit no spirit stat in databse
                {"haste",100}
            }; // bases stats + all buffs should add global dmg modifer for world buffs 
            string jsonFilePath = Directory.GetCurrentDirectory()+"\\items.json";
            string json = File.ReadAllText(jsonFilePath);
            var  allIds = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string,dynamic>>>(json);
            int id = 1282;
            string itemsIdsTxt = File.ReadAllText(Directory.GetCurrentDirectory() + "\\saves.json");
            var itemsIdsDict = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(itemsIdsTxt);
            List<string> testSet = itemsIdsDict["save1"];
            //List<string> testSet = ["211505", "209422", "14749", "209523", "211504", "2868", "211423", "6460", "6087", "209689", "209565","2933", "21568", "211449", "209562"];
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
                        double procChance = 10;
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
                                allIds[entry]["name"], 0,procChance = procChance));
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
            var setNum = 0;
            foreach (var entry in testSet)
            {
                if (entry == "211505" | entry == "211504" | entry == "211504")
                {
                    setNum += 1;
                }
            }
            if (setNum == 2)
            {
                stats["ap"] += 12;
            }
            else if (setNum == 3)
            {
                stats["ap"] += 12;
                stats["hit"] += 1;
            }

            if (statModifiers != null)
            {
                foreach (var entry in statModifiers)
                {
                    Console.WriteLine(entry.Key);
                    stats[entry.Key] += entry.Value;
                }
            }
            var statAbilitys = TalentHandler.return_ability_stats(stats, talents);
            stats = statAbilitys.Item1;
            var abilitys = statAbilitys.Item2;
            foreach (var entry in statAbilitys.Item3)
            {
                dmgProcs.Add(entry.Key,entry.Value);
            }
            // apply % buffs after talents? idk if this is correct
            stats["agi"] *= 1.1; //lion buff
            stats["stg"] *= 1.1; //lion buff
            stats["ap"] += 2 * stats["stg"];
            var dps = ((stats["mindmg"] + stats["maxdmg"]) / 2)/stats["speed"];
            var j = (((stats["mindmg"] + stats["maxdmg"]) / 2) / stats["speed"] + stats["ap"] / 14) * stats["speed"];
            stats.Add("dmg",(dps+stats["ap"]/14)*stats["speed"]);
            stats["crit"] += stats["agi"] / 20 * 0.01;
            stats["mana"] += stats["int"] * 15;
            stats["spell_crit"] += stats["int"] / 54;
            return (stats, abilitys,onUse,dmgProcs);
        }

        private static Dictionary<string,List<double>> RunSim(int iterations, double time,Dictionary<string,double> stats,Dictionary<string,Ability> abilities,Dictionary<string,OnHitUseStat> onHitUseStat,Dictionary<string,Ability> procs)
        {
            var instArray = new Instance[iterations];
                ManualResetEvent signal = new ManualResetEvent(false);
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
        
        static void Main(string[] args)
            {
                Console.WriteLine("Time to sim?");
                double time =Convert.ToDouble(Console.ReadLine()) ;
                Console.WriteLine("Iterations?");
                int iterations =Convert.ToInt32(Console.ReadLine()) ;
                Console.WriteLine("Input Talents: ");
                string talents = Console.ReadLine();
                var statAbilites = GetStats(talents);
                var results = RunSim(iterations, time, statAbilites.Item1, statAbilites.Item2, statAbilites.Item3,statAbilites.Item4);
                OutputDetailed(results,time,iterations);
                Console.ReadLine();
            }
        }
    }


/* add auras (done except ap procs on wf?)
 options for buffs(why sim without buffs?)
 --50230051_156p276sna6nx
 seal of blood mana back
 idiot proof inputs
 no sp items in db?
 */


