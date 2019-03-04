using System;
using Microsoft.Extensions.Primitives;

namespace emptysidecar
{
    public static class AuthHelper
    {
        public static string GetAuthUser(StringValues authHeader)
        {
            if (!string.IsNullOrEmpty(authHeader))
            {
                return authHeader[0];
            }

            return string.Empty;
        }
    }
}
