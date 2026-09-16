using Domain.Common;
using Domain.Services;
using System;

namespace Domain.Entities.Liability
{
	/// <summary>
	/// Una cuota del cuadro de amortización. Pertenece al agregado <see cref="Loan"/>:
	/// solo el préstamo la crea y la marca como pagada.
	/// </summary>
	public class LoanAmortizationEntry : AuditableEntity
	{
		private LoanAmortizationEntry() { }

		public int InstallmentNumber { get; private set; }

		public DateOnly ScheduledDate { get; private set; }

		public decimal ScheduledPayment { get; private set; }

		public decimal Principal { get; private set; }

		public decimal Interest { get; private set; }

		/// <summary>Saldo proyectado del préstamo después de pagar esta cuota.</summary>
		public decimal RemainingBalance { get; private set; }

		public bool IsPaid { get; private set; }

		public DateOnly? PaidDate { get; private set; }

		public Guid LoanId { get; private set; }

		public Loan? Loan { get; private set; }

		/// <summary>Se asigna cuando la cuota se salda con un movimiento real.</summary>
		public Guid? LoanPaymentAllocationId { get; private set; }

		public LoanPaymentAllocation? LoanPaymentAllocation { get; private set; }

		internal static LoanAmortizationEntry FromProjection(Guid loanId, AmortizationInstallment installment) => new()
		{
			LoanId = Guard.AgainstEmpty(loanId),
			InstallmentNumber = installment.Number,
			ScheduledDate = installment.ScheduledDate,
			ScheduledPayment = installment.Payment,
			Principal = installment.Principal,
			Interest = installment.Interest,
			RemainingBalance = installment.RemainingBalance
		};

		internal void SettleWith(Guid allocationId, DateOnly paidDate)
		{
			Guard.Against(IsPaid, $"La cuota {InstallmentNumber} ya fue pagada el {PaidDate:dd/MM/yyyy}.");

			IsPaid = true;
			PaidDate = paidDate;
			LoanPaymentAllocationId = allocationId;
			MarkUpdated();
		}

		public bool IsOverdueOn(DateOnly date) => !IsPaid && date > ScheduledDate;
	}
}
