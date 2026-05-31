using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Enums
{
	public enum SyncStatus
	{
		Synced = 0,
		CreatedOffline = 1,
		UpdatedOffline = 2,
		DeletedOffline = 3
	}
}
