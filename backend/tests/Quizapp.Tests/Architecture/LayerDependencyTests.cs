using Quizapp.Application.DTOs.QuizManager.Quizzes;
using Quizapp.Domain.Entities;
using Quizapp.Infrastructure.Persistence;

namespace Quizapp.Tests.Architecture;

public class LayerDependencyTests
{
    [Fact]
    public void Inner_layers_are_separate_assemblies_without_outer_layer_dependencies()
    {
        var domainAssembly = typeof(User).Assembly;
        var applicationAssembly = typeof(CreateQuizDto).Assembly;
        var infrastructureAssembly = typeof(QuizAppDbContext).Assembly;

        Assert.Equal("Quizapp.Domain", domainAssembly.GetName()
            .Name);

        Assert.Equal("Quizapp.Application", applicationAssembly.GetName()
            .Name);

        Assert.Equal("Quizapp.Infrastructure", infrastructureAssembly.GetName()
            .Name);

        Assert.All(domainAssembly.GetReferencedAssemblies(), reference =>
            Assert.StartsWith("System.", reference.Name!));

        Assert.DoesNotContain(applicationAssembly.GetReferencedAssemblies(), reference =>
            reference.Name is "Quizapp.Api" or "Quizapp.Infrastructure"
            || reference.Name!.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
            || reference.Name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));

        Assert.DoesNotContain(infrastructureAssembly.GetReferencedAssemblies(), reference =>
            reference.Name == "Quizapp.Api");
    }
}
