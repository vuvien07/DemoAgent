using System;
using System.Collections.Generic;

namespace Models;

public partial class Meeting
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime TimeStart { get; set; }

    public DateTime TimeEnd { get; set; }

    public int StatusId { get; set; }

    public string? Creator { get; set; }

    public virtual Status Status { get; set; } = null!;
}
