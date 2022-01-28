using System;
using System.Collections.Generic;

namespace QBTracker.Model;

public class TimeAggregate
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public TimeSpan AggregateTime { get; set; }
    public TimeSpan[] DayAggregate { get; set; } = new TimeSpan[31];

}