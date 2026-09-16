using Application.Common.Exceptions;
using Application.Contracts;
using Domain.Entities.Assets;
using Domain.Entities.Ledger;
using Domain.Entities.Liability;
using Domain.Enums;
using FluentValidation;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Loans;

/// <summary>
/// Abono fuera del calendario. Si va a capital, el cuadro se recalcula según la
/// estrategia del préstamo: mantener la cuota y acortar el plazo, o mantener el
/// plazo y bajar la cuota.
/// </summary>
public record RegisterExtraordinaryLoanPaymentCommand(
	Guid LoanId,
	Guid BankAccountId,
	decimal Amount,
	DateOnly PaidDate,
	ExtraordinaryPaymentTarget Target,
	ExtraordinaryRecalculationStrategy? Strategy,
	string? Description
) : IRequest<LoanPaymentResponse>;

public class RegisterExtraordinaryLoanPaymentValidator : AbstractValidator<RegisterExtraordinaryLoanPaymentCommand>
{
	public RegisterExtraordinaryLoanPaymentValidator()
	{
		RuleFor(x => x.LoanId)
			.NotEmpty();
		RuleFor(x => x.BankAccountId)
			.NotEmpty();
		RuleFor(x => x.Amount)
			.GreaterThan(0);
		RuleFor(x => x.Target)
			.IsInEnum()
			.NotEqual(ExtraordinaryPaymentTarget.NONE)
			.WithMessage("Indique si el abono se aplica a capital o a interés.");
		RuleFor(x => x.Strategy!.Value)
			.IsInEnum()
			.When(x => x.Strategy.HasValue);
		RuleFor(x => x.Description)
			.MaximumLength(Transaction.MaxDescriptionLength);
	}
}

public class RegisterExtraordinaryLoanPaymentHandler(IUnitOfWork uow)
	: IRequestHandler<RegisterExtraordinaryLoanPaymentCommand, LoanPaymentResponse>
{
	private readonly IUnitOfWork _uow = uow;

	public async Task<LoanPaymentResponse> Handle(
		RegisterExtraordinaryLoanPaymentCommand request,
		CancellationToken cancellationToken)
	{
		var loan = await _uow.LoanRepository.GetWithScheduleAsync(request.LoanId, cancellationToken)
			?? throw new NotFoundException(nameof(Loan), request.LoanId);

		var account = await _uow.BankAccountRepository.GetByIdAsync(request.BankAccountId, cancellationToken)
			?? throw new NotFoundException(nameof(BankAccount), request.BankAccountId);

		// La estrategia puede decidirse en el momento del abono; si no viene,
		// se respeta la que se pactó al crear el préstamo.
		if (request.Strategy.HasValue)
			loan.ChangeRecalculationStrategy(request.Strategy.Value);

		var transaction = Transaction.RegisterLoanPayment(
			request.Amount,
			request.PaidDate.ToDateTime(TimeOnly.MinValue),
			account.Id,
			request.Description);

		account.Debit(request.Amount);

		var allocation = loan.RegisterExtraordinaryPayment(
			transaction.Id,
			request.Amount,
			request.PaidDate,
			request.Target);

		await _uow.TransactionRepository.AddAsync(transaction, cancellationToken);
		await _uow.SaveChangesAsync(cancellationToken);

		return new LoanPaymentResponse(
			transaction.Id,
			allocation.Id,
			allocation.PrincipalPaid,
			allocation.InterestPaid,
			loan.RemainingAmount,
			loan.CurrentMonthlyFee,
			loan.PendingInstallmentsCount,
			loan.IsSettled);
	}
}
