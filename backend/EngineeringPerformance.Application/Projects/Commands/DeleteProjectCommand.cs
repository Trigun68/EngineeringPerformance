using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Application.Common.Interfaces;
using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Projects.Commands;

public sealed record DeleteProjectCommand(int Id);

public sealed class DeleteProjectCommandHandler
{
    private readonly IProjectRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserActionLogger _actionLogger;

    public DeleteProjectCommandHandler(
        IProjectRepository repository,
        IUnitOfWork unitOfWork,
        IUserActionLogger actionLogger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _actionLogger = actionLogger;
    }

    public async Task<bool> HandleAsync(DeleteProjectCommand command, CancellationToken cancellationToken = default)
    {
        var project = await _repository.GetEntityByIdAsync(command.Id, cancellationToken);
        if (project is null)
            return false;

        var description = $"{project.Code} {project.Name}";

        await _repository.DeleteAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _actionLogger.LogAsync(
            UserActionType.ProjectDelete,
            entityType: "Project",
            entityId: command.Id,
            description: description,
            cancellationToken: cancellationToken);

        return true;
    }
}
