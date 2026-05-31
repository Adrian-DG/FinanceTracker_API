using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

	namespace Domain.Enums
	{
		public enum ExtraordinaryRecalculationStrategy
		{
			[Description("No recalculation is performed. The loan schedule remains unchanged, and the extraordinary payment is simply applied to the next due payment.")]
			REDUCE_TERM = 1,

			[Description("The loan schedule is recalculated to reduce the installment amount while keeping the original loan term unchanged. The extraordinary payment is applied to the next due payment, and subsequent installments are adjusted accordingly.")]
			REDUCE_INSTALLMENT = 2
		}
	}
