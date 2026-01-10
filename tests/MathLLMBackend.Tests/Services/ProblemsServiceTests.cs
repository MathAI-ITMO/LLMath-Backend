using FluentAssertions;
using MathLLMBackend.Core.Services.ProblemsService;
using MathLLMBackend.DataAccess.Contexts;
using MathLLMBackend.Domain.Entities;
using MathLLMBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MathLLMBackend.Tests.Services;

public class ProblemsServiceTests
{
    private readonly AppDbContext _context;
    private readonly ProblemsService _service;

    public ProblemsServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _service = new ProblemsService(_context);
    }

    [Fact]
    public async Task GetProblems_ReturnsAllProblems()
    {
        // Arrange
        var p1 = new Problem("sol1", "stmt1", "title1") { TheoryLink = "link1" };
        var p2 = new Problem("sol2", "stmt2", "title2") { TheoryLink = "link2" };
        _context.Problems.AddRange(p1, p2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetProblems(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Title == "title1");
        result.Should().Contain(p => p.Title == "title2");
    }

    [Fact]
    public async Task GetProblemsByType_ReturnsOnlyCorrectType()
    {
        // Arrange
        var p1 = new Problem("sol1", "stmt1", "title1") { TheoryLink = "link1" };
        var p2 = new Problem("sol2", "stmt2", "title2") { TheoryLink = "link2" };
        _context.Problems.AddRange(p1, p2);
        
        var pt1 = new ProblemTaskType(p1, TaskType.Learning);
        var pt2 = new ProblemTaskType(p2, TaskType.Exam);
        _context.Set<ProblemTaskType>().AddRange(pt1, pt2);
        
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetProblemsByType(TaskType.Learning, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("title1");
    }

    [Fact]
    public async Task GetProblem_ReturnsCorrectProblem()
    {
        // Arrange
        var p1 = new Problem("sol1", "stmt1", "title1") { TheoryLink = "link1" };
        _context.Problems.Add(p1);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetProblem(p1.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("title1");
    }

    [Fact]
    public async Task CreateProblem_AddsProblemToDb()
    {
        // Arrange
        var p = new Problem("sol", "stmt", "title") { TheoryLink = "link" };

        // Act
        var result = await _service.CreateProblem(p, CancellationToken.None);

        // Assert
        var dbProblem = await _context.Problems.FindAsync(result.Id);
        dbProblem.Should().NotBeNull();
        dbProblem!.Title.Should().Be("title");
    }

    [Fact]
    public async Task UpdateProblem_UpdatesExistingProblem()
    {
        // Arrange
        var p = new Problem("sol", "stmt", "title") { TheoryLink = "link" };
        _context.Problems.Add(p);
        await _context.SaveChangesAsync();

        p.Title = "new title";

        // Act
        await _service.UpdateProblem(p, CancellationToken.None);

        // Assert
        var dbProblem = await _context.Problems.FindAsync(p.Id);
        dbProblem!.Title.Should().Be("new title");
    }

    [Fact]
    public async Task DeleteProblem_RemovesFromDb()
    {
        // Arrange
        var p = new Problem("sol", "stmt", "title") { TheoryLink = "link" };
        _context.Problems.Add(p);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeleteProblem(p.Id, CancellationToken.None);

        // Assert
        var dbProblem = await _context.Problems.FindAsync(p.Id);
        dbProblem.Should().BeNull();
    }

    [Fact]
    public async Task SetType_AddsNewType()
    {
        // Arrange
        var p = new Problem("sol", "stmt", "title") { TheoryLink = "link" };
        _context.Problems.Add(p);
        await _context.SaveChangesAsync();

        // Act
        await _service.SetType(p.Id, TaskType.Learning, CancellationToken.None);

        // Assert
        var dbProblem = await _context.Problems.Include(x => x.Types).FirstOrDefaultAsync(x => x.Id == p.Id);
        dbProblem!.Types.Should().HaveCount(1);
        dbProblem.Types.First().TaskType.Should().Be(TaskType.Learning);
    }

    [Fact]
    public async Task ClearTypes_RemovesAllTypes()
    {
        // Arrange
        var p = new Problem("sol", "stmt", "title") { TheoryLink = "link" };
        _context.Problems.Add(p);
        var pt = new ProblemTaskType(p, TaskType.Learning);
        _context.Set<ProblemTaskType>().Add(pt);
        await _context.SaveChangesAsync();

        // Act
        await _service.ClearTypes(p.Id, CancellationToken.None);

        // Assert
        var types = await _context.Set<ProblemTaskType>().Where(t => t.ProblemId == p.Id).ToListAsync();
        types.Should().BeEmpty();
    }
}
