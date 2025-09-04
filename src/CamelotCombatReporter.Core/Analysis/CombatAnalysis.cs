using CamelotCombatReporter.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CamelotCombatReporter.Core.Analysis
{
    public class CombatAnalysis
    {
        private readonly IEnumerable<LogEvent> _events;
        private static readonly TimeSpan FightInactivityThreshold = TimeSpan.FromSeconds(10);

        public CombatAnalysis(IEnumerable<LogEvent> events)
        {
            _events = events.OrderBy(e => e.Timestamp);
        }

        public List<Fight> Analyze()
        {
            var fights = new List<Fight>();
            if (!_events.Any())
            {
                return fights;
            }

            var currentFight = new Fight();
            fights.Add(currentFight);

            LogEvent lastEvent = null;

            foreach (var currentEvent in _events)
            {
                if (lastEvent != null)
                {
                    var timeSinceLastEvent = currentEvent.Timestamp - lastEvent.Timestamp;
                    if (timeSinceLastEvent > FightInactivityThreshold)
                    {
                        currentFight = new Fight();
                        fights.Add(currentFight);
                    }
                }

                currentFight.Events.Add(currentEvent);
                lastEvent = currentEvent;
            }

            return fights;
        }
    }
}
