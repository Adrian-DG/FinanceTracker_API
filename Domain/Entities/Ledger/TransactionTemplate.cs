using Domain.Common;
using Domain.Enums;
using System;

namespace Domain.Entities.Ledger
{
	/// <summary>
	/// Plantilla de un movimiento recurrente (nómina, alquiler, suscripciones).
	/// No es un asiento: no afecta saldos hasta que <see cref="Materialize"/> genera
	/// la transacción real del período. Por eso es una entidad propia y no una
	/// especialización de <see cref="Transaction"/>.
	/// </summary>
	public class TransactionTemplate : AuditableEntity
	{
		public const int MaxDescriptionLength = 250;

		private TransactionTemplate() { }

		public decimal Amount { get; private set; }

		public TransactionType Type { get; private set; }

		public Frequency Frequency { get; private set; }

		public string? Description { get; private set; }

		public DateOnly StartDate { get; private set; }

		/// <summary>Fecha en que corresponde generar el próximo movimiento.</summary>
		public DateOnly NextOccurrence { get; private set; }

		public DateOnly? EndDate { get; private set; }

		public DateOnly? LastExecutedDate { get; private set; }

		public bool IsActive { get; private set; } = true;

		public Guid? CategoryId { get; private set; }

		public Guid? SubCategoryId { get; private set; }

		public Guid? BankAccountId { get; private set; }

		public Guid? DestinationBankAccountId { get; private set; }

		public Guid? CreditCardId { get; private set; }

		public Guid? CreditCardGroupId { get; private set; }

		public static TransactionTemplate Create(
			TransactionType type,
			decimal amount,
			Frequency frequency,
			DateOnly startDate,
			Guid? categoryId = null,
			Guid? subCategoryId = null,
			Guid? bankAccountId = null,
			Guid? destinationBankAccountId = null,
			Guid? creditCardId = null,
			Guid? creditCardGroupId = null,
			DateOnly? endDate = null,
			string? description = null)
		{
			var template = new TransactionTemplate
			{
				Type = type,
				Amount = Guard.AgainstNonPositive(amount),
				Frequency = frequency,
				StartDate = startDate,
				NextOccurrence = startDate,
				EndDate = endDate,
				Description = NormalizeDescription(description),
				CategoryId = categoryId,
				SubCategoryId = subCategoryId,
				BankAccountId = bankAccountId,
				DestinationBankAccountId = destinationBankAccountId,
				CreditCardId = creditCardId,
				CreditCardGroupId = creditCardGroupId
			};

			Guard.Against(
				endDate.HasValue && endDate.Value < startDate,
				"La fecha de fin de la recurrencia no puede ser anterior a la de inicio.");

			template.EnsureRequiredRelations();

			return template;
		}

		public bool IsDueOn(DateOnly date)
			=> IsActive && !IsDeleted && date >= NextOccurrence && (EndDate is null || NextOccurrence <= EndDate.Value);

		/// <summary>
		/// Genera el movimiento del período vigente y adelanta el calendario.
		/// El proceso que consume esta plantilla es el responsable de aplicar el
		/// movimiento resultante sobre la cuenta o la tarjeta.
		/// </summary>
		public Transaction Materialize()
		{
			Guard.Against(!IsActive, "La plantilla está inactiva y no genera movimientos.");
			Guard.Against(
				EndDate.HasValue && NextOccurrence > EndDate.Value,
				"La recurrencia ya superó su fecha de fin.");

			var occurredOn = NextOccurrence.ToDateTime(TimeOnly.MinValue);

			var transaction = Type switch
			{
				TransactionType.INCOME => Transaction.RegisterIncome(
					Amount, occurredOn, BankAccountId!.Value, CategoryId!.Value, SubCategoryId, Description, Id),

				TransactionType.EXPENSE when CreditCardId.HasValue => Transaction.RegisterExpenseOnCreditCard(
					Amount, occurredOn, CreditCardId.Value, CategoryId!.Value, CreditCardGroupId, SubCategoryId, Description, Id),

				TransactionType.EXPENSE => Transaction.RegisterExpenseFromAccount(
					Amount, occurredOn, BankAccountId!.Value, CategoryId!.Value, SubCategoryId, Description, Id),

				TransactionType.TRANSFER => Transaction.RegisterTransfer(
					Amount, occurredOn, BankAccountId!.Value, DestinationBankAccountId!.Value, Description, Id),

				TransactionType.CREDIT_CARD_PAYMENT => Transaction.RegisterCreditCardPayment(
					Amount, occurredOn, BankAccountId!.Value, CreditCardId!.Value, CreditCardGroupId, Description, Id),

				TransactionType.LOAN_PAYMENT => Transaction.RegisterLoanPayment(
					Amount, occurredOn, BankAccountId!.Value, Description, Id),

				_ => throw new DomainException($"El tipo '{Type}' no se puede automatizar.")
			};

			LastExecutedDate = NextOccurrence;
			AdvanceOccurrence();
			MarkUpdated();

			return transaction;
		}

		public void UpdateAmount(decimal amount)
		{
			Amount = Guard.AgainstNonPositive(amount);
			MarkUpdated();
		}

		public void Pause()
		{
			IsActive = false;
			MarkUpdated();
		}

		public void Resume()
		{
			Guard.Against(
				EndDate.HasValue && NextOccurrence > EndDate.Value,
				"No se puede reactivar una recurrencia que ya superó su fecha de fin.");

			IsActive = true;
			MarkUpdated();
		}

		private void AdvanceOccurrence()
		{
			if (Frequency == Frequency.ONCE)
			{
				IsActive = false;

				return;
			}

			NextOccurrence = Frequency switch
			{
				Frequency.DAILY => NextOccurrence.AddDays(1),
				Frequency.WEEKLY => NextOccurrence.AddDays(7),
				Frequency.BIWEEKLY => NextOccurrence.AddDays(14),
				Frequency.MONTHLY => NextOccurrence.AddMonths(1),
				Frequency.YEARLY => NextOccurrence.AddYears(1),
				_ => throw new DomainException($"La frecuencia '{Frequency}' no tiene un intervalo definido.")
			};

			if (EndDate.HasValue && NextOccurrence > EndDate.Value)
				IsActive = false;
		}

		/// <summary>Cada tipo exige su propio juego de relaciones; sin ellas la plantilla no podría materializarse.</summary>
		private void EnsureRequiredRelations()
		{
			var needsCategory = Type is TransactionType.INCOME or TransactionType.EXPENSE;

			Guard.Against(
				needsCategory && (CategoryId is null || CategoryId == Guid.Empty),
				$"Una plantilla de tipo {Type} requiere una categoría.");

			var needsCard = Type == TransactionType.CREDIT_CARD_PAYMENT;

			Guard.Against(
				needsCard && (CreditCardId is null || CreditCardId == Guid.Empty),
				"Una plantilla de pago de tarjeta requiere la tarjeta a pagar.");

			var usesCard = Type == TransactionType.EXPENSE && CreditCardId.HasValue;

			Guard.Against(
				!usesCard && (BankAccountId is null || BankAccountId == Guid.Empty),
				$"Una plantilla de tipo {Type} requiere una cuenta bancaria.");

			Guard.Against(
				Type == TransactionType.TRANSFER && (DestinationBankAccountId is null || DestinationBankAccountId == Guid.Empty),
				"Una plantilla de transferencia requiere la cuenta de destino.");
		}

		private static string? NormalizeDescription(string? description)
		{
			if (string.IsNullOrWhiteSpace(description))
				return null;

			return Guard.AgainstExceedingLength(description.Trim(), MaxDescriptionLength, nameof(Description));
		}
	}
}
