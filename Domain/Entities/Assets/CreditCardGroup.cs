using Domain.Common;
using Domain.Enums;
using System;

namespace Domain.Entities.Assets
{
	/// <summary>
	/// Agrupación de consumos dentro de una tarjeta (por ejemplo "Suscripciones" o
	/// "Viajes"). Lleva su propio subtotal de deuda y solo se crea desde la tarjeta.
	/// </summary>
	public class CreditCardGroup : CatalogEntity
	{
		private CreditCardGroup() { }

		public CurrencyCode Currency { get; private set; }

		public decimal CurrentBalance { get; private set; }

		public Guid CardId { get; private set; }

		public CreditCard? Card { get; private set; }

		internal static CreditCardGroup Create(Guid cardId, string name, CurrencyCode currency, string? icon, string? colorHex)
		{
			var group = new CreditCardGroup
			{
				CardId = Guard.AgainstEmpty(cardId),
				Currency = currency
			};

			group.SetDescriptor(name, description: null, icon, colorHex, isSystemDefault: false);

			return group;
		}

		internal void Charge(decimal amount)
		{
			CurrentBalance += amount;
			MarkUpdated();
		}

		internal void Relieve(decimal amount)
		{
			CurrentBalance -= amount;
			MarkUpdated();
		}
	}
}
