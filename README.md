# Git2Sharp.Bindings

C# P/Invoke bindings for libgit2, a cross-platform, pure C implementation of Git core methods.

This package provides generated C# bindings to the native libgit2 API, allowing .NET applications to interact directly
with libgit2.

## Installation

Install the package using the .NET CLI:

```bash
dotnet add package Git2Sharp.Bindings
```

Or using the NuGet Package Manager:

```powershell
Install-Package Git2Sharp.Bindings
```

## Requirements

- .NET 10.0 or later.
- A compatible native libgit2 library available at runtime.
- A native libgit2 version matching the bindings version is recommended.

## Usage

The package exposes the native libgit2 API through generated C# declarations.

For example, opening a repository and reading it's current branch name:

```csharp
using System.Runtime.InteropServices;
using Git2Sharp.Bindings;

namespace Git2Sharp.Sample;

public static class Program
{
    public static unsafe void Main(string[] args)
    {
        sbyte* repoName = (sbyte*)Marshal.StringToCoTaskMemUTF8(Environment.CurrentDirectory);

        int error = Native.GitLibgit2Init();
        Console.WriteLine($">> Init: {error}");

        GitRepository* repo = null;
        GitReference* head = null;

        error = Native.GitRepositoryOpen(&repo, repoName);
        Console.WriteLine($">> Repo: {error}, repo: {(nint)repo}");

        error = Native.GitRepositoryHead(&head, repo);
        Console.WriteLine($">> Head: {error}, repo: {(nint)head}");

        sbyte* name = Native.GitReferenceShorthand(head);
        Console.WriteLine($">> Branch: {Marshal.PtrToStringUTF8((nint)name)}");

        Native.GitReferenceFree(head);
        Native.GitRepositoryFree(repo);
        Native.GitLibgit2Shutdown();
    }
}
```

The example illustrates the low-level nature of the bindings. Exact signatures and available APIs depend on the libgit2
release used to generate the package.

The generated C# declarations are post-processed to adjust type, function, and property names to follow C# PascalCase
naming conventions.

For further details, refer to the generated declarations and the official libgit2 documentation.

## Native library

Git2Sharp.Bindings provides managed declarations, not the native libgit2 implementation.

Your application must ensure that a compatible native libgit2 library is installed and discoverable by the platform's
native library loader.

Official libgit2 sources and releases:

- libgit2 GitHub repository
- libgit2 releases

## Versioning

Package versions follow the corresponding libgit2 release version.

For example:

| NuGET package            | Generated from |
|--------------------------|----------------|
| Git2Sharp.Bindings 1.9.5 | libgit2 v1.9.5 | 
| Git2Sharp.Bindings 1.9.7 | libgit2 v1.9.7 | 

Using matching managed bindings and native library versions is strongly recommended.

## About

The bindings are generated from the official libgit2 C headers using ClangSharp, with additional post-processing for C#
naming conventions and generated code adjustments.

## License

This project is licensed under the Apache License 2.0.

libgit2 is separately licensed under the terms specified by its own [repository](https://github.com/libgit2/libgit2?tab=License-1-ov-file).