using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Application.Common.Interfaces;
using EngineeringPerformance.Application.Projects.DTOs;
using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Projects.Commands;

public sealed record ArchiveProjectCommand(int Id);

public sealed class ArchiveProjectCommandHandler
{
    private readonly IProjectRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserActionLogger _actionLogger;

    public ArchiveProjectCommandHandler(
        IProjectRepository repository,
        IUnitOfWork unitOfWork,
        IUserActionLogger actionLogger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _actionLogger = actionLogger;
    }

    public async Task<ProjectDto?> HandleAsync(ArchiveProjectCommand command, CancellationToken cancellationToken = default)
    {
        var project = await _repository.GetEntityByIdAsync(command.Id, cancellationToken);
        if (project is null)
            return null;

        project.IsArchived = true;
        project.ModifiedAt = DateTime.UtcNow;

        _repository.Update(project);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _actionLogger.LogAsync(
            UserActionType.ProjectDelete,
            entityType: "Project",
            entityId: project.Id,
            description: $"{project.Code} {project.Name}",
            cancellationToken: cancellationToken);

        return await _repository.GetByIdAsync(project.Id, cancellationToken);
    }
}
