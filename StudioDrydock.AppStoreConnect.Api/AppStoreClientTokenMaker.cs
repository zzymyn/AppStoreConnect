using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;

namespace StudioDrydock.AppStoreConnect.Api;

public static class AppStoreClientTokenMakerFactory
{
    public static IAppStoreClientTokenMaker CreateTeam(string privateKey, string keyId, string issuerId)
    {
        return new TeamAppStoreClientTokenMaker(privateKey, keyId, issuerId, "appstoreconnect-v1");
    }

    public static IAppStoreClientTokenMaker CreateUser(string privateKey, string keyId)
    {
        return new UserAppStoreClientTokenMaker(privateKey, keyId, "appstoreconnect-v1");
    }

    private sealed class TeamAppStoreClientTokenMaker
        : IAppStoreClientTokenMaker
    {
        private readonly string m_PrivateKey;
        private readonly string m_KeyId;
        private readonly string m_IssuerId;
        private readonly string m_Audience;
        private readonly TimeSpan m_TimeSpan;

        public TeamAppStoreClientTokenMaker(string privateKey, string keyId, string issuerId, string audience, TimeSpan timeSpan = default)
        {
            m_PrivateKey = privateKey;
            m_KeyId = keyId;
            m_IssuerId = issuerId;
            m_Audience = audience;
            m_TimeSpan = timeSpan;
        }

        public string MakeToken()
        {
            return CreateTokenAndSign(m_PrivateKey, m_KeyId, m_IssuerId, m_Audience, m_TimeSpan);
        }
    }

    private sealed class UserAppStoreClientTokenMaker
        : IAppStoreClientTokenMaker
    {
        private readonly string m_PrivateKey;
        private readonly string m_KeyId;
        private readonly string m_Audience;
        private readonly TimeSpan m_TimeSpan;

        public UserAppStoreClientTokenMaker(string privateKey, string keyId, string audience, TimeSpan timeSpan = default)
        {
            m_PrivateKey = privateKey;
            m_KeyId = keyId;
            m_Audience = audience;
            m_TimeSpan = timeSpan;
        }

        public string MakeToken()
        {
            return CreateTokenAndSign(m_PrivateKey, m_KeyId, null, m_Audience, m_TimeSpan);
        }
    }

    // https://github.com/dersia/AppStoreConnect/blob/main/src/AppStoreConnect.Jwt/KeyUtils.cs
    private static string CreateTokenAndSign(string privateKey, string kid, string? issuer, string audience, TimeSpan timeout = default)
    {
        var ecDSA = ECDsa.Create(); // don't dispose as it erroneously causes an ObjectDisposedException
        GetPrivateKey(privateKey, ecDSA);

        if (timeout == default)
        {
            timeout = TimeSpan.FromMinutes(10);
        }
        else if (timeout.TotalMinutes > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        var securityKey = new ECDsaSecurityKey(ecDSA) { KeyId = kid };
        var credentials = new SigningCredentials(securityKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.EcdsaSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Audience = audience,
            Expires = DateTime.UtcNow.Add(timeout),
            TokenType = "JWT",
            SigningCredentials = credentials
        };

        if (issuer != null)
        {
            descriptor.Issuer = issuer;
        }
        else
        {
            descriptor.Claims = new Dictionary<string, object>()
                {
                    { "sub", "user" },
                };
        }

        var handler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();
        var encodedToken = handler.CreateToken(descriptor);
        return encodedToken;
    }

    // https://github.com/dersia/AppStoreConnect/blob/main/src/AppStoreConnect.Jwt/KeyUtils.cs
    private static void GetPrivateKey(string privateKey, ECDsa ecDSA)
    {
        var reader = new StringReader(privateKey);
        var ecPrivateKeyParameters = (ECPrivateKeyParameters)new PemReader(reader).ReadObject();
        var q = ecPrivateKeyParameters.Parameters.G.Multiply(ecPrivateKeyParameters.D);
        var pub = new ECPublicKeyParameters(ecPrivateKeyParameters.AlgorithmName, q, ecPrivateKeyParameters.PublicKeyParamSet);
        var x = pub.Q.AffineXCoord.GetEncoded();
        var y = pub.Q.AffineYCoord.GetEncoded();
        var d = ecPrivateKeyParameters.D.ToByteArrayUnsigned();
        var msEcp = new ECParameters { Curve = ECCurve.NamedCurves.nistP256, Q = { X = x, Y = y }, D = d };
        msEcp.Validate();
        ecDSA.ImportParameters(msEcp);
    }

}
