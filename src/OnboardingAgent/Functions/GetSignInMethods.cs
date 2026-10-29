using OnboardingAgent.Functions.Parameters;

namespace OnboardingAgent.Functions
{
    /// <summary>
    /// Registered authentication methods with phone numbers and email addresses masked.
    /// </summary>
    public static class GetSignInMethods
    {
        /// <summary>Lists the user's registered sign-in methods and whether any of them counts towards MFA.</summary>
        /// <param name="ctx">The request context: Graph client, directory service, validator, configuration and approval ID.</param>
        /// <param name="parameters">The JSON parameters from the agent: userId.</param>
        /// <returns>The ActionResult sent back to the agent, with the masked methods and an mfaCapable flag.</returns>
        public static ActionResult Run(ActionContext ctx, JsonObject parameters)
        {
            var p = UserTargetParameters.Parse(parameters);
            var user = ctx.Validator.ReadableUser(p.UserId);

            var methods = new JsonArray();
            var mfaCapable = false;
            foreach (var method in ctx.Graph.Get("/users/" + user.Id + "/authentication/methods").GetArray("value").Objects())
            {
                var type = (method.GetString("@odata.type") ?? string.Empty).Replace("#microsoft.graph.", string.Empty);
                string name;
                string detail = null;
                var countsForMfa = true;
                switch (type)
                {
                    case "passwordAuthenticationMethod":
                        name = "Password";
                        countsForMfa = false;
                        break;
                    case "emailAuthenticationMethod":
                        name = "Email (self-service password reset only)";
                        detail = MaskEmail(method.GetString("emailAddress"));
                        countsForMfa = false;
                        break;
                    case "phoneAuthenticationMethod":
                        name = "Phone";
                        detail = method.GetString("phoneType") + " " + MaskPhone(method.GetString("phoneNumber"));
                        break;
                    case "microsoftAuthenticatorAuthenticationMethod":
                        name = "Microsoft Authenticator";
                        detail = method.GetString("displayName");
                        break;
                    case "fido2AuthenticationMethod":
                        name = "FIDO2 security key";
                        detail = method.GetString("model") ?? method.GetString("displayName");
                        break;
                    case "windowsHelloForBusinessAuthenticationMethod":
                        name = "Windows Hello for Business";
                        detail = method.GetString("displayName");
                        break;
                    case "softwareOathAuthenticationMethod":
                        name = "Authenticator app (OATH code)";
                        break;
                    case "platformCredentialAuthenticationMethod":
                        name = "Platform credential";
                        detail = method.GetString("displayName");
                        break;
                    case "temporaryAccessPassAuthenticationMethod":
                        name = "Temporary Access Pass";
                        detail = method.GetBool("isUsable", false) ? "usable" : "not usable";
                        countsForMfa = false;
                        break;
                    default:
                        name = type;
                        break;
                }

                mfaCapable |= countsForMfa;
                methods.Add(new JsonObject { { "method", name }, { "detail", detail } });
            }

            return ActionResult.Ok(user.DisplayName + " has " + methods.Count + " sign-in method(s)"
                + (mfaCapable ? "." : " and no MFA method registered."), new JsonObject
            {
                { "user", user.ToJson() },
                { "mfaCapable", mfaCapable },
                { "methods", methods }
            });
        }

        private static string MaskPhone(string phone)
        {
            if (string.IsNullOrEmpty(phone) || phone.Length < 3)
            {
                return phone == null ? null : "***";
            }

            return "******" + phone.Substring(phone.Length - 2);
        }

        private static string MaskEmail(string email)
        {
            var at = email?.IndexOf('@') ?? -1;
            return at <= 0 ? null : email.Substring(0, 1) + "***" + email.Substring(at);
        }
    }
}
