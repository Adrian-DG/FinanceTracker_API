using Application.Common.Exceptions;
using Application.Contracts;
using Domain.Entities.Assets;
using Domain.Entities.Ledger;
using FluentValidation;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Transactions;

public record RegisterIncomeCommand(
	decimal Amount,
	DateTime TransactionDate,
	Guid BankAccountId,
	Guid CategoryId,
	Guid? SubCategoryId,
	string? Description
) : IRequest<Guid>;

public class RegisterIncomeValidator : AbstractValidator<RegisterIncomeCommand>
{
	public RegisterIncomeValidator()
	{
		RuleFor(x => x.Amount)
			.GreaterThan(0);
		RuleFor(x => x.TransactionDate)
			.NotEmpty();
		RuleFor(x => x.BankAccountId)
			.NotEmpty();
		RuleFor(x => x.CategoryId)
			.NotEmpty();
		RuleFor(x => x.Description)
			.MaximumLength(Transaction.MaxDescriptionLength);
	}
}

public class RegisterIncomeHandler(IUnitOfWork uow) : IRequestHandler<RegisterIncomeCommand, Guid>
{
	private readonly IUnitOfWork _uow = uow;

	public async Task<Guid> Handle(RegisterIncomeCommand request, CancellationToken cancellationToken)
	{
		var account = await _uow.BankAccountRepository.GetByIdAsync(request.BankAccountId, cancellationToken)
			?? throw new NotFoundException(nameof(BankAccount), request.BankAccountId);

		await EnsureClassificationExistsAsync(_uow, request.CategoryId, request.SubCategoryId, cancellationToken);

		var transaction = Transaction.RegisterIncome(
			request.Amount,
			request.TransactionDate,
			account.Id,
			request.CategoryId,
			request.SubCategoryId,
			request.Description);

		account.Credit(request.Amount);

		await _uow.TransactionRepository.AddAsync(transaction, cancellationToken);
		await _uow.SaveChangesAsync(cancellationToken);

		return transaction.Id;
	}

	/// <summary>
	/// La categoría y la subcategoría viven en otro agregado, así que su existencia
	/// y su parentesco se comprueban aquí y no dentro del movimiento.
	/// </summary>
	internal static async Task EnsureClassificationExistsAsync(
		IUnitOfWork uow,
		Guid categoryId,
		Guid? subCategoryId,
		CancellationToken cancellationToken)
	{
		if (!await uow.CategoryRepository.ExistsAsync(categoryId, cancellationToken))
			throw new NotFoundException(nameof(Category), categoryId);

		if (subCategoryId is null)
			return;

		if (!await uow.CategoryRepository.SubCategoryBelongsToCategoryAsync(categoryId, subCategoryId.Value, cancellationToken))
			throw new NotFoundException(nameof(SubCategory), subCategoryId.Value);
	}
}
