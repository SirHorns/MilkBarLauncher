namespace BOTWM.Library.Settings;

public class QuestSettings
{
    public bool Vanilla;
    public bool Koroks;
    public bool Towers;
    public bool Shrines;
    public bool Locations;
    public bool DivineBeast;

    public bool AnyTrue { 
        get
        {
            return Vanilla || Koroks || Towers || Shrines || Locations || DivineBeast;
        } 
    }
}