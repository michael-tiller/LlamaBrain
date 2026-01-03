using NUnit.Framework;
using LlamaBrain.Persona;

namespace LlamaBrain.Tests.Persona
{
    /// <summary>
    /// Tests for IIdGenerator implementations.
    /// Covers GuidIdGenerator and SequentialIdGenerator.
    /// </summary>
    [TestFixture]
    public class IdGeneratorTests
    {
        #region GuidIdGenerator Tests

        [Test]
        public void GuidIdGenerator_GenerateId_ReturnsNonNullString()
        {
            // Arrange
            var generator = new GuidIdGenerator();

            // Act
            var id = generator.GenerateId();

            // Assert
            Assert.IsNotNull(id);
            Assert.IsNotEmpty(id);
        }

        [Test]
        public void GuidIdGenerator_GenerateId_Returns8CharacterString()
        {
            // Arrange
            var generator = new GuidIdGenerator();

            // Act
            var id = generator.GenerateId();

            // Assert
            Assert.AreEqual(8, id.Length);
        }

        [Test]
        public void GuidIdGenerator_GenerateId_ReturnsHexadecimalString()
        {
            // Arrange
            var generator = new GuidIdGenerator();

            // Act
            var id = generator.GenerateId();

            // Assert - should be valid hex (0-9, a-f)
            Assert.That(id, Does.Match("^[0-9a-f]{8}$"));
        }

        [Test]
        public void GuidIdGenerator_GenerateId_ReturnsUniqueIds()
        {
            // Arrange
            var generator = new GuidIdGenerator();
            var ids = new System.Collections.Generic.HashSet<string>();

            // Act - generate multiple IDs
            for (int i = 0; i < 100; i++)
            {
                ids.Add(generator.GenerateId());
            }

            // Assert - all should be unique
            Assert.AreEqual(100, ids.Count);
        }

        [Test]
        public void GuidIdGenerator_MultipleInstances_GenerateUniqueIds()
        {
            // Arrange
            var generator1 = new GuidIdGenerator();
            var generator2 = new GuidIdGenerator();

            // Act
            var id1 = generator1.GenerateId();
            var id2 = generator2.GenerateId();

            // Assert
            Assert.AreNotEqual(id1, id2);
        }

        #endregion

        #region SequentialIdGenerator Default Constructor Tests

        [Test]
        public void SequentialIdGenerator_DefaultConstructor_StartsAtZero()
        {
            // Arrange
            var generator = new SequentialIdGenerator();

            // Act
            var id = generator.GenerateId();

            // Assert
            Assert.AreEqual("id_0000", id);
        }

        [Test]
        public void SequentialIdGenerator_GenerateId_IncrementsSequentially()
        {
            // Arrange
            var generator = new SequentialIdGenerator();

            // Act
            var id1 = generator.GenerateId();
            var id2 = generator.GenerateId();
            var id3 = generator.GenerateId();

            // Assert
            Assert.AreEqual("id_0000", id1);
            Assert.AreEqual("id_0001", id2);
            Assert.AreEqual("id_0002", id3);
        }

        [Test]
        public void SequentialIdGenerator_GenerateId_UsesFixedWidthFormatting()
        {
            // Arrange
            var generator = new SequentialIdGenerator();

            // Act - generate 10 IDs
            for (int i = 0; i < 10; i++)
            {
                var id = generator.GenerateId();
                // Assert - all IDs should be same length
                Assert.AreEqual(7, id.Length); // "id_" + 4 digits = 7
            }
        }

        #endregion

        #region SequentialIdGenerator Parameterized Constructor Tests

        [Test]
        public void SequentialIdGenerator_WithStartValue_StartsAtSpecifiedValue()
        {
            // Arrange
            var generator = new SequentialIdGenerator(100);

            // Act
            var id = generator.GenerateId();

            // Assert
            Assert.AreEqual("id_0100", id);
        }

        [Test]
        public void SequentialIdGenerator_WithStartValue_IncrementsFromStartValue()
        {
            // Arrange
            var generator = new SequentialIdGenerator(50);

            // Act
            var id1 = generator.GenerateId();
            var id2 = generator.GenerateId();
            var id3 = generator.GenerateId();

            // Assert
            Assert.AreEqual("id_0050", id1);
            Assert.AreEqual("id_0051", id2);
            Assert.AreEqual("id_0052", id3);
        }

        [Test]
        public void SequentialIdGenerator_WithZeroStartValue_EquivalentToDefaultConstructor()
        {
            // Arrange
            var generator1 = new SequentialIdGenerator(0);
            var generator2 = new SequentialIdGenerator();

            // Act
            var id1 = generator1.GenerateId();
            var id2 = generator2.GenerateId();

            // Assert
            Assert.AreEqual(id1, id2);
        }

        [Test]
        public void SequentialIdGenerator_WithLargeStartValue_FormatsCorrectly()
        {
            // Arrange
            var generator = new SequentialIdGenerator(9999);

            // Act
            var id1 = generator.GenerateId();
            var id2 = generator.GenerateId();

            // Assert
            Assert.AreEqual("id_9999", id1);
            Assert.AreEqual("id_10000", id2); // Overflows 4 digits, but that's expected
        }

        #endregion

        #region SequentialIdGenerator Reset Tests

