using System.IO;
using System;

namespace i18nex.ZipBsonLoader
{
	public class PathExt
	{
		/// <summary>
		/// Simple method that emulates Path.GetRelativePath of later versions of .net.
		/// </summary>
		/// <param name="relativeTo"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static string GetRelativePath(string relativeTo, string path)
		{
			if (string.IsNullOrEmpty(relativeTo))
			{
				throw new ArgumentException("The 'relativeTo' argument must not be null or empty.", nameof(relativeTo));
			}

			if (string.IsNullOrEmpty(path))
			{
				throw new ArgumentException("The 'path' argument must not be null or empty.", nameof(path));
			}

			relativeTo = Path.GetFullPath(relativeTo);
			path = Path.GetFullPath(path);

			if (!relativeTo.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
			{
				relativeTo += Path.DirectorySeparatorChar;
			}

			var relativeToUri = new Uri(relativeTo, UriKind.RelativeOrAbsolute);
			var pathUri = new Uri(path, UriKind.RelativeOrAbsolute);

			if (relativeToUri.Scheme != pathUri.Scheme)
			{
				return path;
			}

			var relativeUri = relativeToUri.MakeRelativeUri(pathUri);
			var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

			return relativePath.Replace('/', Path.DirectorySeparatorChar);
		}
		/// <summary>
		/// Returns the first found directory in the given path.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static string GetFirstDirectory(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				throw new ArgumentException("Path must not be null or empty.", nameof(path));
			}

			// Normalize path separators
			path = path.Replace('\\', '/');

			// Trim leading and trailing slashes
			path = path.Trim('/');

			// Find the index of the first slash
			var index = path.IndexOf('/');

			// If there's no slash, return the original path
			if (index == -1)
			{
				return path;
			}

			// Return the substring before the first slash
			return path.Substring(0, index);
		}
		public static string GetPathAfterDirectory(string fullPath, string directoryName)
		{
			// Split the full path into directories
			var directories = fullPath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

			// Find the index of the directory name
			var index = Array.IndexOf(directories, directoryName);

			// If directory name not found or it's the last directory, return null
			if (index == -1 || index == directories.Length - 1)
				return null;

			// Build the path after the directory name
			var pathAfterDirectory = string.Join("\\", directories, index + 1, directories.Length - index - 1);

			return pathAfterDirectory;
		}
	}
}