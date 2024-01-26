using Force.DeepCloner;
using Newtonsoft.Json;

namespace retsodsim
{
    class Program
    {
        static (Dictionary<string,double>,Dictionary<string,Ability>) GetStats(string talents)
        {
            Dictionary<string, double> stats = new Dictionary<string, double>
            {
                { "agi", 34 + 8 + 4 + 3 },
                { "sta", 0 },
                { "stg", 48 + 8 + 3 + 4 },
                { "ap", 151 + 20 + 60 + 55 },
                { "speed", 0 },
                { "mindmg", 3 },
                { "maxdmg", 3 },
                { "crit", 0 },
                { "hit", 0 },
                { "sp", 25 },
                { "sp_crit", 4.8 },
                { "int", 47 + 5 }
            }; // bases stats + all buffs should add global dmg modifer for world buffs
            string jsonFilePath = Directory.GetCurrentDirectory()+"\\items.json";
            string json = File.ReadAllText(jsonFilePath);
            var  allIds = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string,dynamic>>>(json);
            int id = 1282;
            string itemsIdsTxt = File.ReadAllText(Directory.GetCurrentDirectory() + "\\saves.json");
            var itemsIdsDict = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(itemsIdsTxt);
            List<string> testSet = itemsIdsDict["save1"];
            //List<string> testSet = ["211505", "209422", "14749", "209523", "211504", "2868", "211423", "6460", "6087", "209689", "209565","2933", "21568", "211449", "209562"];
            foreach (var entry in testSet)
            {
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
            var statAbilitys = TalentHandler.return_ability_stats(stats, talents);
            stats = statAbilitys.Item1;
            var abilitys = statAbilitys.Item2;
            stats["agi"] *= 1.1;
            stats["stg"] *= 1.1; //lion buff
            stats["ap"] += 2 * stats["stg"];
            var dps = ((stats["mindmg"] + stats["maxdmg"]) / 2)/stats["speed"];
            stats.Add("dmg",(dps+stats["ap"]/14)*stats["speed"]);
            stats["crit"] += stats["agi"] / 20 * 0.01;
            return (stats, abilitys);
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
                var stats = statAbilites.Item1;
                var abilities = statAbilites.Item2;
                var instArray = new Instance[iterations];
                ManualResetEvent signal = new ManualResetEvent(false);
                var tasks = new List<Task>();
                for (int i = 0; i < iterations; i++)
                {
                    var f = new Instance(abilities.DeepClone(), stats, time);
                    instArray[i] = f;
                    var task = Task.Run(() => f.RunInstance());
                    tasks.Add(task);
                }
                Task.WaitAll(tasks.ToArray());
                Dictionary<string, List<double>> dmg = instArray[0].Output();
                for (int i = 1; i < iterations; i++)
                {
                    foreach (var entry in instArray[i].Output())
                    {
                        dmg[entry.Key][0] += entry.Value[0];
                        dmg[entry.Key][1] += entry.Value[1];
                    }
                }

                double dmgTotal = 0;
                foreach (var entry in dmg)
                {
                    dmgTotal += 1.15 * entry.Value[1] / (time * iterations);
                    Console.WriteLine(entry.Key + ": " + entry.Value[0] / iterations + " attacks, " +
                                      1.15 * entry.Value[1] / (double)iterations + " total dmg, " +
                                      1.15 * entry.Value[1] / (time * iterations) + " dps, " +
                                      1.15 * entry.Value[1] / (double)entry.Value[0] + " average dmg");
                }
                foreach (var entry in Ability.Procs)
                {
                    dmgTotal += 1.15 * entry.Value.AbilityDmgTotal / (time * iterations);
                    Console.WriteLine(entry.Value.Name + ": " + entry.Value._attacks / iterations + " attacks, " +
                                      1.15 * entry.Value.AbilityDmgTotal / (double)iterations + " total dmg, " +
                                      1.15 * entry.Value.AbilityDmgTotal / (time * iterations) + " dps, " +
                                      1.15 * entry.Value.AbilityDmgTotal / (double)entry.Value._attacks +
                                      " average dmg");
                }
                Console.WriteLine(dmgTotal + " dps");
                Console.ReadLine();
            }
        }
    }





