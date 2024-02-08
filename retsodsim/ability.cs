namespace retsodsim
{
    public class Ability
    {
         // this is a very stupid way of doing procs when they add procs with cds this should be changed
        public string School { get; }
        public  Dictionary<string,Ability>? Procs { get; set; }
        public double PercentMod { get; set; } = 1;
        public double Prio { get; }
        public string Name { get; }
        public double Cd { get; set; }
        public double Flatmod { get; set; }
        public double AbilityDmgTotal { get; set; }
        public double _attacks;
        public double _currentCd;
        private double _procChance;
        private Func<Dictionary<string, double>, double> _dmgFunc;
        private static readonly Random GetRandom = new Random();
        private List<string>? _procNames {get; set; }
        public string DmgType;
        public double ManaCost;
        public bool CanCrit;
        public (List<string>,double) OnCritProc = (["0"],0); //list<cd refunds> mana refund
        public double modCrit;
        
        public Ability(double cd, Func<Dictionary<string, double>, double> dmgFunc, int prio, string school,
            string name,double manaCost, double procChance = 100, List<string>? procNames = null, string dmgType = "",bool cancrit = true)
        {
            Cd = cd;
            _dmgFunc = dmgFunc;
            Prio = prio;
            School = school;
            Name = name;
            _procChance = procChance;
            _procNames = procNames;
            ManaCost = manaCost;
            CanCrit = cancrit;
            if (dmgType == "")
            {
                DmgType = school;
            }
            else
            {
                DmgType = dmgType; 
            }
        }

        public (List<string>,double) do_dmg(Dictionary<string, double> stats)
        {
            if (GetRandom.Next(0, 100) <= _procChance && _currentCd <=0)
            {
                _currentCd = Cd;
                var hitRatio = DoCrit(stats);
                AbilityDmgTotal += (hitRatio * _dmgFunc(stats) + Flatmod) * PercentMod;
                _attacks += 1;
                if (hitRatio > 0 && Procs != null) // this assumes soc and wf cant proc of dodges?
                {
                    foreach (var entry in Procs)
                    {   
                        entry.Value.do_dmg(stats);
                    }
                }
                if(hitRatio==2.0)
                {
                    return OnCritProc;
                }
            }
            return (["0"],0);
        }

        private double DoCrit(Dictionary<string, double> stats)
        {

            if (CanCrit)
            {
                double hit = stats["hit"];
            double crit = stats["crit"];
            float glanceChance = 10 + 2 * 3;
            double ratingDif = 15-stats["skill"];
            var roll = GetRandom.Next(0, 100);
            var attackTable = new Dictionary<(double, double), double>()
            {
                { (0, 5 - hit), 0 }
            };
            switch (School) 
            {
                case "aa":
                    attackTable = new Dictionary<(double, double), double>()
                    {
                        { (0, 5 - hit), 0 },
                        {(5 - hit, 5 - hit + 6.5),0 },
                        {(5-hit+6.5,glanceChance+5-hit+6.5),GetRandomDouble(Math.Min(0.91, 1.3 - 0.05 * ratingDif), 1.2 - 0.03 * ratingDif)},
                        { (glanceChance + 5 - hit + 6.5, glanceChance + 5 - hit + 6.5 + crit+modCrit), 2 },
                        { (glanceChance + 5 - hit + 6.5 + crit+modCrit, 100), 1 }
                    };
                    break;
                
                case "physical":
                    attackTable = new Dictionary<(double, double), double>()
                    {
                        { (0, 5 - hit), 0 },
                        { (5 - hit + 6.5, 5 - hit + 6.5 + crit+modCrit), 2 },
                        { (5 - hit + 6.5 + crit+modCrit, 100), 1 }
                    };
                    break;
                    
                case "spell": //doesnt work properly but only spell is dynamite
                    attackTable = new Dictionary<(double, double), double>()
                    {
                        { (0, 4), 0 },
                        { (4, 5 - stats["sp_crit"] +modCrit+ 6.5 + 9), 2 },
                        { (4 + stats["sp_crit"]+modCrit, 100), 1 }
                    };
                    break;
                case "holy":
                    if (GetRandomDouble(0, 100) >= 17 - stats["hp_hit"])
                    {
                        attackTable = new Dictionary<(double, double), double>()
                        {
                            { (0, stats["hp_crit"]+modCrit), 2 },
                            { (stats["hp_crit"], 100), 1 }
                        };
                    }
                    else
                    {
                        attackTable = new Dictionary<(double, double), double>()
                        {
                            { (0, 100), 0 },
                        };
                    }
                    break;
            }
            foreach (KeyValuePair<(double, double), double> entry in attackTable)
            {
                if (entry.Key.Item1 < roll && roll <= entry.Key.Item2)
                {   
                    return entry.Value;
                }
            }
            return 0;
            }
            else
            {
                return 1;
            }
        }

        public void Reset()
        {
            _currentCd = 0;
        }

        public void ReduceCd(double time)
        {
            _currentCd -= time;
        }
        
        public static double GetRandomDouble(double minimum, double maximum)
        {
            return GetRandom.NextDouble() * (maximum - minimum) + minimum;
        }
    }

    
    public class OnHitUseStat
    {
        
        private double _cd;
        public double CdLeft { get; set; }
        public string Stat { get; }
        private double _amount;
        private double _duration;
        
        
        public OnHitUseStat(double duration,double cd,string stat,double amount)
        {
            _duration = duration;
            _cd = cd;
            Stat = stat;
            _amount = amount;
        }
        
        public Dictionary<string,double> DoEffect(Dictionary<string, double> stats)
        {
            stats[Stat] += _amount;
            CdLeft = _cd;
            return stats;
        }
        
        public Dictionary<string,double> UndoEffect(Dictionary<string, double> stats)
        {
            stats[Stat] -= _amount;
            return stats;
        }
 
        public void ReduceCd(double time)
        {
            CdLeft -= time;
        }
        public bool IsActive()
        {
            if ((_cd - CdLeft) <= _duration)
            {
                return true;
            }
            return false;
        }
    }
}
/* add auras (done except ap procs on wf?)
 options for buffs(why sim without buffs?)
 --50230001_116xg56p266wb76sn86sp96xka6nx
 seal of blood mana back
 no sp items in db?
 add new rune affects or effects?
 add armour
 add errors
 add benidictiion
 
 */

