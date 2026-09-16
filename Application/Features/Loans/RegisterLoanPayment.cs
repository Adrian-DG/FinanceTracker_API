using Application.Common.Exceptions;
using Application.Contracts;
using Domain.Entities.Assets;
using Domain.Entities.Ledger;
using Domain.Entities.Liability;
using FluentValidation;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Loans;

public record LoanPaymentResponse(
	Guid TransactionId,
	Guid AllocationId,
	decimal PrincipalPaid,
	decimal InterestPaid,
	decimal RemainingAmount,
	decimal CurrentMonthlyFee,
	int PendingInstallments,
	bool IsSettled);

/// <summary>Paga la próxima cuota pendiente. El excedente se abona a capital.</summary>
public record RegisterLoanPaymentCommand(
	Guid LoanId,
	Guid BankAccountId,
	decimal Amount,
	DateOnly PaidDate,
	string? Description
) : IRequest<LoanPaymentResponse>;

public class RegisterLoanPaymentValidator : AbstractValidator<RegisterLoanPaymentCommand>
{
	public RegisterLoanPaymentValidator()
	{
		RuleFor(x => x.LoanId)
			.NotEmpty();
		RuleFor(x => x.BankAccountId)
			.NotEmpty();
		RuleFor(x => x.Amount)
			.GreaterThan(0);
		RuleFor(x => x.Description)
			.MaximumLength(Transaction.MaxDescriptionLength);
	}
}

public class RegisterLoanPaymentHandler(IUnitOfWork uow) : IRequestHandler<RegisterLoanPaymentCommand, LoanPaymentResponse>
{
	private readonly IUnitOfWork _uow = uow;

	public async Task<LoanPaymentResponse> Handle(RegisterLoanPaymentCommand request, CancellationToken cancellationToken)
	{
		var loan = await _uow.LoanRepository.GetWithScheduleAsync(request.LoanId, cancellationToken)
			?? throw new NotFoundException(nameof(Loan), request.LoanId);

		var account = await _uow.BankAccountRepository.GetByIdAsync(request.BankAccountId, cancellationToken)
			?? throw new NotFoundException(nameof(BankAccount), request.BankAccountId);

		var transaction = Transaction.RegisterLoanPayment(
			request.Amount,
			request.PaidDate.ToDateTime(TimeOnly.MinValue),
			account.Id,
			request.Description);

		account.Debit(request.Amount);

		// El préstamo decide cuánto del pago va a capital y cuánto a interés,
		// marca la cuota como saldada y recalcula el cuadro si hubo excedente.
		var allocation = loan.RegisterPayment(transaction.Id, request.Amount, request.PaidDate);

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
