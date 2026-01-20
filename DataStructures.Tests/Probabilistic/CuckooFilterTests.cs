using DataStructures.Probabilistic;

namespace DataStructures.Tests.Probabilistic;

public class CuckooFilterTests
{
    [Test]
    public void Constructor_WithValidParameters_CreatesFilter()
    {
        var filter = new CuckooFilter<int>(capacity: 1000);
        Assert.That(filter, Is.Not.Null);
        Assert.That(filter.Count, Is.EqualTo(0));
        Assert.That(filter.Capacity, Is.GreaterThan(0));
    }

    [Test]
    public void Constructor_WithInvalidCapacity_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new CuckooFilter<int>(capacity: 0));
        Assert.Throws<ArgumentException>(() => new CuckooFilter<int>(capacity: -1));
    }

    [Test]
    public void Constructor_WithInvalidBucketSize_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new CuckooFilter<int>(capacity: 1000, bucketSize: 0));
        Assert.Throws<ArgumentException>(() => new CuckooFilter<int>(capacity: 1000, bucketSize: 9));
    }

    [Test]
    public void Constructor_WithInvalidFingerprintBits_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new CuckooFilter<int>(capacity: 1000, bucketSize: 4, fingerprintBits: 3));
        Assert.Throws<ArgumentException>(() => new CuckooFilter<int>(capacity: 1000, bucketSize: 4, fingerprintBits: 17));
    }

    [Test]
    public void Insert_SingleItem_ReturnsTrue()
    {
        var filter = new CuckooFilter<int>(capacity: 100);
        var result = filter.Insert(42);
        Assert.That(result, Is.True);
        Assert.That(filter.Count, Is.EqualTo(1));
    }

    [Test]
    public void Insert_MultipleDistinctItems_ReturnsTrue()
    {
        var filter = new CuckooFilter<int>(capacity: 100);
        for (int i = 0; i < 50; i++)
        {
            var result = filter.Insert(i);
            Assert.That(result, Is.True);
        }
        Assert.That(filter.Count, Is.EqualTo(50));
    }

    [Test]
    public void Insert_DuplicateItems_IncreasesCount()
    {
        var filter = new CuckooFilter<int>(capacity: 100);
        filter.Insert(42);
        filter.Insert(42);
        Assert.That(filter.Count, Is.EqualTo(2));
    }

    [Test]
    public void Contains_InsertedItem_ReturnsTrue()
    {
        var filter = new CuckooFilter<int>(capacity: 100);
        filter.Insert(42);
        Assert.That(filter.Contains(42), Is.True);
    }

    [Test]
    public void Contains_NotInsertedItem_ReturnsFalse()
    {
        var filter = new CuckooFilter<int>(capacity: 100);
        filter.Insert(42);
        Assert.That(filter.Contains(100), Is.False);
    }

    [Test]
    public void Contains_MultipleInsertedItems_AllReturnTrue()
    {
        var filter = new CuckooFilter<int>(capacity: 100);
        var items = new[] { 1, 2, 3, 5, 8, 13, 21, 34 };
        
        foreach (var item in items)
        {
            filter.Insert(item);
        }

        foreach (var item in items)
        {
            Assert.That(filter.Contains(item), Is.True);
        }
    }

    [Test]
    public void Delete_InsertedItem_ReturnsTrue()
    {
        var filter = new CuckooFilter<int>(capacity: 100);
        filter.Insert(42);
        var result = filter.Delete(42);
        Assert.That(result, Is.True);
        Assert.That(filter.Count, Is.EqualTo(0));
    }

    [Test]
    public void Delete_NotInsertedItem_ReturnsFalse()
    {
        var filter = new CuckooFilter<int>(capacity: 100);
        var result = filter.Delete(42);
        Assert.That(result, Is.False);
    }

    [Test]
    public void Delete_AfterDeletion_ContainsReturnsFalse()
    {
        var filter = new CuckooFilter<int>(capacity: 100);
        filter.Insert(42);
        filter.Delete(42);
        Assert.That(filter.Contains(42), Is.False);
    }

    [Test]
    public void Delete_DuplicateItems_DeletesOnlyOne()
    {
        var filter = new CuckooFilter<int>(capacity: 100);
        filter.Insert(42);
        filter.Insert(42);
        filter.Delete(42);
        Assert.That(filter.Contains(42), Is.True);
        Assert.That(filter.Count, Is.EqualTo(1));
    }

    [Test]
    public void InsertDeleteCycle_MaintainsCorrectness()
    {
        var filter = new CuckooFilter<int>(capacity: 100);
        
        // Insert items
        for (int i = 0; i < 10; i++)
        {
            filter.Insert(i);
        }

        // Delete some items
        for (int i = 0; i < 5; i++)
        {
            filter.Delete(i);
        }

        // Check remaining items
        for (int i = 0; i < 5; i++)
        {
            Assert.That(filter.Contains(i), Is.False);
        }

        for (int i = 5; i < 10; i++)
        {
            Assert.That(filter.Contains(i), Is.True);
        }

        Assert.That(filter.Count, Is.EqualTo(5));
    }

    [Test]
    public void FalsePositiveRate_WithinExpectedBounds()
    {
        var capacity = 10000;
        var filter = new CuckooFilter<int>(capacity: capacity, bucketSize: 4, fingerprintBits: 8);
        var rand = new Random(42);
        var insertedSet = new HashSet<int>();

        // Insert items
        for (int i = 0; i < capacity / 2; i++)
        {
            var value = rand.Next(0, 1000000);
            filter.Insert(value);
            insertedSet.Add(value);
        }

        // Test false positives
        int falsePositives = 0;
        int tested = 0;
        for (int i = 0; i < 10000; i++)
        {
            var value = rand.Next(1000000, 2000000); // Values not in inserted range
            if (!insertedSet.Contains(value))
            {
                tested++;
                if (filter.Contains(value))
                {
                    falsePositives++;
                }
            }
        }

        var falsePositiveRate = (double)falsePositives / tested;
        
        // For 8-bit fingerprints and bucket size 4, expect FPR around 3-5%
        Assert.That(falsePositiveRate, Is.LessThan(0.10), 
            $"False positive rate {falsePositiveRate:P2} exceeds expected maximum of 10%");
    }

    [Test]
    public void LoadFactor_CalculatedCorrectly()
    {
        var filter = new CuckooFilter<int>(capacity: 100);
        
        Assert.That(filter.LoadFactor, Is.EqualTo(0.0).Within(0.001));
        
        for (int i = 0; i < 50; i++)
        {
            filter.Insert(i);
        }

        var expectedLoadFactor = 50.0 / filter.Capacity;
        Assert.That(filter.LoadFactor, Is.EqualTo(expectedLoadFactor).Within(0.001));
    }

    [Test]
    public void HighLoadFactor_StillAcceptsInsertions()
    {
        var filter = new CuckooFilter<int>(capacity: 100, bucketSize: 4);
        var successfulInsertions = 0;

        // Try to fill the filter to high capacity
        for (int i = 0; i < 200; i++)
        {
            if (filter.Insert(i))
            {
                successfulInsertions++;
            }
        }

        // Should be able to insert a significant portion
        Assert.That(successfulInsertions, Is.GreaterThan(80));
    }

    [Test]
    public void WorksWithStringType()
    {
        var filter = new CuckooFilter<string>(capacity: 100);
        var strings = new[] { "hello", "world", "cuckoo", "filter", "test" };

        foreach (var str in strings)
        {
            filter.Insert(str);
        }

        foreach (var str in strings)
        {
            Assert.That(filter.Contains(str), Is.True);
        }

        Assert.That(filter.Contains("notinserted"), Is.False);
    }

    [Test]
    public void WorksWithCustomType()
    {
        var filter = new CuckooFilter<CustomObject>(capacity: 100);
        var obj1 = new CustomObject("Alice", 30);
        var obj2 = new CustomObject("Bob", 25);
        var obj3 = new CustomObject("Charlie", 35);

        filter.Insert(obj1);
        filter.Insert(obj2);

        Assert.That(filter.Contains(obj1), Is.True);
        Assert.That(filter.Contains(obj2), Is.True);
        Assert.That(filter.Contains(obj3), Is.False);
    }

    [Test]
    public void StressTest_LargeNumberOfOperations()
    {
        var filter = new CuckooFilter<int>(capacity: 10000);
        var rand = new Random(123);
        var insertedItems = new HashSet<int>();

        // Perform mixed operations
        for (int i = 0; i < 5000; i++)
        {
            var value = rand.Next(0, 100000);
            
            if (rand.Next(2) == 0) // 50% insert
            {
                if (filter.Insert(value))
                {
                    insertedItems.Add(value);
                }
            }
            else if (insertedItems.Count > 0 && rand.Next(3) == 0) // 16.7% delete
            {
                var itemToDelete = insertedItems.ElementAt(rand.Next(insertedItems.Count));
                filter.Delete(itemToDelete);
                insertedItems.Remove(itemToDelete);
            }
        }

        // Verify all inserted items are found
        int notFound = 0;
        foreach (var item in insertedItems)
        {
            if (!filter.Contains(item))
            {
                notFound++;
            }
        }

        // Should have no false negatives
        Assert.That(notFound, Is.EqualTo(0), "Filter should not have false negatives");
    }

    [Test]
    public void DifferentBucketSizes_AllWork()
    {
        var bucketSizes = new[] { 2, 4, 8 };

        foreach (var bucketSize in bucketSizes)
        {
            var filter = new CuckooFilter<int>(capacity: 100, bucketSize: bucketSize);
            
            for (int i = 0; i < 50; i++)
            {
                filter.Insert(i);
            }

            for (int i = 0; i < 50; i++)
            {
                Assert.That(filter.Contains(i), Is.True, 
                    $"Bucket size {bucketSize} failed for item {i}");
            }
        }
    }

    [Test]
    public void DifferentFingerprintSizes_AllWork()
    {
        var fingerprintBits = new[] { 4, 8, 12, 16 };

        foreach (var bits in fingerprintBits)
        {
            var filter = new CuckooFilter<int>(capacity: 100, bucketSize: 4, fingerprintBits: bits);
            
            for (int i = 0; i < 50; i++)
            {
                filter.Insert(i);
            }

            for (int i = 0; i < 50; i++)
            {
                Assert.That(filter.Contains(i), Is.True, 
                    $"Fingerprint bits {bits} failed for item {i}");
            }
        }
    }

    [Test]
    public void FailedInsert_RollsBackChanges_NoDataLoss()
    {
        // Create a small filter that will fill up quickly
        var filter = new CuckooFilter<int>(capacity: 20, bucketSize: 2, fingerprintBits: 8);
        var insertedItems = new HashSet<int>();
        
        // Fill the filter to near capacity
        for (int i = 0; i < 30; i++)
        {
            if (filter.Insert(i))
            {
                insertedItems.Add(i);
            }
        }

        // Track items before attempting to overflow
        var itemsBeforeOverflow = new HashSet<int>(insertedItems);
        
        // Try to insert more items until we get a failure
        bool failureOccurred = false;
        for (int i = 100; i < 200 && !failureOccurred; i++)
        {
            if (!filter.Insert(i))
            {
                failureOccurred = true;
                
                // Verify all previously inserted items are still in the filter
                foreach (var item in itemsBeforeOverflow)
                {
                    Assert.That(filter.Contains(item), Is.True, 
                        $"Item {item} was lost after failed insertion! This indicates data loss.");
                }
            }
            else
            {
                insertedItems.Add(i);
                itemsBeforeOverflow.Add(i);
            }
        }

        // Ensure we actually tested the failure case
        Assert.That(failureOccurred, Is.True, "Test should have triggered at least one insertion failure");
    }

    private class CustomObject(string name, int age)
    {
        public string Name { get; } = name;
        public int Age { get; } = age;

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Age);
        }

        public override bool Equals(object? obj)
        {
            return obj is CustomObject other && Name == other.Name && Age == other.Age;
        }
    }
}
