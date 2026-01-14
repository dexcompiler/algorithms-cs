# Probabilistic Data Structures

## Introduction

Probabilistic data structures are specialized data structures that use randomization and hashing to provide memory-efficient solutions to common computational problems. Unlike traditional deterministic data structures, probabilistic structures trade perfect accuracy for significant gains in space efficiency and performance. They are particularly valuable when working with massive datasets where exact answers are not strictly required.

### Key Characteristics

- **Space Efficiency**: Use significantly less memory compared to deterministic alternatives
- **Probabilistic Guarantees**: Provide approximate answers with quantifiable error bounds
- **One-Sided Errors**: Typically designed to never produce false negatives (no missed items)
- **Hashing-Based**: Rely on hash functions to distribute data across a compact representation
- **Streaming-Friendly**: Well-suited for processing continuous data streams

## Common Probabilistic Data Structures

### Bloom Filter

A Bloom filter is a space-efficient probabilistic data structure used to test whether an element is a member of a set. It was invented by Burton Howard Bloom in 1970.

#### How It Works

- Uses a bit array of size `m` and `k` independent hash functions
- **Insertion**: Hash the element with all `k` functions and set corresponding bits to 1
- **Lookup**: Hash the element and check if all corresponding bits are 1
- **Deletion**: Not supported in standard Bloom filters

#### Properties

