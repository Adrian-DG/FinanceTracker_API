using Domain.Common;
using System;
using System.Collections.Generic;

namespace Domain.Services
{
	/// <summary>Una cuota proyectada del cuadro de amortización.</summary>
	public readonly record struct AmortizationInstallment(
		int Number,
		DateOnly ScheduledDate,
		decimal Payment,
		decimal Principal,
		decimal Interest,
		decimal RemainingBalance);

	/// <summary>
	/// Cálculo de cuotas por el sistema francés (cuota fija):
	///
	///     cuota = P * i / (1 - (1 + i)^-n)
	///
	/// donde <c>P</c> es el capital, <c>i</c> la tasa mensual e <c>n</c> el plazo en meses.
	/// Las tasas se expresan como porcentaje anual (12.5 significa 12.5 %).
	/// Todos los importes se redondean a dos decimales y la última cuota absorbe el
	/// residuo, de modo que la suma de capitales siempre iguala el monto prestado.
	/// </summary>
	public static class AmortizationCalculator
	{
		/// <summary>Tope defensivo para que una cuota insuficiente no genere un cuadro infinito.</summary>
		public const int MaxTermMonths = 600;

		public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

		public static decimal MonthlyRate(decimal annualRatePercent) => annualRatePercent / 100m / 12m;

		public static decimal MonthlyPayment(decimal principal, decimal annualRatePercent, int termMonths)
		{
			Guard.AgainstNonPositive(principal);
			Guard.AgainstNonPositive(termMonths);
			Guard.AgainstNegative(annualRatePercent);

			var monthlyRate = MonthlyRate(annualRatePercent);

			if (monthlyRate == 0m)
				return Round(principal / termMonths);

			var discountFactor = (decimal)Math.Pow(1d + (double)monthlyRate, -termMonths);

			return Round(principal * monthlyRate / (1m - discountFactor));
		}

		/// <summary>Cuadro completo para un plazo conocido.</summary>
		public static IReadOnlyList<AmortizationInstallment> BuildSchedule(
			decimal principal,
			decimal annualRatePercent,
			int termMonths,
			DateOnly firstPaymentDate,
			int firstInstallmentNumber = 1)
		{
			var payment = MonthlyPayment(principal, annualRatePercent, termMonths);

			return Project(principal, annualRatePercent, payment, termMonths, firstPaymentDate, firstInstallmentNumber);
		}

		/// <summary>
		/// Cuadro para una cuota fija conocida: el plazo resulta del cálculo. Es lo que
		/// se usa al abonar a capital manteniendo la cuota (<see cref="Enums.ExtraordinaryRecalculationStrategy.REDUCE_TERM"/>).
		/// </summary>
		public static IReadOnlyList<AmortizationInstallment> BuildScheduleWithFixedPayment(
			decimal balance,
			decimal annualRatePercent,
			decimal payment,
			DateOnly firstPaymentDate,
			int firstInstallmentNumber = 1)
		{
			Guard.AgainstNonPositive(balance);
			Guard.AgainstNonPositive(payment);
			Guard.AgainstNegative(annualRatePercent);

			var firstInterest = Round(balance * MonthlyRate(annualRatePercent));

			Guard.Against(
				payment <= firstInterest,
				$"La cuota de {payment:N2} no cubre el interés mensual de {firstInterest:N2}: la deuda nunca se saldaría.");

			return Project(balance, annualRatePercent, payment, termMonths: null, firstPaymentDate, firstInstallmentNumber);
		}

		private static List<AmortizationInstallment> Project(
			decimal balance,
			decimal annualRatePercent,
			decimal payment,
			int? termMonths,
			DateOnly firstPaymentDate,
			int firstInstallmentNumber)
		{
			var monthlyRate = MonthlyRate(annualRatePercent);
			var schedule = new List<AmortizationInstallment>();
			var index = 0;

			while (balance > 0m)
			{
				if (termMonths.HasValue && index >= termMonths.Value)
					break;

				Guard.Against(index >= MaxTermMonths, $"El cuadro de amortización supera el máximo de {MaxTermMonths} cuotas.");

				var interest = Round(balance * monthlyRate);
				var principal = Round(payment - interest);
				var isLastInstallment = principal >= balance || (termMonths.HasValue && index == termMonths.Value - 1);

				if (isLastInstallment)
					principal = balance;

				balance = Round(balance - principal);

				schedule.Add(new AmortizationInstallment(
					firstInstallmentNumber + index,
					firstPaymentDate.AddMonths(index),
					Round(principal + interest),
					principal,
					interest,
					balance));

				index++;
			}

			return schedule;
		}
	}
}
