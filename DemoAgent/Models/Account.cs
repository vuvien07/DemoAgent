using System;
using System.Collections.Generic;

namespace Models;

public partial class Account
{
    public string? Username { get; set; }

    public string? PrivateKey { get; set; }

    public string? Name { get; set; }

    public int RoleId { get; set; }

    public int Id { get; set; }

    public int? StatusId { get; set; }

    public string? Mail { get; set; }

    public string? PublicKey { get; set; }

    public string? PasswordKey { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual Status? Status { get; set; }
}
