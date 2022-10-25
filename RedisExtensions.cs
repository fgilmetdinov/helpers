using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Noname.Client.Helpers;

/// <summary>
/// Набор методов расширяющий IDistributedCache использующих возможности StackExchange.Redis
/// </summary>
public class RedisExtensions : IRedisExtensions, IDisposable
{
    private readonly Lazy<IConnectionMultiplexer> _redisConnection;

    /// <summary>
    /// Конструктор
    /// </summary>
    /// <param name="redisConfiguration"></param>
    public RedisExtensions(string redisConfiguration)
    {
        _redisConnection = new Lazy<IConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(redisConfiguration));
    }

    private IDatabase Database => _redisConnection.Value.GetDatabase();

    /// <summary>
    /// Попытка устанавить ключ на указанное время
    /// </summary>
    /// <param name="key">уникальный ключ</param>
    /// <param name="value">значение</param>
    /// <param name="expired">время после котого ключ будет удален</param>
    /// <returns>результат установки</returns>
    public async Task<bool> TrySetKeyAsync(string key, string value, TimeSpan expired)
    {
        return await Database
            .StringSetAsync(key, value, expired, When.NotExists, CommandFlags.None);
    }

    /// <summary>
    /// Записать в кэш на указанное время
    /// </summary>
    /// <param name="key">ключ</param>
    /// <param name="value">значение</param>
    /// <param name="expired">время после котого ключ будет удален</param>
    /// <returns>результат установки</returns>
    public async Task<bool> SetAsync(string key, string value, TimeSpan expired)
    {
        return await Database
            .StringSetAsync(key, value, expired);
    }

    /// <summary>
    /// Прочитать из кэша
    /// </summary>
    /// <param name="key">ключ</param>
    public async Task<string> GetAsync(string key)
    {
        return await Database
            .StringGetAsync(key);
    }

    /// <summary>
    /// Удалить из кэша
    /// </summary>
    /// <param name="key">Ключ</param>
    /// <returns>результат операции</returns>
    public async Task<bool> RemoveAsync(string key)
    {
        return await Database
            .KeyDeleteAsync(key);
    }

    /// <summary>
    /// Обработчик (consumer) очереди
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="channelName">имя очереди</param>
    /// <param name="action">обработчик сообщения</param>
    /// <param name="cancellationToken"></param>
    public async Task SubProcessAsync<T>(string channelName,
        Func<T, CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        var subscriber = _redisConnection.Value
            .GetSubscriber();

        await subscriber.SubscribeAsync(channelName, async (channel, message) =>
        {            
            var data = JsonSerializer.Deserialize<T>(message);
            await action(data, cancellationToken);
        });
    }

    /// <summary>
    /// Загрузчик очереди (producer)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="channelName">имя очереди</param>
    /// <param name="data">коллекция</param>
    public async Task PubProcessAsync<T>(string channelName, IEnumerable<T> data)
    {
        var publisher = _redisConnection.Value
            .GetSubscriber();

        foreach (var item in data)
        {
            string jsonString = JsonSerializer.Serialize(item);
            await publisher.PublishAsync(channelName, jsonString);
        }
    }

    /// <summary>
    /// Возвращает следующее уникальноое значение из зацикленоого sequence с 1000 до 9999 
    /// </summary>
    /// <param name="sequenceKey"></param>
    /// <param name="startValue"></param>
    /// <param name="maxValue"></param>
    /// <returns></returns>
    public async Task<int> GetNextSequenceValueAsync(string sequenceKey, int startValue = 1000, int maxValue = 9999)
    {
        const string script = @"
                local result = redis.call('incr', KEYS[1])
                if result > tonumber(KEYS[3]) or result < tonumber(KEYS[2]) then
                    result = tonumber(KEYS[2])
                    redis.call('set', KEYS[1], result)
                end
                return result";

        var result = await Database
            .ScriptEvaluateAsync(script, new RedisKey[] { sequenceKey, startValue.ToString(), maxValue.ToString() });

        return (int)result;
    }


    /// <summary>
    /// Освобождение ресурсов
    /// </summary>
    public void Dispose()
    {
        if (_redisConnection != null)
        {
            _redisConnection.Value
                .Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
