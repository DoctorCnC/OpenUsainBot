using System;

namespace OpenCryptShot
{
  public static class Utilities
  {
    public static void Write(ConsoleColor color, string msg)
    {
      Console.ForegroundColor = color;
      Console.WriteLine($"\r[{getTime()}] - {msg}");
    }

    public static void tag(){
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.WriteLine(@"
        ________                             ____ ___                 .__         __________           __   
        \_____  \  ______    ____    ____   |    |   \  ___________   |__|  ____  \______   \  ____  _/  |_ 
        /   |   \ \____ \ _/ __ \  /    \  |    |   / /  ___/\__  \  |  | /    \  |    |  _/ /  _ \ \   __\
        /    |    \|  |_> >\  ___/ |   |  \ |    |  /  \___ \  / __ \_|  ||   |  \ |    |   \(  <_> ) |  |  
        \_______  /|   __/  \___  >|___|  / |______/  /____  >(____  /|__||___|  / |______  / \____/  |__|  
                \/ |__|         \/      \/                 \/      \/          \/         \/                ");    
    }

        public static string getTime(){
            return DateTime.Now.TimeOfDay.ToString().Substring(0,12);
        }
  }
}
