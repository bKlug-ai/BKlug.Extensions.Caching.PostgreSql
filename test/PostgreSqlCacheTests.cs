using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Xunit;

namespace BKlug.Extensions.Caching.PostgreSql.Tests
{
    [Collection("PostgreSqlCache collection")]
    public class PostgreSqlCacheTests
    {
        private readonly IDistributedCache _cache;
        public PostgreSqlCacheTests(PostgreSqlCacheFixture fixture)
        {
            _cache = fixture.Cache;
        }

        [Fact]
        public void Set_And_Get_Works_Synchronously()
        {
            var key = Guid.NewGuid().ToString();
            var value = new byte[] { 1, 2, 3 };
            var options = new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(5) };
            _cache.Set(key, value, options);
            var result = _cache.Get(key);
            Assert.NotNull(result);
            Assert.Equal(value, result);
        }

        [Fact]
        public async Task SetAsync_And_GetAsync_Works()
        {
            var key = Guid.NewGuid().ToString();
            var value = new byte[] { 4, 5, 6 };
            var options = new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(5) };
            await _cache.SetAsync(key, value, options);
            var result = await _cache.GetAsync(key);
            Assert.NotNull(result);
            Assert.Equal(value, result);
        }

        [Fact]
        public void Remove_Works_Synchronously()
        {
            var key = Guid.NewGuid().ToString();
            var value = new byte[] { 7, 8, 9 };
            var options = new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(5) };
            _cache.Set(key, value, options);
            _cache.Remove(key);
            var result = _cache.Get(key);
            Assert.Null(result);
        }

        [Fact]
        public async Task RemoveAsync_Works()
        {
            var key = Guid.NewGuid().ToString();
            var value = new byte[] { 10, 11, 12 };
            var options = new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(5) };
            await _cache.SetAsync(key, value, options);
            await _cache.RemoveAsync(key);
            var result = await _cache.GetAsync(key);
            Assert.Null(result);
        }

        [Fact]
        public void Set_NullKey_Throws()
        {
            var value = new byte[] { 1 };
            var options = new DistributedCacheEntryOptions();
            Assert.Throws<ArgumentNullException>(() => _cache.Set(null, value, options));
        }

        [Fact]
        public void Set_NullValue_Throws()
        {
            var key = Guid.NewGuid().ToString();
            var options = new DistributedCacheEntryOptions();
            Assert.Throws<ArgumentNullException>(() => _cache.Set(key, null, options));
        }

        [Fact]
        public void Set_NullOptions_Throws()
        {
            var key = Guid.NewGuid().ToString();
            var value = new byte[] { 1 };
            Assert.Throws<ArgumentNullException>(() => _cache.Set(key, value, null));
        }

        [Fact]
        public void Get_NullKey_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _cache.Get(null));
        }

        [Fact]
        public void Remove_NullKey_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _cache.Remove(null));
        }

        [Fact]
        public async Task SetAsync_NullKey_Throws()
        {
            var value = new byte[] { 1 };
            var options = new DistributedCacheEntryOptions();
            await Assert.ThrowsAsync<ArgumentNullException>(() => _cache.SetAsync(null, value, options));
        }

        [Fact]
        public async Task SetAsync_NullValue_Throws()
        {
            var key = Guid.NewGuid().ToString();
            var options = new DistributedCacheEntryOptions();
            await Assert.ThrowsAsync<ArgumentNullException>(() => _cache.SetAsync(key, null, options));
        }

        [Fact]
        public async Task SetAsync_NullOptions_Throws()
        {
            var key = Guid.NewGuid().ToString();
            var value = new byte[] { 1 };
            await Assert.ThrowsAsync<ArgumentNullException>(() => _cache.SetAsync(key, value, null));
        }

        [Fact]
        public async Task GetAsync_NullKey_Throws()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _cache.GetAsync(null));
        }

        [Fact]
        public async Task RemoveAsync_NullKey_Throws()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _cache.RemoveAsync(null));
        }

        [Fact]
        public void Refresh_Works_Synchronously()
        {
            var key = Guid.NewGuid().ToString();
            var value = new byte[] { 13, 14, 15 };
            var options = new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromSeconds(2) };
            _cache.Set(key, value, options);
            Thread.Sleep(1000);
            _cache.Refresh(key);
            Thread.Sleep(1500);
            var result = _cache.Get(key);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task RefreshAsync_Works()
        {
            var key = Guid.NewGuid().ToString();
            var value = new byte[] { 16, 17, 18 };
            var options = new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromSeconds(2) };
            await _cache.SetAsync(key, value, options);
            await Task.Delay(1000);
            await _cache.RefreshAsync(key);
            await Task.Delay(1500);
            var result = await _cache.GetAsync(key);
            Assert.NotNull(result);
        }

        [Fact]
        public void Refresh_NonExistentKey_DoesNotThrow()
        {
            var key = Guid.NewGuid().ToString();
            _cache.Refresh(key);
        }

        [Fact]
        public async Task RefreshAsync_NonExistentKey_DoesNotThrow()
        {
            var key = Guid.NewGuid().ToString();
            await _cache.RefreshAsync(key);
        }

        [Fact]
        public void Remove_NonExistentKey_DoesNotThrow()
        {
            var key = Guid.NewGuid().ToString();
            _cache.Remove(key);
        }

        [Fact]
        public async Task RemoveAsync_NonExistentKey_DoesNotThrow()
        {
            var key = Guid.NewGuid().ToString();
            await _cache.RemoveAsync(key);
        }

        [Fact]
        public void Set_WithoutExpiration_UsesDefaultSliding()
        {
            var key = Guid.NewGuid().ToString();
            var value = new byte[] { 19, 20, 21 };
            var options = new DistributedCacheEntryOptions();
            _cache.Set(key, value, options);
            var result = _cache.Get(key);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task SetAsync_WithoutExpiration_UsesDefaultSliding()
        {
            var key = Guid.NewGuid().ToString();
            var value = new byte[] { 22, 23, 24 };
            var options = new DistributedCacheEntryOptions();
            await _cache.SetAsync(key, value, options);
            var result = await _cache.GetAsync(key);
            Assert.NotNull(result);
        }
    }
}
