﻿using System.Collections.Generic;
using UnityEngine;

using DecentM.EditorTools;
using DecentM.Metrics.Plugins;
using DecentM.VideoRatelimit;

namespace DecentM.Metrics
{
    public class MetricsAutoFixer : AutoSceneFixer
    {
        private void FixRatelimits()
        {
            List<VideoRatelimitSystem> ratelimits =
                ComponentCollector<VideoRatelimitSystem>.CollectFromActiveScene();

            if (ratelimits.Count <= 0)
                return;

            VideoRatelimitSystem ratelimit = ratelimits[0];

            if (ratelimit == null)
                return;

            List<MetricsSystem> metricsSystems =
                ComponentCollector<MetricsSystem>.CollectFromActiveScene();

            foreach (MetricsSystem metricsSystem in metricsSystems)
            {
                metricsSystem.ratelimit = ratelimit;

                Inspector.SaveModifications(metricsSystem);
            }
        }

        protected override bool OnPerformFixes()
        {
            this.FixRatelimits();

            List<MetricsUI> uis = ComponentCollector<MetricsUI>.CollectFromActiveScene();

            // Metrics not being installed isn't an error, it just means the user doesn't want to collect metrics from this scene
            if (uis.Count == 0)
                return true;

            if (uis.Count > 1)
            {
                Debug.LogError(
                    $"{uis.Count} metrics systems detected, you must have only a single one. Please delete extra metrics prefabs and try again."
                );
                return false;
            }

            MetricsUI ui = uis[0];

            #region Individual plugins

            List<IndividualTrackingPlugin> plugins =
                ComponentCollector<IndividualTrackingPlugin>.CollectFromActiveScene();

            foreach (IndividualTrackingPlugin plugin in plugins)
            {
                PluginRequirements requirements = PluginManager.GetRequirements(ui);
                plugin.system = requirements.system;
                plugin.events = requirements.events;
                plugin.urlStore = requirements.urlStore;
                plugin.pubsubHosts = new Pubsub.PubsubHost[] { requirements.events };

                Inspector.SaveModifications(plugin);
            }

            #endregion

            #region Performance metrics

            List<PerformanceGovernor> governors =
                ComponentCollector<PerformanceGovernor>.CollectFromActiveScene();
            PerformanceGovernor governor = null;

            if (governors.Count == 0)
                Debug.LogWarning(
                    "[DecentM.Metrics] Reporting performance metrics requires a PerformanceGovernor to be present in your scene somewhere."
                );
            else
                governor = governors[0];

            if (governor == null)
            {
                Debug.LogError(
                    "[DecentM.Metrics] Internal error: Unable to find a performance governor, but the list is not empty."
                );
                return false;
            }

            HeartbeatPlugin heartbeatPlugin = ui.GetComponentInChildren<HeartbeatPlugin>();
            if (heartbeatPlugin != null)
            {
                heartbeatPlugin.performanceGovernor = governor;
                Inspector.SaveModifications(heartbeatPlugin);
            }

            Inspector.SaveModifications(ui);

            #endregion

            #region URLStore

            URLStore urlStore = ui.GetComponentInChildren<URLStore>();

            if (urlStore == null)
            {
                Debug.LogError(
                    $"[DecentM.Metrics] Could not find a URLStore under {ui.name}. Please repair the prefab by resetting it, or by deleting and replacing it."
                );
                return false;
            }

            if (urlStore.urls == null)
            {
                urlStore.urls = new object[][] { };
                Inspector.SaveModifications(ui);
            }

            #endregion

            #region Custom Event Plugin

            List<CustomEventTrackingPlugin> ctPlugins =
                ComponentCollector<CustomEventTrackingPlugin>.CollectFromActiveScene();

            foreach (CustomEventTrackingPlugin plugin in ctPlugins)
            {
                if (string.IsNullOrEmpty(plugin.metricName))
                    plugin.metricName = RandomStringGenerator.GenerateRandomString(8);

                Inspector.SaveModifications(plugin);
            }

            #endregion

            return true;
        }
    }
}