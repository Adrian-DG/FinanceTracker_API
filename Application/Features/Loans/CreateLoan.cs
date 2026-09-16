using Application.Common.Exceptions;
using Application.Contracts;
using Domain.Entities.Assets;
using Domain.Entities.Liability;
using Domain.Enums;
using Domain.Services;
using FluentValidation;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Loans;

public record CreateLoanCommand(
	string Concept,
	decimal Principal,
	decimal AnnualRatePercent,
	int TermMonths,
	DateOnly FirstPaymentDate,
	Guid BankId,
	ExtraordinaryRecalculationStrategy RecalculationStrategy
) : IRequest<Guid>;

public class CreateLoanValidator : AbstractValidator<CreateLoanCommand>
{
	public CreateLoanValidator()
	{
		RuleFor(x => x.Concept)
			.NotEmpty()
			.MaximumLength(Loan.MaxConceptLength);
		RuleFor(x => x.Principal)
			.GreaterThan(0);
		// La tasa es un porcentaje anual: 12.5 significa 12.5 %.
		RuleFor(x => x.AnnualRatePercent)
			.InclusiveBetween(0, 99.99m);
		RuleFor(x => x.TermMonths)
			.InclusiveBetween(1, AmortizationCalculator.MaxTermMonths);
		RuleFor(x => x.BankId)
			.NotEmpty();
		RuleFor(x => x.RecalculationStrategy)
			.IsInEnum();
	}
}

public class CreateLoanHandler(IUnitOfWork uow) : IRequestHandler<CreateLoanCommand, Guid>
{
	private readonly IUnitOfWork _uow = uow;

	public async Task<Guid> Handle(CreateLoanCommand request, CancellationToken cancellationToken)
	{
		if (!await _uow.BankRepository.ExistsAsync(request.BankId, cancellationToken))
			throw new NotFoundException(nameof(Bank), request.BankId);

		// Disburse genera el cuadro de amortización completo junto con el préstamo.
		var loan = Loan.Disburse(
			request.Concept,
			request.Principal,
			request.AnnualRatePercent,
			request.TermMonths,
			request.FirstPaymentDate,
			request.BankId,
			request.RecalculationStrategy);

		await _uow.LoanRepository.AddAsync(loan, cancellationToken);
		await _uow.SaveChangesAsync(cancellationToken);

		return loan.Id;
	}
}
