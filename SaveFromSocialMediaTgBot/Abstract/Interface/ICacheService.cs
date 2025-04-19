
namespace SaveFromSocialMediaTgBot.Abstract.Interface;

public interface ICacheService
{
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> fetchFunction);
    Task SetAsync<T>(string key, T value);
}