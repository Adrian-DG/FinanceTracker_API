using Domain.Metadata;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities.Assets
{
	public class Bank : BaseEntity
	{
		public required string Name { get; set; }
		public string? Description { get; set; }
		public string Icon { get; set; } = "default_icon";
		public string ColorHex { get; set; } = "#000000";
		public bool IsSystemDefault { get; set; } = false;
		public virtual ICollection<BankAccount>? BankAccounts { get; set; }
	}
}
