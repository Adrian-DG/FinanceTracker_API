using Domain.Common;
using Domain.Enums;
using Domain.Events.Assets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Entities.Assets
{
	/// <summary>
	/// Tarjeta de crédito. Raíz de agregado: contiene sus agrupaciones de consumo y
	/// es la única que puede mover la deuda. <see cref="CurrentBalance"/> es deuda,
	/// no disponible: sube al consumir y baja al pagar.
	/// </summary>
	public class CreditCard : AuditableEntity
	{
		public const int MaxNameLength = 100;

		private readonly List<CreditCardGroup> _groups = new();

		private CreditCard() { }

		public string Name { get; private set; } = string.Empty;

		public string? Alias { get; private set; }

		public CurrencyCode Currency { get; private set; } = CurrencyCode.DOP;

		public decimal CreditLimit { get; private set; }

		/// <summary>Deuda vigente. Un valor negativo representa saldo a favor.</summary>
		public decimal CurrentBalance { get; private set; }

		/// <summary>Día del mes en que cierra el ciclo de facturación.</summary>
		public int CutOffDay { get; private set; }

		/// <summary>Día del mes en que vence el pago del ciclo cerrado.</summary>
		public int DueDay { get; private set; }

		public DateOnly ExpirationDate { get; private set; }

		public Guid BankId { get; private set; }

		public Bank? Bank { get; private set; }

		public IReadOnlyCollection<CreditCardGroup> Groups => _groups.AsReadOnly();

		public decimal AvailableCredit => CreditLimit - CurrentBalance;

		public static CreditCard Issue(
			string name,
			decimal creditLimit,
			CurrencyCode currency,
			int cutOffDay,
			int dueDay,
			DateOnly expirationDate,
			Guid bankId,
			string? alias = null,
			decimal initialBalance = 0m)
		{
			var card = new CreditCard
			{
				Name = Guard.AgainstExceedingLength(Guard.AgainstNullOrWhiteSpace(name), MaxNameLength),
				Alias = string.IsNullOrWhiteSpace(alias) ? null : alias.Trim(),
				CreditLimit = Guard.AgainstNonPositive(creditLimit),
				Currency = currency,
				CutOffDay = Guard.AgainstOutOfRange(cutOffDay, 1, 28),
				DueDay = Guard.AgainstOutOfRange(dueDay, 1, 28),
				ExpirationDate = expirationDate,
				BankId = Guard.AgainstEmpty(bankId),
				CurrentBalance = Guard.AgainstNegative(initialBalance)
			};

			Guard.Against(
				initialBalance > creditLimit,
				$"La deuda inicial ({initialBalance:N2}) no puede superar el límite de crédito ({creditLimit:N2}).");

			card.Raise(new CreditCardIssuedDomainEvent(card.Id, card.BankId, card.CreditLimit, card.Currency));

			return card;
		}

		public CreditCardGroup AddGroup(string name, CurrencyCode currency, string? icon = null, string? colorHex = null)
		{
			Guard.Against(
				currency != Currency,
				$"La agrupación debe usar la moneda de la tarjeta ({Currency}).");

			var normalized = Guard.AgainstNullOrWhiteSpace(name).ToUpperInvariant();

			Guard.Against(
				_groups.Any(x => !x.IsDeleted && x.Name.ToUpperInvariant() == normalized),
				$"La tarjeta '{Name}' ya tiene una agrupación llamada '{name}'.");

			var group = CreditCardGroup.Create(Id, name, currency, icon, colorHex);
			_groups.Add(group);
			MarkUpdated();

			return group;
		}

		/// <summary>Registra un consumo. Valida el crédito disponible y la vigencia de la tarjeta.</summary>
		public void Charge(decimal amount, DateOnly onDate, Guid? groupId = null)
		{
			Guard.AgainstNonPositive(amount);

			Guard.Against(
				IsExpiredOn(onDate),
				$"La tarjeta '{Name}' venció el {ExpirationDate:dd/MM/yyyy} y no admite consumos.");

			Guard.Against(
				amount > AvailableCredit,
				$"El consumo de {amount:N2} supera el crédito disponible de la tarjeta '{Name}' ({AvailableCredit:N2}).");

			CurrentBalance += amount;
			ResolveGroup(groupId)?.Charge(amount);
			MarkUpdated();

			if (AvailableCredit == 0)
				Raise(new CreditCardLimitReachedDomainEvent(Id, CreditLimit, CurrentBalance));
		}

		/// <summary>Aplica un pago a la deuda. El excedente queda como saldo a favor.</summary>
		public void RegisterPayment(decimal amount, Guid? groupId = null)
		{
			Guard.AgainstNonPositive(amount);

			CurrentBalance -= amount;
			ResolveGroup(groupId)?.Relieve(amount);
			MarkUpdated();
		}

		public void AdjustCreditLimit(decimal newLimit)
		{
			Guard.AgainstNonPositive(newLimit);

			Guard.Against(
				newLimit < CurrentBalance,
				$"El nuevo límite ({newLimit:N2}) no puede ser menor que la deuda actual ({CurrentBalance:N2}).");

			CreditLimit = newLimit;
			MarkUpdated();
		}

		public void UpdateBillingCycle(int cutOffDay, int dueDay)
		{
			CutOffDay = Guard.AgainstOutOfRange(cutOffDay, 1, 28);
			DueDay = Guard.AgainstOutOfRange(dueDay, 1, 28);
			MarkUpdated();
		}

		public void Renew(DateOnly newExpirationDate)
		{
			Guard.Against(
				newExpirationDate <= ExpirationDate,
				"La nueva fecha de vencimiento debe ser posterior a la actual.");

			ExpirationDate = newExpirationDate;
			MarkUpdated();
		}

		public bool IsExpiredOn(DateOnly date) => date > ExpirationDate;

		/// <summary>
		/// Fecha límite de pago del consumo indicado: el ciclo cierra en el próximo
		/// <see cref="CutOffDay"/> y el pago vence en el <see cref="DueDay"/> siguiente al corte.
		/// </summary>
		public DateOnly StatementDueDateFor(DateOnly transactionDate)
		{
			var cutOff = OnDay(transactionDate, CutOffDay);

			if (transactionDate > cutOff)
				cutOff = OnDay(transactionDate.AddMonths(1), CutOffDay);

			var dueDate = OnDay(cutOff, DueDay);

			if (dueDate <= cutOff)
				dueDate = OnDay(cutOff.AddMonths(1), DueDay);

			return dueDate;
		}

		public override void Delete()
		{
			Guard.Against(
				CurrentBalance > 0,
				$"No se puede eliminar la tarjeta '{Name}' porque mantiene una deuda de {CurrentBalance:N2}.");

			base.Delete();
		}

		private CreditCardGroup? ResolveGroup(Guid? groupId)
		{
			if (groupId is null)
				return null;

			return _groups.SingleOrDefault(x => x.Id == groupId.Value && !x.IsDeleted)
				?? throw new DomainException($"La agrupación '{groupId}' no pertenece a la tarjeta '{Name}'.");
		}

		private static DateOnly OnDay(DateOnly reference, int day)
		{
			var daysInMonth = DateTime.DaysInMonth(reference.Year, reference.Month);

			return new DateOnly(reference.Year, reference.Month, Math.Min(day, daysInMonth));
		}
	}
}
