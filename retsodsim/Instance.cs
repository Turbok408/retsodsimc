namespace retsodsim;

public class Instance
{
 
    private double _time;
    private Dictionary<string, double> _stats;
    private Dictionary<string, Ability> _abilities;
    
    public Instance(Dictionary<string, Ability> abilities, Dictionary<string, double> stats, double time)
    {
        _abilities = abilities;
        _stats = stats;
        _time = time;

    }
    public Dictionary<string,List<double>> Output()
    { 
        Dictionary<string,List<double>> dmg = new Dictionary<string, List<double>>();
        foreach (var entry in _abilities)
        {
            dmg.Add(entry.Value.Name,[(long)entry.Value._attacks,(long)entry.Value.AbilityDmgTotal]);
        }
        return dmg;
    }
    public void RunInstance()
    {
        //int threadIndex = (int)threadContext;
        while (_time > 0)
        {
            Iterate();
        }
        foreach (var entry in _abilities)
        {
            entry.Value.Reset();
        }
        _time = 120;
        //Console.WriteLine($"Thread {threadIndex} result calculated...");
    }
    private void Iterate()
            {
                double prio = 100000000;
                string? toPress = null;
                bool buttonPressed = false;
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
                    _abilities[toPress].do_dmg(_stats);
                    if (_abilities[toPress].Name != "Melee")
                    {
                        _time -= 1.5;
                        foreach (var entry in _abilities)
                        {
                            entry.Value.ReduceCd(1.5);
                            if (entry.Value.Name == "Melee" && entry.Value._currentCd <= 0)
                            {
                                if (_time >= 0)
                                {
                                    var cdOffset = entry.Value._currentCd;
                                    entry.Value.do_dmg(_stats);
                                    entry.Value._currentCd = entry.Value.Cd + cdOffset;
                                }
                            }
                        }
                        
                        
                    }
                }
                else
                {
                    _time -= 0.01;
                    foreach (var entry in _abilities)
                    {
                        entry.Value.ReduceCd(0.01);
                    }
                }
            }
}