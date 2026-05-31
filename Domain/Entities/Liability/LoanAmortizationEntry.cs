using Domain.Enums;
using Domain.Metadata;
using System;

namespace Domain.Entities.Liability
{
	public class LoanAmortizationEntry : BaseEntity, ISyncEntity, IAuditableEntity
	{
		public int InstallmentNumber { get; set; }
		public DateOnly ScheduledDate { get; set; }

		public decimal ScheduledPayment { get; set; }
		public decimal Principal { get; set; }
		public decimal Interest { get; set; }
		public decimal RemainingBalance { get; set; }

		public bool IsPaid { get; set; }
		public DateOnly? PaidDate { get; set; }

		public Guid LoanId { get; set; }
		public virtual Loan? Loan { get; set; }

		// Set when the installment is settled via a real transaction
		public Guid? LoanPaymentAllocationId { get; set; }
		public virtual LoanPaymentAllocation? LoanPaymentAllocation { get; set; }

		public SyncStatus SyncStatus { get; set; }
		public DateTime CreateAtUtc { get; set; }
		public DateTime UpdateAtUtc { get; set; }
	}
}
