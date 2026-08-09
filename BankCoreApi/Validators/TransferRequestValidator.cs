using BankCoreApi.Dtos;
using FluentValidation;

namespace BankCoreApi.Validators;

/// <summary>
/// Validator FluentValidation untuk Request Transfer (Pertemuan 7 - Financial Validation)
/// </summary>
public class TransferRequestValidator : AbstractValidator<TransferRequest>
{
    public TransferRequestValidator()
    {
        RuleFor(x => x.SourceAccountNumber)
            .NotEmpty().WithMessage("Nomor rekening asal wajib diisi.")
            .Matches(@"^\d{10}$").WithMessage("Nomor rekening asal harus 10 digit.");

        RuleFor(x => x.TargetAccountNumber)
            .NotEmpty().WithMessage("Nomor rekening tujuan wajib diisi.")
            .Matches(@"^\d{10}$").WithMessage("Nomor rekening tujuan harus 10 digit.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Nominal transfer harus lebih besar dari Rp 0.");

        RuleFor(x => x.Pin)
            .NotEmpty().WithMessage("PIN Transaksi wajib diisi.")
            .Matches(@"^\d{6}$").WithMessage("PIN Transaksi harus 6 digit angka.");
    }
}
