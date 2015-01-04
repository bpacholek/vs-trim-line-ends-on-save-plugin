// Guids.cs
// MUST match guids.h
using System;

namespace idct.trimOnSave
{
    static class GuidList
    {
        public const string guidVSPackage3PkgString = "9f0c0746-31c8-498a-a822-b7a6cf6dccb4";
        public const string guidVSPackage3CmdSetString = "627e5025-5526-4b45-939e-6a0c2142fa4d";

        public static readonly Guid guidVSPackage3CmdSet = new Guid(guidVSPackage3CmdSetString);
    };
}