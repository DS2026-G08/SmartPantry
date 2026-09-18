using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Modularity;
using Volo.Abp.Validation;
using SmartPantry.Authors;
using Xunit;

namespace SmartPantry.Books;

public abstract class BookAppService_Tests<TStartupModule>
    : SmartPantryApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IBookAppService _bookAppService;
    private readonly IAuthorAppService _authorAppService;

    protected BookAppService_Tests()
    {
        _bookAppService = GetRequiredService<IBookAppService>();
        _authorAppService = GetRequiredService<IAuthorAppService>();
    }

    [Fact]
    public async Task Should_Create_A_Valid_Book()
    {
        var author = await _authorAppService.CreateAsync(
            new CreateUpdateAuthorDto
            {
                Name = "Test Author",
                BirthDate = DateTime.Today,
                ShortBio = "Author created for the book test"
            }
        );

        var result = await _bookAppService.CreateAsync(
            new CreateUpdateBookDto
            {
                Name = "New test book 42",
                AuthorId = author.Id,
                Price = 10,
                PublishDate = DateTime.Today,
                Type = BookType.ScienceFiction
            }
        );

        result.Id.ShouldNotBe(Guid.Empty);
        result.Name.ShouldBe("New test book 42");
        result.AuthorId.ShouldBe(author.Id);
    }
}
    
    [Fact]
    public async Task Should_Not_Create_A_Book_Without_Name()
    {
        var exception = await Assert.ThrowsAsync<AbpValidationException>(async () =>
        {
            await _bookAppService.CreateAsync(
                new CreateUpdateBookDto
                {
                    Name = "",
                    Price = 10,
                    PublishDate = DateTime.Now,
                    Type = BookType.ScienceFiction
                }
            );
        });

        exception.ValidationErrors
            .ShouldContain(err => err.MemberNames.Any(mem => mem == "Name"));
    }
}