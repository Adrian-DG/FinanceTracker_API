using System;

namespace Domain.Common
{
	/// <summary>
	/// Hecho de negocio ya ocurrido. El dominio no conoce MediatR: la capa de
	/// aplicación adapta estos eventos a notificaciones al persistir los cambios.
	/// </summary>
	public interface IDomainEvent
	{
		DateTime OccurredOnUtc { get; }
	}
}
