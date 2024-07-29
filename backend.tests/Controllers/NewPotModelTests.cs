namespace MagicPot.Backend.Controllers
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.CompilerServices;
    using MagicPot.Backend.Models;
    using Xunit;

    public class NewPotModelTests
    {
        private const string ValidAddress = "kQCxE6mUtQJKFnGfaROTKOt1lZbDiiX1kCixRv7Nw2Id_ntm";
        private const string InvalidAddress = "invalid" + ValidAddress;
        private const string ValidTokenAddress = "kQBbX2khki4ynoYWgXqmc7_5Xlcley9luaHxoSE0-7R2wqJA";

        private readonly NewPotModel model = new()
        {
            UserAddress = ValidAddress,
            Name = "Test",
            TokenAddress = ValidTokenAddress,
            InitialSize = 1000,
            CountdownTimerMinutes = 37,
            TransactionSize = 10,
            IncreasingTransactionPercentage = 7,
            CreatorPercent = 20,
            LastTransactionsPercent = 20,
            LastTransactionsCount = 10,
            RandomTransactionsPercent = 20,
            RandomTransactionsCount = 10,
            ReferrersPercent = 20,
            BurnPercent = 20,
        };

        [Fact]
        public void ValidIsOk()
        {
            var errors = new List<ValidationResult>();
            var result = Validator.TryValidateObject(model, new ValidationContext(model, null, null), errors, true);

            Assert.Empty(errors);
            Assert.True(result);
        }

        [Theory]
        [InlineData("Test", true)]
        [InlineData("Тест", true)]
        [InlineData("Тест? Test! 123.456.789,0", true)]
        [InlineData("Test-test_test", false)]
        public void NameIsChecked(string value, bool valid)
        {
            model.Name = value;

            var errors = new List<ValidationResult>();
            var result = Validator.TryValidateObject(model, new ValidationContext(model, null, null), errors, true);

            if (valid)
            {
                Assert.Empty(errors);
                Assert.True(result);
            }
            else
            {
                Assert.NotEmpty(errors);
                Assert.Contains(nameof(model.Name), errors[0].MemberNames);
                Assert.False(result);
            }
        }

        [Theory]
        [InlineData(InvalidAddress)]
        [InlineData("")]
        public void FailOnInvalidUserAddress(string value)
        {
            model.UserAddress = value;

            var errors = new List<ValidationResult>();
            var result = Validator.TryValidateObject(model, new ValidationContext(model, null, null), errors, true);

            Assert.NotEmpty(errors);
            Assert.Contains(nameof(model.UserAddress), errors[0].MemberNames);
            Assert.False(result);
        }

        [Theory]
        [InlineData("")]
        public void FailOnInvalidName(string value)
        {
            model.Name = value;

            var errors = new List<ValidationResult>();
            var result = Validator.TryValidateObject(model, new ValidationContext(model, null, null), errors, true);

            Assert.NotEmpty(errors);
            Assert.Contains(nameof(model.Name), errors[0].MemberNames);
            Assert.False(result);
        }

        [Theory]
        [InlineData(InvalidAddress)]
        [InlineData("")]
        public void FailOnInvalidTokenAddress(string value)
        {
            model.TokenAddress = value;

            var errors = new List<ValidationResult>();
            var result = Validator.TryValidateObject(model, new ValidationContext(model, null, null), errors, true);

            Assert.NotEmpty(errors);
            Assert.Contains(nameof(model.TokenAddress), errors[0].MemberNames);
            Assert.False(result);
        }

        [Theory]
        [InlineData(20, 20, 20, 20, 20)]
        [InlineData(50, null, 25, null, 25)]
        [InlineData(100, null, null, null, null)]
        [InlineData(null, 100, null, null, null)]
        [InlineData(null, null, 100, null, null)]
        [InlineData(null, null, null, 100, null)]
        [InlineData(null, null, null, null, 100)]
        public void SucceedsWhenPercentsSum100(int? val1, int? val2, int? val3, int? val4, int? val5)
        {
            model.CreatorPercent = (uint?)val1;
            model.LastTransactionsPercent = (uint?)val2;
            model.LastTransactionsCount = (uint?)val2 / 10;
            model.RandomTransactionsPercent = (uint?)val3;
            model.RandomTransactionsCount = (uint?)val3 / 10;
            model.ReferrersPercent = (uint?)val4;
            model.BurnPercent = (uint?)val5;

            var errors = new List<ValidationResult>();
            var result = Validator.TryValidateObject(model, new ValidationContext(model, null, null), errors, true);

            Assert.Empty(errors);
            Assert.True(result);
        }

        [Theory]
        [InlineData(20, null, 20, null, 20)]
        [InlineData(75, 75, null, null, null)]
        public void FailsWhenPercentsSumNot100(int? val1, int? val2, int? val3, int? val4, int? val5)
        {
            model.CreatorPercent = (uint?)val1;
            model.LastTransactionsPercent = (uint?)val2;
            model.RandomTransactionsPercent = (uint?)val3;
            model.ReferrersPercent = (uint?)val4;
            model.BurnPercent = (uint?)val5;

            var errors = new List<ValidationResult>();
            var result = Validator.TryValidateObject(model, new ValidationContext(model, null, null), errors, true);

            Assert.NotEmpty(errors);

            if (val1.HasValue)
            {
                Assert.Contains(errors, x => x.MemberNames.Contains(nameof(model.CreatorPercent)));
            }

            if (val2.HasValue)
            {
                Assert.Contains(errors, x => x.MemberNames.Contains(nameof(model.LastTransactionsPercent)));
            }

            if (val3.HasValue)
            {
                Assert.Contains(errors, x => x.MemberNames.Contains(nameof(model.RandomTransactionsPercent)));
            }

            if (val4.HasValue)
            {
                Assert.Contains(errors, x => x.MemberNames.Contains(nameof(model.ReferrersPercent)));
            }

            if (val5.HasValue)
            {
                Assert.Contains(errors, x => x.MemberNames.Contains(nameof(model.BurnPercent)));
            }

            Assert.False(result);
        }

        [Fact]
        public void FailsWhenPercentsAllEmpty()
        {
            model.CreatorPercent = null;
            model.LastTransactionsPercent = null;
            model.RandomTransactionsPercent = null;
            model.ReferrersPercent = null;
            model.BurnPercent = null;

            var errors = new List<ValidationResult>();
            var result = Validator.TryValidateObject(model, new ValidationContext(model, null, null), errors, true);

            Assert.NotEmpty(errors);

            Assert.Contains(errors, x => x.MemberNames.Contains(nameof(model.CreatorPercent)));
            Assert.Contains(errors, x => x.MemberNames.Contains(nameof(model.LastTransactionsPercent)));
            Assert.Contains(errors, x => x.MemberNames.Contains(nameof(model.RandomTransactionsPercent)));
            Assert.Contains(errors, x => x.MemberNames.Contains(nameof(model.ReferrersPercent)));
            Assert.Contains(errors, x => x.MemberNames.Contains(nameof(model.BurnPercent)));

            Assert.False(result);
        }

        [Fact]
        public void FailsWhenLastTransactionCountIsZero()
        {
            model.LastTransactionsCount = null;

            var errors = new List<ValidationResult>();
            var result = Validator.TryValidateObject(model, new ValidationContext(model, null, null), errors, true);

            Assert.NotEmpty(errors);
            Assert.Contains(errors, x => x.MemberNames.Contains(nameof(model.LastTransactionsCount)));
            Assert.False(result);
        }

        [Fact]
        public void FailsWhenLastTransactionCountIsNotZero()
        {
            model.CreatorPercent = 25;
            model.LastTransactionsPercent = null;
            model.LastTransactionsCount = 10;
            model.RandomTransactionsPercent = 25;
            model.ReferrersPercent = 25;
            model.BurnPercent = 25;

            var errors = new List<ValidationResult>();
            var result = Validator.TryValidateObject(model, new ValidationContext(model, null, null), errors, true);

            Assert.NotEmpty(errors);
            Assert.Contains(errors, x => x.MemberNames.Contains(nameof(model.LastTransactionsCount)));
            Assert.False(result);
        }

        [Fact]
        public void FailsWhenRandomTransactionCountIsZero()
        {
            model.RandomTransactionsCount = null;

            var errors = new List<ValidationResult>();
            var result = Validator.TryValidateObject(model, new ValidationContext(model, null, null), errors, true);

            Assert.NotEmpty(errors);
            Assert.Contains(errors, x => x.MemberNames.Contains(nameof(model.RandomTransactionsCount)));
            Assert.False(result);
        }

        [Fact]
        public void FailsWhenRandomTransactionCountIsNotZero()
        {
            model.CreatorPercent = 25;
            model.LastTransactionsPercent = 25;
            model.RandomTransactionsPercent = null;
            model.RandomTransactionsCount = 10;
            model.ReferrersPercent = 25;
            model.BurnPercent = 25;

            var errors = new List<ValidationResult>();
            var result = Validator.TryValidateObject(model, new ValidationContext(model, null, null), errors, true);

            Assert.NotEmpty(errors);
            Assert.Contains(errors, x => x.MemberNames.Contains(nameof(model.RandomTransactionsCount)));
            Assert.False(result);
        }
    }
}
