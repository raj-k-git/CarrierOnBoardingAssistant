using System.Collections.Concurrent;
using System.Text.Json;
using CarrierOnboarding.Core.Abstractions;
using CarrierOnboarding.Core.Models;

namespace CarrierOnboarding.Core.Vector;

/// <summary>
/// A process-local vector store backed by a dictionary and brute-force cosine
/// similarity. Implements <see cref="IVectorStore"/> so it can be swapped for a
/// SQLite- or Qdrant-backed store without touching the services that depend on it.
/// </summary>
public sealed class InMemoryVectorStore : IVectorStore
{
    private sealed record Entry(DocumentChunk Chunk, ReadOnlyMemory<float> Embedding);

    private sealed record PersistedEntry(DocumentChunk Chunk, float[] Embedding);

    // carrier (case-insensitive) -> chunkId -> entry
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Entry>> _byCarrier =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly string? _persistencePath;
    private readonly object _ioLock = new();

    public InMemoryVectorStore() : this(null)
    {
    }

    public InMemoryVectorStore(string? persistencePath)
    {
        _persistencePath = persistencePath;
        Load();
    }

    public Task UpsertAsync(
        IReadOnlyList<(DocumentChunk Chunk, ReadOnlyMemory<float> Embedding)> items,
        CancellationToken cancellationToken = default)
    {
        foreach (var (chunk, embedding) in items)
        {
            var map = _byCarrier.GetOrAdd(chunk.Carrier, _ => new ConcurrentDictionary<string, Entry>());
            map[chunk.Id] = new Entry(chunk, embedding);
        }

        Save();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        string carrier,
        ReadOnlyMemory<float> queryEmbedding,
        int topK,
        CancellationToken cancellationToken = default)
    {
        if (!_byCarrier.TryGetValue(carrier, out var map) || map.IsEmpty)
        {
            return Task.FromResult<IReadOnlyList<RetrievedChunk>>(Array.Empty<RetrievedChunk>());
        }

        var results = map.Values
            .Select(e => new RetrievedChunk
            {
                Chunk = e.Chunk,
                Score = CosineSimilarity(queryEmbedding.Span, e.Embedding.Span),
            })
            .OrderByDescending(r => r.Score)
            .Take(Math.Max(1, topK))
            .ToList();

        return Task.FromResult<IReadOnlyList<RetrievedChunk>>(results);
    }

    public Task DeleteCarrierAsync(string carrier, CancellationToken cancellationToken = default)
    {
        _byCarrier.TryRemove(carrier, out _);
        Save();
        return Task.CompletedTask;
    }

    internal static double CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length == 0 || a.Length != b.Length)
        {
            return 0d;
        }

        double dot = 0, magA = 0, magB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        if (magA == 0 || magB == 0)
        {
            return 0d;
        }

        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }

    private void Load()
    {
        if (string.IsNullOrWhiteSpace(_persistencePath) || !File.Exists(_persistencePath))
        {
            return;
        }

        lock (_ioLock)
        {
            var json = File.ReadAllText(_persistencePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, List<PersistedEntry>>>(json);
            if (data is null)
            {
                return;
            }

            foreach (var (carrier, entries) in data)
            {
                var map = _byCarrier.GetOrAdd(carrier, _ => new ConcurrentDictionary<string, Entry>());
                foreach (var e in entries)
                {
                    map[e.Chunk.Id] = new Entry(e.Chunk, e.Embedding);
                }
            }
        }
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(_persistencePath))
        {
            return;
        }

        lock (_ioLock)
        {
            var data = _byCarrier.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Values
                    .Select(e => new PersistedEntry(e.Chunk, e.Embedding.ToArray()))
                    .ToList());

            var dir = Path.GetDirectoryName(_persistencePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = false });
            File.WriteAllText(_persistencePath, json);
        }
    }
}
