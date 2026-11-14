using System;
using Git2Sharp.Bindings;
using Git2Sharp.Common;

namespace Git2Sharp;

/// <summary>
/// Represents an error returned by libgit2.
/// </summary>
/// <remarks>
/// Provides the corresponding libgit2 error code, error class
/// (when available) and descriptive message.
/// </remarks>
public sealed class GitException : Exception
{
    private readonly InteropString _message;

    /// <summary>
    /// Gets the error message describing the libgit2 error.
    /// </summary>
    public override string Message => _message;

    /// <summary>
    /// Gets the libgit2 error code associated with this exception.
    /// </summary>
    public GitErrorCode Code { get; }

    /// <summary>
    /// Gets the libgit2 error class associated with this exception, if available.
    /// </summary>
    public GitErrorT? Klass { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GitException"/> class with the specified libgit2 error code.
    /// </summary>
    /// <param name="code">
    /// The libgit2 error code.
    /// </param>
    public GitException(GitErrorCode code)
    {
        _message = MapErrorCode(code, out GitErrorT? klass);

        Code = code;
        Klass = klass;
    }

    /// <summary>
    /// Throws a <see cref="GitException"/> if the specified error code indicates a failure.
    /// </summary>
    /// <param name="code">
    /// The error code returned by libgit2.
    /// </param>
    /// <exception cref="GitException">
    /// The specified error code is non-zero.
    /// </exception>
    public static void ThrowIfError(int code) => ThrowIfError((GitErrorCode)code);

    /// <summary>
    /// Throws a <see cref="GitException"/> if the specified error code indicates a failure.
    /// </summary>
    /// <param name="code">
    /// The error code returned by libgit2.
    /// </param>
    /// <exception cref="GitException">
    /// The specified error code is non-zero.
    /// </exception>
    public static void ThrowIfError(GitErrorCode code)
    {
        if (code < 0)
        {
            throw new GitException(code);
        }
    }

    private static unsafe string MapErrorCode(GitErrorCode code, out GitErrorT? klass)
    {
        if (code >= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(code), $"Code must be less than zero, got '{code}'");
        }

        klass = null;

        GitError* error = Native.GitErrorLast();
        if (error is null)
        {
            return "Unknown git error, 'git_error_last' didnt return an error";
        }

        klass = (GitErrorT)error->Klass;
        return (InteropString)error->Message;
    }
}