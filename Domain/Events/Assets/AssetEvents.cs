using Domain.Common;
using Domain.Enums;
using System;

namespace Domain.Events.Assets
{
	public sealed record BankAccountOpenedDomainEvent(
		Guid BankAccountId,
		Guid BankId,
		decimal InitialBalance,
		CurrencyCode Currency) : IDomainEvent
	{
		public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
	}

	public sealed record BankAccountOverdrawnDomainEvent(
		Guid BankAccountId,
		decimal CurrentBalance) : IDomainEvent
	{
		public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
	}

	public sealed record CreditCardIssuedDomainEvent(
		Guid CreditCardId,
		Guid BankId,
		decimal CreditLimit,
		CurrencyCode Currency) : IDomainEvent
	{
		public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
	}

	public sealed record CreditCardLimitReachedDomainEvent(
		Guid CreditCardId,
		decimal CreditLimit,
		decimal CurrentBalance) : IDomainEvent
	{
		public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
	}
}
