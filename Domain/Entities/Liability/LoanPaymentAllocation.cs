using Domain.Entities.Ledger;
using Domain.Enums;
using Domain.Metadata;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities.Liability
{
	public class LoanPaymentAllocation : BaseEntity, ISyncEntity, IAuditableEntity
	{

		public decimal PrincipalPaid { get; set; }
		public decimal InterestPaid { get; set; }
		public bool IsExtraordinary { get; set; }
		public ExtraordinaryPaymentTarget ExtraordinaryPaymentTarget { get; set; } = ExtraordinaryPaymentTarget.NONE;
		public Guid LoanId { get; set; }
		public virtual Loan? Loan { get; set; }
		public Guid TransactionId { get; set; }
		public virtual Transaction? Transaction { get; set; }		

		public SyncStatus SyncStatus { get; set; }
		public DateTime CreateAtUtc { get; set; }
		public DateTime UpdateAtUtc { get; set; }
	
	}
}
