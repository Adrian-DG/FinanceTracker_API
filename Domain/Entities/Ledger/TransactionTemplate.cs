using Domain.Enums;
using Domain.Metadata;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities.Ledger
{
	public class TransactionTemplate : Transaction
	{
		public Frequency Frequency { get; set; }
	}
}
