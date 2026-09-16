using Domain.Enums;

namespace Domain.Metadata
{
	/// <summary>
	/// Marca las entidades que viajan en el protocolo de sincronización con la app móvil.
	/// </summary>
	public interface ISyncEntity
	{
		SyncStatus SyncStatus { get; }
	}
}
