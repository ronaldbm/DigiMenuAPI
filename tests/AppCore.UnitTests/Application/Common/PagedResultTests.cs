using AppCore.Application.Common;
using FluentAssertions;

namespace AppCore.UnitTests.Application.Common;

[Trait("Category", "Unit")]
public sealed class PagedResultTests
{
    [Fact]
    public void Create_SetsAllFieldsCorrectly()
    {
        var items = new List<string> { "a", "b", "c" };
        var result = PagedResult<string>.Create(items, total: 25, page: 2, pageSize: 10);

        result.Items.Should().BeEquivalentTo(items);
        result.Total.Should().Be(25);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
    }

    [Theory]
    [InlineData(25, 10, 3)]   // 25 items / 10 per page = 3 pages (2.5 → ceiling)
    [InlineData(10, 10, 1)]   // exact fit
    [InlineData(11, 10, 2)]   // one overflow
    [InlineData(0,  10, 0)]   // empty
    [InlineData(1,  1,  1)]   // one item, one per page
    public void TotalPages_CalculatesCorrectly(int total, int pageSize, int expectedPages)
    {
        var result = PagedResult<int>.Create([], total: total, page: 1, pageSize: pageSize);
        result.TotalPages.Should().Be(expectedPages);
    }

    [Fact]
    public void TotalPages_WithZeroPageSize_ReturnsZero()
    {
        var result = PagedResult<int>.Create([], total: 10, page: 1, pageSize: 0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public void HasNext_TrueWhenCurrentPageLessThanTotalPages()
    {
        var result = PagedResult<int>.Create([], total: 30, page: 1, pageSize: 10);
        result.HasNext.Should().BeTrue();
    }

    [Fact]
    public void HasNext_FalseWhenOnLastPage()
    {
        var result = PagedResult<int>.Create([], total: 30, page: 3, pageSize: 10);
        result.HasNext.Should().BeFalse();
    }

    [Fact]
    public void HasPrev_TrueWhenPageGreaterThanOne()
    {
        var result = PagedResult<int>.Create([], total: 30, page: 2, pageSize: 10);
        result.HasPrev.Should().BeTrue();
    }

    [Fact]
    public void HasPrev_FalseOnFirstPage()
    {
        var result = PagedResult<int>.Create([], total: 30, page: 1, pageSize: 10);
        result.HasPrev.Should().BeFalse();
    }

    [Fact]
    public void Items_DefaultsToEmptyList()
    {
        var result = new PagedResult<string>();
        result.Items.Should().NotBeNull().And.BeEmpty();
    }
}