        [Test]
        public void SequentialIdGenerator_Reset_ResetsCounterToZero()
        {
            // Arrange
            var generator = new SequentialIdGenerator();
            generator.GenerateId(); // id_0000
            generator.GenerateId(); // id_0001
            generator.GenerateId(); // id_0002

            // Act
            generator.Reset();
            var idAfterReset = generator.GenerateId();

            // Assert
            Assert.AreEqual("id_0000", idAfterReset);
        }

        [Test]
        public void SequentialIdGenerator_Reset_CanBeCalledMultipleTimes()
        {
            // Arrange
            var generator = new SequentialIdGenerator();

            // Act & Assert
            generator.GenerateId();
            generator.Reset();
            Assert.AreEqual("id_0000", generator.GenerateId());

            generator.GenerateId();
            generator.GenerateId();
            generator.Reset();
            Assert.AreEqual("id_0000", generator.GenerateId());

            generator.Reset();
            Assert.AreEqual("id_0000", generator.GenerateId());
        }

        [Test]
        public void SequentialIdGenerator_Reset_AfterParameterizedConstruction_ResetsToZero()
        {
            // Arrange
            var generator = new SequentialIdGenerator(100);
            generator.GenerateId(); // id_0100
            generator.GenerateId(); // id_0101

            // Act
            generator.Reset();
            var idAfterReset = generator.GenerateId();

            // Assert - Reset always goes to 0, not back to startValue
            Assert.AreEqual("id_0000", idAfterReset);
        }

        [Test]
        public void SequentialIdGenerator_Reset_BeforeAnyGeneration_HasNoEffect()
        {
            // Arrange
            var generator = new SequentialIdGenerator();

            // Act
            generator.Reset();
            var id = generator.GenerateId();

            // Assert
            Assert.AreEqual("id_0000", id);
        }

        #endregion

        #region Interface Implementation Tests

        [Test]
        public void GuidIdGenerator_ImplementsIIdGenerator()
        {
            // Assert
            Assert.IsTrue(typeof(IIdGenerator).IsAssignableFrom(typeof(GuidIdGenerator)));
        }

        [Test]
        public void SequentialIdGenerator_ImplementsIIdGenerator()
        {
            // Assert
            Assert.IsTrue(typeof(IIdGenerator).IsAssignableFrom(typeof(SequentialIdGenerator)));
        }

        [Test]
        public void IIdGenerator_CanBeUsedPolymorphically()
        {
            // Arrange
            IIdGenerator generator1 = new GuidIdGenerator();
            IIdGenerator generator2 = new SequentialIdGenerator();

            // Act
            var id1 = generator1.GenerateId();
            var id2 = generator2.GenerateId();

            // Assert
            Assert.IsNotNull(id1);
            Assert.IsNotNull(id2);
            Assert.AreEqual("id_0000", id2); // Sequential should be predictable
        }

        #endregion

        #region Determinism Tests

        [Test]
        public void SequentialIdGenerator_IsDeterministic()
        {
            // Arrange
            var generator1 = new SequentialIdGenerator();
            var generator2 = new SequentialIdGenerator();

            // Act
            var ids1 = new string[5];
            var ids2 = new string[5];
            for (int i = 0; i < 5; i++)
            {
                ids1[i] = generator1.GenerateId();
                ids2[i] = generator2.GenerateId();
            }

            // Assert - both generators should produce identical sequences
            CollectionAssert.AreEqual(ids1, ids2);
        }

        [Test]
        public void SequentialIdGenerator_WithSameStartValue_ProducesIdenticalSequences()
        {
            // Arrange
            var generator1 = new SequentialIdGenerator(42);
            var generator2 = new SequentialIdGenerator(42);

            // Act
            var ids1 = new string[10];
            var ids2 = new string[10];
            for (int i = 0; i < 10; i++)
            {
                ids1[i] = generator1.GenerateId();
                ids2[i] = generator2.GenerateId();
            }

            // Assert
            CollectionAssert.AreEqual(ids1, ids2);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void SequentialIdGenerator_GeneratesManyIds_DoesNotOverflow()
        {
            // Arrange
            var generator = new SequentialIdGenerator();

            // Act - generate 10000 IDs
            string? lastId = null;
            for (int i = 0; i < 10000; i++)
            {
                lastId = generator.GenerateId();
            }

            // Assert
            Assert.AreEqual("id_9999", lastId);
        }

        [Test]
        public void SequentialIdGenerator_WithNegativeStartValue_StartsAtNegative()
        {
            // Arrange - This is allowed by the signature
            var generator = new SequentialIdGenerator(-5);

            // Act
            var id = generator.GenerateId();

            // Assert - Format with negative number
            Assert.That(id, Does.StartWith("id_-"));
        }

        [Test]
        public void GuidIdGenerator_ThreadSafety_GeneratesUniqueIds()
        {
            // Arrange
            var generator = new GuidIdGenerator();
            var ids = new System.Collections.Concurrent.ConcurrentBag<string>();
            var tasks = new System.Threading.Tasks.Task[10];

            // Act - generate IDs from multiple threads
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    for (int j = 0; j < 100; j++)
                    {
                        ids.Add(generator.GenerateId());
                    }
                });
            }
            System.Threading.Tasks.Task.WaitAll(tasks);

            // Assert - all 1000 IDs should be unique
            var uniqueIds = new System.Collections.Generic.HashSet<string>(ids);
            Assert.AreEqual(1000, uniqueIds.Count);
        }

        #endregion
    }
}
