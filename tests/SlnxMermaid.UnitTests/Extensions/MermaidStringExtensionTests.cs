using SlnxMermaid.Core.Extensions;

namespace SlnxMermaid.UnitTests.Extensions
{
    public class MermaidStringExtensionTests
    {
        public class ConvertToAllowedMermaidString
        {
            [Theory]
            [InlineData(null, "")]
            [InlineData("", "")]
            public void Returns_empty_for_null_or_empty(string input, string expected)
            {
                // act
                var result = input.ConvertToAllowedMermaidString();

                // assert
                Assert.Equal(expected, result);
            }

            [Theory]
            [InlineData("Abc_123", "Abc_123")]
            [InlineData("A B C", "A_B_C")]
            [InlineData("A.B.C", "A_B_C")]
            [InlineData("A B.C", "A_B_C")]
            public void Keeps_letters_digits_and_underscore_and_maps_space_and_dot_to_underscore(string input, string expected)
            {
                var result = input.ConvertToAllowedMermaidString();

                Assert.Equal(expected, result);
            }

            [Theory]
            [InlineData("a-b", "ab")]
            [InlineData("a/b\\c", "abc")]
            [InlineData("a:b;c", "abc")]
            [InlineData("a@b#c!", "abc")]
            [InlineData("[](){}", "")]
            public void Removes_other_characters(string input, string expected)
            {
                var result = input.ConvertToAllowedMermaidString();

                Assert.Equal(expected, result);
            }

            [Fact]
            public void Removes_non_ascii_letters_that_are_not_letter_or_digit_in_current_unicode_category()
            {
                // Uwaga: char.IsLetterOrDigit przepuszcza wiele znaków Unicode (np. 'Ż', 'ą').
                // Ten test pokazuje aktualny kontrakt: Unicode litery i cyfry są dozwolone.

                var input = "Zażółć gęślą 123";
                var result = input.ConvertToAllowedMermaidString();

                Assert.Equal("Zażółć_gęślą_123", result);
            }
        }

        public class PrepareStripPrefixToDisplayOnDiagram
        {
            [Theory]
            [InlineData(null)]
            [InlineData("")]
            [InlineData("   ")]
            public void Returns_empty_for_null_or_whitespace_path(string input)
            {
                var result = input.PrepareToDisplayOnMermaidDiagram();

                Assert.Equal(string.Empty, result);
            }
        }
    }
}
