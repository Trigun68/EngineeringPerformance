using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Import;

/// <summary>Lookup/create DocSet по коду из DocumentationCode (часть после '|').</summary>
internal sealed class DocSetImportBinder(AppDbContext context)
{
    private readonly Dictionary<string, int> _ids = new(StringComparer.OrdinalIgnoreCase);

    public async Task<int?> ResolveAsync(string? documentationCode, CancellationToken cancellationToken = default)
    {
        var code = Application.Registers.DocSetCodeParser.Extract(documentationCode);
        if (code is null)
            return null;

        if (_ids.TryGetValue(code, out var cached))
            return cached;

        var existing = await context.DocSets
            .FirstOrDefaultAsync(d => d.Code == code, cancellationToken);
        if (existing is not null)
        {
            _ids[code] = existing.Id;
            return existing.Id;
        }

        var created = new DocSet
        {
            Code = code,
            Name = code,
            CreatedAt = DateTime.UtcNow,
        };
        context.DocSets.Add(created);
        await context.SaveChangesAsync(cancellationToken);
        _ids[code] = created.Id;
        return created.Id;
    }
}
