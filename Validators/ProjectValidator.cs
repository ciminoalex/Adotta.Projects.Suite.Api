using ADOTTA.Projects.Suite.Api.DTOs;
using FluentValidation;

namespace ADOTTA.Projects.Suite.Api.Validators;

public class ProjectValidator : AbstractValidator<ProjectDto>
{
    public ProjectValidator()
    {
        RuleFor(x => x.NumeroProgetto)
            .NotEmpty().WithMessage("Numero progetto is required")
            .MaximumLength(30).WithMessage("Numero progetto must not exceed 30 characters");

        RuleFor(x => x.Cliente)
            .NotEmpty().WithMessage("Cliente is required")
            .MaximumLength(100).WithMessage("Cliente must not exceed 100 characters");

        RuleFor(x => x.NomeProgetto)
            .NotEmpty().WithMessage("Nome progetto is required")
            .MaximumLength(200).WithMessage("Nome progetto must not exceed 200 characters");

        RuleFor(x => x.DataCreazione)
            .NotEmpty().WithMessage("Data creazione is required");

        RuleFor(x => x.StatoProgetto)
            .IsInEnum().WithMessage("Invalid stato progetto value");

        RuleFor(x => x.DataFineInstallazione)
            .GreaterThanOrEqualTo(x => x.DataInizioInstallazione)
            .When(x => x.DataInizioInstallazione.HasValue && x.DataFineInstallazione.HasValue)
            .WithMessage("Data fine installazione must be greater than or equal to data inizio installazione");
    }
}

public class LivelloProgettoValidator : AbstractValidator<LivelloProgettoDto>
{
    public LivelloProgettoValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome is required")
            .MaximumLength(100).WithMessage("Nome must not exceed 100 characters");

        RuleFor(x => x.Ordine)
            .GreaterThanOrEqualTo(1).WithMessage("Ordine must be greater than or equal to 1");

        RuleFor(x => x.DataFineInstallazione)
            .GreaterThanOrEqualTo(x => x.DataInizioInstallazione)
            .When(x => x.DataInizioInstallazione.HasValue && x.DataFineInstallazione.HasValue)
            .WithMessage("Data fine installazione must be greater than or equal to data inizio installazione");
    }
}

public class ProdottoProgettoValidator : AbstractValidator<ProdottoProgettoDto>
{
    public ProdottoProgettoValidator()
    {
        RuleFor(x => x.TipoProdotto)
            .NotEmpty().WithMessage("Tipo prodotto is required")
            .MaximumLength(50).WithMessage("Tipo prodotto must not exceed 50 characters");

        RuleFor(x => x.Variante)
            .NotEmpty().WithMessage("Variante is required")
            .MaximumLength(100).WithMessage("Variante must not exceed 100 characters");

        RuleFor(x => x.QMq)
            .GreaterThanOrEqualTo(0).WithMessage("QMq must be greater than or equal to 0");

        RuleFor(x => x.QFt)
            .GreaterThanOrEqualTo(0).WithMessage("QFt must be greater than or equal to 0");
    }
}

public class UserValidator : AbstractValidator<UserDto>
{
    public UserValidator()
    {
        // Code non è obbligatorio perché viene generato automaticamente dall'API tramite GUID
        // Non aggiungiamo regole per Code
        
        RuleFor(x => x.UserCode)
            .NotEmpty().WithMessage("UserCode is required")
            .MaximumLength(100).WithMessage("UserCode must not exceed 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("UserName is required")
            .MaximumLength(200).WithMessage("UserName must not exceed 200 characters");

        RuleFor(x => x.Ruolo)
            .NotEmpty().WithMessage("Ruolo is required")
            .MaximumLength(50).WithMessage("Ruolo must not exceed 50 characters");

        RuleFor(x => x.TeamTecnico)
            .MaximumLength(100).WithMessage("TeamTecnico must not exceed 100 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.TeamTecnico));
    }
}

