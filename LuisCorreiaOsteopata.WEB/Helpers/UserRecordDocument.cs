using LuisCorreiaOsteopata.WEB.Data.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LuisCorreiaOsteopata.WEB.Helpers;

public class UserRecordDocument : IDocument
{
    private readonly User _user;

    public UserRecordDocument(User user)
    {
        _user = user;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);

            page.Margin(50);

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
           
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("Ficha de Cliente").Bold().FontSize(20);
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(5);

            column.Item().Text($"Name: {_user.Names} {_user.LastName}");
            column.Item().Text($"Email: {_user.Email}");
            column.Item().Text($"Birthdate: {_user.Birthdate?.ToString("dd/MM/yyyy") ?? "N/A"}");
            column.Item().Text($"NIF: {_user.Nif ?? "N/A"}");
            column.Item().Text($"Phone: {_user.PhoneNumber ?? "N/A"}");
            column.Item().Text($"Role: {_user.StripeCustomerId ?? "N/A"}");
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(x =>
        {
            x.Span("Generated on ");
            x.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        });
    }
}
