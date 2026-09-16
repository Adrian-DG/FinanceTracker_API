using Domain.Common;
using Domain.Enums;
using System;

namespace Domain.Events.Liability
{
	public sealed record LoanDisbursedDomainEvent(
		Guid LoanId,
		Guid BankId,
		decimal Principal,
		decimal MonthlyFee,
		int TermMonths) : IDomainEvent
	{
		public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
	}

	public sealed record LoanInstallmentPaidDomainEvent(
		Guid LoanId,
		int InstallmentNumber,
		decimal PrincipalPaid,
		decimal InterestPaid,
		decimal RemainingAmount) : IDomainEvent
	{
		public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
	}

	public sealed record ExtraordinaryPaymentAppliedDomainEvent(
		Guid LoanId,
		decimal Amount,
		ExtraordinaryPaymentTarget Target,
		ExtraordinaryRecalculationStrategy Strategy,
		decimal RemainingAmount,
		decimal NewMonthlyFee,
		int RemainingInstallments) : IDomainEvent
	{
		public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
	}

	public sealed record LoanRateChangedDomainEvent(
		Guid LoanId,
		decimal PreviousRate,
		decimal NewRate,
		decimal NewMonthlyFee) : IDomainEvent
	{
		public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
	}

	public sealed record LoanSettledDomainEvent(Guid LoanId) : IDomainEvent
	{
		public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
	}
}
