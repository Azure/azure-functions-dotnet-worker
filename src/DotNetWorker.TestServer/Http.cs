using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.Worker.TestServer;

public class Http
{
    public string StatusCode { get; }
    public TypedData Body { get; }
    public TypedData RawBody { get; }
    public IReadOnlyDictionary<string, string> Params { get; }
    public IReadOnlyDictionary<string, string> Query { get; }
    public string Url { get; }
    public string Method { get; }
    public IReadOnlyDictionary<string, string> Headers { get; }
    public IReadOnlyCollection<Cookie> Cookies { get; }
    public IReadOnlyCollection<ClaimsIdentity> Identities { get; }

    private Http(string statusCode, TypedData body, TypedData rawBody, IReadOnlyDictionary<string, string> @params, IReadOnlyDictionary<string, string> query, string url,
        string method, IReadOnlyDictionary<string, string> headers,
        IReadOnlyCollection<Cookie> cookies, IReadOnlyCollection<ClaimsIdentity> identities)
    {
        StatusCode = statusCode;
        Body = body;
        RawBody = rawBody;
        Params = @params;
        Query = query;
        Url = url;
        Method = method;
        Headers = headers;
        Cookies = cookies;
        Identities = identities;
    }

    internal static Http? From(
        RpcHttp? http)
    {
        if (http == null) return null;
        return new Http(http.StatusCode, TypedData.From(http.Body), TypedData.From(http.RawBody), http.Params.Clone(), http.Query.Clone(), http.Url, http.Method, http.Headers.Clone(), http.Cookies.Select(Cookie.From).ToArray() , http.Identities.Select(ClaimsIdentity.From).ToArray());
    }

    public class Cookie
    {
        internal static Cookie From(RpcHttpCookie cookie)
        {
            return new Cookie();
        }
    }

    public class ClaimsIdentity
    {
        public string AuthenticationType { get; }
        public string NameClaimType { get; }
        public string RoleClaimType { get; }
        public IReadOnlyCollection<Claim> Claims { get; }

        private ClaimsIdentity(string authenticationType, string nameClaimType,string roleClaimType, IReadOnlyCollection<Claim> claims)
        {
            AuthenticationType = authenticationType;
            NameClaimType = nameClaimType;
            RoleClaimType = roleClaimType;
            Claims = claims;
        }

        internal static ClaimsIdentity From(RpcClaimsIdentity identity)
        {
            return new ClaimsIdentity(identity.AuthenticationType.Value, identity.NameClaimType.Value,
                identity.RoleClaimType.Value, identity.Claims.Select(Claim.From).ToArray());
        }
    }

    public class Claim
    {
        public string Type { get; }
        public string Value { get; }

        private Claim(string type, string value)
        {
            Type = type;
            Value = value;
        }

        internal static Claim From(RpcClaim claim)
        {
            return new Claim(claim.Type, claim.Value);
        }
    }
}
