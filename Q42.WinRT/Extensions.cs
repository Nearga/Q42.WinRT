﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Q42.WinRT
{
    /// <summary>
    /// Extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Converts Uri to cache key extension method
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static string ToCacheKey(this Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");

            string result = uri.AbsoluteUri
                    .Replace("/", "_")
                    .Replace("?", "_")
                    .Replace("&", "_")
                    .Replace(":", "_");

            //FileIO.WriteBytesAsync crashes if total path length >= 247 characters 
            //https://connect.microsoft.com/VisualStudio/feedback/details/781729/fileio-writebytesasync-crashes-if-total-path-length-247-characters-winrt

            //Hash each value so we wont get long filepaths
            //Max is 260 for fully qualified file name
            IBuffer buffer = CryptographicBuffer.ConvertStringToBinary(result, BinaryStringEncoding.Utf8);
            HashAlgorithmProvider hashAlgorithm = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
            IBuffer hashBuffer = hashAlgorithm.HashData(buffer);
            var hashedResult = CryptographicBuffer.EncodeToBase64String(hashBuffer);

            return hashedResult;

        }

        /// <summary>
        /// Extension method to check if file exist in folder
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static async Task<bool> ContainsFileAsync(this StorageFolder folder, string fileName)
        {
            //This looks nicer, but gave a COM errors in some situations
            //TODO: Check again in final release of Windows 8 (or 9, or 10)
            //return (await folder.GetFilesAsync()).Where(file => file.Name == fileName).Any();

            try
            {
                await folder.GetFileAsync(fileName);
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch(Exception)
            {
                return false;
            }

        }


        /// <summary>
        /// Similar in nature to Parallel.ForEach, with the primary difference being that Parallel.ForEach is a synchronous method and uses synchronous delegates.
        /// http://blogs.msdn.com/b/pfxteam/archive/2012/03/05/10278165.aspx
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="dop"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public static Task ForEachAsync<T>(this IEnumerable<T> source, int dop, Func<T, Task> body)
        {
            return Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(dop)
                select Task.Run(async delegate
                {
                    using (partition)
                        while (partition.MoveNext())
                          await body(partition.Current).ConfigureAwait(false);
                }));
        }
    }
}
