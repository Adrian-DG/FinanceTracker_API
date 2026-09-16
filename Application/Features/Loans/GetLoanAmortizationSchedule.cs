using Application.Common.Exceptions;
using Application.Contracts;
using Domain.Entities.Liability;
using FluentValidation;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Loans;

public record AmortizationEntryResponse(
	int InstallmentNumber,
	DateOnly ScheduledDate,
	decimal ScheduledPayment,
	decimal Principal,
	decimal Interest,
	decimal RemainingBalance,
	bool IsPaid,
	DateOnly? PaidDate);

public record LoanAmortizationResponse(
	Guid LoanId,
	string Concept,
	decimal InitialAmount,
	decimal RemainingAmount,
	decimal CurrentMonthlyFee,
	decimal CurrentAnnualRate,
	int TotalTermMonths,
	int PendingInstallments,
	bool IsSettled,
	IReadOnlyList<AmortizationEntryResponse> Schedule);

public record GetLoanAmortizationScheduleQuery(Guid LoanId) : IRequest<LoanAmortizationResponse>;

public class GetLoanAmortizationScheduleValidator : AbstractValidator<GetLoanAmortizationScheduleQuery>
{
	public GetLoanAmortizationScheduleValidator()
	{
		RuleFor(x => x.LoanId)
			.NotEmpty();
	}
}

public class GetLoanAmortizationScheduleHandler(IUnitOfWork uow)
	: IRequestHandler<GetLoanAmortizationScheduleQuery, LoanAmortizationResponse>
{
	private readonly IUnitOfWork _uow = uow;

	public async Task<LoanAmortizationResponse> Handle(
		GetLoanAmortizationScheduleQuery request,
		CancellationToken cancellationToken)
	{
		var loan = await _uow.LoanRepository.GetWithScheduleAsync(request.LoanId, cancellationToken)
			?? throw new NotFoundException(nameof(Loan), request.LoanId);

		var schedule = loan.AmortizationSchedule
			.Where(entry => !entry.IsDeleted)
			.OrderBy(entry => entry.InstallmentNumber)
			.Select(entry => new AmortizationEntryResponse(
				entry.InstallmentNumber,
				entry.ScheduledDate,
				entry.ScheduledPayment,
				entry.Principal,
				entry.Interest,
				entry.RemainingBalance,
				entry.IsPaid,
				entry.PaidDate))
			.ToList();

		return new LoanAmortizationResponse(
			loan.Id,
			loan.Concept,
			loan.InitialAmount,
			loan.RemainingAmount,
			loan.CurrentMonthlyFee,
			loan.CurrentAnnualRate,
			loan.TotalTermMonths,
			loan.PendingInstallmentsCount,
			loan.IsSettled,
			schedule);
	}
}
