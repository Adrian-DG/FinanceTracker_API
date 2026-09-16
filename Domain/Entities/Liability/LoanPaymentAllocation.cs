using Domain.Common;
using Domain.Entities.Ledger;
using Domain.Enums;
using System;

namespace Domain.Entities.Liability
{
	/// <summary>
	/// Desglose de un pago real: cuánto del movimiento fue a capital y cuánto a interés.
	/// Es el puente entre el libro mayor (<see cref="Transaction"/>) y el préstamo.
	/// </summary>
	public class LoanPaymentAllocation : AuditableEntity
	{
		private LoanPaymentAllocation() { }

		public decimal PrincipalPaid { get; private set; }

		public decimal InterestPaid { get; private set; }

		public DateOnly PaymentDate { get; private set; }

		public bool IsExtraordinary { get; private set; }

		public ExtraordinaryPaymentTarget ExtraordinaryPaymentTarget { get; private set; } = ExtraordinaryPaymentTarget.NONE;

		public Guid LoanId { get; private set; }

		public Loan? Loan { get; private set; }

		public Guid TransactionId { get; private set; }

		public Transaction? Transaction { get; private set; }

		public decimal TotalPaid => PrincipalPaid + InterestPaid;

		internal static LoanPaymentAllocation Create(
			Guid loanId,
			Guid transactionId,
			decimal principalPaid,
			decimal interestPaid,
			DateOnly paymentDate,
			bool isExtraordinary = false,
			ExtraordinaryPaymentTarget target = ExtraordinaryPaymentTarget.NONE) => new()
			{
				LoanId = Guard.AgainstEmpty(loanId),
				TransactionId = Guard.AgainstEmpty(transactionId),
				PrincipalPaid = Guard.AgainstNegative(principalPaid),
				InterestPaid = Guard.AgainstNegative(interestPaid),
				PaymentDate = paymentDate,
				IsExtraordinary = isExtraordinary,
				ExtraordinaryPaymentTarget = target
			};
	}
}
