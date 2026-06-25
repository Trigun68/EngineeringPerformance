using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Application.Common.Interfaces;
using EngineeringPerformance.Application.Projects.DTOs;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Projects.Commands;

public sealed record CreateProjectCommand(CreateProjectRequest Request);

public sealed class CreateProjectCommandHandler
{
    private readonly IProjectRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserActionLogger _actionLogger;

    public CreateProjectCommandHandler(
        IProjectRepository repository,
        IUnitOfWork unitOfWork,
        IUserActionLogger actionLogger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _actionLogger = actionLogger;
    }

    public async Task<ProjectDto?> HandleAsync(CreateProjectCommand command, CancellationToken cancellationToken = default)
    {
        var request = command.Request;

        if (await _repository.CodeExistsAsync(request.Code, cancellationToken: cancellationToken))
            return null;

        var project = new Project
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            WorkNumber = request.WorkNumber,
            ContractNumber = request.ContractNumber,
            CalendarPlanNumber = request.CalendarPlanNumber,
            Type = request.Type,
            Customer = request.Customer,
            Contractor = request.Contractor,
            Gip = request.Gip,
            WorkStatus = request.WorkStatus,
            WorkStart = request.WorkStart,
            Program = request.Program,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _actionLogger.LogAsync(
            UserActionType.ProjectCreate,
            entityType: "Project",
            entityId: project.Id,
            description: $"{project.Code} {project.Name}",
            cancellationToken: cancellationToken);

        return await _repository.GetByIdAsync(project.Id, cancellationToken);
    }
}
