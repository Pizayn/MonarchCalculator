using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonarchCalculator;

namespace MonarchsCalculator.Tests
{
    public class MonarchServiceTests
    {
        // 1) Test scenario: Cache has data (cache hit)
        //    => Should return from cache, not call repository.
        [Fact]
        public async Task GetMonarchsAsync_CacheHit_ReturnsCachedData()
        {
            // Arrange
            var fakeMonarchs = new List<Monarch>
            {
                new Monarch { Id = 1, Name = "Cached King" }
            };

            var mockCacheService = new Mock<IMonarchCacheService>();
            mockCacheService
                .Setup(x => x.TryGetCache(out fakeMonarchs))
                .Returns(true);

            var mockRepository = new Mock<IMonarchRepository>();
            mockRepository
                .Setup(x => x.FetchMonarchsAsync(It.IsAny<string>()))
                .Throws(new Exception("Repository should not be called on cache hit!"));

            var service = new MonarchService(mockRepository.Object, mockCacheService.Object);

            // Act
            var result = await service.GetMonarchsAsync("any_url");

            // Assert
            Assert.True(result.Status);
            Assert.NotNull(result.Data);
            Assert.Single(result.Data);       // exactly 1 item
            Assert.Equal("Cached King", result.Data[0].Name);

            mockRepository.Verify(
                x => x.FetchMonarchsAsync(It.IsAny<string>()),
                Times.Never
            );
        }

        // 2) Test scenario: Cache miss
        //    => Should call repository, store result in cache
        [Fact]
        public async Task GetMonarchsAsync_CacheMiss_CallsRepositoryAndStoresData()
        {
            // Arrange
            var fakeMonarchs = new List<Monarch>
            {
                new Monarch { Id = 2, Name = "Fresh King" }
            };

            var mockCacheService = new Mock<IMonarchCacheService>();
            List<Monarch> outMonarchs;
            mockCacheService
                .Setup(x => x.TryGetCache(out outMonarchs))
                .Returns(false);    // no cache data

            var mockRepository = new Mock<IMonarchRepository>();
            mockRepository
                .Setup(x => x.FetchMonarchsAsync("https://gist.githubusercontent.com/christianpanton/10d65ccef9f29de3acd49d97ed423736/raw/b09563bc0c4b318132c7a738e679d4f984ef0048/kings"))
                .ReturnsAsync(new BaseResponse<List<Monarch>>
                {
                    Status = true,
                    Data = fakeMonarchs,
                    Message = "Mocked fetch success"
                });

            mockCacheService
                .Setup(x => x.StoreCache(It.IsAny<List<Monarch>>()));

            var service = new MonarchService(mockRepository.Object, mockCacheService.Object);

            // Act
            var result = await service.GetMonarchsAsync("https://gist.githubusercontent.com/christianpanton/10d65ccef9f29de3acd49d97ed423736/raw/b09563bc0c4b318132c7a738e679d4f984ef0048/kings");

            // Assert
            Assert.True(result.Status);
            Assert.NotNull(result.Data);
            Assert.Single(result.Data);
            Assert.Equal("Fresh King", result.Data[0].Name);

            mockRepository.Verify(
                x => x.FetchMonarchsAsync("https://gist.githubusercontent.com/christianpanton/10d65ccef9f29de3acd49d97ed423736/raw/b09563bc0c4b318132c7a738e679d4f984ef0048/kings"),
                Times.Once
            );

            mockCacheService.Verify(
                x => x.StoreCache(It.Is<List<Monarch>>(m => m.Count == 1)),
                Times.Once
            );
        }

        [Fact]
        public async Task GetMonarchsAsync_FetchFails_ReturnsError()
        {
            // Arrange
            var mockCacheService = new Mock<IMonarchCacheService>();
            List<Monarch> outMonarchs;
            mockCacheService.Setup(x => x.TryGetCache(out outMonarchs)).Returns(false);

            var mockRepository = new Mock<IMonarchRepository>();
            mockRepository
                .Setup(x => x.FetchMonarchsAsync(It.IsAny<string>()))
                .ReturnsAsync(new BaseResponse<List<Monarch>>
                {
                    Status = false,
                    Message = "Some fetch error"
                });

            var service = new MonarchService(mockRepository.Object, mockCacheService.Object);

            // Act
            var result = await service.GetMonarchsAsync("any_url");

            // Assert
            Assert.False(result.Status);
            Assert.Null(result.Data);
            Assert.Equal("Some fetch error", result.Message);
        }

        // 3) Testing "LongestRulingMonarch" logic
        [Fact]
        public void GetLongestRulingMonarch_ShouldReturnCorrectMonarch()
        {
            // Arrange
            var fakeMonarchs = new List<Monarch>
            {
                new Monarch { Name = "Monarch A", StartYear = 1000, EndYear = 1005 }, // 5 years
                new Monarch { Name = "Monarch B", StartYear = 1100, EndYear = 1112 }, // 12 years
                new Monarch { Name = "Monarch C", StartYear = 1200, EndYear = 1201 }, // 1 year
            };

            var mockRepo = new Mock<IMonarchRepository>();
            var mockCache = new Mock<IMonarchCacheService>();
            var service = new MonarchService(mockRepo.Object, mockCache.Object);

            // Act
            var (name, duration) = service.GetLongestRulingMonarch(fakeMonarchs);

            // Assert
            Assert.Equal("Monarch B", name);
            Assert.Equal(12, duration);
        }

        // 4) Similarly test "GetMostCommonFirstName"
        [Fact]
        public void GetMostCommonFirstName_ReturnsMostCommonName()
        {
            // Arrange
            var fakeMonarchs = new List<Monarch>
            {
                new Monarch { Name = "Henry VIII" },
                new Monarch { Name = "Elizabeth I" },
                new Monarch { Name = "Henry V" },
                new Monarch { Name = "Edward I" }
            };

            var mockRepo = new Mock<IMonarchRepository>();
            var mockCache = new Mock<IMonarchCacheService>();
            var service = new MonarchService(mockRepo.Object, mockCache.Object);

            // Act
            var common = service.GetMostCommonFirstName(fakeMonarchs);

            // Assert
            Assert.Equal("Henry", common); // "Henry" appears 2 times
        }
    }
}
