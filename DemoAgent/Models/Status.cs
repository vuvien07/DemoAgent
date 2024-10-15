using System;
using System.Collections.Generic;

namespace Models;

public partial class Status
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    public virtual ICollection<Meeting> Meetings { get; set; } = new List<Meeting>();
}
