﻿using MaterialDesignColors;

namespace QBTracker.Model
{
    public class Settings
    {
        public int Id { get; set; }
        public string ExportFolder { get; set; }
        public string ExportFileName { get; set; }
        public RoundingInterval RoundingInterval { get; set; } = RoundingInterval.NoRounding;
        public RoundingType RoundingType { get; set; } = RoundingType.MidPointRounding;
        public GroupingType GroupingType { get; set; } = GroupingType.NoGrouping;
        public bool? IsDark { get; set; }
        public PrimaryColor? PrimaryColor { get; set; }
        public SecondaryColor? SecondaryColor { get; set; }
        public bool StartWithWindows { get; set; }
    }

    public enum RoundingInterval
    {
        NoRounding = 0,
        RoundTo15Min,
        RoundTo30Min,
    }

    public enum RoundingType
    {
        MidPointRounding = 0,
        CeilingRounding
    }

    public enum GroupingType
    {
        NoGrouping = 0,
        GroupBeforeRound,
        GroupAfterRound,
    }
}
