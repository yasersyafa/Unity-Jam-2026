# Comic UI smoke validation

Run only in a disposable project copy with Unity 6000.6.8f1. Do not run against an
open working Editor. No Unity Test Framework package is needed.

1. Copy `Assets/`, `Packages/`, and `ProjectSettings/` to a temporary project folder.
2. Copy `FeelsSmokeDriver.cs` from this folder to that copy's `Assets/` folder.
3. Copy `FeelsSmoke.cs` to that copy's `Assets/Editor/` folder.
4. Launch the pinned Unity executable:

```sh
"/Applications/Unity/Hub/Editor/6000.6.8f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -projectPath /absolute/path/to/disposable-copy \
  -executeMethod FeelsSmoke.Run -logFile /tmp/comic-ui-smoke.log
```

Do not add `-quit`: the runner exits after the asynchronous Play Mode checks.
Success prints `FEELS SMOKE COMPLETE: 31 checks` and exits with code 0.
Gameplay errors or failed checks exit with code 1. The harness isolates the known
Editor Search indexing stack from gameplay failures and records it in `/tmp/`.
The runner changes only the disposable copy's UI assets and Editor settings.
Review visual appearance at 16:9 and 16:10 separately in the Editor.
