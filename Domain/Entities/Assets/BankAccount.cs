using Domain.Enums;
using Domain.Metadata;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities.Assets
{
	public class BankAccount : BaseEntity, ISyncEntity, IAuditableEntity
	{
		public required string  Name { get; set; }
		public AccountType Type { get; set; }
		public decimal CurrentBalance { get; set; }
		public CurrencyCode Currency { get; set; } = CurrencyCode.DOP;

		public Guid BankId { get; set; }
		public virtual Bank? Bank { get; set; }

		public SyncStatus SyncStatus {  get; set; }	
		public DateTime CreateAtUtc { get; set; }
		public DateTime UpdateAtUtc { get; set; }

		
	}
}
