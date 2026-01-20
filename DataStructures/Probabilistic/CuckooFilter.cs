using System.Runtime.CompilerServices;

namespace DataStructures.Probabilistic;

/// <summary>
/// A high-performance Cuckoo Filter implementation for approximate set membership queries.
/// Supports insertion, lookup, and deletion operations with configurable false positive rate.
/// Uses modern C# constructs for optimal performance.
/// </summary>
/// <typeparam name="T">The type of items to store in the filter.</typeparam>
public class CuckooFilter<T> where T : notnull
{
    private const int MaxKicks = 500;

    /// <summary>
    /// Returns the next power of two greater than or equal to the input.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int NextPowerOfTwo(int value)
    {
        if (value <= 0)
        {
            return 1;
        }

        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        return value + 1;
    }

    private readonly int bucketSize;
    private readonly int fingerprintSize;
    private readonly uint fingerprintMask;
    private readonly uint[][] buckets;
    private readonly int numBuckets;
    private int count;

    /// <summary>
    /// Initializes a new instance of the <see cref="CuckooFilter{T}"/> class with optimal parameters.
    /// </summary>
    /// <param name="capacity">Expected number of elements to store.</param>
    /// <param name="bucketSize">Number of entries per bucket (default: 4, recommended: 2, 4, or 8).</param>
    /// <param name="fingerprintBits">Size of fingerprint in bits (default: 8, range: 4-16).</param>
    public CuckooFilter(int capacity, int bucketSize = 4, int fingerprintBits = 8)
    {
        if (capacity <= 0)
        {
            throw new ArgumentException("Capacity must be positive", nameof(capacity));
        }

        if (bucketSize <= 0 || bucketSize > 8)
        {
            throw new ArgumentException("Bucket size must be between 1 and 8", nameof(bucketSize));
        }

        if (fingerprintBits < 4 || fingerprintBits > 16)
        {
            throw new ArgumentException("Fingerprint size must be between 4 and 16 bits", nameof(fingerprintBits));
        }

        this.bucketSize = bucketSize;
        fingerprintSize = fingerprintBits;
        fingerprintMask = (1u << fingerprintBits) - 1;

        // Calculate number of buckets for target load factor of ~95%
        numBuckets = NextPowerOfTwo((int)Math.Ceiling(capacity / (bucketSize * 0.95)));

        buckets = new uint[numBuckets][];
        for (int i = 0; i < numBuckets; i++)
        {
            buckets[i] = new uint[bucketSize];
        }

        count = 0;
    }

    /// <summary>
    /// Gets the number of items currently stored in the filter (approximate).
    /// </summary>
    public int Count => count;

    /// <summary>
    /// Gets the current load factor of the filter.
    /// </summary>
    public double LoadFactor => (double)count / (numBuckets * bucketSize);

    /// <summary>
    /// Gets the capacity of the filter.
    /// </summary>
    public int Capacity => numBuckets * bucketSize;

    /// <summary>
    /// Inserts an item into the cuckoo filter.
    /// </summary>
    /// <param name="item">The item to insert.</param>
    /// <returns>true if insertion succeeded; false if filter is full.</returns>
    public bool Insert(T item)
    {
        var (fingerprint, index1) = ComputeIndexAndFingerprint(item);
        var index2 = ComputeAlternateIndex(index1, fingerprint);

        // Try to insert in the first bucket
        if (TryInsertInBucket(index1, fingerprint))
        {
            count++;
            return true;
        }

        // Try to insert in the second bucket
        if (TryInsertInBucket(index2, fingerprint))
        {
            count++;
            return true;
        }

        // Both buckets are full, need to perform cuckoo eviction
        return CuckooInsert(index1, index2, fingerprint);
    }

    /// <summary>
    /// Checks if an item might exist in the filter.
    /// </summary>
    /// <param name="item">The item to look up.</param>
    /// <returns>true if item might exist (possible false positive); false if item definitely does not exist.</returns>
    public bool Contains(T item)
    {
        var (fingerprint, index1) = ComputeIndexAndFingerprint(item);
        var index2 = ComputeAlternateIndex(index1, fingerprint);

        return ContainsFingerprintInBucket(index1, fingerprint) ||
               ContainsFingerprintInBucket(index2, fingerprint);
    }

