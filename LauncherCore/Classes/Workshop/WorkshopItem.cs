using LauncherCore.Views;

namespace LauncherCore.Classes.Workshop
{
    class WorkshopItem
    {
        public string ScenarioName = string.Empty;
        public string ScenarioID = string.Empty;

        public WorkshopItem(string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("scenarioName"))
                {
                    ScenarioName = lines[i].Remove(0, 16);
                }
                else if (lines[i].Contains("scenarioID"))
                {
                    ScenarioID = lines[i].Remove(0, 14);
                }

                if (ScenarioID != string.Empty && ScenarioName != string.Empty)
                    break;
            }

            Console.Log(this.ToString());
        }

        public override string ToString()
        {
            return $"ScenarioName = {ScenarioName}, ScenarioID = {ScenarioID}";
        }
    }
}