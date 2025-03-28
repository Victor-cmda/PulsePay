using Application.DTOs;
using Microsoft.Extensions.Caching.Memory;

namespace Application.Services
{
    public interface IPixValidationCacheService
    {
        Task<PixKeyValidationDto> GetValidationAsync(string validationId);
        Task StoreValidationAsync(PixKeyValidationDto validation);
        Task<bool> RemoveValidationAsync(string validationId);
    }

    public class PixValidationCacheService : IPixValidationCacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30); // Definir um tempo adequado (ex: 30 minutos)

        public PixValidationCacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public Task<PixKeyValidationDto> GetValidationAsync(string validationId)
        {
            if (string.IsNullOrEmpty(validationId))
                return Task.FromResult<PixKeyValidationDto>(null);

            if (_memoryCache.TryGetValue<PixKeyValidationDto>($"PIX_VALIDATION_{validationId}", out var cachedValidation))
            {
                return Task.FromResult(cachedValidation);
            }

            return Task.FromResult<PixKeyValidationDto>(null);
        }

        public Task StoreValidationAsync(PixKeyValidationDto validation)
        {
            if (validation == null || string.IsNullOrEmpty(validation.ValidationId))
                return Task.CompletedTask;

            var cacheKey = $"PIX_VALIDATION_{validation.ValidationId}";
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(_cacheExpiration);

            _memoryCache.Set(cacheKey, validation, cacheEntryOptions);

            return Task.CompletedTask;
        }

        public Task<bool> RemoveValidationAsync(string validationId)
        {
            if (string.IsNullOrEmpty(validationId))
                return Task.FromResult(false);

            var cacheKey = $"PIX_VALIDATION_{validationId}";
            _memoryCache.Remove(cacheKey);

            return Task.FromResult(true);
        }
    }
}