using Domain.Enums;
using Domain.Metadata;
using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;

namespace Domain.Entities.Assets
{
	public class Category : BaseEntity, ISyncEntity, IAuditableEntity
	{
		public required string Name { get; set; }
		public string? Description { get; set; }
		public string Icon { get; set; } = "default_icon";
		public string ColorHex { get; set; } = "#000000";
		public bool IsSystemDefault { get; set; } = false;
		public virtual ICollection<Transaction>? Transactions { get; set; }

		public SyncStatus SyncStatus { get; set; }
		public DateTime CreateAtUtc { get; set; }
		public DateTime UpdateAtUtc { get; set; }
	}
}
