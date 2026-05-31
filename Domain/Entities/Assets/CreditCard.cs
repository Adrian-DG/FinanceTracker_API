using Domain.Enums;
using Domain.Metadata;
using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;

namespace Domain.Entities.Assets
{
	public class CreditCard : BaseEntity, ISyncEntity, IAuditableEntity
	{
		public string? Name { get; set; }
		public string? Alias { get; set; }
		public decimal CreditLimit { get; set; }
		public decimal CurrentBalance { get; set; }

		public int CutOfDay { get; set; }
		public int DueDay { get; set; }
		public DateOnly ExpirationDate { get; set; }

		public Guid BankId { get; set; }
		public virtual Bank? Bank { get; set; }

		public SyncStatus SyncStatus { get; set; }
		public DateTime CreateAtUtc { get; set; }
		public DateTime UpdateAtUtc { get; set; }

		public virtual ICollection<CreditCardGroup>? CardGroups { get; set; }
		public virtual ICollection<Transaction>? Transactions { get; set; }

	}
}
