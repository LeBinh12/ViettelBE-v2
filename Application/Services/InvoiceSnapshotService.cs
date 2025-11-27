using System.Text.Json;
using Domain.Entities;

namespace Application.Services;

public class InvoiceSnapshotService
{
    public async Task SaveSnapshotAsync(Invoice invoice)
    {
        var dir = Path.Combine(Directory.GetCurrentDirectory(), "invoice_snapshots");
        Directory.CreateDirectory(dir);

        var path = Path.Combine(dir, $"{invoice.Id}.json");

        var json = JsonSerializer.Serialize(invoice,
            new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(path, json);
    }
    
    
    
    public async Task<Invoice?> LoadSnapshotAsync(Guid invoiceId)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "invoice_snapshots", $"{invoiceId}.json");

        if (!File.Exists(path))
            return null;

        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<Invoice>(json);
    }
}