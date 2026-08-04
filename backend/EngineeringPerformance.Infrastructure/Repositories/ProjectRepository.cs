using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Application.Projects.DTOs;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Repositories;

public sealed class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _context;

    public ProjectRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProjectDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .AsNoTracking()
            .Where(p => !p.IsArchived)
            .OrderBy(p => p.Code)
            .Select(MapToDto())
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectDto>> GetAllForUserAsync(
        int userId,
        IReadOnlyList<int> accessibleProjectIds,
        bool isAdministrator,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Projects.AsNoTracking();

        if (!includeArchived)
            query = query.Where(p => !p.IsArchived);

        if (!isAdministrator)
            query = query.Where(p => accessibleProjectIds.Contains(p.Id));

        return await query.OrderBy(p => p.Code).Select(MapToDto()).ToListAsync(cancellationToken);
    }

    public async Task<ProjectDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(MapToDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Project?> GetEntityByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim();
        var query = _context.Projects.AsQueryable().Where(p => p.Code == normalized);

        if (excludeId.HasValue)
            query = query.Where(p => p.Id != excludeId.Value);

        return query.AnyAsync(cancellationToken);
    }

    public Task AddAsync(Project project, CancellationToken cancellationToken = default) =>
        _context.Projects.AddAsync(project, cancellationToken).AsTask();

    public void Update(Project project) => _context.Projects.Update(project);

    public Task DeleteAsync(Project project, CancellationToken cancellationToken = default)
    {
        _context.Projects.Remove(project);
        return Task.CompletedTask;
    }

    private static System.Linq.Expressions.Expression<Func<Project, ProjectDto>> MapToDto() =>
        p => new ProjectDto(
            p.Id,
            p.Code,
            p.Name,
            p.WorkNumber,
            p.ContractNumber,
            p.CalendarPlanNumber,
            p.Type,
            p.Customer,
            p.Contractor,
            p.Gip,
            p.WorkStatus,
            p.WorkStart,
            p.Program,
            // Как в списке версий UI: только неархивные; отдельно план и факт.
            p.AnalysisVersions.Count(v => !v.IsArchived && v.Direction == AnalysisDirection.Plan),
            p.AnalysisVersions.Count(v => !v.IsArchived && v.Direction == AnalysisDirection.Fact),
            p.IsArchived);
}
