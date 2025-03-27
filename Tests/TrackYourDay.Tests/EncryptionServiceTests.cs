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

        [Theory]
        [InlineData("text")]
        [InlineData("https://gitlab.com")]
        [InlineData("S-1-5-21-656197171-1530334744-581042428-1001")]
        public void GivenSaltIsNotSupplied_WhenTextIsEncrypted_ThenResultsOfEncryptionIsDifferentThanOriginalText(string text)
        {
            //Arrange
            var encryptionService = new EncryptionService();

            // Act          
            var encryptedText = encryptionService.Encrypt(text);

            // Assert
            encryptedText.Should().NotBe(text);
        }

        [Theory]
        [InlineData("text")]
        [InlineData("https://gitlab.com")]
        [InlineData("S-1-5-21-656197171-1530334744-581042428-1001")]
        public void GivenSaltIsNotSupplied_WhenTextIsEncryptedAndDecrypted_ThenDecryptedTextIsEqualToOriginalText(string text)
        {
            //Arrange
            var encryptionService = new EncryptionService();

            // Act
            var encryptedText = encryptionService.Encrypt(text);
            var decryptedText = encryptionService.Decrypt(encryptedText);

            // Assert
            decryptedText.Should().Be(text);
        }

        [Fact]
        public void GivenSaltIsNotSupplied_WhenEmptyTextIsEncrypted_ThenResultsOfEncryptionIsEmptyText()
        {
            //Arrange
            var text = string.Empty;
            var encryptionService = new EncryptionService();

            // Act          
            var encryptedText = encryptionService.Encrypt(text);

            // Assert
            encryptedText.Should().Be(string.Empty);
        }

        [Fact]
        public void GivenSaltIsNotSupplied_WhenEmptyTextIsEncryptedAndDecrypted_ThenDecryptedTextIsEqualToOriginalText()
        {
            //Arrange
            var text = string.Empty;
            var encryptionService = new EncryptionService();

            // Act
            var encryptedText = encryptionService.Encrypt(text);
            var decryptedText = encryptionService.Decrypt(encryptedText);

            // Assert
            decryptedText.Should().Be(string.Empty);
        }
    }
}
