using System;
using GUIApp.DiscordGameSDK;

namespace GUIApp.Classes
{
    public class discordClass
    {

        Discord discord = new Discord(946199926847705088, (UInt64)CreateFlags.Default);

        public discordClass()
        {
            var activityManager = discord.GetActivityManager();

            var activity = new Activity
            {
                State = "Exclusive version :eyes: ",
                Assets =
                {
                    LargeImage = "logo",
                }
            };

            activityManager.UpdateActivity(activity, (res) =>
            {
                
            });

        }
        
        public void update()
        {
            discord.RunCallbacks();
        }

    }
}
