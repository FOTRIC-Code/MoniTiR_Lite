// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;

namespace Firefly.Client.Service.LiteDb
{
    /// <summary>
    /// Resolves in-proc sample/alarm image paths (relative under root_path) for UI/file open.
    /// </summary>
    public static class LocalImagePath
    {
        public static string ResolveAbsolute(string rootPath, string relativeOrAbsolutePath)
        {
            if (string.IsNullOrWhiteSpace(relativeOrAbsolutePath))
            {
                return null;
            }

            string candidate = relativeOrAbsolutePath.Trim();

            if (candidate.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    return new Uri(candidate).LocalPath;
                }
                catch
                {
                    return candidate;
                }
            }

            if (Uri.TryCreate(candidate, UriKind.Absolute, out Uri absoluteUri)
                && (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps))
            {
                return null;
            }

            if (!Path.IsPathRooted(candidate) && !string.IsNullOrWhiteSpace(rootPath))
            {
                candidate = Path.Combine(rootPath, relativeOrAbsolutePath);
            }

            try
            {
                return Path.GetFullPath(candidate);
            }
            catch
            {
                return candidate;
            }
        }

        public static bool TryGetExistingLocalPath(string urlOrPath, out string localPath)
        {
            localPath = null;
            if (string.IsNullOrWhiteSpace(urlOrPath))
            {
                return false;
            }

            string resolved = ResolveAbsolute(null, urlOrPath);
            if (string.IsNullOrWhiteSpace(resolved) || !File.Exists(resolved))
            {
                return false;
            }

            localPath = resolved;
            return true;
        }

        public static string ToFileUri(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                return null;
            }

            try
            {
                return new Uri(Path.GetFullPath(absolutePath)).AbsoluteUri;
            }
            catch
            {
                return absolutePath;
            }
        }
    }
}
