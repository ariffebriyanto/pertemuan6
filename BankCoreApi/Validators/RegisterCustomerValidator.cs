using BankCoreApi.Dtos;
using FluentValidation;

namespace BankCoreApi.Validators;

/// <summary>
/// Validator FluentValidation untuk Request Registrasi Nasabah (Pertemuan 7 - Input Sanitization & Validation)
/// </summary>
public class RegisterCustomerValidator : AbstractValidator<RegisterCustomerRequest>
{
    public RegisterCustomerValidator()
    {
        RuleFor(x => x.Nik)
            .NotEmpty().WithMessage("NIK tidak boleh kosong.")
            .Matches(@"^\d{16}$").WithMessage("NIK harus 16 digit angka.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Nama lengkap wajib diisi.")
            .MinimumLength(3).WithMessage("Nama minimal 3 karakter.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email wajib diisi.")
            .EmailAddress().WithMessage("Format email tidak valid.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password wajib diisi.")
            .MinimumLength(8).WithMessage("Password minimal 8 karakter.");

        RuleFor(x => x.Pin)
            .NotEmpty().WithMessage("PIN Transaksi wajib diisi.")
            .Matches(@"^\d{6}$").WithMessage("PIN Transaksi harus 6 digit angka.");
    }
}
