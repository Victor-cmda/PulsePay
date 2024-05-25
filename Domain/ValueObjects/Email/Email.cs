using System.Text.RegularExpressions;

namespace Domain.ValueObjects.Email
{
    public class Email
    {
        public string Value { get; private set; }

        public Email(string value)
        {
            if (!Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new ArgumentException("Invalid email format");
            Value = value;
        }

        public override string ToString()
        {
            return Value;
        }

        protected Email() { }
    }
}
