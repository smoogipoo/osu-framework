// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;

namespace osu.Framework
{
    public interface IAttestationService
    {
        Task<string> GenerateKey();

        Task<byte[]> AttestKey(string key, byte[] challenge);

        Task<byte[]> GenerateAssertion(string key, byte[] data);
    }
}
