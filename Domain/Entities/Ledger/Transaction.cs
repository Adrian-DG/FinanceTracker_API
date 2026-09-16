using Domain.Common;
using Domain.Entities.Assets;
using Domain.Enums;
using Domain.Events.Ledger;
using System;

namespace Domain.Entities.Ledger
{
	/// <summary>
	/// Asiento del libro mayor. Es inmutable en lo esencial: el monto, el tipo y las
	/// cuentas involucradas se fijan al crearlo mediante la fábrica correspondiente a
	/// cada <see cref="TransactionType"/>, que es la que garantiza qué relaciones deben
	/// venir llenas. Corregir un movimiento significa anularlo y registrar otro.
	/// </summary>
	public class Transaction : AuditableEntity
	{
		public const int MaxDescriptionLength = 250;

		private Transaction() { }

		public decimal Amount { get; private set; }

		public TransactionType Type { get; private set; }

		public DateTime TransactionDate { get; private set; }

		public string? Description { get; private set; }

		public Guid? CategoryId { get; private set; }

		public Category? Category { get; private set; }

		public Guid? SubCategoryId { get; private set; }

		public SubCategory? SubCategory { get; private set; }

		/// <summary>Cuenta de origen (EXPENSE, TRANSFER, CREDIT_CARD_PAYMENT, LOAN_PAYMENT) o destino del ingreso (INCOME).</summary>
		public Guid? BankAccountId { get; private set; }

		public BankAccount? BankAccount { get; private set; }

		/// <summary>Cuenta de destino, solo en TRANSFER.</summary>
		public Guid? DestinationBankAccountId { get; private set; }

		public BankAccount? DestinationBankAccount { get; private set; }

		/// <summary>Tarjeta consumida (EXPENSE con tarjeta) o pagada (CREDIT_CARD_PAYMENT).</summary>
		public Guid? CreditCardId { get; private set; }

		public CreditCard? CreditCard { get; private set; }

		public Guid? CreditCardGroupId { get; private set; }

		public CreditCardGroup? CreditCardGroup { get; private set; }

		/// <summary>Plantilla que originó el movimiento, si vino de una recurrencia.</summary>
		public Guid? TransactionTemplateId { get; private set; }

		public TransactionTemplate? TransactionTemplate { get; private set; }

		/// <summary>Un gasto pagado con tarjeta no descuenta de ninguna cuenta: aumenta la deuda.</summary>
		public bool IsPaidWithCreditCard => Type == TransactionType.EXPENSE && CreditCardId.HasValue;

		public static Transaction RegisterIncome(
			decimal amount,
			DateTime transactionDate,
			Guid bankAccountId,
			Guid categoryId,
			Guid? subCategoryId = null,
			string? description = null,
			Guid? templateId = null)
		{
			var transaction = Create(TransactionType.INCOME, amount, transactionDate, description, templateId);
			transaction.BankAccountId = Guard.AgainstEmpty(bankAccountId);
			transaction.Classify(categoryId, subCategoryId);

			return transaction;
		}

		public static Transaction RegisterExpenseFromAccount(
			decimal amount,
			DateTime transactionDate,
			Guid bankAccountId,
			Guid categoryId,
			Guid? subCategoryId = null,
			string? description = null,
			Guid? templateId = null)
		{
			var transaction = Create(TransactionType.EXPENSE, amount, transactionDate, description, templateId);
			transaction.BankAccountId = Guard.AgainstEmpty(bankAccountId);
			transaction.Classify(categoryId, subCategoryId);

			return transaction;
		}

		public static Transaction RegisterExpenseOnCreditCard(
			decimal amount,
			DateTime transactionDate,
			Guid creditCardId,
			Guid categoryId,
			Guid? creditCardGroupId = null,
			Guid? subCategoryId = null,
			string? description = null,
			Guid? templateId = null)
		{
			var transaction = Create(TransactionType.EXPENSE, amount, transactionDate, description, templateId);
			transaction.CreditCardId = Guard.AgainstEmpty(creditCardId);
			transaction.CreditCardGroupId = creditCardGroupId;
			transaction.Classify(categoryId, subCategoryId);

			return transaction;
		}

		public static Transaction RegisterTransfer(
			decimal amount,
			DateTime transactionDate,
			Guid originBankAccountId,
			Guid destinationBankAccountId,
			string? description = null,
			Guid? templateId = null)
		{
			Guard.Against(
				originBankAccountId == destinationBankAccountId,
				"La cuenta de origen y la de destino de una transferencia deben ser distintas.");

			var transaction = Create(TransactionType.TRANSFER, amount, transactionDate, description, templateId);
			transaction.BankAccountId = Guard.AgainstEmpty(originBankAccountId);
			transaction.DestinationBankAccountId = Guard.AgainstEmpty(destinationBankAccountId);

			return transaction;
		}

		public static Transaction RegisterCreditCardPayment(
			decimal amount,
			DateTime transactionDate,
			Guid bankAccountId,
			Guid creditCardId,
			Guid? creditCardGroupId = null,
			string? description = null,
			Guid? templateId = null)
		{
			var transaction = Create(TransactionType.CREDIT_CARD_PAYMENT, amount, transactionDate, description, templateId);
			transaction.BankAccountId = Guard.AgainstEmpty(bankAccountId);
			transaction.CreditCardId = Guard.AgainstEmpty(creditCardId);
			transaction.CreditCardGroupId = creditCardGroupId;

			return transaction;
		}

		public static Transaction RegisterLoanPayment(
			decimal amount,
			DateTime transactionDate,
			Guid bankAccountId,
			string? description = null,
			Guid? templateId = null)
		{
			var transaction = Create(TransactionType.LOAN_PAYMENT, amount, transactionDate, description, templateId);
			transaction.BankAccountId = Guard.AgainstEmpty(bankAccountId);

			return transaction;
		}

		public void Reclassify(Guid categoryId, Guid? subCategoryId)
		{
			Guard.Against(
				Type == TransactionType.TRANSFER,
				"Una transferencia entre cuentas propias no se clasifica: no es ingreso ni gasto.");

			Classify(categoryId, subCategoryId);
			MarkUpdated();
		}

		public void UpdateDescription(string? description)
		{
			Description = NormalizeDescription(description);
			MarkUpdated();
		}

		private static Transaction Create(
			TransactionType type,
			decimal amount,
			DateTime transactionDate,
			string? description,
			Guid? templateId)
		{
			var transaction = new Transaction
			{
				Type = type,
				Amount = Guard.AgainstNonPositive(amount),
				TransactionDate = transactionDate,
				Description = NormalizeDescription(description),
				TransactionTemplateId = templateId
			};

			transaction.Raise(new TransactionRegisteredDomainEvent(transaction.Id, type, transaction.Amount, transactionDate));

			return transaction;
		}

		private void Classify(Guid categoryId, Guid? subCategoryId)
		{
			CategoryId = Guard.AgainstEmpty(categoryId);

			Guard.Against(
				subCategoryId == Guid.Empty,
				"La subcategoría indicada no es válida.");

			SubCategoryId = subCategoryId;
		}

		private static string? NormalizeDescription(string? description)
		{
			if (string.IsNullOrWhiteSpace(description))
				return null;

			return Guard.AgainstExceedingLength(description.Trim(), MaxDescriptionLength, nameof(Description));
		}
	}
}
