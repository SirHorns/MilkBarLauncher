namespace BOTWM.DedicatedServer;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class AlternateName : Attribute
{
    public string name;

    public AlternateName(string alternateName)
    {
        this.name = alternateName;
    }
}