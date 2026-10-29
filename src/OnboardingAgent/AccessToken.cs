using System;

namespace OnboardingAgent
{
    /// <summary>An app-only Microsoft Graph bearer token and the UTC time it stops being valid.</summary>
    public sealed class AccessToken
    {
        /// <summary>Wraps a token string returned by the token endpoint.</summary>
        /// <param name="value">The raw bearer token.</param>
        /// <param name="expiresUtc">When the token expires, in UTC.</param>
        public AccessToken(string value, DateTime expiresUtc)
        {
            Value = value;
            ExpiresUtc = expiresUtc;
        }

        /// <summary>The raw bearer token sent in the Authorization header.</summary>
        public string Value { get; }

        /// <summary>When the token expires, in UTC, so it can be refreshed before use.</summary>
        public DateTime ExpiresUtc { get; }
    }
}
