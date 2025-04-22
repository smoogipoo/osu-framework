// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Security.Cryptography;
using System.Threading.Tasks;
using DeviceCheck;
using Foundation;

namespace osu.Framework.iOS
{
    public class IOSAttestationService : IAttestationService
    {
        public Task<string> GenerateKey()
        {
            return DCAppAttestService.SharedService.GenerateKeyAsync();
        }

        public async Task<byte[]> AttestKey(string key, byte[] challenge)
        {
            using var nsDataHash = NSData.FromArray(SHA256.HashData(challenge));
            using var attestation = await DCAppAttestService.SharedService.AttestKeyAsync(key, nsDataHash);
            return attestation.ToArray();
        }

        public async Task<byte[]> GenerateAssertion(string key, byte[] data)
        {
            using var nsDataHash = NSData.FromArray(SHA256.HashData(data));
            using var assertion = await DCAppAttestService.SharedService.GenerateAssertionAsync(key, nsDataHash);
            return assertion.ToArray();
        }
    }
}
