using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Application.Common.Interfaces;
using EngineeringPerformance.Application.Projects.DTOs;
using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Projects.Commands;

public sealed record UpdateProjectCommand(int Id, UpdateProjectRequest Request);

public sealed class UpdateProjectCommandHandler
{
    private readonly IProjectRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserActionLogger _actionLogger;

    public UpdateProjectCommandHandler(
        IProjectRepository repository,
        IUnitOfWork unitOfWork,
        IUserActionLogger actionLogger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _actionLogger = actionLogger;
    }

    public async Task<ProjectDto?> HandleAsync(UpdateProjectCommand command, CancellationToken cancellationToken = default)
    {
        var project = await _repository.GetEntityByIdAsync(command.Id, cancellationToken);
        if (project is null || project.IsArchived)
            return null;

        var request = command.Request;

        if (await _repository.CodeExistsAsync(request.Code, command.Id, cancellationToken))
            return null;

        project.Code = request.Code.Trim();
        project.Name = request.Name.Trim();
        project.WorkNumber = request.WorkNumber;
        project.ContractNumber = request.ContractNumber;
        project.CalendarPlanNumber = request.CalendarPlanNumber;
        project.Type = request.Type;
        project.Customer = request.Customer;
        project.Contractor = request.Contractor;
        project.Gip = request.Gip;
        project.WorkStatus = request.WorkStatus;
        project.WorkStart = request.WorkStart;
        project.Program = request.Program;
        project.ModifiedAt = DateTime.UtcNow;

        _repository.Update(project);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _actionLogger.LogAsync(
            UserActionType.ProjectEdit,
            entityType: "Project",
            entityId: project.Id,
            description: $"{project.Code} {project.Name}",
            cancellationToken: cancellationToken);

        return await _repository.GetByIdAsync(project.Id, cancellationToken);
    }
}
