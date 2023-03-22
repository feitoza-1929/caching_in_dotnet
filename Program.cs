using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services => services.AddMemoryCache())
    .Build();

IMemoryCache cache =
    host.Services.GetRequiredService<IMemoryCache>();

static async ValueTask IterateOverAlphabetAsync(Func<char, Task> asyncFunc)
{
    for (char letter = 'A'; letter <= 'Z'; letter++)
    {
        await asyncFunc(letter);
    }
}

static void OnPostEviction(object key, object letter, EvictionReason reason, object? state)
{
    if(letter is AlphabetLetter)
        Console.WriteLine($"letter {letter} was evicted for {reason}");
}

var addLettersToCache = IterateOverAlphabetAsync(letter =>
{
    MemoryCacheEntryOptions options = new()
    { 
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(200)
    };

    _ = options.RegisterPostEvictionCallback(OnPostEviction);

    AlphabetLetter alphabetLetter =
        cache.Set(letter, new AlphabetLetter(letter), options);

    Console.WriteLine($"letter {letter} was cached {DateTime.UtcNow}\n");

    return Task.Delay(TimeSpan.FromMilliseconds(200));
});
await addLettersToCache;

var readLettersFromCache = IterateOverAlphabetAsync(letter =>
{
    if(cache.TryGetValue(letter, out object? value) && value is AlphabetLetter)
        Console.WriteLine($"letter {letter} still in the cache");

    return Task.CompletedTask;
});
await readLettersFromCache;
await host.RunAsync();

record AlphabetLetter(char letter)
{
    internal string Message
        => $"'{letter}' is the {letter - 64} letter in the alphabet";
}



