using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Metadata
{
	public interface ISyncEntity
	{		
		public SyncStatus SyncStatus { get; set; }
	}
}
