using FluentValidation;
using QueueMed.Application.DTOs;

namespace QueueMed.Application.Validators;

public class CreateTicketRequestValidator : AbstractValidator<CreateTicketRequest>
{
    public CreateTicketRequestValidator()
    {
        RuleFor(x => x.TipoAtendimento).IsInEnum();
        RuleFor(x => x.Especialidade).IsInEnum();
    }
}

public class ChamarProximoRequestValidator : AbstractValidator<ChamarProximoRequest>
{
    public ChamarProximoRequestValidator()
    {
        RuleFor(x => x.Especialidade).IsInEnum();
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Usuario).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Senha).NotEmpty().MaximumLength(200);
    }
}
