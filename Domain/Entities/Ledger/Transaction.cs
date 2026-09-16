using Domain.Entities.Assets;
using Domain.Enums;
using Domain.Metadata;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Ledger
{
	public class Transaction : BaseEntity, ISyncEntity, IAuditableEntity
	{
		public decimal Ammount { get; set; }
		public TransactionType Type { get; set; }
		public DateTime TransactionDate { get; set; }
		public string? Description { get; set; }


		// Navigation properties

		public Guid CategoryId { get; set; }
		public virtual Category? Category { get; set; }
		public Guid SubCategoryId { get; set; }
		public virtual SubCategory? SubCategory { get; set; }

		// Source account (EXPENSE, TRANSFER, CREDIT_CARD_PAYMENT, LOAN_PAYMENT)
		public Guid? BankAccountId { get; set; }
		public virtual BankAccount? BankAccount { get; set; }

		// Destination account (TRANSFER only)
		public Guid? DestinationBankAccountId { get; set; }
		public virtual BankAccount? DestinationBankAccount { get; set; }

		// Credit card charge or payment (EXPENSE on card, CREDIT_CARD_PAYMENT)
		public Guid? CreditCardId { get; set; }
		public virtual CreditCard? CreditCard { get; set; }
		public Guid? CreditCardGroupId { get; set; }
		public virtual CreditCardGroup? CreditCardGroup { get; set; }

		// Recurring: links an executed instance back to its template
		public Guid? TransactionTemplateId { get; set; }
		public virtual TransactionTemplate? TransactionTemplate { get; set; }

		public Guid BankId { get; set; }
		public virtual Bank? Bank { get; set; }


		// Sync and audit properties

		public SyncStatus SyncStatus { get; set; }
		public DateTime CreateAtUtc { get; set; }
		public DateTime UpdateAtUtc { get; set; }

	}
}
