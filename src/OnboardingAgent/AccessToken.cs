using System;

namespace OnboardingAgent
{
    public sealed class AccessToken
    {
        public AccessToken(string value, DateTime expiresUtc)
        {
            Value = value;
            ExpiresUtc = expiresUtc;
        }

        public string Value { get; }

        public DateTime ExpiresUtc { get; }
    }
}
