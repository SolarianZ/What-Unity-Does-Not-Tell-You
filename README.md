# What-Unity-does-not-tell-you

Unity APIs that may be useful but not mentioned in the documentation.

This list will continue to update...

-----


Draw UI on the header area of the EditorWindow:

```csharp
void ShowButton(Rect position) {}
```

Keep serialized data after changing name or namespace of type:

```csharp
UnityEngine.Scripting.APIUpdating.MovedFromAttribute
```

Many editor utility functions:

```csharp
UnityEditorInternal.InternalEditorUtility
```

Load asset from Library folder:

```csharp
UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget()
```

Save asset to Library folder:

```csharp
UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget()
```
