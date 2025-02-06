using System.Reflection;

namespace BOTWM.DedicatedServer;

public class Command
{

    public MethodInfo Method;
    public string Name;
    public string Description;
    public List<string> AlternateNames = new List<string>();
    public List<string> LowerAlternateNames = new List<string>();

    public Command(MethodInfo method, string name, string description, List<string> alternateNames)
    {
        Method = method;
        Name = name;
        AlternateNames = alternateNames;
        Description = description;

        foreach(string altName in AlternateNames)
        {
            LowerAlternateNames.Add(altName.ToLower());
        }
    }
}