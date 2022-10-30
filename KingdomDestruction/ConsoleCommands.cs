using System.Collections.Generic;
using TaleWorlds.Library;

namespace KingdomDestruction
{
    public class ConsoleCommands
    {
        [CommandLineFunctionality.CommandLineArgumentFunction("reloadconfig", "kingdomdestruction")]
        private static string CommandReloadConfig(List<string> args)
        {
            Config.LoadConfig();
            return "Config reloaded!";
        }
    }
}