using Domain.Common;

namespace Domain.Entities.Assets
{
	/// <summary>
	/// Entidad financiera emisora de cuentas, tarjetas y préstamos.
	/// Es un catálogo: no contiene saldos ni movimientos.
	/// </summary>
	public class Bank : CatalogEntity
	{
		private Bank() { }

		public static Bank Create(
			string name,
			string? description = null,
			string? icon = null,
			string? colorHex = null,
			bool isSystemDefault = false)
		{
			var bank = new Bank();
			bank.SetDescriptor(name, description, icon, colorHex, isSystemDefault);

			return bank;
		}
	}
}
