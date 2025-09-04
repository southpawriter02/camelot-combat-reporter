using CamelotCombatReporter.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CamelotCombatReporter.Core.Analysis
{
    public class Fight
    {
        public List<LogEvent> Events { get; } = new List<LogEvent>();

        public int TotalDamage => Events.OfType<DamageEvent>().Sum(e => e.DamageAmount);
        public int TotalHealing => Events.OfType<HealingEvent>().Sum(e => e.HealingAmount);
        public TimeSpan Duration => Events.Any() ? Events.Last().Timestamp - Events.First().Timestamp : TimeSpan.Zero;
        public double Dps => Duration.TotalSeconds > 0 ? TotalDamage / Duration.TotalSeconds : 0;
        public double Hps => Duration.TotalSeconds > 0 ? TotalHealing / Duration.TotalSeconds : 0;
    }
}
