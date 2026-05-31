using Domain.Entities.Assets;
using Domain.Enums;
using Domain.Metadata;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities.Liability
{
	public class LoanRateHistory : BaseEntity, ISyncEntity, IAuditableEntity
	{
		public decimal Rate { get; set; }
		public DateOnly EffectiveDate { get; set; }
		public string? Note { get; set; }

		public Guid LoanId { get; set; }
		public virtual Loan? Loan { get; set; }

		public Guid BankId { get; set; }
		public virtual Bank? Bank { get; set; }

		public SyncStatus SyncStatus { get; set; }
		public DateTime CreateAtUtc { get; set; }
		public DateTime UpdateAtUtc { get; set; }
	}
}
