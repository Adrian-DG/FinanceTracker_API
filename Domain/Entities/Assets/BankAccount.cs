using Domain.Common;
using Domain.Enums;
using Domain.Events.Assets;
using System;

namespace Domain.Entities.Assets
{
	/// <summary>
	/// Cuenta de la que salen y entran fondos. Es raíz de agregado: su saldo solo
	/// cambia a través de <see cref="Credit"/> y <see cref="Debit"/>, nunca por asignación directa.
	/// Los movimientos son un agregado aparte y se relacionan por identificador.
	/// </summary>
	public class BankAccount : AuditableEntity
	{
		public const int MaxNameLength = 100;

		private BankAccount() { }

		public string Name { get; private set; } = string.Empty;

		public AccountType Type { get; private set; }

		public decimal CurrentBalance { get; private set; }

		public CurrencyCode Currency { get; private set; } = CurrencyCode.DOP;

		public Guid BankId { get; private set; }

		public Bank? Bank { get; private set; }

		/// <summary>Solo las cuentas corrientes admiten sobregiro.</summary>
		public bool AllowsOverdraft => Type == AccountType.CHECKING;

		public static BankAccount Open(
			string name,
			AccountType type,
			decimal initialBalance,
			CurrencyCode currency,
			Guid bankId)
		{
			var account = new BankAccount
			{
				Name = NormalizeName(name),
				Type = ValidateType(type),
				Currency = ValidateCurrency(currency),
				BankId = Guard.AgainstEmpty(bankId),
				CurrentBalance = initialBalance
			};

			Guard.Against(
				initialBalance < 0 && !account.AllowsOverdraft,
				$"Una cuenta de tipo {type} no puede abrirse con saldo negativo.");

			account.Raise(new BankAccountOpenedDomainEvent(account.Id, account.BankId, account.CurrentBalance, account.Currency));

			return account;
		}

		public void Rename(string name)
		{
			Name = NormalizeName(name);
			MarkUpdated();
		}

		/// <summary>Abona fondos a la cuenta (ingresos, transferencias recibidas, reversos).</summary>
		public void Credit(decimal amount)
		{
			Guard.AgainstNonPositive(amount);

			CurrentBalance += amount;
			MarkUpdated();
		}

		/// <summary>Descuenta fondos de la cuenta (gastos, transferencias enviadas, pagos).</summary>
		public void Debit(decimal amount)
		{
			Guard.AgainstNonPositive(amount);

			Guard.Against(
				!CanCover(amount),
				$"La cuenta '{Name}' no tiene fondos suficientes: saldo {CurrentBalance:N2}, débito solicitado {amount:N2}.");

			CurrentBalance -= amount;
			MarkUpdated();

			if (CurrentBalance < 0)
				Raise(new BankAccountOverdrawnDomainEvent(Id, CurrentBalance));
		}

		public bool CanCover(decimal amount) => AllowsOverdraft || CurrentBalance >= amount;

		public void EnsureCurrencyMatches(CurrencyCode currency)
			=> Guard.Against(
				Currency != currency,
				$"La operación está en {currency} pero la cuenta '{Name}' opera en {Currency}.");

		public override void Delete()
		{
			Guard.Against(
				CurrentBalance != 0,
				$"No se puede eliminar la cuenta '{Name}' porque su saldo es {CurrentBalance:N2}. Debe quedar en cero.");

			base.Delete();
		}

		private static string NormalizeName(string name)
			=> Guard.AgainstExceedingLength(Guard.AgainstNullOrWhiteSpace(name), MaxNameLength);

		private static AccountType ValidateType(AccountType type)
		{
			Guard.Against(!Enum.IsDefined(type), $"'{type}' no es un tipo de cuenta válido.");

			return type;
		}

		private static CurrencyCode ValidateCurrency(CurrencyCode currency)
		{
			Guard.Against(!Enum.IsDefined(currency), $"'{currency}' no es una moneda válida.");

			return currency;
		}
	}
}
