using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Core.Enums;

namespace Core.Jsons
{
    public class Scenario
    {
        [JsonProperty("pilot")] public string Pilot = string.Empty;
        [JsonProperty("scenario_name")] public string ScenarioName { get; set; } = string.Empty;
        [JsonProperty("scenario_id")] public string ScenarioID = string.Empty;
        [JsonProperty("campaign_id")] public string CampaignID = string.Empty;
        [JsonProperty("is_custom")] public bool IsCustom;
        [JsonProperty("is_workshop")] public bool IsWorkshop;

        public override string ToString()
        {
            return $"pilot={Pilot},scenario_name={ScenarioName},scenario_id={ScenarioID}," +
                   $"campaign_id={CampaignID},is_custom={IsCustom},is_workshop={IsWorkshop}";
        }

        public override bool Equals(object obj)
        {
            Scenario scenario = (Scenario)obj;

            if (scenario == null)
                return false;

            return
                scenario.ScenarioName == ScenarioName &&
                scenario.ScenarioID == ScenarioID &&
                scenario.CampaignID == CampaignID &&
                scenario.IsCustom == IsCustom &&
                scenario.IsWorkshop == IsWorkshop;
        }
    }
}