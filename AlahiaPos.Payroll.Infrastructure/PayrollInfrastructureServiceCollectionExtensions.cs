using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Infrastructure.Context;
using AlahiaPos.Payroll.Infrastructure.Persistence;
using AlahiaPos.Payroll.Infrastructure.Providers;
using AlahiaPos.Payroll.Infrastructure.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AlahiaPos.Payroll.Infrastructure;

public static class PayrollInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPayrollInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddDbContext<PayrollDbContext>(configureDb);
        services.AddScoped<IPayrollRunStore, EfPayrollRunStore>();
        services.AddScoped<IConceptAssignmentProvider, EfConceptAssignmentProvider>();
        services.AddScoped<IEmployeeContractAttributeProvider, EfEmployeeContractAttributeProvider>();
        services.AddScoped<IAttendanceFactProvider, EfAttendanceFactProvider>();
        services.AddScoped<IFactProvider, EfFactProvider>();
        services.AddScoped<IEvaluationContextBuilder, EvaluationContextBuilder>();
        return services;
    }
}
