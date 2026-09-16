using System;

namespace Domain.Common
{
	/// <summary>
	/// Se lanza cuando una operación viola una invariante del dominio.
	/// La capa de presentación la traduce a un 409/422; nunca a un 500.
	/// </summary>
	public class DomainException : Exception
	{
		public DomainException(string message) : base(message) { }

		public DomainException(string message, Exception innerException)
			: base(message, innerException) { }
	}
}
