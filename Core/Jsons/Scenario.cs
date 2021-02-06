using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;
using Core.Enums;

namespace Core.Jsons
{
    public class Scenario
    {
        [JsonProperty("pilot")] public string Pilot;
        [JsonProperty("scenario_name")] public string ScenarioName;
        [JsonProperty("scenario_id")] public string ScenarioID;
        [JsonProperty("campaign_id")] public string CampaignID;
        [JsonProperty("is_custom")] public bool IsCustom;
        [JsonProperty("is_workshop")] public bool IsWorkshop;

        public override string ToString()
        {
            return $"pilot={Pilot},scenario_name={ScenarioName},scenario_id={ScenarioID}," +
                $"campaign_id={CampaignID},is_custom={IsCustom},is_workshop={IsWorkshop}";
        }
    }
}
