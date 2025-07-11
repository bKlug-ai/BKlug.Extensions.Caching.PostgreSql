using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace BKlug.Extensions.Caching.PostgreSql.Tests
{
    [Collection("PostgreSqlCache collection")]
    public class PostgreSqlCacheExtensionsTests
    {
        private readonly IDistributedCache _cache;

        public PostgreSqlCacheExtensionsTests(PostgreSqlCacheFixture fixture)
        {
            _cache = fixture.Cache;
        }

        [Fact]
        public void Get_Generic_ReturnsDeserializedValue()
        {
            // Arrange
            var key = "test-key";
            var value = new TestClass { Id = 1, Name = "Test" };
            var serializedValue = JsonSerializer.SerializeToUtf8Bytes(value);
            _cache.Set(key, serializedValue);

            // Act
            var result = _cache.Get<TestClass>(key);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test", result.Name);
        }

        [Fact]
        public void Get_Generic_ReturnsDefault_WhenKeyNotExists()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();

            // Act
            var result = _cache.Get<TestClass>(key);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAsync_Generic_ReturnsDeserializedValue()
        {
            // Arrange
            var key = "test-key";
            var value = new TestClass { Id = 1, Name = "Test" };
            var serializedValue = JsonSerializer.SerializeToUtf8Bytes(value);
            await _cache.SetAsync(key, serializedValue);

            // Act
            var result = await _cache.GetAsync<TestClass>(key);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test", result.Name);
        }

        [Fact]
        public async Task GetAsync_Generic_ReturnsDefault_WhenKeyNotExists()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();

            // Act
            var result = await _cache.GetAsync<TestClass>(key);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void TryGetValue_Generic_ReturnsTrueAndValue_WhenKeyExists()
        {
            // Arrange
            var key = "test-key";
            var value = new TestClass { Id = 1, Name = "Test" };
            var serializedValue = JsonSerializer.SerializeToUtf8Bytes(value);
            _cache.Set(key, serializedValue);

            // Act
            var success = _cache.TryGetValue<TestClass>(key, out var result);

            // Assert
            Assert.True(success);
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test", result.Name);
        }

        [Fact]
        public void TryGetValue_Generic_ReturnsFalseAndDefault_WhenKeyNotExists()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();

            // Act
            var success = _cache.TryGetValue<TestClass>(key, out var result);

            // Assert
            Assert.False(success);
            Assert.Null(result);
        }

        [Fact]
        public async Task TryGetValueAsync_Generic_ReturnsTrueAndValue_WhenKeyExists()
        {
            // Arrange
            var key = "test-key";
            var value = new TestClass { Id = 1, Name = "Test" };
            var serializedValue = JsonSerializer.SerializeToUtf8Bytes(value);
            await _cache.SetAsync(key, serializedValue);

            // Act
            var result = await _cache.TryGetValueAsync<TestClass>(key);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(1, result.Value.Id);
            Assert.Equal("Test", result.Value.Name);
        }

        [Fact]
        public async Task TryGetValueAsync_Generic_ReturnsFalseAndDefault_WhenKeyNotExists()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();

            // Act
            var result = await _cache.TryGetValueAsync<TestClass>(key);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Value);
        }

        [Fact]
        public void Set_Generic_SerializesAndStoresValue()
        {
            // Arrange
            var key = "test-key";
            var value = new TestClass { Id = 1, Name = "Test" };

            // Act
            var returnValue = _cache.Set(key, value);

            // Assert
            var storedBytes = _cache.Get(key);
            Assert.NotNull(storedBytes);
            var storedValue = JsonSerializer.Deserialize<TestClass>(storedBytes);
            Assert.NotNull(storedValue);
            Assert.Equal(1, storedValue.Id);
            Assert.Equal("Test", storedValue.Name);
            Assert.Equal(value.Id, returnValue.Id);
            Assert.Equal(value.Name, returnValue.Name);
        }

        [Fact]
        public void Set_Generic_WithAbsoluteExpiration_SerializesAndStoresValue()
        {
            // Arrange
            var key = "test-key";
            var value = new TestClass { Id = 1, Name = "Test" };
            var expiration = DateTimeOffset.UtcNow.AddMinutes(5);

            // Act
            var returnValue = _cache.Set(key, value, expiration);

            // Assert
            var storedBytes = _cache.Get(key);
            Assert.NotNull(storedBytes);
            var storedValue = JsonSerializer.Deserialize<TestClass>(storedBytes);
            Assert.NotNull(storedValue);
            Assert.Equal(1, storedValue.Id);
            Assert.Equal("Test", storedValue.Name);
            Assert.Equal(value.Id, returnValue.Id);
            Assert.Equal(value.Name, returnValue.Name);
        }

        [Fact]
        public void Set_Generic_WithRelativeExpiration_SerializesAndStoresValue()
        {
            // Arrange
            var key = "test-key";
            var value = new TestClass { Id = 1, Name = "Test" };
            var expiration = TimeSpan.FromMinutes(5);

            // Act
            var returnValue = _cache.Set(key, value, expiration);

            // Assert
            var storedBytes = _cache.Get(key);
            Assert.NotNull(storedBytes);
            var storedValue = JsonSerializer.Deserialize<TestClass>(storedBytes);
            Assert.NotNull(storedValue);
            Assert.Equal(1, storedValue.Id);
            Assert.Equal("Test", storedValue.Name);
            Assert.Equal(value.Id, returnValue.Id);
            Assert.Equal(value.Name, returnValue.Name);
        }

        [Fact]
        public void Set_Generic_WithOptions_SerializesAndStoresValue()
        {
            // Arrange
            var key = "test-key";
            var value = new TestClass { Id = 1, Name = "Test" };
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };

            // Act
            var returnValue = _cache.Set(key, value, options);

            // Assert
            var storedBytes = _cache.Get(key);
            Assert.NotNull(storedBytes);
            var storedValue = JsonSerializer.Deserialize<TestClass>(storedBytes);
            Assert.NotNull(storedValue);
            Assert.Equal(1, storedValue.Id);
            Assert.Equal("Test", storedValue.Name);
            Assert.Equal(value.Id, returnValue.Id);
            Assert.Equal(value.Name, returnValue.Name);
        }

        [Fact]
        public async Task SetAsync_Generic_SerializesAndStoresValue()
        {
            // Arrange
            var key = "test-key";
            var value = new TestClass { Id = 1, Name = "Test" };

            // Act
            var returnValue = await _cache.SetAsync(key, value);

            // Assert
            var storedBytes = await _cache.GetAsync(key);
            Assert.NotNull(storedBytes);
            var storedValue = JsonSerializer.Deserialize<TestClass>(storedBytes);
            Assert.NotNull(storedValue);
            Assert.Equal(1, storedValue.Id);
            Assert.Equal("Test", storedValue.Name);
            Assert.Equal(value.Id, returnValue.Id);
            Assert.Equal(value.Name, returnValue.Name);
        }

        [Fact]
        public void GetOrCreate_ReturnsExistingValue_WhenKeyExists()
        {
            // Arrange
            var key = "test-key";
            var value = new TestClass { Id = 1, Name = "Test" };
            var serializedValue = JsonSerializer.SerializeToUtf8Bytes(value);
            _cache.Set(key, serializedValue);
            bool factoryCalled = false;

            // Act
            var result = _cache.GetOrCreate<TestClass>(key, () =>
            {
                factoryCalled = true;
                return new TestClass { Id = 2, Name = "New Test" };
            });

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test", result.Name);
            Assert.False(factoryCalled);
        }

        [Fact]
        public void GetOrCreate_CreatesAndReturnsNewValue_WhenKeyNotExists()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            bool factoryCalled = false;

            // Act
            var result = _cache.GetOrCreate<TestClass>(key, () =>
            {
                factoryCalled = true;
                return new TestClass { Id = 2, Name = "New Test" };
            });

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Id);
            Assert.Equal("New Test", result.Name);
            Assert.True(factoryCalled);

            // Verify it was added to the cache
            var storedBytes = _cache.Get(key);
            Assert.NotNull(storedBytes);
            var storedValue = JsonSerializer.Deserialize<TestClass>(storedBytes);
            Assert.NotNull(storedValue);
            Assert.Equal(2, storedValue.Id);
            Assert.Equal("New Test", storedValue.Name);
        }

        [Fact]
        public async Task GetOrCreateAsync_ReturnsExistingValue_WhenKeyExists()
        {
            // Arrange
            var key = "test-key";
            var value = new TestClass { Id = 1, Name = "Test" };
            var serializedValue = JsonSerializer.SerializeToUtf8Bytes(value);
            await _cache.SetAsync(key, serializedValue);
            bool factoryCalled = false;

            // Act
            var result = await _cache.GetOrCreateAsync<TestClass>(key, async () =>
            {
                factoryCalled = true;
                await Task.Delay(1); // Simulate async work
                return new TestClass { Id = 2, Name = "New Test" };
            });

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test", result.Name);
            Assert.False(factoryCalled);
        }

        [Fact]
        public async Task GetOrCreateAsync_CreatesAndReturnsNewValue_WhenKeyNotExists()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            bool factoryCalled = false;

            // Act
            var result = await _cache.GetOrCreateAsync<TestClass>(key, async () =>
            {
                factoryCalled = true;
                await Task.Delay(1); // Simulate async work
                return new TestClass { Id = 2, Name = "New Test" };
            });

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Id);
            Assert.Equal("New Test", result.Name);
            Assert.True(factoryCalled);

            // Verify it was added to the cache
            var storedBytes = await _cache.GetAsync(key);
            Assert.NotNull(storedBytes);
            var storedValue = JsonSerializer.Deserialize<TestClass>(storedBytes);
            Assert.NotNull(storedValue);
            Assert.Equal(2, storedValue.Id);
            Assert.Equal("New Test", storedValue.Name);
        }

        private class TestClass
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
