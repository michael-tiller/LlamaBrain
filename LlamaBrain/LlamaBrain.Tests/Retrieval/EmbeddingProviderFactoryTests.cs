using System;
using NUnit.Framework;
using LlamaBrain.Core.Retrieval;

namespace LlamaBrain.Tests.Retrieval
{
    /// <summary>
    /// Tests for EmbeddingProviderFactory.
    /// </summary>
    public class EmbeddingProviderFactoryTests
    {
        #region Create Tests

        [Test]
        public void Create_WithNullConfig_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                EmbeddingProviderFactory.Create(null!));
        }

        [Test]
        public void Create_WithNullProviderType_ReturnsNullEmbeddingProvider()
        {
            var config = EmbeddingConfig.KeywordOnly();

            var provider = EmbeddingProviderFactory.Create(config);

            Assert.That(provider, Is.TypeOf<NullEmbeddingProvider>());
        }

        [Test]
        public void Create_WithLlamaCppProviderType_ReturnsLlamaCppProvider()
        {
            var config = EmbeddingConfig.Default();

            var provider = EmbeddingProviderFactory.Create(config);

            Assert.That(provider, Is.TypeOf<LlamaCppEmbeddingProvider>());
        }

        [Test]
        public void Create_WithInvalidKeywordWeight_ThrowsArgumentException()
        {
            var config = new EmbeddingConfig
            {
                ProviderType = EmbeddingProviderType.Null,
                KeywordWeight = 1.5f // Invalid: > 1
            };

            var ex = Assert.Throws<ArgumentException>(() =>
                EmbeddingProviderFactory.Create(config));

            Assert.That(ex!.Message, Does.Contain("KeywordWeight"));
        }

        [Test]
        public void Create_WithInvalidLlamaCppUrl_ThrowsArgumentException()
        {
            var config = new EmbeddingConfig
            {
                ProviderType = EmbeddingProviderType.LlamaCpp,
                LlamaCppBaseUrl = "not-a-valid-url"
            };

            var ex = Assert.Throws<ArgumentException>(() =>
                EmbeddingProviderFactory.Create(config));

            Assert.That(ex!.Message, Does.Contain("LlamaCppBaseUrl"));
        }

        [Test]
        public void Create_WithOpenAIProviderType_ThrowsNotSupportedException()
        {
            var config = new EmbeddingConfig
            {
                ProviderType = EmbeddingProviderType.OpenAI
            };

            Assert.Throws<NotSupportedException>(() =>
                EmbeddingProviderFactory.Create(config));
        }

        [Test]
        public void Create_WithSemanticEnabledButNullProvider_ThrowsArgumentException()
        {
            var config = new EmbeddingConfig
            {
                ProviderType = EmbeddingProviderType.Null,
                EnableSemanticRetrieval = true
            };

            var ex = Assert.Throws<ArgumentException>(() =>
                EmbeddingProviderFactory.Create(config));

            Assert.That(ex!.Message, Does.Contain("ProviderType is Null"));
        }

        #endregion

        #region CreateKeywordOnly Tests

        [Test]
        public void CreateKeywordOnly_ReturnsNullProvider()
        {
            var provider = EmbeddingProviderFactory.CreateKeywordOnly();

            Assert.That(provider, Is.TypeOf<NullEmbeddingProvider>());
        }

        [Test]
        public void CreateKeywordOnly_ProviderIsNotAvailable()
        {
            var provider = EmbeddingProviderFactory.CreateKeywordOnly();

            Assert.That(provider.IsAvailable, Is.False);
        }

        #endregion

        #region CreateLlamaCpp Tests

        [Test]
        public void CreateLlamaCpp_WithDefaults_ReturnsProvider()
        {
            var provider = EmbeddingProviderFactory.CreateLlamaCpp();

            Assert.That(provider, Is.TypeOf<LlamaCppEmbeddingProvider>());
        }

        [Test]
        public void CreateLlamaCpp_WithCustomUrl_ReturnsProvider()
        {
            var provider = EmbeddingProviderFactory.CreateLlamaCpp(
                baseUrl: "http://192.168.1.100:9090");

            Assert.That(provider, Is.TypeOf<LlamaCppEmbeddingProvider>());
        }

        [Test]
        public void CreateLlamaCpp_WithCustomDimension_HasCorrectDimension()
        {
            var provider = EmbeddingProviderFactory.CreateLlamaCpp(
                embeddingDimension: 768);

            Assert.That(provider.EmbeddingDimension, Is.EqualTo(768));
        }

        #endregion

        #region TryCreate Tests

        [Test]
        public void TryCreate_WithValidConfig_ReturnsTrue()
        {
            var config = EmbeddingConfig.KeywordOnly();

            var result = EmbeddingProviderFactory.TryCreate(config, out var provider, out var error);

            Assert.That(result, Is.True);
            Assert.That(provider, Is.Not.Null);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void TryCreate_WithNullConfig_ReturnsFalse()
        {
            var result = EmbeddingProviderFactory.TryCreate(null!, out var provider, out var error);

            Assert.That(result, Is.False);
            Assert.That(provider, Is.Null);
            Assert.That(error, Does.Contain("null"));
        }

        [Test]
        public void TryCreate_WithInvalidConfig_ReturnsFalse()
        {
            var config = new EmbeddingConfig
            {
                KeywordWeight = -1f // Invalid
            };

            var result = EmbeddingProviderFactory.TryCreate(config, out var provider, out var error);

            Assert.That(result, Is.False);
            Assert.That(provider, Is.Null);
            Assert.That(error, Does.Contain("KeywordWeight"));
        }

        [Test]
        public void TryCreate_WithUnsupportedProvider_ReturnsFalse()
        {
            var config = new EmbeddingConfig
            {
                ProviderType = EmbeddingProviderType.OpenAI
            };

            var result = EmbeddingProviderFactory.TryCreate(config, out var provider, out var error);

            Assert.That(result, Is.False);
            Assert.That(provider, Is.Null);
            Assert.That(error, Does.Contain("not yet implemented"));
        }

        #endregion

        #region EmbeddingConfig Factory Method Tests

        [Test]
        public void EmbeddingConfig_KeywordOnly_HasNullProviderType()
        {
            var config = EmbeddingConfig.KeywordOnly();

            Assert.That(config.ProviderType, Is.EqualTo(EmbeddingProviderType.Null));
            Assert.That(config.EnableSemanticRetrieval, Is.False);
        }

        [Test]
        public void EmbeddingConfig_Default_HasLlamaCppProviderType()
        {
            var config = EmbeddingConfig.Default();

            Assert.That(config.ProviderType, Is.EqualTo(EmbeddingProviderType.LlamaCpp));
            Assert.That(config.EnableSemanticRetrieval, Is.True);
        }

        [Test]
        public void EmbeddingConfig_ForLlamaCpp_HasCorrectSettings()
        {
            var config = EmbeddingConfig.ForLlamaCpp(
                baseUrl: "http://localhost:9090",
                modelName: "custom-model",
                embeddingDimension: 768,
                timeoutSeconds: 60);

            Assert.That(config.ProviderType, Is.EqualTo(EmbeddingProviderType.LlamaCpp));
            Assert.That(config.LlamaCppBaseUrl, Is.EqualTo("http://localhost:9090"));
            Assert.That(config.LlamaCppModelName, Is.EqualTo("custom-model"));
            Assert.That(config.EmbeddingDimension, Is.EqualTo(768));
            Assert.That(config.RequestTimeoutSeconds, Is.EqualTo(60));
        }

        [Test]
        public void EmbeddingConfig_Validate_ValidConfig_NoErrors()
        {
            var config = EmbeddingConfig.Default();

            var errors = config.Validate();

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void EmbeddingConfig_Validate_InvalidTimeout_ReturnsError()
        {
            var config = EmbeddingConfig.Default();
            config.RequestTimeoutSeconds = 0;

            var errors = config.Validate();

            Assert.That(errors, Has.Some.Contain("RequestTimeoutSeconds"));
        }

        #endregion

        #region Determinism Tests

        [Test]
        public void Create_SameConfig_ProducesEquivalentProviders()
        {
            var config1 = EmbeddingConfig.ForLlamaCpp("http://localhost:8080");
            var config2 = EmbeddingConfig.ForLlamaCpp("http://localhost:8080");

            var provider1 = EmbeddingProviderFactory.Create(config1);
            var provider2 = EmbeddingProviderFactory.Create(config2);

            Assert.That(provider1.GetType(), Is.EqualTo(provider2.GetType()));
            Assert.That(provider1.EmbeddingDimension, Is.EqualTo(provider2.EmbeddingDimension));
        }

        #endregion

        #region IDisposable Tests

        [Test]
        public void Create_LlamaCppProvider_IsDisposable()
        {
            var config = EmbeddingConfig.Default();

            var provider = EmbeddingProviderFactory.Create(config);

            Assert.That(provider, Is.AssignableTo<IDisposable>());
            (provider as IDisposable)?.Dispose();
        }

        #endregion
    }
}
