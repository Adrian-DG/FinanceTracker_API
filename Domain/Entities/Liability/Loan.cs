using Domain.Common;
using Domain.Entities.Assets;
using Domain.Enums;
using Domain.Events.Liability;
using Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Entities.Liability
{
	/// <summary>
	/// Préstamo con cuadro de amortización por sistema francés. Es la raíz del agregado:
	/// las cuotas, los pagos y el historial de tasas solo se modifican desde aquí, de modo
	/// que el saldo pendiente y el cuadro nunca puedan contradecirse.
	/// </summary>
	public class Loan : AuditableEntity
	{
		public const int MaxConceptLength = 150;

		private readonly List<LoanAmortizationEntry> _amortizationSchedule = new();
		private readonly List<LoanPaymentAllocation> _paymentAllocations = new();
		private readonly List<LoanRateHistory> _rateHistory = new();

		private Loan() { }

		public string Concept { get; private set; } = string.Empty;

		public decimal InitialAmount { get; private set; }

		/// <summary>Capital pendiente. Solo cambia al aplicar pagos.</summary>
		public decimal RemainingAmount { get; private set; }

		/// <summary>Plazo vigente en meses: cambia si un abono a capital acorta el préstamo.</summary>
		public int TotalTermMonths { get; private set; }

		public decimal CurrentMonthlyFee { get; private set; }

		/// <summary>Tasa anual vigente expresada como porcentaje (12.5 significa 12.5 %).</summary>
		public decimal CurrentAnnualRate { get; private set; }

		public DateOnly FirstPaymentDate { get; private set; }

		/// <summary>Qué hacer con el cuadro cuando se abona a capital.</summary>
		public ExtraordinaryRecalculationStrategy RecalculationStrategy { get; private set; }

		public Guid BankId { get; private set; }

		public Bank? Bank { get; private set; }

		public IReadOnlyCollection<LoanAmortizationEntry> AmortizationSchedule => _amortizationSchedule.AsReadOnly();

		public IReadOnlyCollection<LoanPaymentAllocation> PaymentAllocations => _paymentAllocations.AsReadOnly();

		public IReadOnlyCollection<LoanRateHistory> LoanRates => _rateHistory.AsReadOnly();

		public bool IsSettled => RemainingAmount <= 0m;

		public LoanAmortizationEntry? NextPendingInstallment => _amortizationSchedule
			.Where(x => !x.IsPaid && !x.IsDeleted)
			.OrderBy(x => x.InstallmentNumber)
			.FirstOrDefault();

		public int PendingInstallmentsCount => _amortizationSchedule.Count(x => !x.IsPaid && !x.IsDeleted);

		public decimal TotalInterestProjected => _amortizationSchedule.Where(x => !x.IsDeleted).Sum(x => x.Interest);

		/// <summary>Formaliza el préstamo y genera el cuadro de amortización completo.</summary>
		public static Loan Disburse(
			string concept,
			decimal principal,
			decimal annualRatePercent,
			int termMonths,
			DateOnly firstPaymentDate,
			Guid bankId,
			ExtraordinaryRecalculationStrategy recalculationStrategy = ExtraordinaryRecalculationStrategy.REDUCE_TERM,
			string? rateNote = null)
		{
			var loan = new Loan
			{
				Concept = Guard.AgainstExceedingLength(Guard.AgainstNullOrWhiteSpace(concept), MaxConceptLength),
				InitialAmount = Guard.AgainstNonPositive(principal),
				RemainingAmount = principal,
				TotalTermMonths = Guard.AgainstNonPositive(termMonths),
				CurrentAnnualRate = ValidateRate(annualRatePercent),
				FirstPaymentDate = firstPaymentDate,
				BankId = Guard.AgainstEmpty(bankId),
				RecalculationStrategy = recalculationStrategy
			};

			Guard.Against(
				termMonths > AmortizationCalculator.MaxTermMonths,
				$"El plazo no puede superar {AmortizationCalculator.MaxTermMonths} meses.");

			loan.CurrentMonthlyFee = AmortizationCalculator.MonthlyPayment(principal, annualRatePercent, termMonths);

			var schedule = AmortizationCalculator.BuildSchedule(principal, annualRatePercent, termMonths, firstPaymentDate);

			foreach (var installment in schedule)
				loan._amortizationSchedule.Add(LoanAmortizationEntry.FromProjection(loan.Id, installment));

			loan._rateHistory.Add(LoanRateHistory.Create(loan.Id, annualRatePercent, firstPaymentDate, rateNote ?? "Tasa inicial"));

			loan.Raise(new LoanDisbursedDomainEvent(loan.Id, loan.BankId, principal, loan.CurrentMonthlyFee, termMonths));

			return loan;
		}

		/// <summary>
		/// Aplica el pago de la próxima cuota pendiente. Si el movimiento supera la cuota,
		/// el excedente se abona a capital y el cuadro se recalcula según la estrategia vigente.
		/// </summary>
		public LoanPaymentAllocation RegisterPayment(Guid transactionId, decimal amount, DateOnly paidDate)
		{
			Guard.AgainstNonPositive(amount);
			Guard.Against(IsSettled, $"El préstamo '{Concept}' ya está saldado.");

			var installment = NextPendingInstallment
				?? throw new DomainException($"El préstamo '{Concept}' no tiene cuotas pendientes por aplicar.");

			Guard.Against(
				amount < installment.ScheduledPayment,
				$"El pago de {amount:N2} no cubre la cuota {installment.InstallmentNumber} de {installment.ScheduledPayment:N2}.");

			var excess = AmortizationCalculator.Round(amount - installment.ScheduledPayment);

			var allocation = LoanPaymentAllocation.Create(
				Id,
				transactionId,
				principalPaid: AmortizationCalculator.Round(installment.Principal + excess),
				interestPaid: installment.Interest,
				paymentDate: paidDate,
				isExtraordinary: excess > 0m,
				target: excess > 0m ? ExtraordinaryPaymentTarget.PRINCIPAL : ExtraordinaryPaymentTarget.NONE);

			_paymentAllocations.Add(allocation);
			installment.SettleWith(allocation.Id, paidDate);

			ReducePrincipal(installment.Principal);

			Raise(new LoanInstallmentPaidDomainEvent(
				Id,
				installment.InstallmentNumber,
				allocation.PrincipalPaid,
				allocation.InterestPaid,
				RemainingAmount));

			if (excess > 0m)
				ApplyPrincipalReduction(excess);
			else
				RaiseIfSettled();

			MarkUpdated();

			return allocation;
		}

		/// <summary>
		/// Registra un pago extraordinario fuera del calendario. Si va a capital reduce el
		/// saldo y recalcula el cuadro; si va a interés solo se deja registrado.
		/// </summary>
		public LoanPaymentAllocation RegisterExtraordinaryPayment(
			Guid transactionId,
			decimal amount,
			DateOnly paidDate,
			ExtraordinaryPaymentTarget target)
		{
			Guard.AgainstNonPositive(amount);
			Guard.Against(IsSettled, $"El préstamo '{Concept}' ya está saldado.");

			Guard.Against(
				target == ExtraordinaryPaymentTarget.NONE,
				"Un pago extraordinario debe indicar si se aplica a capital o a interés.");

			var goesToPrincipal = target == ExtraordinaryPaymentTarget.PRINCIPAL;

			Guard.Against(
				goesToPrincipal && amount > RemainingAmount,
				$"El abono de {amount:N2} supera el capital pendiente de {RemainingAmount:N2}.");

			var allocation = LoanPaymentAllocation.Create(
				Id,
				transactionId,
				principalPaid: goesToPrincipal ? amount : 0m,
				interestPaid: goesToPrincipal ? 0m : amount,
				paymentDate: paidDate,
				isExtraordinary: true,
				target: target);

			_paymentAllocations.Add(allocation);

			if (goesToPrincipal)
				ApplyPrincipalReduction(amount);

			MarkUpdated();

			return allocation;
		}

		/// <summary>
		/// Registra una revisión de tasa. El plazo pendiente se mantiene y la cuota se
		/// ajusta, que es como los bancos manejan un préstamo a tasa variable.
		/// </summary>
		public void ChangeRate(decimal newAnnualRatePercent, DateOnly effectiveDate, string? note = null)
		{
			var validated = ValidateRate(newAnnualRatePercent);

			Guard.Against(validated == CurrentAnnualRate, $"El préstamo ya tiene la tasa {validated:N2} %.");
			Guard.Against(IsSettled, $"El préstamo '{Concept}' ya está saldado.");

			var previousRate = CurrentAnnualRate;
			CurrentAnnualRate = validated;
			_rateHistory.Add(LoanRateHistory.Create(Id, validated, effectiveDate, note));

			RebuildPendingSchedule(ExtraordinaryRecalculationStrategy.REDUCE_INSTALLMENT);

			Raise(new LoanRateChangedDomainEvent(Id, previousRate, validated, CurrentMonthlyFee));

			MarkUpdated();
		}

		public void ChangeRecalculationStrategy(ExtraordinaryRecalculationStrategy strategy)
		{
			RecalculationStrategy = strategy;
			MarkUpdated();
		}

		public override void Delete()
		{
			Guard.Against(
				!IsSettled,
				$"No se puede eliminar el préstamo '{Concept}' porque mantiene un saldo de {RemainingAmount:N2}.");

			base.Delete();
		}

		private void ApplyPrincipalReduction(decimal amount)
		{
			ReducePrincipal(amount);

			if (IsSettled)
			{
				RaiseIfSettled();
				return;
			}

			RebuildPendingSchedule(RecalculationStrategy);

			Raise(new ExtraordinaryPaymentAppliedDomainEvent(
				Id,
				amount,
				ExtraordinaryPaymentTarget.PRINCIPAL,
				RecalculationStrategy,
				RemainingAmount,
				CurrentMonthlyFee,
				PendingInstallmentsCount));
		}

		private void ReducePrincipal(decimal amount)
		{
			RemainingAmount = AmortizationCalculator.Round(RemainingAmount - amount);

			if (RemainingAmount < 0m)
				RemainingAmount = 0m;
		}

		/// <summary>
		/// Vuelve a proyectar las cuotas no pagadas sobre el capital pendiente.
		/// Las cuotas ya saldadas son historia y no se tocan.
		/// </summary>
		private void RebuildPendingSchedule(ExtraordinaryRecalculationStrategy strategy)
		{
			var pending = _amortizationSchedule
				.Where(x => !x.IsPaid && !x.IsDeleted)
				.OrderBy(x => x.InstallmentNumber)
				.ToList();

			if (pending.Count == 0)
				return;

			var firstNumber = pending[0].InstallmentNumber;
			var firstDate = pending[0].ScheduledDate;
			var paidCount = _amortizationSchedule.Count(x => x.IsPaid && !x.IsDeleted);

			foreach (var entry in pending)
				_amortizationSchedule.Remove(entry);

			if (RemainingAmount <= 0m)
			{
				TotalTermMonths = paidCount;
				CurrentMonthlyFee = 0m;

				return;
			}

			var projection = strategy == ExtraordinaryRecalculationStrategy.REDUCE_TERM
				? AmortizationCalculator.BuildScheduleWithFixedPayment(RemainingAmount, CurrentAnnualRate, CurrentMonthlyFee, firstDate, firstNumber)
				: AmortizationCalculator.BuildSchedule(RemainingAmount, CurrentAnnualRate, pending.Count, firstDate, firstNumber);

			foreach (var installment in projection)
				_amortizationSchedule.Add(LoanAmortizationEntry.FromProjection(Id, installment));

			TotalTermMonths = paidCount + projection.Count;

			if (strategy == ExtraordinaryRecalculationStrategy.REDUCE_INSTALLMENT && projection.Count > 0)
				CurrentMonthlyFee = projection[0].Payment;
		}

		private void RaiseIfSettled()
		{
			if (!IsSettled)
				return;

			foreach (var entry in _amortizationSchedule.Where(x => !x.IsPaid && !x.IsDeleted).ToList())
				_amortizationSchedule.Remove(entry);

			TotalTermMonths = _amortizationSchedule.Count(x => !x.IsDeleted);
			CurrentMonthlyFee = 0m;

			Raise(new LoanSettledDomainEvent(Id));
		}

		private static decimal ValidateRate(decimal annualRatePercent)
		{
			Guard.AgainstNegative(annualRatePercent);
			Guard.Against(annualRatePercent >= 100m, "La tasa anual debe ser menor que 100 %.");

			return annualRatePercent;
		}
	}
}