    /// <summary>
    /// Deletes an item from the filter.
    /// </summary>
    /// <param name="item">The item to delete.</param>
    /// <returns>true if item was found and deleted; false otherwise.</returns>
    /// <remarks>
    /// Note: Deleting an item that was never inserted may cause false negatives for other items.
    /// Only delete items that were previously inserted.
    /// </remarks>
    public bool Delete(T item)
    {
        var (fingerprint, index1) = ComputeIndexAndFingerprint(item);
        var index2 = ComputeAlternateIndex(index1, fingerprint);

        if (DeleteFingerprintFromBucket(index1, fingerprint))
        {
            count--;
            return true;
        }

        if (DeleteFingerprintFromBucket(index2, fingerprint))
        {
            count--;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Computes the fingerprint and primary index for an item.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (uint Fingerprint, int Index) ComputeIndexAndFingerprint(T item)
    {
        var hash = (uint)item.GetHashCode();

        // Use upper bits for fingerprint, lower bits for index
        var fingerprint = (hash >> 16) & fingerprintMask;

        // Ensure fingerprint is never zero (would cause issues with XOR)
        if (fingerprint == 0)
        {
            fingerprint = 1;
        }

        var index = (int)(hash & (numBuckets - 1));

        return (fingerprint, index);
    }

    /// <summary>
    /// Computes the alternate index using partial-key cuckoo hashing.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ComputeAlternateIndex(int index, uint fingerprint)
    {
        // Hash the fingerprint using a simple but effective hash function
        var hash = HashFingerprint(fingerprint);
        return (int)((index ^ hash) & (numBuckets - 1));
    }

    /// <summary>
    /// Tries to insert a fingerprint in a bucket.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryInsertInBucket(int bucketIndex, uint fingerprint)
    {
        var bucket = buckets[bucketIndex];
        for (int i = 0; i < bucketSize; i++)
        {
            if (bucket[i] == 0)
            {
                bucket[i] = fingerprint;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a fingerprint exists in a bucket.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ContainsFingerprintInBucket(int bucketIndex, uint fingerprint)
    {
        var bucket = buckets[bucketIndex];
        for (int i = 0; i < bucketSize; i++)
        {
            if (bucket[i] == fingerprint)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Deletes a fingerprint from a bucket.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool DeleteFingerprintFromBucket(int bucketIndex, uint fingerprint)
    {
        var bucket = buckets[bucketIndex];
        for (int i = 0; i < bucketSize; i++)
        {
            if (bucket[i] == fingerprint)
            {
                bucket[i] = 0;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Performs cuckoo insertion with eviction.
    /// </summary>
    private bool CuckooInsert(int index1, int index2, uint fingerprint)
    {
        var random = Random.Shared;
        var currentIndex = random.Next(2) == 0 ? index1 : index2;
        var currentFingerprint = fingerprint;

        // Track the path for potential rollback
        var swapHistory = new List<(int BucketIndex, int Position, uint OldValue)>();

        for (int i = 0; i < MaxKicks; i++)
        {
            // Randomly select a position in the bucket to evict
            var pos = random.Next(bucketSize);
            var bucket = buckets[currentIndex];

            // Record the swap for potential rollback
            var temp = bucket[pos];
            swapHistory.Add((currentIndex, pos, temp));

            // Swap the fingerprints
            bucket[pos] = currentFingerprint;
            currentFingerprint = temp;

            // Calculate alternate index for the evicted fingerprint
            currentIndex = ComputeAlternateIndex(currentIndex, currentFingerprint);

            // Try to insert the evicted fingerprint
            if (TryInsertInBucket(currentIndex, currentFingerprint))
            {
                count++;
                return true;
            }
        }

        // Failed to insert after max kicks - rollback all changes to prevent data loss
        for (int i = swapHistory.Count - 1; i >= 0; i--)
        {
            var (bucketIndex, position, oldValue) = swapHistory[i];
            buckets[bucketIndex][position] = oldValue;
        }

        return false;
    }

    /// <summary>
    /// Hashes a fingerprint value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint HashFingerprint(uint fingerprint)
    {
        // Simple multiplicative hash using MurmurHash2 constant for good distribution
        return fingerprint * 0x5bd1e995;
    }
}
