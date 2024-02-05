using Force.DeepCloner;

namespace retsodsim;

public class Instance
{
    private  Dictionary<string,Ability>? Procs { get; set; }
    private double _time;
    private Dictionary<string, double> _normalStats;
    private Dictionary<string, double> _proccedStats;
    private Dictionary<string, Ability> _abilities;
    private double _mana;
    private const double _baseRegen = 0.017936;
    private double _fiveSecondTimer;
    private Dictionary<string, OnHitUseStat> _onHitUseStats;
    private List<string> _activeProcs =[];
    private bool _onGcd = false;
    private double _maxMana;
    public int OomTicks;

    public Instance(Dictionary<string, Ability> abilities, Dictionary<string, double> stats, double time,
        Dictionary<string, OnHitUseStat> onHitUseStats, Dictionary<string, Ability>? procs)
    {
        _abilities = abilities;
        _normalStats = stats;
        _time = time;
        _mana = stats["mana"];
        _maxMana = stats["mana"];
        _onHitUseStats = onHitUseStats;
        _proccedStats = stats;
        Procs = procs;
        procs["wf"].Procs = new Dictionary<string, Ability> {{ "seal", procs["seal"]}};
    foreach (var entry in procs)
        {
            _abilities["melee"].Procs = procs;
        }

        if (procs["seal"].Name == "Seal of Blood")
        {
            onHitUseStats.Add("sobMana",new OnHitUseStat(1,stats["speed"],"mana",(1+stats["crit"]/100)*0.3*0.4*stats["dmg"])); // good enough
        }
    }
    public Dictionary<string,List<double>> Output()
    { 
        Dictionary<string,List<double>> dmg = new Dictionary<string, List<double>>();
        foreach (var entry in _abilities)
        {
            dmg.Add(entry.Value.Name,[(long)entry.Value._attacks,(long)entry.Value.AbilityDmgTotal]);
        }
        foreach (var entry in Procs)
        {
            dmg.Add(entry.Value.Name,[(long)entry.Value._attacks,(long)entry.Value.AbilityDmgTotal]);
        }
        return dmg;
    }
    public void RunInstance()
    {
        while (_time > 0)
        {
            Iterate();
        }
        foreach (var entry in _abilities)
        {
            entry.Value.Reset();
        }
        _time = 120; 
    }

    private void DoCritProc((List<string>, double) procList)
    {
        foreach (var entry in procList.Item1)
        {
            try
            {
                _abilities[entry]._currentCd = 0;
            }catch{}
        }
        _mana += procList.Item2;
    }
    private void Iterate() // clean this up a little
            {
                double prio = 100000000;
                string? toPress = null;
                bool buttonPressed = false;
                foreach (var entry in _onHitUseStats)
                {
                    if(entry.Value.CdLeft<=0 & !_activeProcs.Contains(entry.Key))
                    {
                        _proccedStats = entry.Value.DoEffect(_proccedStats); // this only works for flat additions of stats
                        _activeProcs.Add(entry.Key);
                        if (entry.Value.Stat == "haste")
                        {
                            _abilities["melee"].Cd = _proccedStats["speed"] / (_proccedStats["haste"]*0.01);
                            _abilities["melee"]._currentCd = (_abilities["melee"]._currentCd / _abilities["melee"].Cd) *(_proccedStats["speed"] / _proccedStats["haste"]);
                        }
                        else if (entry.Value.Stat == "mana")
                        {   
                            _mana += entry.Value.DoEffect(new Dictionary<string, double> {{"mana",0}})["mana"]; // kinda stupid
                        } 
                    }else if (!entry.Value.IsActive() & _activeProcs.Contains(entry.Key))
                    {
                        _proccedStats = entry.Value.UndoEffect(_proccedStats); // this only works for flat additions of stats
                        _activeProcs.Remove(entry.Key);
                        if (entry.Value.Stat == "haste")
                        {
                            _abilities["melee"].Cd = _proccedStats["speed"] / (_proccedStats["haste"]*0.01);
                            _abilities["melee"]._currentCd = (_abilities["melee"]._currentCd / _abilities["melee"].Cd) *(_proccedStats["speed"] / _proccedStats["haste"]);
                        } 
                    }
                    entry.Value.ReduceCd(0.01);
                }
                foreach (var entry in _abilities)
                {
                    if (entry.Value._currentCd <= 0)
                    {
                        buttonPressed = true;
                        if (entry.Value.Prio < prio)
                        {
                            toPress = entry.Key;
                            prio = entry.Value.Prio;
                        }
                    }
                }

                if (buttonPressed)
                {
                    if (_mana >= _abilities[toPress].ManaCost && !_onGcd)
                    {
                        {
                            DoCritProc(_abilities[toPress].do_dmg(_proccedStats));
                            _mana -= _abilities[toPress].ManaCost;
                            if (_abilities[toPress].Name != "Melee")
                            {
                                
                                _fiveSecondTimer = 0;
                                _onGcd = true;
                            }
                        }
                    }
                    else if (_abilities[toPress].Name == "Melee")
                    {
                        DoCritProc(_abilities[toPress].do_dmg(_proccedStats));
                        _mana -= _abilities[toPress].ManaCost;
                        
                    }else if (_mana <= _abilities[toPress].ManaCost && !_onGcd)
                    {
                        OomTicks +=1;
                    }
                }

                _mana += _proccedStats["%manaPer3"] / (3 * 0.01)*_maxMana; // this acts continuously and can go over your max mana but that prob wouldnt happend unless you afk
                _fiveSecondTimer += 0.01;
                foreach (var entry in _abilities)
                {
                    entry.Value.ReduceCd(0.01);
                }
                foreach (var entry in Procs)
                {
                    entry.Value.ReduceCd(0.01);
                }
                foreach (var entry in _onHitUseStats)
                {
                    entry.Value.ReduceCd(0.01);
                }
                if (_fiveSecondTimer > 5)
                { 
                    _mana += (5 * (0.001 + Math.Sqrt(_proccedStats["int"]) * _proccedStats["spirit"] * _baseRegen) * 0.6)*0.01; //not sure this is right also conituosuly updates mana not ever 5 sec 
                }
                if (_fiveSecondTimer >= 1.5)
                {
                    _onGcd = false;
                }
                _time -= 0.01;
            }
}