#!/usr/bin/env bash
set -euo pipefail

DRY_RUN=false

while [[ $# -gt 0 ]]; do
    case "$1" in
    --dry-run)
        DRY_RUN=true
        ;;
    -h | --help)
        echo "Usage: $0 [--dry-run]"
        echo ""
        echo "Options:"
        echo "  --dry-run   Preview planned changes without modifying anything"
        echo "  -h, --help  Show this help"
        exit 0
        ;;
    *)
        echo "::error::Unknown argument: $1"
        exit 1
        ;;
    esac
    shift
done

BASELINE_VERSION="${BASELINE_VERSION:-1.9.0}"
TARGET_VERSION="${INPUT_VERSION:-}"
REPOSITORY="${GITHUB_REPOSITORY:-$(gh repo view --json nameWithOwner --jq .nameWithOwner)}"
UPSTREAM_REPOSITORY="libgit2/libgit2"
BASE_BRANCH="master"

echo "Repository: $REPOSITORY"
echo "Baseline: $BASELINE_VERSION"
echo "Dry run: $DRY_RUN"

die() {
    echo "::error::$*" >&2
    exit 1
}

is_version() {
    [[ "$1" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]
}

version_lt() {
    [[ "$(printf '%s\n' "$1" "$2" | sort -V | head -n1)" == "$1" && "$1" != "$2" ]]
}

version_le() {
    [[ "$1" == "$2" ]] || version_lt "$1" "$2"
}

branch_for() {
    echo "libgit2/v$1"
}

# ------------------------------------------------------------
# 1. Validate configuration and discover repository information.
# ------------------------------------------------------------

is_version "$BASELINE_VERSION" || die "Invalid baseline version: $BASELINE_VERSION"

if [[ -n "$TARGET_VERSION" ]]; then
    TARGET_VERSION="${TARGET_VERSION#v}"
    is_version "$TARGET_VERSION" ||
        die "Invalid target version: $TARGET_VERSION"
fi

echo "Baseline: $BASELINE_VERSION"

if [[ "$DRY_RUN" != true ]]; then
    git fetch origin --tags
    git fetch origin "$BASE_BRANCH"
fi

# ------------------------------------------------------------
# 2. Discover upstream stable releases.
# ------------------------------------------------------------

echo "Discovering upstream releases..."

mapfile -t upstream_versions < <(
    gh api --paginate \
        "repos/$UPSTREAM_REPOSITORY/releases?per_page=100" \
        --jq '.[] | select(.draft == false and .prerelease == false) | .tag_name' |
        sed 's/^v//' |
        grep -E '^[0-9]+\.[0-9]+\.[0-9]+$' |
        sort -Vu
)

[[ ${#upstream_versions[@]} -gt 0 ]] || die "No upstream stable releases found"

if [[ -z "$TARGET_VERSION" ]]; then
    TARGET_VERSION="${upstream_versions[-1]}"
fi

# Verify that the target is an actual upstream release.
printf '%s\n' "${upstream_versions[@]}" |
    grep -Fxq "$TARGET_VERSION" || die "Target version $TARGET_VERSION is not an upstream release"

echo "Target: $TARGET_VERSION"

# ------------------------------------------------------------
# 3. Discover local release tags.
# ------------------------------------------------------------

declare -A local_tags=()

while IFS= read -r tag; do
    [[ -n "$tag" ]] || continue

    version="${tag#refs/tags/}"
    version="${version#v}"

    is_version "$version" || continue
    version_lt "$version" "$BASELINE_VERSION" && continue

    # Prefer the v-prefixed tag if both forms exist.
    if [[ -z "${local_tags[$version]:-}" ]] || [[ "$tag" == "refs/tags/v$version" ]]; then
        local_tags["$version"]="$tag"
    fi
done < <(
    git ls-remote --tags origin |
        awk '{print $2}' |
        grep -v '\^{}$'
)

# ------------------------------------------------------------
# 4. Determine missing upstream releases.
# ------------------------------------------------------------

missing_versions=()

for version in "${upstream_versions[@]}"; do
    version_lt "$version" "$BASELINE_VERSION" && continue
    version_le "$version" "$TARGET_VERSION" || continue

    # The baseline itself is already considered integrated.
    [[ "$version" == "$BASELINE_VERSION" ]] && continue

    if [[ -z "${local_tags[$version]:-}" ]]; then
        missing_versions+=("$version")
    fi
done

if [[ ${#missing_versions[@]} -eq 0 ]]; then
    echo "No missing releases. Everything is up to date."
    exit 0
fi

echo "Missing releases:"
printf '  - %s\n' "${missing_versions[@]}"

# ------------------------------------------------------------
# 5. Build the complete version inventory.
# ------------------------------------------------------------

declare -A known_versions=()

for version in "${upstream_versions[@]}"; do
    version_lt "$version" "$BASELINE_VERSION" && continue
    version_le "$version" "$TARGET_VERSION" || continue
    known_versions["$version"]=1
done

for version in "${!local_tags[@]}"; do
    version_le "$version" "$TARGET_VERSION" || continue
    known_versions["$version"]=1
done

mapfile -t all_versions < <(
    printf '%s\n' "${!known_versions[@]}" | sort -Vu
)

# ------------------------------------------------------------
# 6. Generate missing releases using their SemVer predecessor.
# ------------------------------------------------------------
declare -A planned_branches=()

for version in "${missing_versions[@]}"; do
    branch="$(branch_for "$version")"

    echo ""
    echo "============================================================"
    echo "Processing libgit2 v$version"
    echo "============================================================"

    # Find the greatest lower SemVer version.
    predecessor=""

    for candidate in "${all_versions[@]}"; do
        version_lt "$candidate" "$version" || continue
        predecessor="$candidate"
    done

    [[ -n "$predecessor" ]] || die "No predecessor found for $version"

    echo "SemVer predecessor: $predecessor"

    # Resolve predecessor to its local release tag or update branch.
    if [[ "$predecessor" == "$BASELINE_VERSION" ]]; then

        # The baseline is the starting point, not an update branch.
        if [[ -n "${local_tags[$predecessor]:-}" ]]; then
            parent_ref="${local_tags[$predecessor]}"
        else
            parent_ref="origin/$BASE_BRANCH"
        fi

    elif [[ -n "${local_tags[$predecessor]:-}" ]]; then
        parent_ref="${local_tags[$predecessor]}"

    else
        parent_branch="$(branch_for "$predecessor")"

        # The predecessor may have been planned earlier in this dry run.
        if [[ "$DRY_RUN" == true ]] &&
            [[ -n "${planned_branches[$predecessor]:-}" ]]; then

            parent_ref="${planned_branches[$predecessor]}"

        else
            if ! git ls-remote --exit-code --heads origin "$parent_branch" >/dev/null 2>&1; then
                die "Missing predecessor branch $parent_branch for $version"
            fi

            if [[ "$DRY_RUN" != true ]]; then
                git fetch origin \
                    "+refs/heads/$parent_branch:refs/remotes/origin/$parent_branch"
            fi

            parent_ref="origin/$parent_branch"
        fi
    fi

    # ----------------------------------------------------------
    # Dry-run: report planned actions without modifying anything.
    # ----------------------------------------------------------

    if [[ "$DRY_RUN" == true ]]; then

        echo ""
        echo "DRY RUN: libgit2 v$version"
        echo "  Branch: $branch"
        echo "  Base version: $predecessor"
        echo "  Parent: $parent_ref"
        echo "  PR target: $BASE_BRANCH"

        if git ls-remote --exit-code --heads origin "$branch" >/dev/null 2>&1; then
            echo "  Branch status: EXISTS"

            if pr_data="$(gh pr view "$branch" \
                --repo "$REPOSITORY" \
                --json state,mergedAt,baseRefName 2>/dev/null)"; then

                state="$(jq -r '.state' <<<"$pr_data")"
                merged_at="$(jq -r '.mergedAt // empty' <<<"$pr_data")"
                base="$(jq -r '.baseRefName' <<<"$pr_data")"

                if [[ "$state" == "CLOSED" && -z "$merged_at" ]]; then
                    die "PR for $branch was closed without merging"
                fi

                if [[ -n "$merged_at" ]]; then
                    die "PR for $branch is merged, but local release tag v$version is missing"
                fi

                echo "  PR status: EXISTS"
                echo "  Current PR target: $base"

                if [[ "$base" != "$BASE_BRANCH" ]]; then
                    echo "  Planned action: CHANGE PR target to $BASE_BRANCH"
                else
                    echo "  Planned action: REUSE EXISTING PR"
                fi

            else
                echo "  PR status: MISSING"
                echo "  Planned action: CREATE PR"
            fi

        else
            echo "  Branch status: MISSING"
            echo "  Planned action: GENERATE, BUILD, COMMIT, PUSH AND CREATE PR"
        fi

        # Register this branch so subsequent versions can use it.
        planned_branches["$version"]="origin/$branch"

        continue
    fi

    echo "Parent ref: $parent_ref"

    # ----------------------------------------------------------
    # Reuse or create the update branch.
    # ----------------------------------------------------------

    if git ls-remote --exit-code --heads origin "$branch" >/dev/null 2>&1; then
        echo "Branch already exists. Reusing it."

        git fetch origin \
            "+refs/heads/$branch:refs/remotes/origin/$branch"

        if ! git merge-base --is-ancestor \
            "$parent_ref" "origin/$branch"; then
            die "Existing branch $branch does not contain predecessor $parent_ref"
        fi

    else
        echo "Creating branch $branch from $parent_ref"

        git switch --detach "$parent_ref"
        git switch -c "$branch"

        # Generate and validate bindings.
        ./scripts/generate.sh "$version"

        dotnet build src/Git2Sharp.Bindings -c Release

        git add -A

        # An empty commit is intentional: a release can require no
        # generated binding changes but still needs a release PR.
        git commit --allow-empty \
            -m "Generate bindings for libgit2 v$version"

        git push origin "$branch"

        git fetch origin \
            "+refs/heads/$branch:refs/remotes/origin/$branch"
    fi

    # ----------------------------------------------------------
    # Create or validate the PR.
    # ----------------------------------------------------------

    if gh pr view "$branch" \
        --repo "$REPOSITORY" \
        --json state,mergedAt >/dev/null 2>&1; then

        pr_data="$(gh pr view "$branch" \
            --repo "$REPOSITORY" \
            --json state,mergedAt)"

        state="$(jq -r '.state' <<<"$pr_data")"
        merged_at="$(jq -r '.mergedAt // empty' <<<"$pr_data")"

        if [[ "$state" == "CLOSED" && -z "$merged_at" ]]; then
            die "PR for $branch was closed without merging"
        fi

        if [[ -n "$merged_at" ]]; then
            die "PR for $branch is merged, but local release tag v$version is missing"
        fi

        # Ensure every PR always targets master.
        gh pr edit "$branch" \
            --repo "$REPOSITORY" \
            --base "$BASE_BRANCH"

        echo "Existing PR verified for $branch."

    else
        body="Bindings regenerated for [libgit2 v${version}](https://github.com/libgit2/libgit2/releases/tag/v${version}).

    **Predecessor:** v${predecessor}

    This PR is part of the automated libgit2 release update process.

    **Merge requirements:**
    - Merge in ascending SemVer order.
    - Use Create a merge commit.
    - Do not squash or rebase.

    Merging publishes NuGet package ${version}, tags the merge commit v${version}, and creates a GitHub release."

        gh pr create \
            --repo "$REPOSITORY" \
            --base "$BASE_BRANCH" \
            --head "$branch" \
            --title "libgit2 v$version" \
            --body "$body"

        echo "Created PR for v$version."
    fi

done

echo ""
echo "All missing libgit2 releases have been processed."
