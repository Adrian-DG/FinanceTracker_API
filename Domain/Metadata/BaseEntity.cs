using Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Metadata
{
	public abstract class BaseEntity
	{
		private readonly List<IDomainEvent> _domainEvents = new();

		protected BaseEntity() => Id = Guid.CreateVersion7();

		public Guid Id { get; protected set; }

		public bool IsDeleted { get; protected set; }

		/// <summary>
		/// Hechos ocurridos durante la operación actual. La infraestructura los publica
		/// y los limpia al confirmar la transacción.
		/// </summary>
		[NotMapped]
		public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

		protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

		public void ClearDomainEvents() => _domainEvents.Clear();

		/// <summary>Borrado lógico. Las entidades con reglas propias lo sobrescriben.</summary>
		public virtual void Delete() => IsDeleted = true;

		public virtual void Restore() => IsDeleted = false;
	}
}
