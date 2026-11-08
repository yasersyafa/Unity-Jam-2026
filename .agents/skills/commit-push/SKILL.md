---
name: commit-push
description: Commit the current task changes and push the current branch without co-author attribution, using the caveman-commit message style. Use when explicitly requested with $commit-push or by name.
---

Commit and push the changes authorized by the current task in this repository.

1. Inspect the current branch, git status, staged and unstaged diffs, and relevant untracked files. Preserve unrelated user changes; stage only the authorized task files, including Unity .meta files. Never include generated Library, Temp, Logs, or build output.
2. Run git diff --check and appropriate focused checks. Report validation limits honestly. Unity-generated whitespace may be reported without rewriting unrelated assets; stop on substantive unresolved errors.
3. Use the caveman-commit skill if available. Otherwise write an imperative Conventional Commits subject: type(scope): summary, preferably at most 50 characters, never over 72. Add a body only for necessary context, breaking changes, security fixes, migrations, or reverts.
4. Do not add Co-authored-by, Generated-by, or other AI attribution. Preserve the configured Git author and committer identity.
5. Commit the staged task changes. If there are no changes, do not create an empty commit. Stop if a hook or commit fails; do not bypass hooks or amend existing commits.
6. Push the current branch to its configured upstream. Without an upstream, push to origin with the same branch name and set upstream tracking. Never force-push. If rejected, report the reason; do not reset, rebase, or merge automatically.
7. Report the commit hash, subject, and pushed branch. Do not open or merge a PR unless separately requested.
