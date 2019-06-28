using Flunt.Notifications;
using PaymentContext.Domain.Commands;
using PaymentContext.Domain.Entities;
using PaymentContext.Domain.Enums;
using PaymentContext.Domain.Repositories;
using PaymentContext.Domain.Services;
using PaymentContext.Domain.ValueObjects;
using PaymentContext.Shared.Commands;
using PaymentContext.Shared.Handlers;
using System;

namespace PaymentContext.Domain.Handlers
{
    public class SubscriptionHandler : Notifiable, IHandler<CreateBoletoSubscriptionCommand>, IHandler<CreatePayPalSubscriptionCommand>
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IEmailService _emailService;

        public SubscriptionHandler(IStudentRepository studentRepository, IEmailService emailService)
        {
            _studentRepository = studentRepository;
            _emailService = emailService;
        }

        public ICommandResult Handle(CreateBoletoSubscriptionCommand command)
        {
            //Fail fast validations            
            command.Validate();
            if (command.Invalid)
            {
                AddNotifications(command);
                return new CommandResult(false, "Não foi possível realizar sua assinatura");
            }

            //verificar se o documento está cadastrado
            if (_studentRepository.DocumentExists(command.Document))
                AddNotification("Document", "Este CPF já está em uso");

            //verificar se o email ja está cadastrado
            if (_studentRepository.EmailExists(command.Email))
                AddNotification("Email", "Este E-mail já está em uso");

            //gerar os vos
            var name = new Name(command.FirstName, command.LastName);
            var document = new Document(command.Document, EDocumentType.CPF);
            var email = new Email(command.Email);
            var address = new Address(command.Street, command.Number, command.Neighborhood, command.City, command.State, command.Country, command.ZipCode);


            //gerar as entidades
            var student = new Student(name, document, email);
            var subscription = new Subscription(DateTime.Now.AddMonths(1));
            var payment = new BoletoPayment(
                command.BarCode,
                command.BoletoNumber,
                command.PaidDate,
                command.ExpireDate,
                command.Total,
                command.TotalPaid,
                command.Payer,
                new Document(command.PayerDocument, command.PayerDocumentType),
                address,
                email
            );

            //relacionamentos
            subscription.AddPayment(payment);
            student.AddSubscription(subscription);

            //validar
            AddNotifications(name, document, email, address, student, subscription, payment);

            if (Invalid)
                return new CommandResult(false, "Não foi possível realizar sua assinatura");

            //salvar
            _studentRepository.CreateSubscription(student);

            //enviar email
            _emailService.Send(student.Name.ToString(), student.Email.Address, "Aviso", "Sua assinatura foi criada");

            //retornar informações
            return new CommandResult(true, "Assinatura realizada com sucesso");
        }

        public ICommandResult Handle(CreatePayPalSubscriptionCommand command)
        {
            // Verificar se Documento já está cadastrado
            if (_studentRepository.DocumentExists(command.Document))
                AddNotification("Document", "Este CPF já está em uso");

            // Verificar se E-mail já está cadastrado
            if (_studentRepository.EmailExists(command.Email))
                AddNotification("Email", "Este E-mail já está em uso");

            // Gerar os VOs
            var name = new Name(command.FirstName, command.LastName);
            var document = new Document(command.Document, EDocumentType.CPF);
            var email = new Email(command.Email);
            var address = new Address(command.Street, command.Number, command.Neighborhood, command.City, command.State, command.Country, command.ZipCode);

            // Gerar as Entidades
            var student = new Student(name, document, email);
            var subscription = new Subscription(DateTime.Now.AddMonths(1));
            // Só muda a implementação do Pagamento
            var payment = new PayPalPayment(
                command.TransactionCode,
                command.PaidDate,
                command.ExpireDate,
                command.Total,
                command.TotalPaid,
                command.Payer,
                new Document(command.PayerDocument, command.PayerDocumentType),
                address,
                email
            );

            // Relacionamentos
            subscription.AddPayment(payment);
            student.AddSubscription(subscription);

            // Validar
            AddNotifications(name, document, email, address, student, subscription, payment);

            if (Invalid)
                return new CommandResult(false, "Não foi possível realizar sua assinatura");

            // Salvar as Informações
            _studentRepository.CreateSubscription(student);

            // Enviar E-mail de boas vindas
            _emailService.Send(student.Name.ToString(), student.Email.Address, "Aviso", "Sua assinatura foi criada");

            // Retornar informações
            return new CommandResult(true, "Assinatura realizada com sucesso");
        }
    }
}