- **False Positives**: Possible (may claim an element exists when it doesn't)
- **False Negatives**: Impossible (never misses an element that was inserted)
- **Space Complexity**: O(m) where m is the bit array size
- **Time Complexity**: O(k) for both insertion and lookup

#### Optimal Parameters

For `n` expected elements and desired false positive rate `p`:
- Optimal bit array size: `m = -n ln(p) / (ln(2))²`
- Optimal hash functions: `k = (m/n) ln(2)`

#### Use Cases

- Checking if a username is already taken
- Caching which URLs have been crawled
- Preventing weak password selection
- Network packet filtering

### Count-Min Sketch

A Count-Min Sketch is a probabilistic data structure that serves as a frequency table of events in a stream of data. It was introduced by Cormode and Muthukrishnan in 2005.

#### How It Works

- Uses a 2D array (width × depth) where depth equals number of hash functions
- **Insertion**: Hash the element with each hash function, increment counters in each row
- **Query**: Hash the element with each hash function, return minimum count across all rows

#### Properties

- **Over-counting**: Possible (may over-estimate frequency)
- **Under-counting**: Impossible (never under-estimates)
- **Space Complexity**: O(w × d) where w is width and d is depth
- **Time Complexity**: O(d) for both operations

#### Optimal Parameters

For error bound `ε` and probability `δ`:
- Width: `w = ⌈e/ε⌉`
- Depth: `d = ⌈ln(1/δ)⌉`

#### Use Cases

- Finding heavy hitters in network traffic
- Tracking word frequencies in text streams
- Real-time analytics on streaming data
- Database query optimization

### HyperLogLog

HyperLogLog is a probabilistic cardinality estimation algorithm. It can estimate the number of distinct elements in a multiset with very little memory.

#### How It Works

- Divides hash space into `m` buckets (typically m = 2^b where b is precision)
- For each element, uses first `b` bits to determine bucket, counts leading zeros in remaining bits
- Estimates cardinality using harmonic mean of maximum leading zeros in each bucket

#### Properties

- **Standard Error**: ~1.04/√m
- **Space Complexity**: O(m log log n) where n is cardinality
- **Time Complexity**: O(1) for insertion, O(m) for cardinality estimation

#### Use Cases

- Counting unique visitors to a website
- Estimating distinct database values
- Monitoring unique IP addresses in network traffic
- Counting distinct elements in big data systems

## Cuckoo Filter

The Cuckoo Filter is a probabilistic data structure for approximate set membership queries. It was introduced by Fan, Andersen, Kaminsky, and Mitzenmacher in 2014 as an improvement over Bloom filters.

### Why Cuckoo Filter?

Cuckoo filters solve a major limitation of Bloom filters: **the inability to delete elements**. They also provide better lookup performance and better space efficiency than Bloom filters in many practical scenarios.

### How It Works

#### Core Concepts

1. **Buckets and Fingerprints**: Instead of a bit array, uses an array of buckets where each bucket can store multiple fingerprints (compact hashes of items)

2. **Two-Way Associativity**: Each item can be stored in one of two possible buckets, determined by:
   - Primary bucket: `h₁(x)`
   - Secondary bucket: `h₁(x) ⊕ h₂(fingerprint(x))`
   - The XOR relationship allows calculating either position from the other

3. **Insertion Algorithm**:
   - Calculate fingerprint of item
   - Try to insert into primary bucket
   - If full, try secondary bucket
   - If both full, evict random entry and reinsert it (cuckoo hashing)
   - Repeat eviction process with maximum number of kicks
   - If kicks exceeded, filter is full

4. **Lookup Algorithm**:
   - Check if fingerprint exists in either of the two possible buckets
   - Return true if found, false otherwise

5. **Deletion Algorithm**:
   - Find fingerprint in one of the two buckets
   - Remove it
   - This is safe because we know the item was inserted

#### Properties

- **False Positives**: Possible but controllable (better than Bloom filter)
- **False Negatives**: Impossible for inserted items
- **Deletion**: Supported (major advantage)
- **Space Complexity**: Comparable to Bloom filter for same false positive rate
- **Lookup Performance**: Better than Bloom filter (fewer hash lookups)

#### Parameters

- **Bucket Size (b)**: Number of entries per bucket (typically 2, 4, or 8)
- **Fingerprint Size (f)**: Bits per fingerprint (typically 4-16 bits)
- **Load Factor (α)**: Ratio of inserted items to total capacity (typically ≤ 95%)

#### Optimal Configuration

For target false positive rate `ε`:
- Fingerprint size: `f ≥ ⌈log₂(1/ε) + log₂(2b)⌉` bits
- Bucket size: 4 provides good balance of space and performance
- Load factor: 95% achievable with bucket size 4

### Advantages Over Bloom Filter

1. **Deletion Support**: Can remove items without rebuilding the entire structure
2. **Better Lookup Performance**: Requires checking fewer locations
3. **Better Space Efficiency**: For false positive rates < 3%, uses less space
4. **Better Cache Locality**: Consecutive bucket checks improve CPU cache hits
5. **Simplicity**: Uses only one or two hash computations per operation

### Disadvantages

1. **Insertion Complexity**: May require multiple relocations (kicks)
2. **Insertion Failure**: Can fail when filter is nearly full
3. **False Positive on Deletion**: Deleting non-existent items can create false negatives

### Use Cases

- **Deduplication**: Detecting duplicate content where items may be removed
- **Cache Filtering**: Web caches that need to track and remove expired entries
- **Network Routing**: Router tables where routes are added and removed
- **Database Systems**: Query optimization with dynamic data
- **Security**: Tracking and removing malicious IPs or patterns

## Performance Comparison

### Space Efficiency

For 1% false positive rate with 1 million elements:

| Structure | Space per Element | Total Space |
|-----------|------------------|-------------|
| Hash Set | 32-64 bits | 4-8 MB |
| Bloom Filter | ~9.6 bits | ~1.2 MB |
| Cuckoo Filter | ~12.5 bits | ~1.5 MB |

For 0.1% false positive rate:

| Structure | Space per Element | Total Space |
|-----------|------------------|-------------|
| Bloom Filter | ~14.4 bits | ~1.8 MB |
| Cuckoo Filter | ~13.5 bits | ~1.7 MB |

### Operation Performance

| Structure | Insert | Lookup | Delete | Hash Computations |
|-----------|--------|--------|--------|-------------------|
| Bloom Filter | O(k) | O(k) | ❌ | k (typically 7-8) |
| Cuckoo Filter | O(1)* | O(2) | O(2) | 2-3 |
| Count-Min Sketch | O(d) | O(d) | ❌ | d |
| HyperLogLog | O(1) | ❌ | ❌ | 1 |

*Amortized; worst case may require relocations

## Implementation Considerations

### Modern C# Optimizations

When implementing probabilistic data structures in C#, consider:

1. **Span&lt;T&gt; and Memory&lt;T&gt;**: Reduce allocations for temporary buffers
2. **stackalloc**: Stack-allocate small arrays for better cache locality
3. **BitArray vs byte[]**: Choose based on access patterns
4. **HashCode.Combine**: Use built-in hash combining for better distribution
5. **SIMD**: Use System.Runtime.Intrinsics for vectorized operations
6. **Aggressive Inlining**: Use `[MethodImpl(MethodImplOptions.AggressiveInlining)]` for hot paths
7. **Generic Math**: Use .NET 7+ generic math interfaces for flexibility
8. **ArrayPool**: Reuse arrays to reduce GC pressure

### Hash Function Selection

Good hash functions for probabilistic structures should:
- Provide uniform distribution
- Be fast to compute
- Minimize collisions
- Be deterministic

Common choices:
- **MurmurHash3**: Fast and good distribution
- **xxHash**: Very fast, excellent for non-cryptographic use
- **FNV-1a**: Simple and fast for small inputs
- **System.HashCode**: Built-in, combines multiple values well

### Testing Strategies

1. **False Positive Rate Testing**: Insert known set, test random non-members
2. **Capacity Testing**: Test behavior near theoretical limits
3. **Collision Testing**: Verify hash distribution quality
4. **Performance Benchmarking**: Measure actual vs theoretical performance
5. **Memory Profiling**: Verify space usage matches expectations

## Further Reading

### Papers

- Bloom, B. H. (1970). "Space/time trade-offs in hash coding with allowable errors"
- Fan, B., et al. (2014). "Cuckoo Filter: Practically Better Than Bloom"
- Cormode, G., & Muthukrishnan, S. (2005). "An improved data stream summary: the count-min sketch"
- Flajolet, P., et al. (2007). "HyperLogLog: the analysis of a near-optimal cardinality estimation algorithm"

### Resources

- [Probabilistic Data Structures and Algorithms](https://github.com/tylertreat/BoomFilters)
- [Modern Probabilistic Data Structures](https://www.slideshare.net/slideshow/probabilistic-data-structures/32665336)
- [Redis Probabilistic Data Structures](https://redis.io/docs/data-types/probabilistic/)

## Conclusion

Probabilistic data structures are powerful tools for solving problems at scale. By accepting small, controlled error rates, they provide dramatic improvements in space efficiency and performance. The choice between different structures depends on specific requirements:

- **Need membership testing only**: Bloom Filter
- **Need membership with deletion**: Cuckoo Filter
- **Need frequency counting**: Count-Min Sketch  
- **Need cardinality estimation**: HyperLogLog

Understanding the trade-offs and optimal parameters for each structure enables building efficient, scalable systems.
