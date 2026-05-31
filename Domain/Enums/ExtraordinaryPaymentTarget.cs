using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Domain.Enums
{
	public enum ExtraordinaryPaymentTarget
	{
		NONE = 0,

		[Description("The payment is applied to the principal amount of the loan, reducing the outstanding balance and potentially shortening the loan term.")]
		PRINCIPAL = 1,

		[Description("The payment is applied to the interest amount of the loan, reducing the interest owed without affecting the principal balance.")]
		INTEREST = 2
	}
}
