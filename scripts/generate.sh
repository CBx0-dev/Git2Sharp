#!/usr/bin/env bash
# Generates the C# bindings for a libgit2 release with ClangSharpPInvokeGenerator.
#
# Usage:
#   scripts/generate.sh [VERSION]
#
#   VERSION  libgit2 release, with or without the leading "v" (1.9.7 or v1.9.7).
#            Defaults to the version recorded in LIBGIT2_VERSION.
#
# Environment (all optional):
#   LIBGIT2_SOURCE_DIR       Use this libgit2 checkout instead of cloning the release tag.
#   CLANGSHARP_NATIVE_PATH   Directory with the libclang / libClangSharp native libraries,
#                            if the generator cannot find them on its own.
#
# Requirements: bash, git, .NET SDK, and clang or gcc (for the compiler's builtin headers
# such as stddef.h). Runs on Linux, macOS and WSL.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1

OUT_DIR="$ROOT/src/Git2Sharp.Bindings/Generated"
CACHE_DIR="$ROOT/.cache"

# --- version ---------------------------------------------------------------
VERSION="${1:-}"
if [[ -z "$VERSION" && -f LIBGIT2_VERSION ]]; then
    VERSION="$(tr -d '[:space:]' <LIBGIT2_VERSION)"
fi
if [[ -z "$VERSION" ]]; then
    echo "usage: scripts/generate.sh <libgit2 version, e.g. 1.9.7>" >&2
    exit 2
fi
VERSION="${VERSION#v}"
if ! [[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "error: '$VERSION' is not a stable libgit2 version (expected x.y.z)" >&2
    exit 2
fi

# --- libgit2 sources -------------------------------------------------------
mkdir -p "$CACHE_DIR"
if [[ -n "${LIBGIT2_SOURCE_DIR:-}" ]]; then
    SRC="$LIBGIT2_SOURCE_DIR"
    actual="$(git -C "$SRC" describe --tags --exact-match 2>/dev/null || true)"
    if [[ "$actual" != "v$VERSION" ]]; then
        echo "warning: $SRC is at '${actual:-no tag}', not v$VERSION" >&2
    fi
else
    SRC="$CACHE_DIR/libgit2-$VERSION"
    if [[ ! -d "$SRC/include/git2" ]]; then
        echo "==> Cloning libgit2 v$VERSION"
        rm -rf "$SRC"
        git clone --quiet --depth 1 --branch "v$VERSION" https://github.com/libgit2/libgit2 "$SRC"
    fi
fi
INCLUDE_DIR="$SRC/include"
[[ -f "$INCLUDE_DIR/git2.h" ]] || {
    echo "error: $INCLUDE_DIR/git2.h not found" >&2
    exit 1
}

# --- umbrella header -------------------------------------------------------
# Some public headers (blame.h, notes.h, ...) do not compile on their own, so everything is
# parsed as one translation unit: git2.h plus the git2/sys/* headers it does not pull in.
UMBRELLA="$CACHE_DIR/libgit2-$VERSION-umbrella.h"
{
    echo "#include <git2.h>"
    (cd "$INCLUDE_DIR" && find git2/sys -name '*.h' | LC_ALL=C sort | sed 's|.*|#include <&>|')
} >"$UMBRELLA"

# Headers whose declarations are emitted. git2/stdint.h and git2/inttypes.h are old MSVC shims.
TRAVERSE=()
while IFS= read -r header; do
    TRAVERSE+=("$INCLUDE_DIR/$header")
done < <(cd "$INCLUDE_DIR" && {
    echo git2.h
    find git2 -name '*.h' ! -name stdint.h ! -name inttypes.h | LC_ALL=C sort
})

# --- compiler builtin headers (stddef.h, stdarg.h, ...) ---------------------
INCLUDE_DIRS=("$INCLUDE_DIR")
builtin=""
if command -v clang >/dev/null 2>&1; then
    builtin="$(clang -print-resource-dir 2>/dev/null)/include"
fi
if [[ ! -d "$builtin" ]] && command -v gcc >/dev/null 2>&1; then
    builtin="$(gcc -print-file-name=include 2>/dev/null)"
fi
if [[ -d "$builtin" ]]; then
    INCLUDE_DIRS+=("$builtin")
else
    echo "warning: no clang or gcc builtin headers found; parsing may fail on stddef.h" >&2
fi

# --- native libraries ------------------------------------------------------
if [[ -n "${CLANGSHARP_NATIVE_PATH:-}" ]]; then
    export LD_LIBRARY_PATH="$CLANGSHARP_NATIVE_PATH${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"
    export DYLD_LIBRARY_PATH="$CLANGSHARP_NATIVE_PATH${DYLD_LIBRARY_PATH:+:$DYLD_LIBRARY_PATH}"
fi

# --- generate --------------------------------------------------------------
echo "==> Generating file header information"
sed \
    -e "s|\$VERSION|${VERSION}|g" \
    generator/header.txt.template >generator/header.txt

echo "==> Restoring ClangSharpPInvokeGenerator"
dotnet tool restore >/dev/null

echo "==> Generating bindings for libgit2 v$VERSION"
rm -rf "$OUT_DIR"
mkdir -p "$OUT_DIR"

# --file must come last: it takes every remaining value.
dotnet tool run ClangSharpPInvokeGenerator \
    @generator/libgit2.rsp \
    --include-directory "${INCLUDE_DIRS[@]}" \
    --output "$OUT_DIR" \
    --traverse "${TRAVERSE[@]}" \
    --file "$UMBRELLA" || true

# --- post-process ----------------------------------------------------------
# Fixes enum-valued macro constants and converts snake_case names to PascalCase.
# Works on the generated source text, so the code does not have to compile yet.
echo "==> Post-processing generated code"
dotnet run --project src/Git2Sharp.Bindings.PostProcess -c Release -- "$OUT_DIR"

# --- sanity checks ---------------------------------------------------------
count="$(find "$OUT_DIR" -name '*.cs' | wc -l | tr -d ' ')"
if [[ "$count" -eq 0 ]] || ! grep -rqs 'git_repository_open' "$OUT_DIR"; then
    echo "error: generator produced no usable output ($count files)" >&2
    exit 1
fi

echo "$VERSION" >LIBGIT2_VERSION
echo "==> Done: $count files in ${OUT_DIR#"$ROOT/"}, LIBGIT2_VERSION=$VERSION"
