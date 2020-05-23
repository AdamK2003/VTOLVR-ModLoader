/*
This class is used for saving the data collected in DataCollector class
 */
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ModLoader.Classes
{
    public class CampaignsScenarios
    {
        public CampaignSave[] Campaigns { get; set; }
        public CampaignsScenarios(List<VTCampaignInfo> list)
        {
            Campaigns = new CampaignSave[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                List<VTScenarioInfo> scenarios = VTResources.GetBuiltInCampaign(list[i].campaignID).allScenarios;
                ScenarioSave[] scenariosSave = new ScenarioSave[scenarios.Count];

                for (int s = 0; s < scenarios.Count; s++)
                {
                    scenariosSave[s] = new ScenarioSave(
                        scenarios[s].id,
                        scenarios[s].name,
                        scenarios[s].description,
                        scenarios[s].campaignOrderIdx);
                }

                Campaigns[i] = new CampaignSave(
                    list[i].campaignID,
                    list[i].campaignName,
                    list[i].description,
                    list[i].vehicle,
                    scenariosSave);
            }
        }
    }
    public class CampaignSave
    {
        public string CampaignID { get; set; }
        public string CampaignName { get; set; }
        public string Description { get; set; }
        public string Vehicle { get; set; }
        public ScenarioSave[] Scenarios { get; set; }

        public CampaignSave(string campaignID, string campaignName, string description, string vehicle, ScenarioSave[] scenarios)
        {
            CampaignID = campaignID;
            CampaignName = campaignName;
            Description = description;
            Vehicle = vehicle;
            Scenarios = scenarios;
        }
    }
    public class ScenarioSave
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int CampaignOrderIdx { get; set; }

        public ScenarioSave(string id, string name, string description, int campaignOrderIdx)
        {
            Id = id;
            Name = name;
            Description = description;
            CampaignOrderIdx = campaignOrderIdx;
        }
    }
}
