using Domain.Enums;
using Domain.Metadata;
using System;

namespace Domain.Common
{
	/// <summary>
	/// Raíz común de las entidades auditables y sincronizables. Centraliza las marcas
	/// de tiempo y el estado de sincronización que antes se repetían en cada entidad.
	/// </summary>
	public abstract class AuditableEntity : BaseEntity, IAuditableEntity, ISyncEntity
	{
		protected AuditableEntity()
		{
			var now = DateTime.UtcNow;
			CreatedAtUtc = now;
			UpdatedAtUtc = now;
			// El API es la fuente de verdad: lo que nace aquí ya está sincronizado.
			// El endpoint de sincronización móvil ajusta este valor cuando corresponde.
			SyncStatus = SyncStatus.Synced;
		}

		public DateTime CreatedAtUtc { get; private set; }

		public DateTime UpdatedAtUtc { get; private set; }

		public SyncStatus SyncStatus { get; private set; }

		/// <summary>Registra que el agregado cambió. Toda mutación del dominio debe llamarlo.</summary>
		protected void MarkUpdated() => UpdatedAtUtc = DateTime.UtcNow;

		public void ApplySyncStatus(SyncStatus status)
		{
			SyncStatus = status;
			MarkUpdated();
		}

		public override void Delete()
		{
			base.Delete();
			MarkUpdated();
		}

		public override void Restore()
		{
			base.Restore();
			MarkUpdated();
		}
	}
}
