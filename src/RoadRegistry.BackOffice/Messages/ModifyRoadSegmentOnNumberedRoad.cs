﻿namespace RoadRegistry.BackOffice.Messages;

public class ModifyRoadSegmentOnNumberedRoad
{
    public int AttributeId { get; set; }
    public string Number { get; set; }
    public int SegmentId { get; set; }
    public string Direction { get; set; }
    public int Ordinal { get; set; }
}
