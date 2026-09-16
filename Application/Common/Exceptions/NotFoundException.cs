using System;

namespace Application.Common.Exceptions
{
	/// <summary>
	/// El caso de uso pidió un agregado que no existe. La presentación la traduce a 404.
	/// </summary>
	public class NotFoundException : Exception
	{
		public NotFoundException(string entityName, object key)
			: base($"No se encontró {entityName} con identificador '{key}'.")
		{
			EntityName = entityName;
			Key = key;
		}

		public string EntityName { get; }

		public object Key { get; }
	}
}
