using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Microsoft.Learn.AzureFunctionsTesting
{
    public static class TestCertificateGenerator
    {
        public static X509Certificate2 Generate(string commonName)
        {
            var rsa = RSA.Create();
            var req = new CertificateRequest($"cn={commonName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
            return cert;
        }

        public static X509Certificate2 Generate(string commonName, string issuerName)
        {
            var rsa = RSA.Create();
            var issuerReq = new CertificateRequest($"cn={issuerName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            issuerReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 1, false));
            var issuerCert = issuerReq.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
            var req = new CertificateRequest($"cn={commonName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var cert = req.Create(issuerCert, issuerCert.NotBefore, issuerCert.NotAfter, Encoding.UTF8.GetBytes(issuerCert.SerialNumber));
            cert = cert.CopyWithPrivateKey(rsa);
            return cert;
        }
    }
}
