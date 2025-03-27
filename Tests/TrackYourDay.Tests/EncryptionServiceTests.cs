using FluentAssertions;
using TrackYourDay.Core;

namespace TrackYourDay.Tests
{

    public class EncryptionServiceTests
    {
        [Fact]
        public void GivenSaltIsSupplied_WhenTextIsEncrypted_ThenResultsOfEncryptionIsDifferentThanOriginalText()
        {
            //Arrange
            var salt = "salt";
            var text = "text";
            var encryptionService = new EncryptionService(salt);

            // Act          
            var encryptedText = encryptionService.Encrypt(text);

            // Assert
            encryptedText.Should().NotBe(text);
        }

        [Fact]
        public void GivenSaltIsSupplied_WhenTextIsEncryptedAndDecrypted_ThenDecryptedTextIsEqualToOriginalText()
        {
            //Arrange
            var salt = "salt";
            var text = "text";
            var encryptionService = new EncryptionService(salt);
            
            // Act
            var encryptedText = encryptionService.Encrypt(text);
            var decryptedText = encryptionService.Decrypt(encryptedText);

            // Assert
            decryptedText.Should().Be(text);
        }

        [Fact]
        public void GivenSaltIsNotSupplied_WhenTextIsEncrypted_ThenResultsOfEncryptionIsDifferentThanOriginalText()
        {
            //Arrange
            var text = "text";
            var encryptionService = new EncryptionService(string.Empty);

            // Act          
            var encryptedText = encryptionService.Encrypt(text);

            // Assert
            encryptedText.Should().NotBe(text);
        }

        [Fact]
        public void GivenSaltIsNotSupplied_WhenTextIsEncryptedAndDecrypted_ThenDecryptedTextIsEqualToOriginalText()
        {
            //Arrange
            var salt = "salt";
            var text = "text";
            var encryptionService = new EncryptionService(salt);

            // Act
            var encryptedText = encryptionService.Encrypt(text);
            var decryptedText = encryptionService.Decrypt(encryptedText);

            // Assert
            decryptedText.Should().Be(text);
        }
    }
}
